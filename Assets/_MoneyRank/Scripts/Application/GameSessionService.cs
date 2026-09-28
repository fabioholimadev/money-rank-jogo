using System;
using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public sealed class GameSessionService
    {
        private readonly IGameSessionRepository _repository;

        public GameSessionService(IGameSessionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public GameSession Create(SessionId sessionId, SessionRules rules)
        {
            return Create(sessionId, rules, null);
        }

        public GameSession Create(SessionId sessionId, SessionRules rules, BoardDefinition board)
        {
            var session = new GameSession(sessionId, rules, new GameFlowStateMachine(), board);
            _repository.Save(session);
            return session;
        }

        public OperationResult AddPlayer(SessionId sessionId, Player player)
        {
            var session = RequireSession(sessionId);
            var result = session.AddPlayer(player);
            if (result.Succeeded)
            {
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult<BoardMoveResult> RegisterMove(SessionId sessionId, int steps)
        {
            var session = RequireSession(sessionId);
            var result = session.RegisterMove(steps);
            if (result.Succeeded)
            {
                _repository.Save(session);
            }

            return result;
        }

        public OperationResult Start(SessionId sessionId)
        {
            var session = RequireSession(sessionId);
            var result = session.Start();
            if (result.Succeeded)
            {
                _repository.Save(session);
            }

            return result;
        }

        private GameSession RequireSession(SessionId sessionId) =>
            _repository.Get(sessionId) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }
}

