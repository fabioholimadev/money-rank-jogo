using System;
using System.Collections.Generic;
using MoneyRank.Domain;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "InnerBoardDefinition", menuName = "Money Rank/Inner Board Definition")]
    public sealed class InnerBoardDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField, Min(0)] private int startIndex;
        [SerializeField] private string movementPolicyId;
        [SerializeField] private string capturePolicyId;
        [SerializeField] private string victoryPolicyId;
        [SerializeField] private List<InnerBoardSpaceData> spaces = new List<InnerBoardSpaceData>();

        public InnerBoardDefinition ToDomain()
        {
            var definitions = new List<InnerBoardSpaceDefinition>(spaces.Count);
            foreach (var space in spaces)
            {
                if (space == null)
                    throw new InvalidOperationException("Inner board definition contains a missing space.");
                definitions.Add(space.ToDomain());
            }

            return new InnerBoardDefinition(
                id,
                definitions,
                startIndex,
                movementPolicyId,
                capturePolicyId,
                victoryPolicyId);
        }
    }

    [Serializable]
    public sealed class InnerBoardSpaceData
    {
        [SerializeField] private string id;
        [SerializeField, Min(0)] private int index;
        [SerializeField] private string typeId;
        [SerializeField] private bool isCheckpoint;
        [SerializeField] private bool isProtected;
        [SerializeField] private bool isGoal;

        public InnerBoardSpaceData(
            string id,
            int index,
            string typeId,
            bool isCheckpoint = false,
            bool isProtected = false,
            bool isGoal = false)
        {
            this.id = id;
            this.index = index;
            this.typeId = typeId;
            this.isCheckpoint = isCheckpoint;
            this.isProtected = isProtected;
            this.isGoal = isGoal;
        }

        public InnerBoardSpaceDefinition ToDomain() =>
            new InnerBoardSpaceDefinition(id, index, typeId, isCheckpoint, isProtected, isGoal);
    }
}
