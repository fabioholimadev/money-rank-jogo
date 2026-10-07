using System.Collections.Generic;
using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class BuiltInProgressionPolicyTests
    {
        [Test]
        public void CarryOverflowPreservesRemainderAndCompletedCycles()
        {
            var handler = new CarryAttributeOverflowHandler("carry");

            var result = handler.Calculate(2, 5, 3);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.CurrentProgress, Is.EqualTo(1));
            Assert.That(result.Value.CompletedCycles, Is.EqualTo(2));
        }

        [Test]
        public void CarryOverflowRejectsCycleCountsOutsideSupportedRange()
        {
            var handler = new CarryAttributeOverflowHandler("carry");

            var result = handler.Calculate(int.MaxValue, int.MaxValue, 1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ATTRIBUTE_CYCLE_COUNT_OVERFLOW"));
        }

        [Test]
        public void LinearMovementClampsAtGoalAndUpdatesCheckpoint()
        {
            var definition = CreateBoard();
            var state = new InnerBoardState(new PlayerId("p1"), 0, 0);
            var policy = new LinearClampedInnerBoardMovementPolicy("linear");

            var checkpoint = policy.Calculate(definition, state, 2);
            state.Apply(checkpoint.Value);
            var goal = policy.Calculate(definition, state, 99);

            Assert.That(checkpoint.Value.CheckpointIndex, Is.EqualTo(2));
            Assert.That(goal.Value.Destination, Is.EqualTo(3));
            Assert.That(goal.Value.CheckpointIndex, Is.EqualTo(2));
        }

        [Test]
        public void SharedUnprotectedSpaceReturnsOpponentToOwnCheckpoint()
        {
            var definition = CreateBoard();
            var mover = new InnerBoardState(new PlayerId("p1"), 1, 0);
            var opponent = new InnerBoardState(new PlayerId("p2"), 1, 2);
            var policy = new SharedSpaceCheckpointCapturePolicy("capture");

            var result = policy.Calculate(
                definition,
                mover.PlayerId,
                new List<InnerBoardState> { mover, opponent });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Relocations, Has.Count.EqualTo(1));
            Assert.That(result.Value.Relocations[0].Destination, Is.EqualTo(2));
        }

        [Test]
        public void GoalPolicyEndsOnlyForMoverOnConfiguredGoalSpace()
        {
            var definition = CreateBoard();
            var mover = new InnerBoardState(new PlayerId("p1"), 3, 2);
            var policy = new GoalSpaceVictoryPolicy("goal", "GOAL_REACHED");

            var result = policy.Evaluate(
                definition,
                mover.PlayerId,
                new List<InnerBoardState> { mover });

            Assert.That(result.GameEnded, Is.True);
            Assert.That(result.WinnerId, Is.EqualTo(mover.PlayerId));
            Assert.That(result.ReasonCode, Is.EqualTo("GOAL_REACHED"));
        }

        private static InnerBoardDefinition CreateBoard() =>
            new InnerBoardDefinition(
                "inner",
                new[]
                {
                    new InnerBoardSpaceDefinition("start", 0, "start", true, true),
                    new InnerBoardSpaceDefinition("open", 1, "progress"),
                    new InnerBoardSpaceDefinition("checkpoint", 2, "checkpoint", true, true),
                    new InnerBoardSpaceDefinition("goal", 3, "goal", false, true, true)
                },
                0,
                "linear",
                "capture",
                "goal");
    }
}
