using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardVictoryService
    {
        private readonly Dictionary<string, IInnerBoardVictoryPolicy> _policies =
            new Dictionary<string, IInnerBoardVictoryPolicy>(StringComparer.Ordinal);

        public InnerBoardVictoryService(IEnumerable<IInnerBoardVictoryPolicy> policies)
        {
            if (policies == null)
                throw new ArgumentNullException(nameof(policies));

            foreach (var policy in policies)
            {
                if (policy == null)
                    throw new ArgumentException("Victory policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId))
                    throw new ArgumentException("Victory policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId))
                    throw new ArgumentException($"Victory policy '{policy.PolicyId}' is registered more than once.", nameof(policies));
                _policies.Add(policy.PolicyId, policy);
            }
        }

        public OperationResult<InnerBoardVictoryEvaluation> Evaluate(
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
            if (!_policies.TryGetValue(definition.VictoryPolicyId, out var policy))
            {
                return OperationResult<InnerBoardVictoryEvaluation>.Failure(
                    "INNER_BOARD_VICTORY_POLICY_NOT_FOUND",
                    $"No victory policy is registered for '{definition.VictoryPolicyId}'.");
            }

            var evaluation = policy.Evaluate(definition, movedPlayerId, stateList.AsReadOnly());
            if (evaluation == null)
                throw new InvalidOperationException($"Victory policy '{policy.PolicyId}' returned no evaluation.");
            if (evaluation.GameEnded && stateList.All(state => state.PlayerId != evaluation.WinnerId))
            {
                return OperationResult<InnerBoardVictoryEvaluation>.Failure(
                    "INNER_BOARD_WINNER_NOT_FOUND",
                    $"Victory policy selected unknown player '{evaluation.WinnerId}'.");
            }

            return OperationResult<InnerBoardVictoryEvaluation>.Success(evaluation);
        }
    }
}
