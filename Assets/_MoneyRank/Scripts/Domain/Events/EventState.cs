using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventState
    {
        private readonly Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public EventState(IEnumerable<DefinitionParameter> values)
        {
            ReplaceValues(values);
        }

        public bool TryGetValue(string key, out string value) =>
            _values.TryGetValue(key, out value);

        public void SetValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("State key cannot be empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _values[key] = value;
        }

        public bool RemoveValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("State key cannot be empty.", nameof(key));

            return _values.Remove(key);
        }

        public EventStateSnapshot CreateSnapshot() =>
            new EventStateSnapshot(
                _values.Select(pair => new DefinitionParameter(pair.Key, pair.Value)));

        public void Restore(EventStateSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            ReplaceValues(snapshot.Values.Select(pair => new DefinitionParameter(pair.Key, pair.Value)));
        }

        public static EventState FromSnapshot(EventStateSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            return new EventState(
                snapshot.Values.Select(pair => new DefinitionParameter(pair.Key, pair.Value)));
        }

        private void ReplaceValues(IEnumerable<DefinitionParameter> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var replacement = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (value == null)
                    throw new ArgumentException("State values cannot contain null entries.", nameof(values));
                if (replacement.ContainsKey(value.Key))
                    throw new ArgumentException("State value keys must be unique.", nameof(values));

                replacement.Add(value.Key, value.Value);
            }

            _values.Clear();
            foreach (var value in replacement)
            {
                _values.Add(value.Key, value.Value);
            }
        }
    }
}
