using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "EventDefinition", menuName = "Money Rank/Event Definition")]
    public sealed class EventDefinitionAsset : ScriptableObject
    {
        [SerializeField] private EventDefinitionData definition = new EventDefinitionData();

        public EventDefinitionData Definition => definition;
        public EventDefinition ToDomain() => definition.ToDomain();
    }
}
