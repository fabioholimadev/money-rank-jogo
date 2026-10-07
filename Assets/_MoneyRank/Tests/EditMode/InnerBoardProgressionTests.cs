using System;
using MoneyRank.Content;
using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class InnerBoardProgressionTests
    {
        [Test]
        public void DefinitionOrdersSpacesAndKeepsSpecialPositionsDataDriven()
        {
            var definition = CreateDefinition();

            Assert.That(definition.SpaceCount, Is.EqualTo(5));
            Assert.That(definition.GetSpace(2).IsCheckpoint, Is.True);
            Assert.That(definition.GetSpace(3).IsProtected, Is.True);
            Assert.That(definition.GetSpace(4).IsGoal, Is.True);
            Assert.That(definition.MovementPolicyId, Is.EqualTo("test-policy"));
        }

        [Test]
        public void RegisteredPolicyControlsAdvanceCheckpointRegressionAndGoal()
        {
            var definition = CreateDefinition();
            var state = new InnerBoardState(new PlayerId("player"), 0, 0);
            var service = CreateService();

            var checkpointMove = service.Move(definition, state, 2);
            var goalMove = service.Move(definition, state, 2);
            var regression = service.Move(definition, state, -4);

            Assert.That(checkpointMove.Succeeded, Is.True);
            Assert.That(checkpointMove.Value.CheckpointIndex, Is.EqualTo(2));
            Assert.That(goalMove.Succeeded, Is.True);
            Assert.That(goalMove.Value.ReachedGoal, Is.True);
            Assert.That(regression.Succeeded, Is.True);
            Assert.That(regression.Value.Destination, Is.EqualTo(2));
            Assert.That(state.CheckpointIndex, Is.EqualTo(2));
        }

        [Test]
        public void InvalidPolicyOutputDoesNotMutateState()
        {
            var definition = new InnerBoardDefinition(
                "inner-board",
                new[] { new InnerBoardSpaceDefinition("start", 0, "start") },
                0,
                "invalid",
                "test-capture",
                "test-victory");
            var state = new InnerBoardState(new PlayerId("player"), 0, 0);
            var service = new InnerBoardProgressionService(
                new IInnerBoardMovementPolicy[] { new InvalidPolicy() });

            var result = service.Move(definition, state, 1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INNER_BOARD_DESTINATION_OUT_OF_RANGE"));
            Assert.That(state.Position, Is.Zero);
        }

        [Test]
        public void SerializableSpaceDataKeepsFlagsConfigurable()
        {
            var data = new InnerBoardSpaceData("space", 3, "custom", true, true, true);

            var definition = data.ToDomain();

            Assert.That(definition.Id, Is.EqualTo("space"));
            Assert.That(definition.Index, Is.EqualTo(3));
            Assert.That(definition.TypeId, Is.EqualTo("custom"));
            Assert.That(definition.IsCheckpoint, Is.True);
            Assert.That(definition.IsProtected, Is.True);
            Assert.That(definition.IsGoal, Is.True);
        }

        private static InnerBoardProgressionService CreateService() =>
            new InnerBoardProgressionService(
                new IInnerBoardMovementPolicy[] { new TestMovementPolicy() });

        private static InnerBoardDefinition CreateDefinition() =>
            new InnerBoardDefinition(
                "inner-board",
                new[]
                {
                    new InnerBoardSpaceDefinition("start", 0, "start"),
                    new InnerBoardSpaceDefinition("step-1", 1, "progress"),
                    new InnerBoardSpaceDefinition("checkpoint", 2, "progress", isCheckpoint: true),
                    new InnerBoardSpaceDefinition("safe", 3, "progress", isProtected: true),
                    new InnerBoardSpaceDefinition("goal", 4, "goal", isGoal: true)
                },
                0,
                "test-policy",
                "test-capture",
                "test-victory");

        private sealed class TestMovementPolicy : IInnerBoardMovementPolicy
        {
            public string PolicyId => "test-policy";

            public OperationResult<InnerBoardMovementCalculation> Calculate(
                InnerBoardDefinition definition,
                InnerBoardState state,
                int steps)
            {
                var destination = Math.Max(
                    state.CheckpointIndex,
                    Math.Min(definition.SpaceCount - 1, state.Position + steps));
                var checkpoint = definition.GetSpace(destination).IsCheckpoint
                    ? destination
                    : state.CheckpointIndex;
                return OperationResult<InnerBoardMovementCalculation>.Success(
                    new InnerBoardMovementCalculation(destination, checkpoint));
            }
        }

        private sealed class InvalidPolicy : IInnerBoardMovementPolicy
        {
            public string PolicyId => "invalid";

            public OperationResult<InnerBoardMovementCalculation> Calculate(
                InnerBoardDefinition definition,
                InnerBoardState state,
                int steps) =>
                OperationResult<InnerBoardMovementCalculation>.Success(
                    new InnerBoardMovementCalculation(99, 0));
        }
    }
}
