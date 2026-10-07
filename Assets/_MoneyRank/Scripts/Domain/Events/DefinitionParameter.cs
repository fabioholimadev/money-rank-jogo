using System;

namespace MoneyRank.Domain.Events
{
    public sealed class DefinitionParameter
    {
        public DefinitionParameter(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Parameter key cannot be empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }
    }
}
