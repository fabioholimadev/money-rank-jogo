using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Characters;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class LocalSessionSetupServiceTests
    {
        [Test]
        public void CreatesAndStartsTwoPlayerSessionWithAssignedCharacters()
        {
            var repository = new MemoryRepository();
            var service = new LocalSessionSetupService(repository);

            var result = service.CreateAndStart(CreateRequest(2));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Players.Count, Is.EqualTo(2));
            Assert.That(result.Value.Phase, Is.EqualTo(GamePhase.AwaitMove));
            Assert.That(result.Value.CurrentPlayer.Id, Is.EqualTo(new PlayerId("player-1")));
            Assert.That(result.Value.TryGetCharacter(new PlayerId("player-1"), out var firstCharacter), Is.True);
            Assert.That(firstCharacter.Id, Is.EqualTo("character-a"));
            Assert.That(result.Value.TryGetCharacter(new PlayerId("player-2"), out var secondCharacter), Is.True);
            Assert.That(secondCharacter.Id, Is.EqualTo("character-b"));
            Assert.That(repository.Get(result.Value.Id), Is.SameAs(result.Value));
        }

        [Test]
        public void CreatesAndStartsFourPlayerSessionWithReusableCharacterDefinitions()
        {
            var repository = new MemoryRepository();
            var service = new LocalSessionSetupService(repository);

            var result = service.CreateAndStart(CreateRequest(4));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Players.Count, Is.EqualTo(4));
            Assert.That(result.Value.Phase, Is.EqualTo(GamePhase.AwaitMove));
            foreach (var player in result.Value.Players)
                Assert.That(result.Value.TryGetCharacter(player.Id, out _), Is.True);
        }

        [TestCase(1, "SETUP_PLAYER_COUNT_INVALID")]
        [TestCase(5, "SETUP_PLAYER_COUNT_INVALID")]
        public void InvalidParticipantCountIsRejectedWithoutSaving(int participantCount, string expectedCode)
        {
            var repository = new MemoryRepository();
            var service = new LocalSessionSetupService(repository);

            var result = service.CreateAndStart(CreateRequest(participantCount));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(repository.SaveCount, Is.Zero);
        }

        [Test]
        public void DuplicateTurnOrderIsRejectedWithoutSaving()
        {
            var repository = new MemoryRepository();
            var service = new LocalSessionSetupService(repository);
            var request = CreateRequest(
                new SessionSetupParticipant(new PlayerId("player-1"), "Player 1", 0, "character-a"),
                new SessionSetupParticipant(new PlayerId("player-2"), "Player 2", 0, "character-b"));

            var result = service.CreateAndStart(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SETUP_TURN_ORDER_DUPLICATED"));
            Assert.That(repository.SaveCount, Is.Zero);
        }

        [Test]
        public void UnknownCharacterIsRejectedWithoutSaving()
        {
            var repository = new MemoryRepository();
            var service = new LocalSessionSetupService(repository);
            var request = CreateRequest(
                new SessionSetupParticipant(new PlayerId("player-1"), "Player 1", 0, "character-a"),
                new SessionSetupParticipant(new PlayerId("player-2"), "Player 2", 1, "missing"));

            var result = service.CreateAndStart(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SETUP_CHARACTER_NOT_FOUND"));
            Assert.That(repository.SaveCount, Is.Zero);
        }

        private static LocalSessionSetupRequest CreateRequest(int participantCount)
        {
            var participants = new List<SessionSetupParticipant>();
            for (var index = 0; index < participantCount; index++)
            {
                participants.Add(new SessionSetupParticipant(
                    new PlayerId($"player-{index + 1}"),
                    $"Player {index + 1}",
                    index,
                    index % 2 == 0 ? "character-a" : "character-b"));
            }

            return CreateRequest(participants.ToArray());
        }

        private static LocalSessionSetupRequest CreateRequest(params SessionSetupParticipant[] participants) =>
            new LocalSessionSetupRequest(
                new SessionId("setup-test"),
                new SessionRules(2, 4),
                new BoardDefinition(
                    "setup-board",
                    new[]
                    {
                        new BoardSpaceDefinition("start", 0, "start"),
                        new BoardSpaceDefinition("event", 1, "event")
                    },
                    0),
                participants,
                characters: new[] { CreateCharacter("character-a"), CreateCharacter("character-b") });

        private static CharacterDefinition CreateCharacter(string id) =>
            new CharacterDefinition(
                id,
                id,
                "Generic test character.",
                Array.Empty<CharacterRuleDefinition>(),
                Array.Empty<GameEffectDefinition>(),
                Array.Empty<GameEffectDefinition>(),
                Array.Empty<CharacterRuleDefinition>(),
                Array.Empty<CharacterRuleDefinition>());

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
