using System;
using System.Collections.Generic;
using System.Globalization;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "TaxRuleDefinition", menuName = "Money Rank/Tax Rule Definition")]
    public sealed class TaxRuleDefinitionAsset : ScriptableObject
    {
        [SerializeField] private TaxRuleDefinitionData definition = new TaxRuleDefinitionData();

        public TaxRuleDefinition ToDomain() => definition.ToDomain();
    }

    [Serializable]
    public sealed class TaxRuleDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string sourceAccountId;
        [SerializeField] private string rate;
        [SerializeField] private string roundingPolicyId;
        [SerializeField] private List<TaxDestinationData> destinations = new List<TaxDestinationData>();
        [SerializeField] private List<RequirementDefinitionData> exemptions = new List<RequirementDefinitionData>();

        public TaxRuleDefinitionData()
        {
        }

        public TaxRuleDefinitionData(
            string id,
            string sourceAccountId,
            string rate,
            string roundingPolicyId,
            IEnumerable<TaxDestinationData> destinations,
            IEnumerable<RequirementDefinitionData> exemptions)
        {
            this.id = id;
            this.sourceAccountId = sourceAccountId;
            this.rate = rate;
            this.roundingPolicyId = roundingPolicyId;
            this.destinations = new List<TaxDestinationData>(destinations ?? throw new ArgumentNullException(nameof(destinations)));
            this.exemptions = new List<RequirementDefinitionData>(exemptions ?? throw new ArgumentNullException(nameof(exemptions)));
        }

        public TaxRuleDefinition ToDomain()
        {
            if (!decimal.TryParse(rate, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedRate))
                throw new FormatException($"Tax rate '{rate}' is not a valid invariant decimal.");

            var domainDestinations = new List<TaxDestinationDefinition>(destinations.Count);
            foreach (var destination in destinations)
            {
                if (destination == null)
                    throw new InvalidOperationException("Tax rule contains a missing destination.");
                domainDestinations.Add(destination.ToDomain());
            }

            var domainExemptions = new List<RequirementDefinition>(exemptions.Count);
            foreach (var exemption in exemptions)
            {
                if (exemption == null)
                    throw new InvalidOperationException("Tax rule contains a missing exemption.");
                domainExemptions.Add(exemption.ToDomain());
            }

            return new TaxRuleDefinition(
                id,
                sourceAccountId,
                parsedRate,
                roundingPolicyId,
                domainDestinations,
                domainExemptions);
        }
    }

    [Serializable]
    public sealed class TaxDestinationData
    {
        [SerializeField] private string accountId;
        [SerializeField] private string share;

        public TaxDestinationData(string accountId, string share)
        {
            this.accountId = accountId;
            this.share = share;
        }

        public TaxDestinationDefinition ToDomain()
        {
            if (!decimal.TryParse(share, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedShare))
                throw new FormatException($"Tax destination share '{share}' is not a valid invariant decimal.");
            return new TaxDestinationDefinition(accountId, parsedShare);
        }
    }
}
