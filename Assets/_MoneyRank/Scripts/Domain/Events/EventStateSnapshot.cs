using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventStateSnapshot
    {
        private readonly ReadOnlyDictionary<string, string> _values;

        public EventStateSnapshot(IEnumerable<DefinitionParameter> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var valueMap = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (value == null)
                    throw new ArgumentException("Snapshot values cannot contain null entries.", nameof(values));
                if (valueMap.ContainsKey(value.Key))
                    throw new ArgumentException("Snapshot value keys must be unique.", nameof(values));

                valueMap.Add(value.Key, value.Value);
            }

            _values = new ReadOnlyDictionary<string, string>(valueMap);
        }

        public IReadOnlyDictionary<string, string> Values => _values;

        public bool ContentEquals(EventStateSnapshot other)
        {
            if (other == null || _values.Count != other._values.Count) return false;
            return _values.All(pair =>
                other._values.TryGetValue(pair.Key, out var value) &&
                string.Equals(pair.Value, value, StringComparison.Ordinal));
        }
    }
}
