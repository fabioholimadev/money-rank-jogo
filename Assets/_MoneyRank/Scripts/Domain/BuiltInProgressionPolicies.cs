using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class CarryAttributeOverflowHandler : IAttributeOverflowHandler
    {
        public CarryAttributeOverflowHandler(string behaviorId)
        {
            BehaviorId = string.IsNullOrWhiteSpace(behaviorId)
                ? throw new ArgumentException("Attribute behavior ID cannot be empty.", nameof(behaviorId))
                : behaviorId;
        }

        public string BehaviorId { get; }

        public OperationResult<AttributeProgressionCalculation> Calculate(
            int currentProgress,
            int delta,
            int threshold)
        {
            if (threshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(threshold));

            var total = (long)currentProgress + delta;
            if (total < 0)
            {
                return OperationResult<AttributeProgressionCalculation>.Failure(
                    "ATTRIBUTE_PROGRESS_BELOW_ZERO",
                    "This overflow policy does not allow progress below zero.");
            }

            var completedCycles = total / threshold;
            if (completedCycles > int.MaxValue)
            {
                return OperationResult<AttributeProgressionCalculation>.Failure(
                    "ATTRIBUTE_CYCLE_COUNT_OVERFLOW",
                    "The completed attribute cycle count exceeds the supported range.");
            }

            return OperationResult<AttributeProgressionCalculation>.Success(
                new AttributeProgressionCalculation(
                    (int)(total % threshold),
                    (int)completedCycles));
        }
    }

    public sealed class LinearClampedInnerBoardMovementPolicy : IInnerBoardMovementPolicy
    {
        public LinearClampedInnerBoardMovementPolicy(string policyId)
        {
            PolicyId = string.IsNullOrWhiteSpace(policyId)
                ? throw new ArgumentException("Movement policy ID cannot be empty.", nameof(policyId))
                : policyId;
        }

        public string PolicyId { get; }

        public OperationResult<InnerBoardMovementCalculation> Calculate(
            InnerBoardDefinition definition,
            InnerBoardState state,
            int steps)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (steps < 0)
            {
                return OperationResult<InnerBoardMovementCalculation>.Failure(
                    "INNER_BOARD_STEPS_INVALID",
                    "Inner board steps cannot be negative.");
            }

            var destination = (int)Math.Min((long)state.Position + steps, definition.SpaceCount - 1L);
            var destinationSpace = definition.GetSpace(destination);
            var checkpoint = destinationSpace.IsCheckpoint ? destination : state.CheckpointIndex;
            return OperationResult<InnerBoardMovementCalculation>.Success(
                new InnerBoardMovementCalculation(destination, checkpoint));
        }
    }

    public sealed class SharedSpaceCheckpointCapturePolicy : IInnerBoardCapturePolicy
    {
        public SharedSpaceCheckpointCapturePolicy(string policyId)
        {
            PolicyId = string.IsNullOrWhiteSpace(policyId)
                ? throw new ArgumentException("Capture policy ID cannot be empty.", nameof(policyId))
                : policyId;
        }

        public string PolicyId { get; }

        public OperationResult<InnerBoardCaptureCalculation> Calculate(
            InnerBoardDefinition definition,
            PlayerId movedPlayerId,
            IReadOnlyList<InnerBoardState> states)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (states == null) throw new ArgumentNullException(nameof(states));

            var mover = states.FirstOrDefault(state => state.PlayerId == movedPlayerId);
            if (mover == null)
            {
                return OperationResult<InnerBoardCaptureCalculation>.Failure(
                    "INNER_BOARD_MOVED_PLAYER_NOT_FOUND",
                    "The moved player does not have an inner board state.");
            }
            if (definition.GetSpace(mover.Position).IsProtected)
            {
                return OperationResult<InnerBoardCaptureCalculation>.Success(
                    new InnerBoardCaptureCalculation(Array.Empty<InnerBoardRelocation>()));
            }

            var relocations = states
                .Where(state => state.PlayerId != movedPlayerId && state.Position == mover.Position)
                .Select(state => new InnerBoardRelocation(
                    state.PlayerId,
                    state.CheckpointIndex,
                    state.CheckpointIndex));
            return OperationResult<InnerBoardCaptureCalculation>.Success(
                new InnerBoardCaptureCalculation(relocations));
        }
    }

    public sealed class GoalSpaceVictoryPolicy : IInnerBoardVictoryPolicy
    {
        public GoalSpaceVictoryPolicy(string policyId, string reasonCode)
        {
            PolicyId = string.IsNullOrWhiteSpace(policyId)
                ? throw new ArgumentException("Victory policy ID cannot be empty.", nameof(policyId))
                : policyId;
            ReasonCode = string.IsNullOrWhiteSpace(reasonCode)
                ? throw new ArgumentException("Victory reason code cannot be empty.", nameof(reasonCode))
                : reasonCode;
        }

        public string PolicyId { get; }
        public string ReasonCode { get; }

        public InnerBoardVictoryEvaluation Evaluate(
            InnerBoardDefinition definition,
            PlayerId movedPlayerId,
            IReadOnlyList<InnerBoardState> states)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (states == null) throw new ArgumentNullException(nameof(states));

            var mover = states.FirstOrDefault(state => state.PlayerId == movedPlayerId);
            return mover != null && definition.GetSpace(mover.Position).IsGoal
                ? InnerBoardVictoryEvaluation.End(movedPlayerId, ReasonCode)
                : InnerBoardVictoryEvaluation.Continue();
        }
    }
}
