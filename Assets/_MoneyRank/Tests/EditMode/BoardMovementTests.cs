using System.Collections.Generic;
using MoneyRank.Domain;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class BoardMovementTests
    {
        [Test]
        public void MovesWithoutWrapping()
        {
            var board = CreateBoard(20);
            var result = new BoardMovementCalculator().Move(board, 2, 4);

            Assert.That(result.Destination, Is.EqualTo(6));
            Assert.That(result.CompletedLaps, Is.Zero);
            Assert.That(result.DestinationSpace.Id, Is.EqualTo("space-6"));
        }

        [Test]
        public void WrapsAndCountsCompletedLap()
        {
            var board = CreateBoard(20);
            var result = new BoardMovementCalculator().Move(board, 18, 4);

            Assert.That(result.Destination, Is.EqualTo(2));
            Assert.That(result.CompletedLaps, Is.EqualTo(1));
        }

        [Test]
        public void SupportsMoreThanOneLapInSingleMove()
        {
            var board = CreateBoard(8);
            var result = new BoardMovementCalculator().Move(board, 1, 18);

            Assert.That(result.Destination, Is.EqualTo(3));
            Assert.That(result.CompletedLaps, Is.EqualTo(2));
        }

        [Test]
        public void SupportsConfiguredStartIndex()
        {
            var board = CreateBoard(8, startIndex: 3);
            var result = new BoardMovementCalculator().Move(board, 2, 1);

            Assert.That(result.Destination, Is.EqualTo(3));
            Assert.That(result.CompletedLaps, Is.EqualTo(1));
        }

        [Test]
        public void PlayerBoardStateAccumulatesLaps()
        {
            var board = CreateBoard(8);
            var state = new ExternalBoardState(new PlayerId("p1"), board);
            var calculator = new BoardMovementCalculator();

            state.Move(board, 10, calculator);
            state.Move(board, 7, calculator);

            Assert.That(state.Position, Is.EqualTo(1));
            Assert.That(state.CompletedLaps, Is.EqualTo(2));
        }

        [Test]
        public void BoardSizeIsNotHardcoded()
        {
            var board = CreateBoard(5);
            var result = new BoardMovementCalculator().Move(board, 4, 2);

            Assert.That(result.Destination, Is.EqualTo(1));
        }

        private static BoardDefinition CreateBoard(int count, int startIndex = 0)
        {
            var spaces = new List<BoardSpaceDefinition>();
            for (var index = 0; index < count; index++)
                spaces.Add(new BoardSpaceDefinition($"space-{index}", index, index == startIndex ? "start" : "event"));

            return new BoardDefinition("board-test", spaces, startIndex);
        }
    }
}
