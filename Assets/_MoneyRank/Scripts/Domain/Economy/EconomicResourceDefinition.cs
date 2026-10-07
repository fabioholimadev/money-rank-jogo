using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomicResourceDefinition
    {
        private readonly ReadOnlyCollection<GameEffectDefinition> _benefits;
        private readonly ReadOnlyCollection<GameEffectDefinition> _annualEffects;

        public EconomicResourceDefinition(
            string id,
            string displayName,
            string categoryId,
            string unitId,
            string transactionPolicyId,
            IEnumerable<GameEffectDefinition> benefits,
            IEnumerable<GameEffectDefinition> annualEffects,
            decimal? maximumPerOwner = null,
            string presentationId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Economic resource ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Economic resource display name cannot be empty.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(categoryId))
                throw new ArgumentException("Economic resource category ID cannot be empty.", nameof(categoryId));
            if (string.IsNullOrWhiteSpace(unitId))
                throw new ArgumentException("Economic resource unit ID cannot be empty.", nameof(unitId));
            if (string.IsNullOrWhiteSpace(transactionPolicyId))
                throw new ArgumentException("Economic resource transaction policy ID cannot be empty.", nameof(transactionPolicyId));
            if (maximumPerOwner.HasValue && maximumPerOwner.Value < 0m)
                throw new ArgumentOutOfRangeException(nameof(maximumPerOwner));
            if (benefits == null) throw new ArgumentNullException(nameof(benefits));
            if (annualEffects == null) throw new ArgumentNullException(nameof(annualEffects));

            var benefitList = benefits.ToList();
            var annualList = annualEffects.ToList();
            if (benefitList.Any(effect => effect == null) || annualList.Any(effect => effect == null))
                throw new ArgumentException("Economic resource effects cannot contain null entries.");

            Id = id;
            DisplayName = displayName;
            CategoryId = categoryId;
            UnitId = unitId;
            TransactionPolicyId = transactionPolicyId;
            MaximumPerOwner = maximumPerOwner;
            PresentationId = presentationId;
            _benefits = benefitList.AsReadOnly();
            _annualEffects = annualList.AsReadOnly();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string CategoryId { get; }
        public string UnitId { get; }
        public string TransactionPolicyId { get; }
        public decimal? MaximumPerOwner { get; }
        public string PresentationId { get; }
        public IReadOnlyList<GameEffectDefinition> Benefits => _benefits;
        public IReadOnlyList<GameEffectDefinition> AnnualEffects => _annualEffects;
    }
}
