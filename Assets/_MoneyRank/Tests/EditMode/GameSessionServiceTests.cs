using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameSessionServiceTests
    {
        [Test]
        public void SuccessfulCommandsPersistTheSession()
        {
            var repository = new MemoryRepository();
            var service = new GameSessionService(repository);
            var sessionId = new SessionId("application-test");
            service.Create(sessionId, new SessionRules(1, 4));

            var result = service.AddPlayer(sessionId, new Player(new PlayerId("p1"), "Player", 0));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void FailedCommandsDoNotPersistASecondTime()
        {
            var repository = new MemoryRepository();
            var service = new GameSessionService(repository);
            var sessionId = new SessionId("application-test");
            service.Create(sessionId, new SessionRules(2, 4));

            var result = service.Start(sessionId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
        }

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions = new Dictionary<SessionId, GameSession>();

            public int SaveCount { get; private set; }

            public GameSession Get(SessionId sessionId) =>
                _sessions.TryGetValue(sessionId, out var session) ? session : null;

            public void Save(GameSession session)
            {
                _sessions[session.Id] = session;
                SaveCount++;
            }
        }
    }
}
