using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "EventTable", menuName = "Money Rank/Event Table")]
    public sealed class EventTableDefinitionAsset : ScriptableObject
    {
        [SerializeField] private EventTableDefinitionData definition = new EventTableDefinitionData();

        public EventTableDefinitionData Definition => definition;
        public EventTableDefinition ToDomain() => definition.ToDomain();
    }
}
