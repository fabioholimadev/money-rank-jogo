using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardCaptureService
    {
        private readonly Dictionary<string, IInnerBoardCapturePolicy> _policies =
            new Dictionary<string, IInnerBoardCapturePolicy>(StringComparer.Ordinal);

        public InnerBoardCaptureService(IEnumerable<IInnerBoardCapturePolicy> policies)
        {
            if (policies == null)
                throw new ArgumentNullException(nameof(policies));

            foreach (var policy in policies)
            {
                if (policy == null)
                    throw new ArgumentException("Capture policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId))
                    throw new ArgumentException("Capture policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId))
                    throw new ArgumentException($"Capture policy '{policy.PolicyId}' is registered more than once.", nameof(policies));
                _policies.Add(policy.PolicyId, policy);
            }
        }

        public OperationResult<InnerBoardCaptureResult> Resolve(
            InnerBoardDefinition definition,
            PlayerId movedPlayerId,
            IEnumerable<InnerBoardState> states)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (states == null)
                throw new ArgumentNullException(nameof(states));

            var stateList = states.ToList();
            if (stateList.Any(state => state == null))
                throw new ArgumentException("Inner board states cannot contain null entries.", nameof(states));
            if (stateList.Select(state => state.PlayerId).Distinct().Count() != stateList.Count)
                throw new ArgumentException("Inner board player states must be unique.", nameof(states));
            var statesByPlayer = stateList.ToDictionary(state => state.PlayerId);
            if (!statesByPlayer.ContainsKey(movedPlayerId))
            {
                return OperationResult<InnerBoardCaptureResult>.Failure(
                    "INNER_BOARD_MOVED_PLAYER_NOT_FOUND",
                    "The moved player does not have an inner board state.");
            }
            if (!_policies.TryGetValue(definition.CapturePolicyId, out var policy))
            {
                return OperationResult<InnerBoardCaptureResult>.Failure(
                    "INNER_BOARD_CAPTURE_POLICY_NOT_FOUND",
                    $"No capture policy is registered for '{definition.CapturePolicyId}'.");
            }

            var calculation = policy.Calculate(definition, movedPlayerId, stateList.AsReadOnly());
            if (!calculation.Succeeded)
                return OperationResult<InnerBoardCaptureResult>.Failure(calculation.ErrorCode, calculation.Message);

            foreach (var relocation in calculation.Value.Relocations)
            {
                if (!statesByPlayer.ContainsKey(relocation.PlayerId))
                {
                    return OperationResult<InnerBoardCaptureResult>.Failure(
                        "INNER_BOARD_CAPTURE_PLAYER_NOT_FOUND",
                        $"Capture policy referenced unknown player '{relocation.PlayerId}'.");
                }
                if (definition.GetSpace(relocation.Destination) == null)
                {
                    return OperationResult<InnerBoardCaptureResult>.Failure(
                        "INNER_BOARD_CAPTURE_DESTINATION_INVALID",
                        "Capture policy produced an invalid destination.");
                }
                var checkpoint = definition.GetSpace(relocation.CheckpointIndex);
                if (checkpoint == null ||
                    (relocation.CheckpointIndex != definition.StartIndex && !checkpoint.IsCheckpoint))
                {
                    return OperationResult<InnerBoardCaptureResult>.Failure(
                        "INNER_BOARD_CAPTURE_CHECKPOINT_INVALID",
                        "Capture policy produced an invalid checkpoint.");
                }
            }

            foreach (var relocation in calculation.Value.Relocations)
            {
                statesByPlayer[relocation.PlayerId].Apply(
                    new InnerBoardMovementCalculation(relocation.Destination, relocation.CheckpointIndex));
            }

            return OperationResult<InnerBoardCaptureResult>.Success(
                new InnerBoardCaptureResult(movedPlayerId, calculation.Value.Relocations));
        }
    }
}
