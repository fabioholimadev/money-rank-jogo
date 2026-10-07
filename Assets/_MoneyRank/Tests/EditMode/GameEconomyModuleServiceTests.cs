using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using MoneyRank.Domain.Hospital;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameEconomyModuleServiceTests
    {
        [Test]
        public void AnnualCycleResolvesOnlyInCheckProgressAndPersistsSuccess()
        {
            var repository = new MemoryRepository();
            var session = CreateConfiguredSession();
            repository.Save(session);
            var service = new GameAnnualCycleService(
                repository,
                new AnnualCycleService(
                    new IAnnualCyclePolicy[] { new TestAnnualPolicy() },
                    session.Economy.CreateTransactionService()));
            var savesBefore = repository.SaveCount;

            var wrongPhase = service.ResolveCurrentPlayer(
                session.Id,
                "annual-1",
                DateTimeOffset.UtcNow,
                "facilitator");
            MoveToCheckProgress(session);
            var success = service.ResolveCurrentPlayer(
                session.Id,
                "annual-1",
                DateTimeOffset.UtcNow,
                "facilitator");

            Assert.That(wrongPhase.Succeeded, Is.False);
            Assert.That(wrongPhase.ErrorCode, Is.EqualTo("ANNUAL_CYCLE_PHASE_INVALID"));
            Assert.That(success.Succeeded, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore + 1));
            Assert.That(session.EconomyHistory.Records, Has.Count.EqualTo(1));
            session.TryGetAnnualCycleState(new PlayerId("p1"), out var annualState);
            Assert.That(annualState.ProcessedCycles, Is.EqualTo(1));
        }

        [Test]
        public void HospitalCommandsPersistOnlyWhenTheyChangeState()
        {
            var repository = new MemoryRepository();
            var session = CreateConfiguredSession();
            repository.Save(session);
            var service = new GameHospitalService(
                repository,
                new HospitalService(new IHospitalResolutionPolicy[] { new TestHospitalPolicy() }));
            var savesBefore = repository.SaveCount;

            var admission = service.Admit(session.Id, new PlayerId("p1"), "test-crisis", 1);
            var skip = service.TryConsumeMovementSkip(session.Id, new PlayerId("p1"));
            var noSkip = service.TryConsumeMovementSkip(session.Id, new PlayerId("p1"));
            var recovery = service.Resolve(
                session.Id,
                new PlayerId("p1"),
                new HospitalResolutionCommand("recover", Array.Empty<DefinitionParameter>()));

            Assert.That(admission.Succeeded, Is.True);
            Assert.That(skip.Value, Is.True);
            Assert.That(noSkip.Value, Is.False);
            Assert.That(recovery.Succeeded, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore + 3));
            session.TryGetHospitalState(new PlayerId("p1"), out var state);
            Assert.That(state.IsHospitalized, Is.False);
        }

        [Test]
        public void MissingConfiguredModulesReturnStableFailuresWithoutSaving()
        {
            var repository = new MemoryRepository();
            var board = CreateBoard();
            var session = new GameSession(
                new SessionId("plain-session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                board);
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            repository.Save(session);
            var savesBefore = repository.SaveCount;
            var hospital = new GameHospitalService(
                repository,
                new HospitalService(Array.Empty<IHospitalResolutionPolicy>()));

            var result = hospital.Admit(session.Id, new PlayerId("p1"), "crisis", 1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("HOSPITAL_NOT_CONFIGURED"));
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore));
        }

        private static GameSession CreateConfiguredSession()
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
            var session = new GameSession(
                new SessionId("configured-session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                CreateBoard(),
                annualCycle: new AnnualCycleDefinition("year", 1, "test-annual"),
                hospital: new HospitalDefinition("hospital", "test-hospital"),
                economy: economy);
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            return session;
        }

        private static BoardDefinition CreateBoard() =>
            new BoardDefinition(
                "board",
                Enumerable.Range(0, 2)
                    .Select(index => new BoardSpaceDefinition($"space-{index}", index, "event")),
                0);

        private static void MoveToCheckProgress(GameSession session)
        {
            session.Start();
            session.BeginTurn();
            session.RegisterMove(2);
            session.TryTransition(GamePhase.ApplyEffects);
            session.TryTransition(GamePhase.CheckProgress);
        }

        private sealed class TestAnnualPolicy : IAnnualCyclePolicy
        {
            public string PolicyId => "test-annual";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                AnnualCycleDefinition definition,
                AnnualCycleContext context) =>
                OperationResult<EconomyTransactionRequest>.Success(
                    new EconomyTransactionRequest(
                        context.TransactionId,
                        new[]
                        {
                            new EconomyTransactionEntry("bank", -4m),
                            new EconomyTransactionEntry("player-cash", 4m)
                        },
                        definition.Id,
                        context.CreatedAt,
                        context.OperatorId,
                        "Test annual cycle"));
        }

        private sealed class TestHospitalPolicy : IHospitalResolutionPolicy
        {
            public string PolicyId => "test-hospital";

            public OperationResult<HospitalResolutionCalculation> Calculate(
                HospitalDefinition definition,
                HospitalState state,
                HospitalResolutionCommand command) =>
                OperationResult<HospitalResolutionCalculation>.Success(
                    new HospitalResolutionCalculation(false, 0, Array.Empty<GameEffectDefinition>()));
        }

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions =
                new Dictionary<SessionId, GameSession>();

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
