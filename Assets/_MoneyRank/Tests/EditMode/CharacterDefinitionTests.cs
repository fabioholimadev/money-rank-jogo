using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Content;
using MoneyRank.Domain;
using MoneyRank.Domain.Characters;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class CharacterDefinitionTests
    {
        [Test]
        public void DefinitionKeepsAdvantagesDisadvantagesAndPresentationAsData()
        {
            var character = CreateCharacter("character-a");

            Assert.That(character.SalaryRules, Has.Count.EqualTo(1));
            Assert.That(character.PassiveAbilities, Has.Count.EqualTo(1));
            Assert.That(character.Disadvantages, Has.Count.EqualTo(1));
            Assert.That(character.StartingResources[0].TargetId, Is.EqualTo("configured-resource"));
            Assert.That(character.ProgressionTrackId, Is.EqualTo("configured-track"));
            Assert.That(character.VisualPrefabId, Is.EqualTo("configured-prefab"));
            Assert.That(character.PortraitId, Is.EqualTo("configured-portrait"));
        }

        [Test]
        public void SerializableCharacterDoesNotRequireFixedProfessionOrAbilityEnums()
        {
            var rule = new CharacterRuleDefinitionData(
                "custom-rule",
                "Custom rule",
                "Authored rule",
                "custom-type",
                new[] { new DefinitionParameterData("custom-key", "custom-value") },
                Array.Empty<GameEffectDefinitionData>());
            var data = new CharacterDefinitionData(
                "custom-profession",
                "Custom profession",
                "Authored profile",
                new[] { rule },
                Array.Empty<GameEffectDefinitionData>(),
                Array.Empty<GameEffectDefinitionData>(),
                Array.Empty<CharacterRuleDefinitionData>(),
                new[] { rule },
                "track-x",
                "prefab-x",
                "portrait-x");

            var character = data.ToDomain();

            Assert.That(character.Id, Is.EqualTo("custom-profession"));
            Assert.That(character.SalaryRules[0].TypeId, Is.EqualTo("custom-type"));
            Assert.That(character.Disadvantages[0].Parameters[0].Value, Is.EqualTo("custom-value"));
        }

        [Test]
        public void CharacterAssignmentIsExplicitSetupStateAndRoundTrips()
        {
            var source = CreateSession();
            var assignment = source.AssignCharacter(new PlayerId("p1"), "character-a");
            var memento = source.CreateMemento();
            var restored = CreateSession();

            var restore = restored.Restore(memento);

            Assert.That(assignment.Succeeded, Is.True);
            Assert.That(restore.Succeeded, Is.True);
            Assert.That(restored.TryGetCharacter(new PlayerId("p1"), out var character), Is.True);
            Assert.That(character.Id, Is.EqualTo("character-a"));
            Assert.That(memento.Players[0].CharacterId, Is.EqualTo("character-a"));
        }

        [Test]
        public void AssignmentAfterSessionStartIsRejectedWithoutPersistence()
        {
            var repository = new MemoryRepository();
            var session = CreateSession();
            repository.Save(session);
            var service = new GameSessionService(repository);
            service.Start(session.Id);
            var savesBefore = repository.SaveCount;

            var result = service.AssignCharacter(session.Id, new PlayerId("p1"), "character-a");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("CHARACTER_ASSIGNMENT_CLOSED"));
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore));
        }

        private static GameSession CreateSession()
        {
            var session = new GameSession(
                new SessionId("character-session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                null,
                characterDefinitions: new[] { CreateCharacter("character-a") });
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            return session;
        }

        private static CharacterDefinition CreateCharacter(string id)
        {
            var salary = new CharacterRuleDefinition(
                "salary-rule",
                "Salary rule",
                "Configured salary behavior",
                "configured-salary",
                Array.Empty<DefinitionParameter>(),
                Array.Empty<GameEffectDefinition>());
            var passive = new CharacterRuleDefinition(
                "passive-rule",
                "Passive rule",
                "Configured advantage",
                "configured-passive",
                Array.Empty<DefinitionParameter>(),
                Array.Empty<GameEffectDefinition>());
            var disadvantage = new CharacterRuleDefinition(
                "disadvantage-rule",
                "Disadvantage rule",
                "Configured limitation",
                "configured-disadvantage",
                Array.Empty<DefinitionParameter>(),
                Array.Empty<GameEffectDefinition>());
            return new CharacterDefinition(
                id,
                "Configured character",
                "Configured description",
                new[] { salary },
                new[] { new GameEffectDefinition("configured-resource", "set", "5", null, id) },
                Array.Empty<GameEffectDefinition>(),
                new[] { passive },
                new[] { disadvantage },
                "configured-track",
                "configured-prefab",
                "configured-portrait");
        }

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions = new Dictionary<SessionId, GameSession>();
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
