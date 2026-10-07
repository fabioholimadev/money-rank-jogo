using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MoneyRank.Domain.Events
{
    public sealed class RequirementEvaluationContext
    {
        private readonly ReadOnlyDictionary<string, string> _stateValues;

        public RequirementEvaluationContext(
            SessionId sessionId,
            PlayerId playerId,
            IEnumerable<DefinitionParameter> stateValues)
        {
            if (string.IsNullOrWhiteSpace(sessionId.Value))
                throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Player ID cannot be empty.", nameof(playerId));
            if (stateValues == null)
                throw new ArgumentNullException(nameof(stateValues));

            var stateMap = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var stateValue in stateValues)
            {
                if (stateValue == null)
                    throw new ArgumentException("State values cannot contain null entries.", nameof(stateValues));
                if (stateMap.ContainsKey(stateValue.Key))
                    throw new ArgumentException("State value keys must be unique.", nameof(stateValues));

                stateMap.Add(stateValue.Key, stateValue.Value);
            }

            SessionId = sessionId;
            PlayerId = playerId;
            _stateValues = new ReadOnlyDictionary<string, string>(stateMap);
        }

        public SessionId SessionId { get; }
        public PlayerId PlayerId { get; }
        public IReadOnlyDictionary<string, string> StateValues => _stateValues;

        public EventStateSnapshot CreateStateSnapshot() =>
            new EventStateSnapshot(
                System.Linq.Enumerable.Select(
                    _stateValues,
                    pair => new DefinitionParameter(pair.Key, pair.Value)));
    }
}
