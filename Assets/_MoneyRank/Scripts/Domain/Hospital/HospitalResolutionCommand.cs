using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain.Events;

namespace MoneyRank.Domain.Hospital
{
    public sealed class HospitalResolutionCommand
    {
        private readonly ReadOnlyCollection<DefinitionParameter> _parameters;

        public HospitalResolutionCommand(string id, IEnumerable<DefinitionParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Hospital command ID cannot be empty.", nameof(id));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var values = parameters.ToList();
            if (values.Any(parameter => parameter == null))
                throw new ArgumentException("Hospital command parameters cannot contain null entries.", nameof(parameters));
            if (values.Select(parameter => parameter.Key).Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Hospital command parameter keys must be unique.", nameof(parameters));

            Id = id;
            _parameters = values.AsReadOnly();
        }

        public string Id { get; }
        public IReadOnlyList<DefinitionParameter> Parameters => _parameters;
    }
}
