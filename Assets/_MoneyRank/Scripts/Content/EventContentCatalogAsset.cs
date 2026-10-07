using System;
using System.Collections.Generic;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "EventContentCatalog", menuName = "Money Rank/Event Content Catalog")]
    public sealed class EventContentCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<EventDefinitionAsset> events = new List<EventDefinitionAsset>();
        [SerializeField] private List<EventTableDefinitionAsset> tables = new List<EventTableDefinitionAsset>();

        public IReadOnlyList<EventDefinitionAsset> Events => events;
        public IReadOnlyList<EventTableDefinitionAsset> Tables => tables;

        public EventContentCatalog ToDomain()
        {
            var eventDefinitions = new List<EventDefinition>(events.Count);
            foreach (var eventAsset in events)
            {
                if (eventAsset == null)
                    throw new InvalidOperationException("Event content catalog contains a missing event asset.");
                eventDefinitions.Add(eventAsset.ToDomain());
            }

            var tableDefinitions = new List<EventTableDefinition>(tables.Count);
            foreach (var tableAsset in tables)
            {
                if (tableAsset == null)
                    throw new InvalidOperationException("Event content catalog contains a missing table asset.");
                tableDefinitions.Add(tableAsset.ToDomain());
            }

            return new EventContentCatalog(eventDefinitions, tableDefinitions);
        }
    }
}
