using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using MoneyRank.Infrastructure.Persistence;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameEventSelectionServiceTests
    {
        [Test]
        public void SelectionOpensAndPersistsExactActiveEvent()
        {
            var fixture = CreateFixture();
            var savesBefore = fixture.Repository.SaveCount;

            var result = fixture.Service.SelectCurrentSpace(
                fixture.Session.Id,
                "table-index",
                1,
                Array.Empty<DefinitionParameter>());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Definition.Id, Is.EqualTo("event-b"));
            Assert.That(fixture.Session.Phase, Is.EqualTo(GamePhase.AwaitChoice));
            Assert.That(fixture.Session.ActiveEvent.EventId, Is.EqualTo("event-b"));
            Assert.That(fixture.Session.ActiveEvent.EventVersion, Is.EqualTo(1));
            Assert.That(fixture.Session.ActiveEvent.SpaceId, Is.EqualTo("event-space"));
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBefore + 1));
        }

        [Test]
        public void ActiveSelectionSurvivesJsonAndSessionRestore()
        {
            var fixture = CreateFixture();
            fixture.Service.SelectCurrentSpace(
                fixture.Session.Id,
                "table-index",
                3,
                Array.Empty<DefinitionParameter>());
            var serializer = new JsonGameSessionMementoSerializer();
            var restoredMemento = serializer.Deserialize(serializer.Serialize(fixture.Session.CreateMemento()));
            var target = CreateStartedSession(fixture.Session.Id.Value);

            var restore = target.Restore(restoredMemento);

            Assert.That(restore.Succeeded, Is.True);
            Assert.That(target.Phase, Is.EqualTo(GamePhase.AwaitChoice));
            Assert.That(target.ActiveEvent.EventId, Is.EqualTo("event-b"));
            Assert.That(target.ActiveEvent.DrawIndex, Is.EqualTo(3));
        }

        [Test]
        public void SelectionFailureDoesNotAdvanceOrPersist()
        {
            var fixture = CreateFixture();
            var savesBefore = fixture.Repository.SaveCount;

            var result = fixture.Service.SelectCurrentSpace(
                fixture.Session.Id,
                "missing-policy",
                0,
                Array.Empty<DefinitionParameter>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EVENT_SELECTION_POLICY_NOT_FOUND"));
            Assert.That(fixture.Session.Phase, Is.EqualTo(GamePhase.ResolveSpace));
            Assert.That(fixture.Session.ActiveEvent, Is.Null);
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBefore));
        }

        [Test]
        public void ActiveDefinitionIsResolvedFromCatalogAfterSelection()
        {
            var fixture = CreateFixture();
            fixture.Service.SelectCurrentSpace(
                fixture.Session.Id,
                "table-index",
                0,
                Array.Empty<DefinitionParameter>());

            var active = fixture.Service.GetActiveDefinition(fixture.Session.Id);

            Assert.That(active.Succeeded, Is.True);
            Assert.That(active.Value.Id, Is.EqualTo("event-a"));
            Assert.That(active.Value.Version, Is.EqualTo(1));
        }

        private static Fixture CreateFixture()
        {
            var repository = new MemoryRepository();
            var session = CreateStartedSession("selection-session");
            repository.Save(session);
            var first = CreateEvent("event-a");
            var second = CreateEvent("event-b");
            var catalog = new EventContentCatalog(
                new[] { first, second },
                new[]
                {
                    new EventTableDefinition(
                        "events-test",
                        new[]
                        {
                            new EventDefinitionKey(first.Id, first.Version),
                            new EventDefinitionKey(second.Id, second.Version)
                        })
                });
            var service = new GameEventSelectionService(
                repository,
                catalog,
                new IEventSelectionPolicy[] { new TableIndexEventSelectionPolicy("table-index") });
            return new Fixture(repository, session, service);
        }

        private static GameSession CreateStartedSession(string id)
        {
            var board = new BoardDefinition(
                "board",
                new[]
                {
                    new BoardSpaceDefinition("start", 0, "start"),
                    new BoardSpaceDefinition("event-space", 1, "event", "test", "events-test")
                },
                0);
            var session = new GameSession(
                new SessionId(id),
                new SessionRules(1, 1),
                new GameFlowStateMachine(),
                board);
            session.AddPlayer(new Player(new PlayerId("player"), "Player", 0));
            session.Start();
            session.BeginTurn();
            session.RegisterMove(1);
            return session;
        }

        private static EventDefinition CreateEvent(string id) =>
            new EventDefinition(
                id,
                1,
                "test",
                id,
                "Temporary educational text.",
                Array.Empty<string>(),
                Array.Empty<RequirementDefinition>(),
                new[]
                {
                    new EventOptionDefinition(
                        "continue",
                        "Continue",
                        "Temporary option.",
                        Array.Empty<RequirementDefinition>(),
                        Array.Empty<GameEffectDefinition>(),
                        "always")
                });

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

        private sealed class Fixture
        {
            public Fixture(
                MemoryRepository repository,
                GameSession session,
                GameEventSelectionService service)
            {
                Repository = repository;
                Session = session;
                Service = service;
            }

            public MemoryRepository Repository { get; }
            public GameSession Session { get; }
            public GameEventSelectionService Service { get; }
        }
    }
}
