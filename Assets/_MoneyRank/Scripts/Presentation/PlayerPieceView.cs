using System.Collections;
using System.Collections.Generic;
using MoneyRank.Domain;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    public sealed class PlayerPieceView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float secondsPerSpace = 0.18f;
        [SerializeField, Min(0f)] private float verticalOffset = 0.9f;

        public bool IsAnimating { get; private set; }

        public void SnapTo(int index, IReadOnlyList<Transform> anchors)
        {
            transform.position = GetTargetPosition(index, anchors);
        }

        public IEnumerator Animate(BoardMoveResult result, IReadOnlyList<Transform> anchors)
        {
            IsAnimating = true;
            var index = result.Origin;

            for (var step = 0; step < result.Steps; step++)
            {
                index = (index + 1) % anchors.Count;
                var origin = transform.position;
                var target = GetTargetPosition(index, anchors);
                var elapsed = 0f;

                while (elapsed < secondsPerSpace)
                {
                    elapsed += Time.deltaTime;
                    transform.position = Vector3.Lerp(origin, target, Mathf.Clamp01(elapsed / secondsPerSpace));
                    yield return null;
                }

                transform.position = target;
            }

            IsAnimating = false;
        }

        private Vector3 GetTargetPosition(int index, IReadOnlyList<Transform> anchors)
        {
            return anchors[index].position + Vector3.up * verticalOffset;
        }
    }
}
