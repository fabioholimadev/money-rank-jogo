using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public interface IGameSessionRepository
    {
        GameSession Get(SessionId sessionId);
        void Save(GameSession session);
    }
}

