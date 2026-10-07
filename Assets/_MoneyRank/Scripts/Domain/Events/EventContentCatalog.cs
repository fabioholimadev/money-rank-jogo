using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventContentCatalog
    {
        private readonly ReadOnlyCollection<EventDefinition> _events;
        private readonly ReadOnlyCollection<EventTableDefinition> _tables;
        private readonly Dictionary<EventDefinitionKey, EventDefinition> _eventsByKey;
        private readonly Dictionary<string, EventTableDefinition> _tablesById;

        public EventContentCatalog(
            IEnumerable<EventDefinition> events,
            IEnumerable<EventTableDefinition> tables)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            var eventList = events.ToList();
            var tableList = tables.ToList();
            if (eventList.Any(definition => definition == null))
                throw new ArgumentException("Event catalog cannot contain null definitions.", nameof(events));
            if (tableList.Any(table => table == null))
                throw new ArgumentException("Event catalog cannot contain null tables.", nameof(tables));

            _eventsByKey = new Dictionary<EventDefinitionKey, EventDefinition>();
            foreach (var definition in eventList)
            {
                var key = new EventDefinitionKey(definition.Id, definition.Version);
                if (_eventsByKey.ContainsKey(key))
                    throw new ArgumentException($"Duplicate event definition '{key}'.", nameof(events));

                _eventsByKey.Add(key, definition);
            }

            _tablesById = new Dictionary<string, EventTableDefinition>(StringComparer.Ordinal);
            foreach (var table in tableList)
            {
                if (_tablesById.ContainsKey(table.Id))
                    throw new ArgumentException($"Duplicate event table '{table.Id}'.", nameof(tables));

                foreach (var eventKey in table.Events)
                {
                    if (!_eventsByKey.ContainsKey(eventKey))
                    {
                        throw new ArgumentException(
                            $"Event table '{table.Id}' references missing definition '{eventKey}'.",
                            nameof(tables));
                    }
                }

                _tablesById.Add(table.Id, table);
            }

            _events = eventList.AsReadOnly();
            _tables = tableList.AsReadOnly();
        }

        public IReadOnlyList<EventDefinition> Events => _events;
        public IReadOnlyList<EventTableDefinition> Tables => _tables;

        public EventDefinition GetEvent(EventDefinitionKey key) =>
            _eventsByKey.TryGetValue(key, out var definition) ? definition : null;

        public EventDefinition GetEvent(string eventId, int version) =>
            GetEvent(new EventDefinitionKey(eventId, version));

        public EventTableDefinition GetTable(string tableId)
        {
            if (string.IsNullOrWhiteSpace(tableId))
                throw new ArgumentException("Event table ID cannot be empty.", nameof(tableId));

            return _tablesById.TryGetValue(tableId, out var table) ? table : null;
        }
    }
}
