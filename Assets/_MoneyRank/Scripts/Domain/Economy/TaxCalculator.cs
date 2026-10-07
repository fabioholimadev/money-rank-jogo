using System;
using System.Collections.Generic;

namespace MoneyRank.Domain.Economy
{
    public sealed class TaxCalculator
    {
        private readonly Dictionary<string, ITaxRoundingPolicy> _roundingPolicies =
            new Dictionary<string, ITaxRoundingPolicy>(StringComparer.Ordinal);

        public TaxCalculator(IEnumerable<ITaxRoundingPolicy> roundingPolicies)
        {
            if (roundingPolicies == null)
                throw new ArgumentNullException(nameof(roundingPolicies));

            foreach (var policy in roundingPolicies)
            {
                if (policy == null)
                    throw new ArgumentException("Tax rounding policies cannot contain null entries.", nameof(roundingPolicies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId))
                    throw new ArgumentException("Tax rounding policy ID cannot be empty.", nameof(roundingPolicies));
                if (_roundingPolicies.ContainsKey(policy.PolicyId))
                    throw new ArgumentException($"Tax rounding policy '{policy.PolicyId}' is registered more than once.", nameof(roundingPolicies));
                _roundingPolicies.Add(policy.PolicyId, policy);
            }
        }

        public OperationResult<TaxCalculationResult> Calculate(
            TaxRuleDefinition rule,
            decimal taxableAmount)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (taxableAmount < 0m)
            {
                return OperationResult<TaxCalculationResult>.Failure(
                    "TAXABLE_AMOUNT_NEGATIVE",
                    "Taxable amount cannot be negative.");
            }
            if (!_roundingPolicies.TryGetValue(rule.RoundingPolicyId, out var roundingPolicy))
            {
                return OperationResult<TaxCalculationResult>.Failure(
                    "TAX_ROUNDING_POLICY_NOT_FOUND",
                    $"No tax rounding policy is registered for '{rule.RoundingPolicyId}'.");
            }

            var taxAmount = roundingPolicy.Round(taxableAmount * rule.Rate);
            if (taxAmount < 0m)
            {
                return OperationResult<TaxCalculationResult>.Failure(
                    "TAX_ROUNDING_RESULT_INVALID",
                    "Tax rounding policy produced a negative amount.");
            }
            if (taxAmount == 0m)
            {
                return OperationResult<TaxCalculationResult>.Success(
                    new TaxCalculationResult(rule.Id, taxableAmount, 0m, Array.Empty<EconomyTransactionEntry>()));
            }

            var entries = new List<EconomyTransactionEntry>
            {
                new EconomyTransactionEntry(rule.SourceAccountId, -taxAmount)
            };
            var allocated = 0m;
            for (var index = 0; index < rule.Destinations.Count; index++)
            {
                var destination = rule.Destinations[index];
                var amount = index == rule.Destinations.Count - 1
                    ? taxAmount - allocated
                    : roundingPolicy.Round(taxAmount * destination.Share);
                if (amount < 0m || allocated + amount > taxAmount)
                {
                    return OperationResult<TaxCalculationResult>.Failure(
                        "TAX_ALLOCATION_INVALID",
                        "Tax rounding policy produced an invalid destination allocation.");
                }
                allocated += amount;
                if (amount != 0m)
                    entries.Add(new EconomyTransactionEntry(destination.AccountId, amount));
            }

            return OperationResult<TaxCalculationResult>.Success(
                new TaxCalculationResult(rule.Id, taxableAmount, taxAmount, entries));
        }
    }
}
