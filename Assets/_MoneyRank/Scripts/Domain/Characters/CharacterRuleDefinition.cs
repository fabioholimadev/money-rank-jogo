using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Characters
{
    public sealed class CharacterRuleDefinition
    {
        private readonly ReadOnlyCollection<DefinitionParameter> _parameters;
        private readonly ReadOnlyCollection<GameEffectDefinition> _effects;

        public CharacterRuleDefinition(
            string id,
            string displayName,
            string description,
            string typeId,
            IEnumerable<DefinitionParameter> parameters,
            IEnumerable<GameEffectDefinition> effects)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Character rule ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Character rule display name cannot be empty.", nameof(displayName));
            if (description == null)
                throw new ArgumentNullException(nameof(description));
            if (string.IsNullOrWhiteSpace(typeId))
                throw new ArgumentException("Character rule type ID cannot be empty.", nameof(typeId));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));

            var parameterList = parameters.ToList();
            var effectList = effects.ToList();
            if (parameterList.Any(parameter => parameter == null))
                throw new ArgumentException("Character rule parameters cannot contain null entries.", nameof(parameters));
            if (parameterList.Select(parameter => parameter.Key).Distinct(StringComparer.Ordinal).Count() != parameterList.Count)
                throw new ArgumentException("Character rule parameter keys must be unique.", nameof(parameters));
            if (effectList.Any(effect => effect == null))
                throw new ArgumentException("Character rule effects cannot contain null entries.", nameof(effects));

            Id = id;
            DisplayName = displayName;
            Description = description;
            TypeId = typeId;
            _parameters = parameterList.AsReadOnly();
            _effects = effectList.AsReadOnly();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string TypeId { get; }
        public IReadOnlyList<DefinitionParameter> Parameters => _parameters;
        public IReadOnlyList<GameEffectDefinition> Effects => _effects;
    }
}
