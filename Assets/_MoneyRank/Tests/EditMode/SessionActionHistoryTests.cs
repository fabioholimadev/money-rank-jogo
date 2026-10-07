using System;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using MoneyRank.Infrastructure.Persistence;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class SessionActionHistoryTests
    {
        [Test]
        public void RecorderMetadataAndParametersSurviveJsonRoundTrip()
        {
            var session = CreateSession();
            var recorder = new SessionActionRecorder("test-ui", "facilitator");
            recorder.Record(
                session,
                "test.action",
                new PlayerId("p1"),
                GamePhase.Setup,
                new[] { new DefinitionParameter("value", "configured") });
            var serializer = new JsonGameSessionMementoSerializer();

            var restored = serializer.Deserialize(serializer.Serialize(session.CreateMemento()));

            Assert.That(restored.ActionHistory, Has.Count.EqualTo(1));
            Assert.That(restored.ActionHistory[0].TypeId, Is.EqualTo("test.action"));
            Assert.That(restored.ActionHistory[0].PlayerId, Is.EqualTo(new PlayerId("p1")));
            Assert.That(restored.ActionHistory[0].OperatorId, Is.EqualTo("facilitator"));
            Assert.That(restored.ActionHistory[0].SourceId, Is.EqualTo("test-ui"));
            Assert.That(restored.ActionHistory[0].Parameters[0].Value, Is.EqualTo("configured"));
        }

        [Test]
        public void HistoryRejectsDuplicateActionIds()
        {
            var history = new SessionActionHistory();
            var record = new SessionActionRecord(
                "same-id",
                "test.action",
                null,
                GamePhase.Setup,
                GamePhase.Setup,
                new DateTimeOffset(2026, 10, 4, 18, 0, 0, TimeSpan.Zero),
                "facilitator",
                "test",
                Array.Empty<DefinitionParameter>());

            var first = history.Append(record);
            var duplicate = history.Append(record);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.ErrorCode, Is.EqualTo("DUPLICATE_ACTION_ID"));
        }

        private static GameSession CreateSession()
        {
            var session = new GameSession(
                new SessionId("action-session"),
                new SessionRules(1, 1),
                new GameFlowStateMachine());
            session.AddPlayer(new Player(new PlayerId("p1"), "Player", 0));
            return session;
        }
    }
}
