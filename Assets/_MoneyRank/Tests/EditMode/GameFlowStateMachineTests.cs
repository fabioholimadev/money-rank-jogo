using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class GameFlowStateMachineTests
    {
        [TestCase(GamePhase.Setup, GamePhase.StartTurn)]
        [TestCase(GamePhase.StartTurn, GamePhase.AwaitMove)]
        [TestCase(GamePhase.AwaitMove, GamePhase.ResolveSpace)]
        [TestCase(GamePhase.ResolveSpace, GamePhase.AwaitChoice)]
        [TestCase(GamePhase.ResolveSpace, GamePhase.ApplyEffects)]
        [TestCase(GamePhase.AwaitChoice, GamePhase.ApplyEffects)]
        [TestCase(GamePhase.ApplyEffects, GamePhase.CheckProgress)]
        [TestCase(GamePhase.CheckProgress, GamePhase.EndTurn)]
        [TestCase(GamePhase.EndTurn, GamePhase.StartTurn)]
        [TestCase(GamePhase.CheckProgress, GamePhase.GameEnd)]
        [TestCase(GamePhase.GameEnd, GamePhase.Results)]
        public void AllowsBaselineTransitions(GamePhase current, GamePhase next)
        {
            var stateMachine = new GameFlowStateMachine();

            Assert.That(stateMachine.CanTransition(current, next), Is.True);
        }

        [TestCase(GamePhase.Setup, GamePhase.AwaitMove)]
        [TestCase(GamePhase.AwaitMove, GamePhase.ApplyEffects)]
        [TestCase(GamePhase.Results, GamePhase.StartTurn)]
        public void RejectsInvalidTransitions(GamePhase current, GamePhase next)
        {
            var stateMachine = new GameFlowStateMachine();

            Assert.That(stateMachine.CanTransition(current, next), Is.False);
        }
    }
}

