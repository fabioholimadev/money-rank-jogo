using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MoneyRank.Domain.Events
{
    public sealed class RequirementDefinition
    {
        private readonly ReadOnlyDictionary<string, string> _parameters;

        public RequirementDefinition(string typeId, IEnumerable<DefinitionParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(typeId))
                throw new ArgumentException("Requirement type ID cannot be empty.", nameof(typeId));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var parameterMap = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var parameter in parameters)
            {
                if (parameter == null)
                    throw new ArgumentException("Requirement parameters cannot contain null entries.", nameof(parameters));
                if (parameterMap.ContainsKey(parameter.Key))
                    throw new ArgumentException("Requirement parameter keys must be unique.", nameof(parameters));

                parameterMap.Add(parameter.Key, parameter.Value);
            }

            TypeId = typeId;
            _parameters = new ReadOnlyDictionary<string, string>(parameterMap);
        }

        public string TypeId { get; }
        public IReadOnlyDictionary<string, string> Parameters => _parameters;
    }
}
