using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class InnerBoardResolutionTests
    {
        [Test]
        public void CapturePolicyCanRelocateAnOpponentWithoutChangingOtherState()
        {
            var definition = CreateDefinition();
            var mover = new InnerBoardState(new PlayerId("mover"), 2, 0);
            var opponent = new InnerBoardState(new PlayerId("opponent"), 2, 0);
            var service = new InnerBoardCaptureService(
                new IInnerBoardCapturePolicy[] { new TestCapturePolicy() });

            var result = service.Resolve(definition, mover.PlayerId, new[] { mover, opponent });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Relocations, Has.Count.EqualTo(1));
            Assert.That(mover.Position, Is.EqualTo(2));
            Assert.That(opponent.Position, Is.Zero);
        }

        [Test]
        public void ProtectedPositionBehaviorRemainsInsideTheConfiguredPolicy()
        {
            var definition = CreateDefinition();
            var mover = new InnerBoardState(new PlayerId("mover"), 1, 0);
            var opponent = new InnerBoardState(new PlayerId("opponent"), 1, 0);
            var service = new InnerBoardCaptureService(
                new IInnerBoardCapturePolicy[] { new TestCapturePolicy() });

            var result = service.Resolve(definition, mover.PlayerId, new[] { mover, opponent });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Relocations, Is.Empty);
            Assert.That(opponent.Position, Is.EqualTo(1));
        }

        [Test]
        public void InvalidCaptureCalculationIsTransactional()
        {
            var definition = CreateDefinition();
            var mover = new InnerBoardState(new PlayerId("mover"), 2, 0);
            var opponent = new InnerBoardState(new PlayerId("opponent"), 2, 0);
            var service = new InnerBoardCaptureService(
                new IInnerBoardCapturePolicy[] { new InvalidCapturePolicy() });

            var result = service.Resolve(definition, mover.PlayerId, new[] { mover, opponent });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INNER_BOARD_CAPTURE_DESTINATION_INVALID"));
            Assert.That(mover.Position, Is.EqualTo(2));
            Assert.That(opponent.Position, Is.EqualTo(2));
        }

        [Test]
        public void VictoryPolicyControlsWhetherTheGameEnds()
        {
            var definition = CreateDefinition();
            var player = new InnerBoardState(new PlayerId("player"), 3, 0);
            var service = new InnerBoardVictoryService(
                new IInnerBoardVictoryPolicy[] { new TestVictoryPolicy() });

            var result = service.Evaluate(definition, player.PlayerId, new[] { player });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.GameEnded, Is.True);
            Assert.That(result.Value.WinnerId, Is.EqualTo(player.PlayerId));
            Assert.That(result.Value.ReasonCode, Is.EqualTo("TEST_GOAL_REACHED"));
        }

        private static InnerBoardDefinition CreateDefinition() =>
            new InnerBoardDefinition(
                "inner-board",
                new[]
                {
                    new InnerBoardSpaceDefinition("start", 0, "start"),
                    new InnerBoardSpaceDefinition("protected", 1, "progress", isProtected: true),
                    new InnerBoardSpaceDefinition("progress", 2, "progress"),
                    new InnerBoardSpaceDefinition("goal", 3, "goal", isGoal: true)
                },
                0,
                "test-movement",
                "test-capture",
                "test-victory");

        private sealed class TestCapturePolicy : IInnerBoardCapturePolicy
        {
            public string PolicyId => "test-capture";

            public OperationResult<InnerBoardCaptureCalculation> Calculate(
                InnerBoardDefinition definition,
                PlayerId movedPlayerId,
                IReadOnlyList<InnerBoardState> states)
            {
                var mover = states.Single(state => state.PlayerId == movedPlayerId);
                if (definition.GetSpace(mover.Position).IsProtected)
                {
                    return OperationResult<InnerBoardCaptureCalculation>.Success(
                        new InnerBoardCaptureCalculation(Array.Empty<InnerBoardRelocation>()));
                }

                var relocations = states
                    .Where(state => state.PlayerId != movedPlayerId && state.Position == mover.Position)
                    .Select(state => new InnerBoardRelocation(
                        state.PlayerId,
                        definition.StartIndex,
                        definition.StartIndex));
                return OperationResult<InnerBoardCaptureCalculation>.Success(
                    new InnerBoardCaptureCalculation(relocations));
            }
        }

        private sealed class InvalidCapturePolicy : IInnerBoardCapturePolicy
        {
            public string PolicyId => "test-capture";

            public OperationResult<InnerBoardCaptureCalculation> Calculate(
                InnerBoardDefinition definition,
                PlayerId movedPlayerId,
                IReadOnlyList<InnerBoardState> states) =>
                OperationResult<InnerBoardCaptureCalculation>.Success(
                    new InnerBoardCaptureCalculation(
                        new[] { new InnerBoardRelocation(states[1].PlayerId, 99, 0) }));
        }

        private sealed class TestVictoryPolicy : IInnerBoardVictoryPolicy
        {
            public string PolicyId => "test-victory";

            public InnerBoardVictoryEvaluation Evaluate(
                InnerBoardDefinition definition,
                PlayerId movedPlayerId,
                IReadOnlyList<InnerBoardState> states)
            {
                var mover = states.Single(state => state.PlayerId == movedPlayerId);
                return definition.GetSpace(mover.Position).IsGoal
                    ? InnerBoardVictoryEvaluation.End(movedPlayerId, "TEST_GOAL_REACHED")
                    : InnerBoardVictoryEvaluation.Continue();
            }
        }
    }
}
