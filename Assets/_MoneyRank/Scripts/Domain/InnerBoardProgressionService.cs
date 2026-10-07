using System;
using System.Collections.Generic;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardProgressionService
    {
        private readonly Dictionary<string, IInnerBoardMovementPolicy> _policies =
            new Dictionary<string, IInnerBoardMovementPolicy>(StringComparer.Ordinal);

        public InnerBoardProgressionService(IEnumerable<IInnerBoardMovementPolicy> policies)
        {
            if (policies == null)
                throw new ArgumentNullException(nameof(policies));

            foreach (var policy in policies)
            {
                if (policy == null)
                    throw new ArgumentException("Inner board policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId))
                    throw new ArgumentException("Inner board policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId))
                    throw new ArgumentException($"Inner board policy '{policy.PolicyId}' is registered more than once.", nameof(policies));

                _policies.Add(policy.PolicyId, policy);
            }
        }

        public OperationResult<InnerBoardMoveResult> Move(
            InnerBoardDefinition definition,
            InnerBoardState state,
            int steps)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (definition.GetSpace(state.Position) == null || definition.GetSpace(state.CheckpointIndex) == null)
            {
                return OperationResult<InnerBoardMoveResult>.Failure(
                    "INNER_BOARD_STATE_OUT_OF_RANGE",
                    "The player inner board state is outside this definition.");
            }
            if (!_policies.TryGetValue(definition.MovementPolicyId, out var policy))
            {
                return OperationResult<InnerBoardMoveResult>.Failure(
                    "INNER_BOARD_POLICY_NOT_FOUND",
                    $"No movement policy is registered for '{definition.MovementPolicyId}'.");
            }

            var origin = state.Position;
            var calculation = policy.Calculate(definition, state, steps);
            if (!calculation.Succeeded)
            {
                return OperationResult<InnerBoardMoveResult>.Failure(
                    calculation.ErrorCode,
                    calculation.Message);
            }

            var destinationSpace = definition.GetSpace(calculation.Value.Destination);
            var checkpointSpace = definition.GetSpace(calculation.Value.CheckpointIndex);
            if (destinationSpace == null)
            {
                return OperationResult<InnerBoardMoveResult>.Failure(
                    "INNER_BOARD_DESTINATION_OUT_OF_RANGE",
                    "The movement policy produced an invalid destination.");
            }
            if (checkpointSpace == null ||
                (calculation.Value.CheckpointIndex != definition.StartIndex && !checkpointSpace.IsCheckpoint))
            {
                return OperationResult<InnerBoardMoveResult>.Failure(
                    "INNER_BOARD_CHECKPOINT_INVALID",
                    "The movement policy produced an invalid checkpoint.");
            }

            state.Apply(calculation.Value);
            return OperationResult<InnerBoardMoveResult>.Success(
                new InnerBoardMoveResult(
                    state.PlayerId,
                    origin,
                    state.Position,
                    state.CheckpointIndex,
                    destinationSpace.IsGoal));
        }
    }
}
