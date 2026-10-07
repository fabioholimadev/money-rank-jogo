using System;

namespace MoneyRank.Domain
{
    public sealed class InnerBoardSpaceDefinition
    {
        public InnerBoardSpaceDefinition(
            string id,
            int index,
            string typeId,
            bool isCheckpoint = false,
            bool isProtected = false,
            bool isGoal = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Inner board space ID cannot be empty.", nameof(id));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (string.IsNullOrWhiteSpace(typeId))
                throw new ArgumentException("Inner board space type ID cannot be empty.", nameof(typeId));

            Id = id;
            Index = index;
            TypeId = typeId;
            IsCheckpoint = isCheckpoint;
            IsProtected = isProtected;
            IsGoal = isGoal;
        }

        public string Id { get; }
        public int Index { get; }
        public string TypeId { get; }
        public bool IsCheckpoint { get; }
        public bool IsProtected { get; }
        public bool IsGoal { get; }
    }
}
