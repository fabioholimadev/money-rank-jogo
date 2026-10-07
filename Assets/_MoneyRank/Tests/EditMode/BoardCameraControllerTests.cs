using System;
using MoneyRank.Presentation.CameraSystem;
using NUnit.Framework;
using UnityEngine;

namespace MoneyRank.Tests.EditMode
{
    public sealed class BoardCameraControllerTests
    {
        [Test]
        public void StartsInOverviewAndCanReturnAfterFocusingATarget()
        {
            var cameraObject = new GameObject("Camera", typeof(Camera));
            var targetObject = new GameObject("Target");

            try
            {
                cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 15f, -15f), Quaternion.Euler(45f, 0f, 0f));
                var controller = cameraObject.AddComponent<BoardCameraController>();

                Assert.That(controller.State, Is.EqualTo(BoardCameraState.Overview));
                Assert.That(controller.FocusTarget, Is.Null);

                controller.SetState(BoardCameraState.FocusCurrentPlayer, targetObject.transform);
                Assert.That(controller.State, Is.EqualTo(BoardCameraState.FocusCurrentPlayer));
                Assert.That(controller.FocusTarget, Is.SameAs(targetObject.transform));

                controller.ReturnToOverview();
                Assert.That(controller.State, Is.EqualTo(BoardCameraState.Overview));
                Assert.That(controller.FocusTarget, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [TestCase(BoardCameraState.FocusCurrentPlayer)]
        [TestCase(BoardCameraState.FocusDice)]
        [TestCase(BoardCameraState.FollowPiece)]
        [TestCase(BoardCameraState.FocusSpace)]
        [TestCase(BoardCameraState.FocusSpecialBuilding)]
        public void TargetedStatesRejectMissingTarget(BoardCameraState state)
        {
            var cameraObject = new GameObject("Camera", typeof(Camera));

            try
            {
                var controller = cameraObject.AddComponent<BoardCameraController>();
                Assert.Throws<ArgumentNullException>(() => controller.SetState(state));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void SnapToDesiredPoseUsesOffsetAndLooksAtTarget()
        {
            var cameraObject = new GameObject("Camera", typeof(Camera));
            var targetObject = new GameObject("Target");

            try
            {
                var controller = cameraObject.AddComponent<BoardCameraController>();
                targetObject.transform.position = new Vector3(4f, 1f, 3f);

                controller.SetState(BoardCameraState.FollowPiece, targetObject.transform);
                controller.SnapToDesiredPose();

                Assert.That(cameraObject.transform.position, Is.EqualTo(targetObject.transform.position + controller.FocusOffset));
                var directionToTarget = targetObject.transform.position - cameraObject.transform.position;
                Assert.That(Vector3.Angle(cameraObject.transform.forward, directionToTarget), Is.LessThan(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ResultsUsesTheOverviewPoseWithoutATarget()
        {
            var cameraObject = new GameObject("Camera", typeof(Camera));

            try
            {
                var overviewPosition = new Vector3(0f, 15f, -15f);
                cameraObject.transform.position = overviewPosition;
                var controller = cameraObject.AddComponent<BoardCameraController>();

                controller.SetState(BoardCameraState.Results);
                controller.SnapToDesiredPose();

                Assert.That(controller.State, Is.EqualTo(BoardCameraState.Results));
                Assert.That(controller.FocusTarget, Is.Null);
                Assert.That(cameraObject.transform.position, Is.EqualTo(overviewPosition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
