using System;
using System.Collections.Generic;
using System.Globalization;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "EconomicResourceDefinition", menuName = "Money Rank/Economic Resource Definition")]
    public sealed class EconomicResourceDefinitionAsset : ScriptableObject
    {
        [SerializeField] private EconomicResourceDefinitionData definition = new EconomicResourceDefinitionData();

        public EconomicResourceDefinition ToDomain() => definition.ToDomain();
    }

    [Serializable]
    public sealed class EconomicResourceDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string categoryId;
        [SerializeField] private string unitId;
        [SerializeField] private string transactionPolicyId;
        [SerializeField] private bool hasMaximumPerOwner;
        [SerializeField] private string maximumPerOwner;
        [SerializeField] private string presentationId;
        [SerializeField] private List<GameEffectDefinitionData> benefits = new List<GameEffectDefinitionData>();
        [SerializeField] private List<GameEffectDefinitionData> annualEffects = new List<GameEffectDefinitionData>();

        public EconomicResourceDefinitionData()
        {
        }

        public EconomicResourceDefinitionData(
            string id,
            string displayName,
            string categoryId,
            string unitId,
            string transactionPolicyId,
            string maximumPerOwner,
            string presentationId,
            IEnumerable<GameEffectDefinitionData> benefits,
            IEnumerable<GameEffectDefinitionData> annualEffects)
        {
            this.id = id;
            this.displayName = displayName;
            this.categoryId = categoryId;
            this.unitId = unitId;
            this.transactionPolicyId = transactionPolicyId;
            hasMaximumPerOwner = maximumPerOwner != null;
            this.maximumPerOwner = maximumPerOwner;
            this.presentationId = presentationId;
            this.benefits = new List<GameEffectDefinitionData>(benefits ?? throw new ArgumentNullException(nameof(benefits)));
            this.annualEffects = new List<GameEffectDefinitionData>(annualEffects ?? throw new ArgumentNullException(nameof(annualEffects)));
        }

        public EconomicResourceDefinition ToDomain()
        {
            decimal? maximum = null;
            if (hasMaximumPerOwner)
            {
                if (!decimal.TryParse(maximumPerOwner, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    throw new FormatException($"Economic resource maximum '{maximumPerOwner}' is not a valid invariant decimal.");
                maximum = parsed;
            }

            return new EconomicResourceDefinition(
                id,
                displayName,
                categoryId,
                unitId,
                transactionPolicyId,
                ConvertEffects(benefits, "benefit"),
                ConvertEffects(annualEffects, "annual effect"),
                maximum,
                presentationId);
        }

        private static IEnumerable<GameEffectDefinition> ConvertEffects(
            IEnumerable<GameEffectDefinitionData> values,
            string label)
        {
            foreach (var value in values)
            {
                if (value == null) throw new InvalidOperationException($"Economic resource contains a missing {label}.");
                yield return value.ToDomain();
            }
        }
    }
}
