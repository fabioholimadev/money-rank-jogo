using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Economy
{
    public sealed class TaxRuleDefinition
    {
        private readonly ReadOnlyCollection<TaxDestinationDefinition> _destinations;
        private readonly ReadOnlyCollection<RequirementDefinition> _exemptions;

        public TaxRuleDefinition(
            string id,
            string sourceAccountId,
            decimal rate,
            string roundingPolicyId,
            IEnumerable<TaxDestinationDefinition> destinations,
            IEnumerable<RequirementDefinition> exemptions)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tax rule ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(sourceAccountId))
                throw new ArgumentException("Tax source account ID cannot be empty.", nameof(sourceAccountId));
            if (rate < 0m || rate > 1m)
                throw new ArgumentOutOfRangeException(nameof(rate), "Tax rate must be between zero and one.");
            if (string.IsNullOrWhiteSpace(roundingPolicyId))
                throw new ArgumentException("Tax rounding policy ID cannot be empty.", nameof(roundingPolicyId));
            if (destinations == null)
                throw new ArgumentNullException(nameof(destinations));
            if (exemptions == null)
                throw new ArgumentNullException(nameof(exemptions));

            var destinationList = destinations.ToList();
            var exemptionList = exemptions.ToList();
            if (destinationList.Count == 0)
                throw new ArgumentException("Tax rule requires at least one destination.", nameof(destinations));
            if (destinationList.Any(destination => destination == null))
                throw new ArgumentException("Tax destinations cannot contain null entries.", nameof(destinations));
            if (destinationList.Select(destination => destination.AccountId).Distinct(StringComparer.Ordinal).Count() != destinationList.Count)
                throw new ArgumentException("Tax destination account IDs must be unique.", nameof(destinations));
            if (destinationList.Sum(destination => destination.Share) != 1m)
                throw new ArgumentException("Tax destination shares must sum exactly to one.", nameof(destinations));
            if (exemptionList.Any(exemption => exemption == null))
                throw new ArgumentException("Tax exemptions cannot contain null entries.", nameof(exemptions));

            Id = id;
            SourceAccountId = sourceAccountId;
            Rate = rate;
            RoundingPolicyId = roundingPolicyId;
            _destinations = destinationList.AsReadOnly();
            _exemptions = exemptionList.AsReadOnly();
        }

        public string Id { get; }
        public string SourceAccountId { get; }
        public decimal Rate { get; }
        public string RoundingPolicyId { get; }
        public IReadOnlyList<TaxDestinationDefinition> Destinations => _destinations;
        public IReadOnlyList<RequirementDefinition> Exemptions => _exemptions;
    }
}
