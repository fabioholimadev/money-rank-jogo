using System.Collections.Generic;

namespace MoneyRank.Domain
{
    public interface IInnerBoardVictoryPolicy
    {
        string PolicyId { get; }

        InnerBoardVictoryEvaluation Evaluate(
            InnerBoardDefinition definition,
            PlayerId movedPlayerId,
            IReadOnlyList<InnerBoardState> states);
    }
}
