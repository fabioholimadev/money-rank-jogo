using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventOptionDefinition
    {
        private readonly ReadOnlyCollection<RequirementDefinition> _requirements;
        private readonly ReadOnlyCollection<GameEffectDefinition> _effects;

        public EventOptionDefinition(
            string id,
            string label,
            string description,
            IEnumerable<RequirementDefinition> requirements,
            IEnumerable<GameEffectDefinition> effects,
            string visibilityId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Event option ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Event option label cannot be empty.", nameof(label));
            if (description == null)
                throw new ArgumentNullException(nameof(description));
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));
            if (string.IsNullOrWhiteSpace(visibilityId))
                throw new ArgumentException("Event option visibility ID cannot be empty.", nameof(visibilityId));

            var requirementList = requirements.ToList();
            var effectList = effects.ToList();
            if (requirementList.Any(requirement => requirement == null))
                throw new ArgumentException("Event option requirements cannot contain null entries.", nameof(requirements));
            if (effectList.Any(effect => effect == null))
                throw new ArgumentException("Event option effects cannot contain null entries.", nameof(effects));

            Id = id;
            Label = label;
            Description = description;
            VisibilityId = visibilityId;
            _requirements = requirementList.AsReadOnly();
            _effects = effectList.AsReadOnly();
        }

        public string Id { get; }
        public string Label { get; }
        public string Description { get; }
        public string VisibilityId { get; }
        public IReadOnlyList<RequirementDefinition> Requirements => _requirements;
        public IReadOnlyList<GameEffectDefinition> Effects => _effects;
    }
}
