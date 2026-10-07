using System.Collections.Generic;

namespace MoneyRank.Domain
{
    public interface IInnerBoardCapturePolicy
    {
        string PolicyId { get; }

        OperationResult<InnerBoardCaptureCalculation> Calculate(
            InnerBoardDefinition definition,
            PlayerId movedPlayerId,
            IReadOnlyList<InnerBoardState> states);
    }
}
