using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class AdministrativeAdjustmentTests
    {
        [Test]
        public void AuthorizedAdjustmentRecordsReasonOperatorTimeAndBeforeAfter()
        {
            var repository = new MemoryRepository();
            var session = CreateSession();
            repository.Save(session);
            var service = new AdministrativeEconomyAdjustmentService(
                repository,
                new FixedAuthorizationPolicy(true));
            var createdAt = new DateTimeOffset(2026, 10, 4, 18, 0, 0, TimeSpan.FromHours(-3));

            var result = service.Apply(
                session.Id,
                CreateRequest("adjustment-1", "Playtest correction", createdAt, "facilitator-1"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.SourceId, Is.EqualTo(AdministrativeEconomyAdjustmentService.OperationId));
            Assert.That(result.Value.Description, Is.EqualTo("Playtest correction"));
            Assert.That(result.Value.OperatorId, Is.EqualTo("facilitator-1"));
            Assert.That(result.Value.CreatedAt, Is.EqualTo(createdAt));
            Assert.That(result.Value.BeforeState.Balances["player-cash"], Is.Zero);
            Assert.That(result.Value.AfterState.Balances["player-cash"], Is.EqualTo(5m));
            Assert.That(session.EconomyHistory.Records, Has.Count.EqualTo(1));
            Assert.That(repository.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void DeniedAdjustmentDoesNotMutatePersistOrAppendHistory()
        {
            var repository = new MemoryRepository();
            var session = CreateSession();
            repository.Save(session);
            var service = new AdministrativeEconomyAdjustmentService(
                repository,
                new FixedAuthorizationPolicy(false));

            var result = service.Apply(
                session.Id,
                CreateRequest("adjustment-1", "Attempted correction", DateTimeOffset.UtcNow, "student"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ADMINISTRATION_NOT_AUTHORIZED"));
            Assert.That(session.EconomyHistory.Records, Is.Empty);
            session.EconomyState.TryGetBalance("player-cash", out var cash);
            Assert.That(cash, Is.Zero);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void AdjustmentRequiresExplicitReasonBeforeExecution()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRequest("adjustment-1", " ", DateTimeOffset.UtcNow, "facilitator"));
        }

        private static ManualEconomyAdjustmentRequest CreateRequest(
            string id,
            string reason,
            DateTimeOffset createdAt,
            string operatorId) =>
            new ManualEconomyAdjustmentRequest(
                id,
                new[]
                {
                    new EconomyTransactionEntry("bank", -5m),
                    new EconomyTransactionEntry("player-cash", 5m)
                },
                reason,
                createdAt,
                operatorId);

        private static GameSession CreateSession()
        {
            var economy = new EconomyDefinition(
                new[]
                {
                    new EconomyAccountDefinition("bank", "Bank", "bank", "cash", 0m),
                    new EconomyAccountDefinition("player-cash", "Player cash", "p1", "cash", 0m)
                },
                new[]
                {
                    new EconomyBalanceDefinition("bank", 100m),
                    new EconomyBalanceDefinition("player-cash", 0m)
                });
            return new GameSession(
                new SessionId("administration-session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                null,
                economy: economy);
        }

        private sealed class FixedAuthorizationPolicy : IAdministrationAuthorizationPolicy
        {
            private readonly bool _authorized;

            public FixedAuthorizationPolicy(bool authorized)
            {
                _authorized = authorized;
            }

            public OperationResult Authorize(
                SessionId sessionId,
                string operatorId,
                string operationId) =>
                _authorized
                    ? OperationResult.Success()
                    : OperationResult.Failure(
                        "ADMINISTRATION_NOT_AUTHORIZED",
                        "The configured authorization policy denied this operation.");
        }

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions =
                new Dictionary<SessionId, GameSession>();

            public int SaveCount { get; private set; }
            public GameSession Get(SessionId sessionId) => _sessions[sessionId];

            public void Save(GameSession session)
            {
                _sessions[session.Id] = session;
                SaveCount++;
            }
        }
    }
}
