using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain
{
    public sealed class AttributeDefinition
    {
        private readonly ReadOnlyCollection<GameEffectDefinition> _completionEffects;

        public AttributeDefinition(
            string id,
            string displayName,
            string typeId,
            int threshold,
            string overflowBehaviorId,
            IEnumerable<GameEffectDefinition> completionEffects,
            string iconId = null,
            string presentationId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Attribute ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Attribute display name cannot be empty.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(typeId))
                throw new ArgumentException("Attribute type ID cannot be empty.", nameof(typeId));
            if (threshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Attribute threshold must be positive.");
            if (string.IsNullOrWhiteSpace(overflowBehaviorId))
                throw new ArgumentException("Attribute overflow behavior ID cannot be empty.", nameof(overflowBehaviorId));
            if (completionEffects == null)
                throw new ArgumentNullException(nameof(completionEffects));

            var effects = completionEffects.ToList();
            if (effects.Any(effect => effect == null))
                throw new ArgumentException("Attribute completion effects cannot contain null entries.", nameof(completionEffects));

            Id = id;
            DisplayName = displayName;
            TypeId = typeId;
            Threshold = threshold;
            OverflowBehaviorId = overflowBehaviorId;
            IconId = iconId;
            PresentationId = presentationId;
            _completionEffects = effects.AsReadOnly();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string TypeId { get; }
        public int Threshold { get; }
        public string OverflowBehaviorId { get; }
        public string IconId { get; }
        public string PresentationId { get; }
        public IReadOnlyList<GameEffectDefinition> CompletionEffects => _completionEffects;
    }
}
