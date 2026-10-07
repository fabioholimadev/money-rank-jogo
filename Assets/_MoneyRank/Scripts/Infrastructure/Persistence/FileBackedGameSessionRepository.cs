using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;

namespace MoneyRank.Infrastructure.Persistence
{
    public sealed class FileBackedGameSessionRepository : IGameSessionRepository
    {
        private readonly Dictionary<SessionId, GameSession> _sessions =
            new Dictionary<SessionId, GameSession>();
        private readonly IGameSessionSaveStore _saveStore;

        public FileBackedGameSessionRepository(IGameSessionSaveStore saveStore)
        {
            _saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
        }

        public GameSession Get(SessionId sessionId) =>
            _sessions.TryGetValue(sessionId, out var session) ? session : null;

        public void Save(GameSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var save = _saveStore.Save(session.CreateMemento());
            if (!save.Succeeded)
                throw new InvalidOperationException($"{save.ErrorCode}: {save.Message}");

            _sessions[session.Id] = session;
        }

        public OperationResult Attach(GameSession session, bool restoreExistingSave)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (restoreExistingSave)
            {
                var load = _saveStore.Load(session.Id);
                if (!load.Succeeded)
                    return OperationResult.Failure(load.ErrorCode, load.Message);

                var restore = session.Restore(load.Value);
                if (!restore.Succeeded)
                    return restore;
            }

            _sessions[session.Id] = session;
            return OperationResult.Success();
        }
    }
}
