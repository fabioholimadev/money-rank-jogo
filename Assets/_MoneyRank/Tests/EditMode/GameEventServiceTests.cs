using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameEventServiceTests
    {
        [Test]
        public void PreviewIsReadOnlyAndDoesNotPersistSession()
        {
            var fixture = CreateFixture();
            var savesBeforePreview = fixture.Repository.SaveCount;

            var result = fixture.EventService.PreviewOption(
                fixture.SessionId,
                CreateEvent(),
                "accept");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.CanConfirm, Is.True);
            Assert.That(fixture.Session.Phase, Is.EqualTo(GamePhase.ResolveSpace));
            Assert.That(fixture.Session.EventHistory.Records, Is.Empty);
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBeforePreview));
        }

        [Test]
        public void BeginChoiceAndConfirmationPersistAndAdvanceTheSession()
        {
            var fixture = CreateFixture();
            var definition = CreateEvent();
            var beginChoice = fixture.SessionService.BeginEventChoice(
                fixture.SessionId,
                new EventSelectionResult(definition, "events-test", "test-policy", 0),
                "event-space");
            var preview = fixture.EventService.PreviewOption(
                fixture.SessionId,
                definition,
                "accept").Value;
            var savesBeforeConfirmation = fixture.Repository.SaveCount;

            var confirmation = fixture.EventService.ConfirmOption(
                fixture.SessionId,
                preview,
                CreateRequest("resolution-1"));

            Assert.That(beginChoice.Succeeded, Is.True);
            Assert.That(confirmation.Succeeded, Is.True);
            Assert.That(fixture.Session.Phase, Is.EqualTo(GamePhase.ApplyEffects));
            Assert.That(fixture.Session.EventHistory.Records, Has.Count.EqualTo(1));
            Assert.That(
                fixture.Session.TryGetEventState(new PlayerId("player"), out var eventState),
                Is.True);
            Assert.That(eventState.TryGetValue("test-target", out var value), Is.True);
            Assert.That(value, Is.EqualTo("after"));
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBeforeConfirmation + 1));
        }

        [Test]
        public void ConfirmationOutsideAwaitChoiceDoesNotMutateOrPersist()
        {
            var fixture = CreateFixture();
            var preview = fixture.EventService.PreviewOption(
                fixture.SessionId,
                CreateEvent(),
                "accept").Value;
            var savesBeforeConfirmation = fixture.Repository.SaveCount;

            var result = fixture.EventService.ConfirmOption(
                fixture.SessionId,
                preview,
                CreateRequest("resolution-1"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EVENT_CONFIRMATION_NOT_ALLOWED"));
            Assert.That(fixture.Session.Phase, Is.EqualTo(GamePhase.ResolveSpace));
            Assert.That(fixture.Session.EventHistory.Records, Is.Empty);
            Assert.That(fixture.Repository.SaveCount, Is.EqualTo(savesBeforeConfirmation));
        }

        private static Fixture CreateFixture()
        {
            var repository = new MemoryRepository();
            var sessionService = new GameSessionService(repository);
            var sessionId = new SessionId("event-session");
            var board = new BoardDefinition(
                "board",
                new[]
                {
                    new BoardSpaceDefinition("start", 0, "start"),
                    new BoardSpaceDefinition("event-space", 1, "event", "test-category", "events-test")
                },
                0);
            var session = sessionService.Create(sessionId, new SessionRules(1, 1), board);
            sessionService.AddPlayer(sessionId, new Player(new PlayerId("player"), "Player", 0));
            sessionService.Start(sessionId);
            sessionService.BeginTurn(sessionId);
            sessionService.RegisterMove(sessionId, 1);

            var effectExecutor = new EffectExecutor(new IEffectHandler[] { new SetValueEffectHandler() });
            var previewService = new EventPreviewService(
                new RequirementEvaluator(Array.Empty<IRequirementHandler>()),
                effectExecutor);
            var confirmationService = new EventConfirmationService(effectExecutor);
            var eventService = new GameEventService(repository, previewService, confirmationService);
            return new Fixture(
                repository,
                sessionService,
                eventService,
                sessionId,
                session);
        }

        private static EventDefinition CreateEvent()
        {
            var option = new EventOptionDefinition(
                "accept",
                "Accept",
                "Test option",
                Array.Empty<RequirementDefinition>(),
                new[]
                {
                    new GameEffectDefinition(
                        "test-target",
                        "set-value",
                        "after",
                        null,
                        "event:test@1")
                },
                "always");
            return new EventDefinition(
                "test",
                1,
                "test-category",
                "Test event",
                "Educational text",
                Array.Empty<string>(),
                Array.Empty<RequirementDefinition>(),
                new[] { option });
        }

        private static EventConfirmationRequest CreateRequest(string id) =>
            new EventConfirmationRequest(
                id,
                "event-space",
                "local-master",
                new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero),
                "facilitator");

        private sealed class SetValueEffectHandler : IEffectHandler
        {
            public string OperationId => "set-value";

            public EffectApplicationResult Apply(GameEffectDefinition effect, EventState state)
            {
                state.SetValue(effect.TargetId, effect.Value);
                return EffectApplicationResult.Success();
            }
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

        private sealed class Fixture
        {
            public Fixture(
                MemoryRepository repository,
                GameSessionService sessionService,
                GameEventService eventService,
                SessionId sessionId,
                GameSession session)
            {
                Repository = repository;
                SessionService = sessionService;
                EventService = eventService;
                SessionId = sessionId;
                Session = session;
            }

            public MemoryRepository Repository { get; }
            public GameSessionService SessionService { get; }
            public GameEventService EventService { get; }
            public SessionId SessionId { get; }
            public GameSession Session { get; }
        }
    }
}
