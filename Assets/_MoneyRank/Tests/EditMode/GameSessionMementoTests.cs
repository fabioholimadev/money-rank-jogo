using System;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using MoneyRank.Domain.Hospital;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameSessionMementoTests
    {
        [Test]
        public void MementoRestoresSessionStateAndHistoryRoundTrip()
        {
            var source = CreateConfiguredSession();
            source.Start();
            source.BeginTurn();
            source.RegisterMove(3);
            source.TryGetEventState(new PlayerId("p1"), out var sourceEventState);
            sourceEventState.SetValue("test-value", "saved");
            source.TryGetAttributeState(new PlayerId("p1"), "test-attribute", out var sourceAttribute);
            sourceAttribute.Restore(2, 4);
            source.TryGetInnerBoardState(new PlayerId("p1"), out var sourceInnerBoard);
            sourceInnerBoard.Restore(1, 0);
            source.TryGetBoardState(new PlayerId("p1"), out var sourceBoardState);
            sourceBoardState.Restore(source.Board, sourceBoardState.Position, 4);
            source.TryGetAnnualCycleState(new PlayerId("p1"), out var sourceAnnualCycle);
            sourceAnnualCycle.Restore(1);
            source.TryGetHospitalState(new PlayerId("p1"), out var sourceHospital);
            new HospitalService(Array.Empty<IHospitalResolutionPolicy>())
                .Admit(sourceHospital, "saved-health-crisis", 2);
            source.Economy.CreateTransactionService().Apply(
                source.EconomyState,
                source.EconomyHistory,
                new EconomyTransactionRequest(
                    "saved-transaction",
                    new[]
                    {
                        new EconomyTransactionEntry("bank", -6m),
                        new EconomyTransactionEntry("player-cash", 6m)
                    },
                    "test-source",
                    new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero),
                    "facilitator",
                    "Saved transaction"));
            source.EventHistory.Append(CreateHistoryRecord());
            var memento = source.CreateMemento();

            var restored = CreateConfiguredSession();
            var result = restored.Restore(memento);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(restored.Phase, Is.EqualTo(GamePhase.ResolveSpace));
            Assert.That(restored.CurrentPlayer.Id, Is.EqualTo(new PlayerId("p1")));
            restored.TryGetBoardState(new PlayerId("p1"), out var boardState);
            Assert.That(boardState.Position, Is.EqualTo(3));
            restored.TryGetEventState(new PlayerId("p1"), out var eventState);
            Assert.That(eventState.TryGetValue("test-value", out var eventValue), Is.True);
            Assert.That(eventValue, Is.EqualTo("saved"));
            restored.TryGetAttributeState(new PlayerId("p1"), "test-attribute", out var attribute);
            Assert.That(attribute.CurrentProgress, Is.EqualTo(2));
            Assert.That(attribute.CompletedCycles, Is.EqualTo(4));
            restored.TryGetInnerBoardState(new PlayerId("p1"), out var innerBoard);
            Assert.That(innerBoard.Position, Is.EqualTo(1));
            restored.TryGetAnnualCycleState(new PlayerId("p1"), out var annualCycle);
            Assert.That(annualCycle.ProcessedCycles, Is.EqualTo(1));
            restored.TryGetHospitalState(new PlayerId("p1"), out var hospital);
            Assert.That(hospital.IsHospitalized, Is.True);
            Assert.That(hospital.PendingMovementSkips, Is.EqualTo(2));
            Assert.That(hospital.ReasonId, Is.EqualTo("saved-health-crisis"));
            Assert.That(restored.EconomyState.TryGetBalance("player-cash", out var cash), Is.True);
            Assert.That(cash, Is.EqualTo(6m));
            Assert.That(restored.EconomyHistory.Records, Has.Count.EqualTo(1));
            Assert.That(restored.EconomyHistory.Records[0].Id, Is.EqualTo("saved-transaction"));
            Assert.That(restored.EventHistory.Records, Has.Count.EqualTo(1));
            Assert.That(restored.EventHistory.Records[0].Id, Is.EqualTo("resolution-1"));
        }

        [Test]
        public void InvalidMementoIsRejectedBeforeAnyStateChanges()
        {
            var session = CreateConfiguredSession();
            var baseline = session.CreateMemento();
            var malformedPlayers = baseline.Players
                .Select(player => player.PlayerId == new PlayerId("p1")
                    ? new PlayerStateMemento(
                        player.PlayerId,
                        new ExternalBoardStateMemento(99, 0),
                        player.EventState,
                        player.Attributes,
                        player.InnerBoard,
                        player.AnnualCycle,
                        player.Hospital)
                    : player);
            var malformed = new GameSessionMemento(
                GameSession.CurrentSaveSchemaVersion,
                baseline.SessionId,
                baseline.Phase,
                baseline.CurrentPlayerId,
                malformedPlayers,
                baseline.EventHistory);

            var result = session.Restore(malformed);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SAVE_EXTERNAL_BOARD_INVALID"));
            session.TryGetBoardState(new PlayerId("p1"), out var boardState);
            Assert.That(boardState.Position, Is.Zero);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Setup));
        }

        [Test]
        public void UnsupportedSchemaIsRejected()
        {
            var session = CreateConfiguredSession();
            var current = session.CreateMemento();
            var future = new GameSessionMemento(
                GameSession.CurrentSaveSchemaVersion + 1,
                current.SessionId,
                current.Phase,
                current.CurrentPlayerId,
                current.Players,
                current.EventHistory);

            var result = session.Restore(future);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SAVE_SCHEMA_UNSUPPORTED"));
        }

        private static GameSession CreateConfiguredSession()
        {
            var board = new BoardDefinition(
                "board",
                Enumerable.Range(0, 4)
                    .Select(index => new BoardSpaceDefinition($"space-{index}", index, "event")),
                0);
            var innerBoard = new InnerBoardDefinition(
                "inner-board",
                new[]
                {
                    new InnerBoardSpaceDefinition("inner-start", 0, "start"),
                    new InnerBoardSpaceDefinition("inner-progress", 1, "progress"),
                    new InnerBoardSpaceDefinition("inner-goal", 2, "goal", isGoal: true)
                },
                0,
                "movement",
                "capture",
                "victory");
            var attribute = new AttributeDefinition(
                "test-attribute",
                "Test attribute",
                "test-type",
                3,
                "overflow",
                Array.Empty<GameEffectDefinition>());
            var session = new GameSession(
                new SessionId("session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                board,
                innerBoard,
                new[] { attribute },
                new AnnualCycleDefinition("fiscal-year", 3, "configured-annual-policy"),
                new HospitalDefinition("hospital", "configured-hospital-policy"),
                new EconomyDefinition(
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

        private static EventResolutionRecord CreateHistoryRecord()
        {
            var before = new EventStateSnapshot(Array.Empty<DefinitionParameter>());
            var after = new EventStateSnapshot(new[] { new DefinitionParameter("test-value", "saved") });
            return new EventResolutionRecord(
                "resolution-1",
                "test-event",
                1,
                new PlayerId("p1"),
                "space-3",
                "accept",
                before,
                after,
                "local-master",
                new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero),
                "facilitator");
        }
    }
}
