using System;
using System.Collections.Generic;

namespace MoneyRank.Domain.Economy
{
    public sealed class AnnualCycleService
    {
        private readonly Dictionary<string, IAnnualCyclePolicy> _policies =
            new Dictionary<string, IAnnualCyclePolicy>(StringComparer.Ordinal);
        private readonly EconomyTransactionService _transactionService;

        public AnnualCycleService(
            IEnumerable<IAnnualCyclePolicy> policies,
            EconomyTransactionService transactionService)
        {
            if (policies == null)
                throw new ArgumentNullException(nameof(policies));

            foreach (var policy in policies)
            {
                if (policy == null)
                    throw new ArgumentException("Annual cycle policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId))
                    throw new ArgumentException("Annual cycle policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId))
                    throw new ArgumentException($"Annual cycle policy '{policy.PolicyId}' is registered more than once.", nameof(policies));

                _policies.Add(policy.PolicyId, policy);
            }

            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        }

        public int GetPendingCycleCount(
            AnnualCycleDefinition definition,
            AnnualCycleState state,
            int completedLaps)
        {
            ValidateState(definition, state, completedLaps);
            return completedLaps / definition.CompletedLapsPerCycle - state.ProcessedCycles;
        }

        public OperationResult<AnnualCycleResolutionResult> ResolveNext(
            AnnualCycleDefinition definition,
            AnnualCycleState state,
            int completedLaps,
            EconomyState economy,
            EconomyTransactionHistory history,
            string transactionId,
            DateTimeOffset createdAt,
            string operatorId)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (economy == null)
                throw new ArgumentNullException(nameof(economy));
            if (history == null)
                throw new ArgumentNullException(nameof(history));

            int pendingCycles;
            try
            {
                pendingCycles = GetPendingCycleCount(definition, state, completedLaps);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_STATE_INVALID",
                    exception.Message);
            }

            if (pendingCycles == 0)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_NOT_DUE",
                    "The player has no pending annual cycle.");
            }
            if (!_policies.TryGetValue(definition.PolicyId, out var policy))
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_POLICY_NOT_FOUND",
                    $"No annual cycle policy is registered for '{definition.PolicyId}'.");
            }

            AnnualCycleContext context;
            try
            {
                context = new AnnualCycleContext(
                    state.PlayerId,
                    state.ProcessedCycles + 1,
                    economy.CreateSnapshot(),
                    transactionId,
                    createdAt,
                    operatorId);
            }
            catch (ArgumentException exception)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_CONTEXT_INVALID",
                    exception.Message);
            }

            var planned = policy.CreateTransaction(definition, context);
            if (!planned.Succeeded)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    planned.ErrorCode,
                    planned.Message);
            }
            if (planned.Value == null)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_TRANSACTION_MISSING",
                    "The annual cycle policy did not produce a transaction.");
            }
            if (!string.Equals(planned.Value.Id, transactionId, StringComparison.Ordinal))
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    "ANNUAL_CYCLE_TRANSACTION_ID_MISMATCH",
                    "The annual cycle policy changed the requested transaction ID.");
            }

            var transaction = _transactionService.Apply(economy, history, planned.Value);
            if (!transaction.Succeeded)
            {
                return OperationResult<AnnualCycleResolutionResult>.Failure(
                    transaction.ErrorCode,
                    transaction.Message);
            }

            state.MarkProcessed(context.CycleNumber);
            return OperationResult<AnnualCycleResolutionResult>.Success(
                new AnnualCycleResolutionResult(
                    state.PlayerId,
                    context.CycleNumber,
                    pendingCycles - 1,
                    transaction.Value));
        }

        private static void ValidateState(
            AnnualCycleDefinition definition,
            AnnualCycleState state,
            int completedLaps)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (completedLaps < 0)
                throw new ArgumentOutOfRangeException(nameof(completedLaps));

            var eligibleCycles = completedLaps / definition.CompletedLapsPerCycle;
            if (state.ProcessedCycles > eligibleCycles)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    "Processed annual cycles cannot exceed the cycles earned from completed laps.");
            }
        }
    }
}
