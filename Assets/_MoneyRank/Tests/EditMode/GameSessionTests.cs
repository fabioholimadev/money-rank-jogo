using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameSessionTests
    {
        [Test]
        public void CannotStartBeforeConfiguredMinimumPlayers()
        {
            var session = CreateSession(minimumPlayers: 2, maximumPlayers: 4);
            session.AddPlayer(CreatePlayer("p1", 0));

            var result = session.Start();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("NOT_ENOUGH_PLAYERS"));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Setup));
        }

        [Test]
        public void StartSelectsPlayerWithLowestTurnOrder()
        {
            var session = CreateSession();
            session.AddPlayer(CreatePlayer("p2", 1));
            session.AddPlayer(CreatePlayer("p1", 0));

            var result = session.Start();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.StartTurn));
            Assert.That(session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("p1")));
        }

        [Test]
        public void CompleteTurnRotatesToNextPlayer()
        {
            var session = CreateStartedSession();
            AdvanceToEndTurn(session);

            var result = session.CompleteTurn();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.StartTurn));
            Assert.That(session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("p2")));
        }

        [Test]
        public void CompleteTurnWrapsToFirstPlayer()
        {
            var session = CreateStartedSession();
            AdvanceToEndTurn(session);
            session.CompleteTurn();
            AdvanceToEndTurn(session);

            session.CompleteTurn();

            Assert.That(session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("p1")));
        }

        [Test]
        public void DuplicatePlayerIdIsRejected()
        {
            var session = CreateSession();
            session.AddPlayer(CreatePlayer("same", 0));

            var result = session.AddPlayer(CreatePlayer("same", 1));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("DUPLICATE_PLAYER_ID"));
        }

        [Test]
        public void ConfiguredPlayerLimitIsEnforced()
        {
            var session = CreateSession(minimumPlayers: 1, maximumPlayers: 1);
            session.AddPlayer(CreatePlayer("p1", 0));

            var result = session.AddPlayer(CreatePlayer("p2", 1));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("PLAYER_LIMIT_REACHED"));
        }

        [Test]
        public void PlayerCannotBeAddedAfterStart()
        {
            var session = CreateStartedSession();

            var result = session.AddPlayer(CreatePlayer("p3", 2));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SESSION_ALREADY_STARTED"));
        }

        private static GameSession CreateSession(int minimumPlayers = 2, int maximumPlayers = 4) =>
            new GameSession(
                new SessionId("session-test"),
                new SessionRules(minimumPlayers, maximumPlayers),
                new GameFlowStateMachine());

        private static GameSession CreateStartedSession()
        {
            var session = CreateSession();
            session.AddPlayer(CreatePlayer("p1", 0));
            session.AddPlayer(CreatePlayer("p2", 1));
            session.Start();
            return session;
        }

        private static Player CreatePlayer(string id, int turnOrder) =>
            new Player(new PlayerId(id), id, turnOrder);

        private static void AdvanceToEndTurn(GameSession session)
        {
            session.TryTransition(GamePhase.AwaitMove);
            session.TryTransition(GamePhase.ResolveSpace);
            session.TryTransition(GamePhase.ApplyEffects);
            session.TryTransition(GamePhase.CheckProgress);
            session.TryTransition(GamePhase.EndTurn);
        }
    }
}

