using System;
using MoneyRank.Content;
using MoneyRank.Domain;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    public sealed class BoardSandboxController : MonoBehaviour
    {
        [SerializeField] private BoardDefinitionAsset definition;
        [SerializeField] private BoardLayoutView boardView;
        [SerializeField] private PlayerPieceView playerPiece;
        [SerializeField, Min(0)] private int demonstrationSteps = 3;

        private BoardDefinition _board;
        private BoardMovementCalculator _movement;
        private ExternalBoardState _playerState;

        private void Start()
        {
            if (definition == null || boardView == null || playerPiece == null)
                throw new InvalidOperationException("The board definition, board view and player piece are required.");

            _board = definition.ToDomain();
            _movement = new BoardMovementCalculator();
            _playerState = new ExternalBoardState(new PlayerId("sandbox-player"), _board);
            playerPiece.SnapTo(_playerState.Position, boardView.SpaceAnchors);
        }

        [ContextMenu("Move Demonstration Steps")]
        public void MoveDemonstrationSteps()
        {
            Move(demonstrationSteps);
        }

        public bool Move(int steps)
        {
            if (_playerState == null || playerPiece.IsAnimating) return false;

            var result = _playerState.Move(_board, steps, _movement);
            StartCoroutine(playerPiece.Animate(result, boardView.SpaceAnchors));
            return true;
        }
    }
}
