using System;
using System.Collections.Generic;
using MoneyRank.Domain;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "BoardDefinition", menuName = "Money Rank/Board Definition")]
    public sealed class BoardDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id = "vertical-slice-board";
        [SerializeField, Min(0)] private int startIndex;
        [SerializeField] private List<BoardSpaceData> spaces = CreateTemporarySpaces();

        public string Id => id;
        public int StartIndex => startIndex;
        public IReadOnlyList<BoardSpaceData> Spaces => spaces;

        public BoardDefinition ToDomain()
        {
            var definitions = new List<BoardSpaceDefinition>(spaces.Count);
            foreach (var space in spaces)
            {
                definitions.Add(space.ToDomain());
            }

            return new BoardDefinition(id, definitions, startIndex);
        }

        private static List<BoardSpaceData> CreateTemporarySpaces()
        {
            var result = new List<BoardSpaceData>(20);
            var categories = new[] { "expense", "investment", "citizenship", "opportunity" };

            for (var index = 0; index < 20; index++)
            {
                var typeId = index == 0 ? "start" : "event";
                var categoryId = index == 0 ? null : categories[(index - 1) % categories.Length];
                var eventTableId = index == 0 ? null : $"events-{categoryId}";
                result.Add(new BoardSpaceData($"space-{index:00}", index, typeId, categoryId, eventTableId));
            }

            return result;
        }
    }

    [Serializable]
    public sealed class BoardSpaceData
    {
        [SerializeField] private string id;
        [SerializeField, Min(0)] private int index;
        [SerializeField] private string typeId;
        [SerializeField] private string categoryId;
        [SerializeField] private string eventTableId;

        public BoardSpaceData(string id, int index, string typeId, string categoryId, string eventTableId)
        {
            this.id = id;
            this.index = index;
            this.typeId = typeId;
            this.categoryId = categoryId;
            this.eventTableId = eventTableId;
        }

        public string Id => id;
        public int Index => index;
        public string TypeId => typeId;
        public string CategoryId => categoryId;
        public string EventTableId => eventTableId;

        public BoardSpaceDefinition ToDomain()
        {
            return new BoardSpaceDefinition(id, index, typeId, categoryId, eventTableId);
        }
    }
}
