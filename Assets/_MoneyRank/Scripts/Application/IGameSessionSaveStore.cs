using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public interface IGameSessionSaveStore
    {
        OperationResult Save(GameSessionMemento memento);
        OperationResult<GameSessionMemento> Load(SessionId sessionId);
    }
}
