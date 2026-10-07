using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain
{
    public sealed class AttributeProgressionResult
    {
        private readonly ReadOnlyCollection<GameEffectDefinition> _plannedCompletionEffects;

        public AttributeProgressionResult(
            string attributeId,
            int beforeProgress,
            int afterProgress,
            int completedCycles,
            int totalCompletedCycles,
            IEnumerable<GameEffectDefinition> plannedCompletionEffects)
        {
            if (string.IsNullOrWhiteSpace(attributeId))
                throw new ArgumentException("Attribute ID cannot be empty.", nameof(attributeId));
            if (beforeProgress < 0)
                throw new ArgumentOutOfRangeException(nameof(beforeProgress));
            if (afterProgress < 0)
                throw new ArgumentOutOfRangeException(nameof(afterProgress));
            if (completedCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(completedCycles));
            if (totalCompletedCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(totalCompletedCycles));
            if (plannedCompletionEffects == null)
                throw new ArgumentNullException(nameof(plannedCompletionEffects));

            AttributeId = attributeId;
            BeforeProgress = beforeProgress;
            AfterProgress = afterProgress;
            CompletedCycles = completedCycles;
            TotalCompletedCycles = totalCompletedCycles;
            _plannedCompletionEffects = plannedCompletionEffects.ToList().AsReadOnly();
        }

        public string AttributeId { get; }
        public int BeforeProgress { get; }
        public int AfterProgress { get; }
        public int CompletedCycles { get; }
        public int TotalCompletedCycles { get; }
        public IReadOnlyList<GameEffectDefinition> PlannedCompletionEffects => _plannedCompletionEffects;
    }
}
