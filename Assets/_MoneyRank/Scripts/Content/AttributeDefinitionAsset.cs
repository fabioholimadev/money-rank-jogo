using System;
using System.Collections.Generic;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "AttributeDefinition", menuName = "Money Rank/Attribute Definition")]
    public sealed class AttributeDefinitionAsset : ScriptableObject
    {
        [SerializeField] private AttributeDefinitionData definition = new AttributeDefinitionData();

        public AttributeDefinitionData Definition => definition;
        public AttributeDefinition ToDomain() => definition.ToDomain();
    }

    [Serializable]
    public sealed class AttributeDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string typeId;
        [SerializeField, Min(1)] private int threshold = 1;
        [SerializeField] private string overflowBehaviorId;
        [SerializeField] private List<GameEffectDefinitionData> completionEffects =
            new List<GameEffectDefinitionData>();
        [SerializeField] private string iconId;
        [SerializeField] private string presentationId;

        public AttributeDefinitionData()
        {
        }

        public AttributeDefinitionData(
            string id,
            string displayName,
            string typeId,
            int threshold,
            string overflowBehaviorId,
            IEnumerable<GameEffectDefinitionData> completionEffects,
            string iconId = null,
            string presentationId = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.typeId = typeId;
            this.threshold = threshold;
            this.overflowBehaviorId = overflowBehaviorId;
            this.completionEffects = new List<GameEffectDefinitionData>(
                completionEffects ?? throw new ArgumentNullException(nameof(completionEffects)));
            this.iconId = iconId;
            this.presentationId = presentationId;
        }

        public AttributeDefinition ToDomain()
        {
            var effects = new List<GameEffectDefinition>(completionEffects.Count);
            foreach (var effect in completionEffects)
            {
                if (effect == null)
                    throw new InvalidOperationException("Attribute definition contains a missing completion effect.");
                effects.Add(effect.ToDomain());
            }

            return new AttributeDefinition(
                id,
                displayName,
                typeId,
                threshold,
                overflowBehaviorId,
                effects,
                iconId,
                presentationId);
        }
    }
}
