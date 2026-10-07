using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventTableDefinition
    {
        private readonly ReadOnlyCollection<EventDefinitionKey> _events;

        public EventTableDefinition(string id, IEnumerable<EventDefinitionKey> events)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Event table ID cannot be empty.", nameof(id));
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            var eventList = events.ToList();
            if (eventList.Count == 0)
                throw new ArgumentException("An event table requires at least one event reference.", nameof(events));
            if (eventList.Distinct().Count() != eventList.Count)
                throw new ArgumentException("Event references must be unique within a table.", nameof(events));

            Id = id;
            _events = eventList.AsReadOnly();
        }

        public string Id { get; }
        public IReadOnlyList<EventDefinitionKey> Events => _events;
    }
}
