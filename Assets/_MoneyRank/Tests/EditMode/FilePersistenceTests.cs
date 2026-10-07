using System;
using System.IO;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using MoneyRank.Infrastructure.Persistence;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class FilePersistenceTests
    {
        private string _temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            _temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "money-rank-tests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temporaryDirectory))
                Directory.Delete(_temporaryDirectory, true);
        }

        [Test]
        public void JsonSerializerRoundTripsVersionedSessionData()
        {
            var session = CreateMovedSession("serializer-session");
            session.TryGetEventState(new PlayerId("p1"), out var eventState);
            eventState.SetValue("configured-key", "configured-value");
            session.EventHistory.Append(CreateHistoryRecord());
            session.Economy.CreateTransactionService().Apply(
                session.EconomyState,
                session.EconomyHistory,
                CreateEconomyRequest());
            var serializer = new JsonGameSessionMementoSerializer();

            var json = serializer.Serialize(session.CreateMemento());
            var restored = serializer.Deserialize(json);

            Assert.That(restored.SchemaVersion, Is.EqualTo(GameSession.CurrentSaveSchemaVersion));
            Assert.That(restored.SessionId, Is.EqualTo(session.Id));
            Assert.That(restored.Phase, Is.EqualTo(GamePhase.ResolveSpace));
            Assert.That(restored.Players, Has.Count.EqualTo(2));
            Assert.That(restored.Players[0].EventState.Values["configured-key"], Is.EqualTo("configured-value"));
            Assert.That(restored.EventHistory, Has.Count.EqualTo(1));
            Assert.That(restored.EventHistory[0].CreatedAt, Is.EqualTo(CreateHistoryRecord().CreatedAt));
            Assert.That(restored.Economy.Balances.Single(balance => balance.AccountId == "player-cash").Balance, Is.EqualTo(5m));
            Assert.That(restored.EconomyHistory, Has.Count.EqualTo(1));
        }

        [Test]
        public void FileStoreOverwritesAtomicallyAndKeepsPreviousBackup()
        {
            var store = new FileGameSessionSaveStore(_temporaryDirectory);
            var session = CreateMovedSession("atomic-session");
            var first = store.Save(session.CreateMemento());
            session.TryGetBoardState(new PlayerId("p1"), out var boardState);
            boardState.Restore(session.Board, 1, 3);

            var second = store.Save(session.CreateMemento());
            var loaded = store.Load(session.Id);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(loaded.Succeeded, Is.True);
            Assert.That(loaded.Value.Players[0].ExternalBoard.CompletedLaps, Is.EqualTo(3));
            Assert.That(File.Exists(store.GetSavePath(session.Id) + ".bak"), Is.True);
            Assert.That(File.Exists(store.GetSavePath(session.Id) + ".tmp"), Is.False);
        }

        [Test]
        public void CorruptFileReturnsStableFailureInsteadOfPartialMemento()
        {
            var store = new FileGameSessionSaveStore(_temporaryDirectory);
            var sessionId = new SessionId("corrupt-session");
            Directory.CreateDirectory(_temporaryDirectory);
            File.WriteAllText(store.GetSavePath(sessionId), "{ definitely-not-a-save }");

            var result = store.Load(sessionId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SESSION_LOAD_FAILED"));
        }

        [Test]
        public void RepositoryCanAttachConfiguredSessionAndRestoreLocalSave()
        {
            var store = new FileGameSessionSaveStore(_temporaryDirectory);
            var repository = new FileBackedGameSessionRepository(store);
            var source = CreateMovedSession("repository-session");
            repository.Save(source);
            var target = CreateSession("repository-session");

            var attach = repository.Attach(target, true);

            Assert.That(attach.Succeeded, Is.True);
            Assert.That(repository.Get(target.Id), Is.SameAs(target));
            Assert.That(target.Phase, Is.EqualTo(GamePhase.ResolveSpace));
            target.TryGetBoardState(new PlayerId("p1"), out var boardState);
            Assert.That(boardState.Position, Is.EqualTo(3));
        }

        [Test]
        public void SaveFileNameDoesNotExposeOrInterpretSessionPathCharacters()
        {
            var store = new FileGameSessionSaveStore(_temporaryDirectory);
            var path = store.GetSavePath(new SessionId("../../outside/session"));

            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(Path.GetFullPath(_temporaryDirectory)));
            Assert.That(Path.GetFileName(path), Does.Match("^session-[a-f0-9]{64}\\.json$"));
        }

        private static GameSession CreateMovedSession(string id)
        {
            var session = CreateSession(id);
            session.Start();
            session.BeginTurn();
            session.RegisterMove(3);
            return session;
        }

        private static GameSession CreateSession(string id)
        {
            var board = new BoardDefinition(
                "board",
                Enumerable.Range(0, 4)
                    .Select(index => new BoardSpaceDefinition($"space-{index}", index, "event")),
                0);
            var session = new GameSession(
                new SessionId(id),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                board,
                economy: new EconomyDefinition(
                    new[]
                    {
                        new EconomyAccountDefinition("bank", "Bank", "bank", "cash", 0m),
                        new EconomyAccountDefinition("player-cash", "Player cash", "p1", "cash", 0m)
                    },
                    new[]
                    {
                        new EconomyBalanceDefinition("bank", 100m),
                        new EconomyBalanceDefinition("player-cash", 0m)
                    }));
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            return session;
        }

        private static EventResolutionRecord CreateHistoryRecord() =>
            new EventResolutionRecord(
                "resolution-1",
                "test-event",
                2,
                new PlayerId("p1"),
                "space-3",
                "accept",
                new EventStateSnapshot(Array.Empty<DefinitionParameter>()),
                new EventStateSnapshot(new[] { new DefinitionParameter("configured-key", "configured-value") }),
                "test-source",
                new DateTimeOffset(2026, 10, 4, 15, 30, 0, TimeSpan.FromHours(-3)),
                "facilitator");

        private static EconomyTransactionRequest CreateEconomyRequest() =>
            new EconomyTransactionRequest(
                "economy-1",
                new[]
                {
                    new EconomyTransactionEntry("bank", -5m),
                    new EconomyTransactionEntry("player-cash", 5m)
                },
                "test-source",
                new DateTimeOffset(2026, 10, 4, 15, 31, 0, TimeSpan.FromHours(-3)),
                "facilitator",
                "Test economy transaction");
    }
}
