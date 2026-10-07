using System;
using System.Collections.Generic;
using MoneyRank.Domain.Characters;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Money Rank/Character Definition")]
    public sealed class CharacterDefinitionAsset : ScriptableObject
    {
        [SerializeField] private CharacterDefinitionData definition = new CharacterDefinitionData();

        public CharacterDefinition ToDomain() => definition.ToDomain();
    }

    [Serializable]
    public sealed class CharacterDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<CharacterRuleDefinitionData> salaryRules = new List<CharacterRuleDefinitionData>();
        [SerializeField] private List<GameEffectDefinitionData> startingResources = new List<GameEffectDefinitionData>();
        [SerializeField] private List<GameEffectDefinitionData> startingAttributes = new List<GameEffectDefinitionData>();
        [SerializeField] private List<CharacterRuleDefinitionData> passiveAbilities = new List<CharacterRuleDefinitionData>();
        [SerializeField] private List<CharacterRuleDefinitionData> disadvantages = new List<CharacterRuleDefinitionData>();
        [SerializeField] private string progressionTrackId;
        [SerializeField] private string visualPrefabId;
        [SerializeField] private string portraitId;

        public CharacterDefinitionData()
        {
        }

        public CharacterDefinitionData(
            string id,
            string displayName,
            string description,
            IEnumerable<CharacterRuleDefinitionData> salaryRules,
            IEnumerable<GameEffectDefinitionData> startingResources,
            IEnumerable<GameEffectDefinitionData> startingAttributes,
            IEnumerable<CharacterRuleDefinitionData> passiveAbilities,
            IEnumerable<CharacterRuleDefinitionData> disadvantages,
            string progressionTrackId,
            string visualPrefabId,
            string portraitId)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.salaryRules = new List<CharacterRuleDefinitionData>(salaryRules ?? throw new ArgumentNullException(nameof(salaryRules)));
            this.startingResources = new List<GameEffectDefinitionData>(startingResources ?? throw new ArgumentNullException(nameof(startingResources)));
            this.startingAttributes = new List<GameEffectDefinitionData>(startingAttributes ?? throw new ArgumentNullException(nameof(startingAttributes)));
            this.passiveAbilities = new List<CharacterRuleDefinitionData>(passiveAbilities ?? throw new ArgumentNullException(nameof(passiveAbilities)));
            this.disadvantages = new List<CharacterRuleDefinitionData>(disadvantages ?? throw new ArgumentNullException(nameof(disadvantages)));
            this.progressionTrackId = progressionTrackId;
            this.visualPrefabId = visualPrefabId;
            this.portraitId = portraitId;
        }

        public CharacterDefinition ToDomain() =>
            new CharacterDefinition(
                id,
                displayName,
                description,
                ConvertRules(salaryRules, "salary rule"),
                ConvertEffects(startingResources, "starting resource"),
                ConvertEffects(startingAttributes, "starting attribute"),
                ConvertRules(passiveAbilities, "passive ability"),
                ConvertRules(disadvantages, "disadvantage"),
                progressionTrackId,
                visualPrefabId,
                portraitId);

        private static IEnumerable<CharacterRuleDefinition> ConvertRules(
            IEnumerable<CharacterRuleDefinitionData> values,
            string label)
        {
            foreach (var value in values)
            {
                if (value == null) throw new InvalidOperationException($"Character contains a missing {label}.");
                yield return value.ToDomain();
            }
        }

        private static IEnumerable<GameEffectDefinition> ConvertEffects(
            IEnumerable<GameEffectDefinitionData> values,
            string label)
        {
            foreach (var value in values)
            {
                if (value == null) throw new InvalidOperationException($"Character contains a missing {label}.");
                yield return value.ToDomain();
            }
        }
    }

    [Serializable]
    public sealed class CharacterRuleDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string typeId;
        [SerializeField] private List<DefinitionParameterData> parameters = new List<DefinitionParameterData>();
        [SerializeField] private List<GameEffectDefinitionData> effects = new List<GameEffectDefinitionData>();

        public CharacterRuleDefinitionData()
        {
        }

        public CharacterRuleDefinitionData(
            string id,
            string displayName,
            string description,
            string typeId,
            IEnumerable<DefinitionParameterData> parameters,
            IEnumerable<GameEffectDefinitionData> effects)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.typeId = typeId;
            this.parameters = new List<DefinitionParameterData>(parameters ?? throw new ArgumentNullException(nameof(parameters)));
            this.effects = new List<GameEffectDefinitionData>(effects ?? throw new ArgumentNullException(nameof(effects)));
        }

        public CharacterRuleDefinition ToDomain()
        {
            var domainParameters = new List<DefinitionParameter>(parameters.Count);
            foreach (var parameter in parameters)
            {
                if (parameter == null) throw new InvalidOperationException("Character rule contains a missing parameter.");
                domainParameters.Add(parameter.ToDomain());
            }
            var domainEffects = new List<GameEffectDefinition>(effects.Count);
            foreach (var effect in effects)
            {
                if (effect == null) throw new InvalidOperationException("Character rule contains a missing effect.");
                domainEffects.Add(effect.ToDomain());
            }
            return new CharacterRuleDefinition(id, displayName, description, typeId, domainParameters, domainEffects);
        }
    }
}
