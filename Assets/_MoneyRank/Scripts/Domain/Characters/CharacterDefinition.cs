using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Characters
{
    public sealed class CharacterDefinition
    {
        private readonly ReadOnlyCollection<CharacterRuleDefinition> _salaryRules;
        private readonly ReadOnlyCollection<GameEffectDefinition> _startingResources;
        private readonly ReadOnlyCollection<GameEffectDefinition> _startingAttributes;
        private readonly ReadOnlyCollection<CharacterRuleDefinition> _passiveAbilities;
        private readonly ReadOnlyCollection<CharacterRuleDefinition> _disadvantages;

        public CharacterDefinition(
            string id,
            string displayName,
            string description,
            IEnumerable<CharacterRuleDefinition> salaryRules,
            IEnumerable<GameEffectDefinition> startingResources,
            IEnumerable<GameEffectDefinition> startingAttributes,
            IEnumerable<CharacterRuleDefinition> passiveAbilities,
            IEnumerable<CharacterRuleDefinition> disadvantages,
            string progressionTrackId = null,
            string visualPrefabId = null,
            string portraitId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Character ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Character display name cannot be empty.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Character description cannot be empty.", nameof(description));

            Id = id;
            DisplayName = displayName;
            Description = description;
            ProgressionTrackId = progressionTrackId;
            VisualPrefabId = visualPrefabId;
            PortraitId = portraitId;
            _salaryRules = CopyRules(salaryRules, nameof(salaryRules));
            _startingResources = CopyEffects(startingResources, nameof(startingResources));
            _startingAttributes = CopyEffects(startingAttributes, nameof(startingAttributes));
            _passiveAbilities = CopyRules(passiveAbilities, nameof(passiveAbilities));
            _disadvantages = CopyRules(disadvantages, nameof(disadvantages));
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public IReadOnlyList<CharacterRuleDefinition> SalaryRules => _salaryRules;
        public IReadOnlyList<GameEffectDefinition> StartingResources => _startingResources;
        public IReadOnlyList<GameEffectDefinition> StartingAttributes => _startingAttributes;
        public IReadOnlyList<CharacterRuleDefinition> PassiveAbilities => _passiveAbilities;
        public IReadOnlyList<CharacterRuleDefinition> Disadvantages => _disadvantages;
        public string ProgressionTrackId { get; }
        public string VisualPrefabId { get; }
        public string PortraitId { get; }

        private static ReadOnlyCollection<CharacterRuleDefinition> CopyRules(
            IEnumerable<CharacterRuleDefinition> values,
            string parameterName)
        {
            if (values == null) throw new ArgumentNullException(parameterName);
            var list = values.ToList();
            if (list.Any(value => value == null))
                throw new ArgumentException("Character rules cannot contain null entries.", parameterName);
            if (list.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != list.Count)
                throw new ArgumentException("Character rule IDs must be unique within their collection.", parameterName);
            return list.AsReadOnly();
        }

        private static ReadOnlyCollection<GameEffectDefinition> CopyEffects(
            IEnumerable<GameEffectDefinition> values,
            string parameterName)
        {
            if (values == null) throw new ArgumentNullException(parameterName);
            var list = values.ToList();
            if (list.Any(value => value == null))
                throw new ArgumentException("Character effects cannot contain null entries.", parameterName);
            return list.AsReadOnly();
        }
    }
}
