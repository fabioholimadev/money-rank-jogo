using System;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyAccountDefinition
    {
        public EconomyAccountDefinition(
            string id,
            string displayName,
            string ownerId,
            string resourceTypeId,
            decimal? minimumBalance = null,
            decimal? maximumBalance = null,
            string unitId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Economy account ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Economy account display name cannot be empty.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Economy account owner ID cannot be empty.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(resourceTypeId))
                throw new ArgumentException("Economy resource type ID cannot be empty.", nameof(resourceTypeId));
            if (minimumBalance.HasValue && maximumBalance.HasValue && minimumBalance > maximumBalance)
                throw new ArgumentException("Economy account minimum cannot exceed its maximum.");

            Id = id;
            DisplayName = displayName;
            OwnerId = ownerId;
            ResourceTypeId = resourceTypeId;
            UnitId = string.IsNullOrWhiteSpace(unitId) ? resourceTypeId : unitId;
            MinimumBalance = minimumBalance;
            MaximumBalance = maximumBalance;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string OwnerId { get; }
        public string ResourceTypeId { get; }
        public string UnitId { get; }
        public decimal? MinimumBalance { get; }
        public decimal? MaximumBalance { get; }

        public bool Accepts(decimal balance) =>
            (!MinimumBalance.HasValue || balance >= MinimumBalance.Value) &&
            (!MaximumBalance.HasValue || balance <= MaximumBalance.Value);
    }
}
