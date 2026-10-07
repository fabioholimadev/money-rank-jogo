using System;
using UnityEngine;

namespace MoneyRank.Presentation.CameraSystem
{
    public enum BoardCameraState
    {
        Overview = 0,
        FocusCurrentPlayer = 10,
        FocusDice = 20,
        FollowPiece = 30,
        FocusSpace = 40,
        FocusSpecialBuilding = 50,
        Results = 60
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class BoardCameraController : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float positionSharpness = 6f;
        [SerializeField, Min(0.01f)] private float rotationSharpness = 8f;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 6f, -6f);

        private Vector3 _overviewPosition;
        private Quaternion _overviewRotation;
        private bool _hasOverviewPose;

        public BoardCameraState State { get; private set; } = BoardCameraState.Overview;
        public Transform FocusTarget { get; private set; }
        public Vector3 FocusOffset => focusOffset;

        private void Awake()
        {
            CaptureOverviewPose();
        }

        private void LateUpdate()
        {
            ApplyDesiredPose(false);
        }

        public void CaptureOverviewPose()
        {
            _overviewPosition = transform.position;
            _overviewRotation = transform.rotation;
            _hasOverviewPose = true;
            State = BoardCameraState.Overview;
            FocusTarget = null;
        }

        public void SetState(BoardCameraState state, Transform focusTarget = null)
        {
            if (RequiresTarget(state) && focusTarget == null)
                throw new ArgumentNullException(nameof(focusTarget), $"Camera state {state} requires a focus target.");

            if (!_hasOverviewPose)
            {
                CaptureOverviewPose();
            }

            State = state;
            FocusTarget = RequiresTarget(state) ? focusTarget : null;
        }

        public void ReturnToOverview()
        {
            SetState(BoardCameraState.Overview);
        }

        public void SnapToDesiredPose()
        {
            ApplyDesiredPose(true);
        }

        private void ApplyDesiredPose(bool immediately)
        {
            if (!_hasOverviewPose)
            {
                CaptureOverviewPose();
            }

            GetDesiredPose(out var desiredPosition, out var desiredRotation);
            if (immediately)
            {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                return;
            }

            var positionBlend = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            var rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
        }

        private void GetDesiredPose(out Vector3 position, out Quaternion rotation)
        {
            if (FocusTarget == null)
            {
                position = _overviewPosition;
                rotation = _overviewRotation;
                return;
            }

            position = FocusTarget.position + focusOffset;
            var lookDirection = FocusTarget.position - position;
            rotation = lookDirection.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(lookDirection, Vector3.up)
                : _overviewRotation;
        }

        private static bool RequiresTarget(BoardCameraState state)
        {
            switch (state)
            {
                case BoardCameraState.FocusCurrentPlayer:
                case BoardCameraState.FocusDice:
                case BoardCameraState.FollowPiece:
                case BoardCameraState.FocusSpace:
                case BoardCameraState.FocusSpecialBuilding:
                    return true;
                default:
                    return false;
            }
        }
    }
}
