using System;
using System.IO;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Presentation.Board;
using MoneyRank.Presentation.CameraSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MoneyRank.Tests.EditMode
{
    public sealed class DevSandboxSceneTests
    {
        [Test]
        public void SceneRunsTwoPlayerTurnCycleThroughSessionService()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                OpenSceneMode.Additive);

            try
            {
                var roots = scene.GetRootGameObjects();
                var cameraController = roots
                    .SelectMany(root => root.GetComponentsInChildren<BoardCameraController>(true))
                    .Single();
                var sandboxController = roots
                    .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                    .Single();
                var serializedSandbox = new SerializedObject(sandboxController);
                serializedSandbox.FindProperty("usePersistentSave").boolValue = false;
                serializedSandbox.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(
                    serializedSandbox.FindProperty("cameraController").objectReferenceValue,
                    Is.SameAs(cameraController));

                sandboxController.Initialize();

                Assert.That(sandboxController.Session.Players, Has.Count.EqualTo(2));
                Assert.That(sandboxController.Session.CharacterDefinitions, Has.Count.EqualTo(2));
                Assert.That(sandboxController.Session.AttributeDefinitions, Has.Count.EqualTo(3));
                Assert.That(sandboxController.Session.InnerBoard, Is.Not.Null);
                Assert.That(sandboxController.Session.InnerBoard.SpaceCount, Is.EqualTo(6));
                Assert.That(
                    sandboxController.Session.TryGetCharacter(
                        new PlayerId("sandbox-player-1"),
                        out var firstCharacter),
                    Is.True);
                Assert.That(firstCharacter.Id, Is.EqualTo("sandbox-character-a"));
                Assert.That(
                    sandboxController.Session.TryGetCharacter(
                        new PlayerId("sandbox-player-2"),
                        out var secondCharacter),
                    Is.True);
                Assert.That(secondCharacter.Id, Is.EqualTo("sandbox-character-b"));
                Assert.That(sandboxController.PlayerPieceCount, Is.EqualTo(2));
                Assert.That(sandboxController.EventPanel, Is.Not.Null);
                Assert.That(sandboxController.EventPanel.Model.IsVisible, Is.False);
                Assert.That(sandboxController.ControlPanel, Is.Not.Null);
                Assert.That(sandboxController.ControlPanel.IsBound, Is.True);
                Assert.That(sandboxController.Session.Phase, Is.EqualTo(GamePhase.AwaitMove));
                Assert.That(sandboxController.Session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("sandbox-player-1")));

                Assert.That(sandboxController.Move(-1), Is.False);
                Assert.That(sandboxController.LastErrorCode, Is.EqualTo("INVALID_MOVE_STEPS"));
                Assert.That(sandboxController.Session.Phase, Is.EqualTo(GamePhase.AwaitMove));

                sandboxController.ControlPanel.SetStepInput("not-a-number");
                Assert.That(sandboxController.ControlPanel.SubmitMove(), Is.False);
                Assert.That(sandboxController.Session.Phase, Is.EqualTo(GamePhase.AwaitMove));

                sandboxController.ControlPanel.SetStepInput("3");
                Assert.That(sandboxController.ControlPanel.SubmitMove(), Is.True);
                Assert.That(sandboxController.Session.Phase, Is.EqualTo(GamePhase.AwaitChoice));
                Assert.That(sandboxController.EventPanel.Model.IsVisible, Is.True);
                Assert.That(sandboxController.EventPanel.Model.EventId, Is.EqualTo("sandbox-citizenship-01"));
                Assert.That(sandboxController.ControlPanel.AdvanceTurn(), Is.False);
                sandboxController.EventPanel.RequestOption("continue");
                Assert.That(sandboxController.ActivePreview, Is.Not.Null);
                sandboxController.EventPanel.RequestCancellation();
                Assert.That(sandboxController.ActivePreview, Is.Null);
                Assert.That(sandboxController.EventPanel.Model.HasPreview, Is.False);
                Assert.That(sandboxController.Session.Phase, Is.EqualTo(GamePhase.AwaitChoice));
                ResolveVisibleEvent(sandboxController);
                Assert.That(sandboxController.Session.Phase, Is.EqualTo(GamePhase.ApplyEffects));
                sandboxController.ControlPanel.SetProgressionStepInput("2");
                Assert.That(sandboxController.ControlPanel.SubmitProgression(), Is.True);
                Assert.That(sandboxController.Session.Phase, Is.EqualTo(GamePhase.AwaitMove));
                Assert.That(sandboxController.Session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("sandbox-player-2")));

                Assert.That(sandboxController.Move(2), Is.True);
                ResolveVisibleEvent(sandboxController);
                sandboxController.ControlPanel.SetProgressionStepInput("1");
                Assert.That(sandboxController.ControlPanel.SubmitProgression(), Is.True);
                Assert.That(sandboxController.Session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("sandbox-player-1")));
                Assert.That(
                    sandboxController.Session.TryGetBoardState(new PlayerId("sandbox-player-1"), out var firstState),
                    Is.True);
                Assert.That(
                    sandboxController.Session.TryGetBoardState(new PlayerId("sandbox-player-2"), out var secondState),
                    Is.True);
                Assert.That(firstState.Position, Is.EqualTo(3));
                Assert.That(secondState.Position, Is.EqualTo(2));
                Assert.That(
                    sandboxController.Session.TryGetInnerBoardState(
                        new PlayerId("sandbox-player-1"),
                        out var firstInner),
                    Is.True);
                Assert.That(firstInner.Position, Is.EqualTo(2));
                Assert.That(
                    sandboxController.Session.TryGetInnerBoardState(
                        new PlayerId("sandbox-player-2"),
                        out var secondInner),
                    Is.True);
                Assert.That(secondInner.Position, Is.EqualTo(1));
                Assert.That(
                    sandboxController.Session.ActionHistory.Records.Select(record => record.TypeId),
                    Does.Contain("board.moved"));
                Assert.That(
                    sandboxController.Session.ActionHistory.Records.Select(record => record.TypeId),
                    Does.Contain("event.selected"));
                Assert.That(
                    sandboxController.Session.ActionHistory.Records.Select(record => record.TypeId),
                    Does.Contain("event.confirmed"));
                Assert.That(
                    sandboxController.Session.ActionHistory.Records.Select(record => record.TypeId),
                    Does.Contain("progress.inner.resolved"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void SceneAutosaveRestoresThePreviousSandboxSession()
        {
            var saveRoot = Path.Combine(
                Path.GetTempPath(),
                "MoneyRank-SandboxPersistenceTests",
                Guid.NewGuid().ToString("N"));

            try
            {
                var firstScene = EditorSceneManager.OpenScene(
                    "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                    OpenSceneMode.Additive);
                try
                {
                    var firstController = firstScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                        .Single();
                    firstController.ConfigurePersistence(saveRoot, true);
                    firstController.Initialize();

                    Assert.That(firstController.RestoredExistingSave, Is.False);
                    Assert.That(firstController.Move(4), Is.True);
                    ResolveVisibleEvent(firstController);
                    firstController.ControlPanel.SetProgressionStepInput("1");
                    Assert.That(firstController.ControlPanel.SubmitProgression(), Is.True);
                }
                finally
                {
                    EditorSceneManager.CloseScene(firstScene, true);
                }

                var restoredScene = EditorSceneManager.OpenScene(
                    "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                    OpenSceneMode.Additive);
                try
                {
                    var restoredController = restoredScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                        .Single();
                    restoredController.ConfigurePersistence(saveRoot, true);
                    restoredController.Initialize();

                    Assert.That(restoredController.RestoredExistingSave, Is.True);
                    Assert.That(restoredController.ActiveSaveRoot, Is.EqualTo(Path.GetFullPath(saveRoot)));
                    Assert.That(restoredController.Session.Phase, Is.EqualTo(GamePhase.AwaitMove));
                    Assert.That(
                        restoredController.Session.CurrentPlayer.Id,
                        Is.EqualTo(new PlayerId("sandbox-player-2")));
                    Assert.That(
                        restoredController.Session.TryGetBoardState(
                            new PlayerId("sandbox-player-1"),
                            out var restoredPlayer),
                        Is.True);
                    Assert.That(restoredPlayer.Position, Is.EqualTo(4));
                    Assert.That(
                        restoredController.Session.TryGetInnerBoardState(
                            new PlayerId("sandbox-player-1"),
                            out var restoredInner),
                        Is.True);
                    Assert.That(restoredInner.Position, Is.EqualTo(1));
                    Assert.That(restoredController.Session.ActionHistory.Records, Is.Not.Empty);
                }
                finally
                {
                    EditorSceneManager.CloseScene(restoredScene, true);
                }
            }
            finally
            {
                if (Directory.Exists(saveRoot))
                    Directory.Delete(saveRoot, true);
            }
        }

        [Test]
        public void SceneAutosaveRestoresActiveEventChoiceAndPanel()
        {
            var saveRoot = Path.Combine(
                Path.GetTempPath(),
                "MoneyRank-SandboxActiveEventTests",
                Guid.NewGuid().ToString("N"));

            try
            {
                var firstScene = EditorSceneManager.OpenScene(
                    "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                    OpenSceneMode.Additive);
                try
                {
                    var controller = firstScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                        .Single();
                    controller.ConfigurePersistence(saveRoot, true);
                    controller.Initialize();
                    Assert.That(controller.Move(1), Is.True);
                    Assert.That(controller.Session.Phase, Is.EqualTo(GamePhase.AwaitChoice));
                    Assert.That(controller.Session.ActiveEvent.EventId, Is.EqualTo("sandbox-expense-01"));
                }
                finally
                {
                    EditorSceneManager.CloseScene(firstScene, true);
                }

                var restoredScene = EditorSceneManager.OpenScene(
                    "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                    OpenSceneMode.Additive);
                try
                {
                    var restored = restoredScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                        .Single();
                    restored.ConfigurePersistence(saveRoot, true);
                    restored.Initialize();

                    Assert.That(restored.RestoredExistingSave, Is.True);
                    Assert.That(restored.Session.Phase, Is.EqualTo(GamePhase.AwaitChoice));
                    Assert.That(restored.EventPanel.Model.IsVisible, Is.True);
                    Assert.That(restored.EventPanel.Model.EventId, Is.EqualTo("sandbox-expense-01"));
                    ResolveVisibleEvent(restored);
                    Assert.That(restored.Session.Phase, Is.EqualTo(GamePhase.ApplyEffects));
                }
                finally
                {
                    EditorSceneManager.CloseScene(restoredScene, true);
                }
            }
            finally
            {
                if (Directory.Exists(saveRoot))
                    Directory.Delete(saveRoot, true);
            }
        }

        [Test]
        public void SceneShowsAndRestoresPersistedWinnerResult()
        {
            var saveRoot = Path.Combine(
                Path.GetTempPath(),
                "MoneyRank-SandboxResultTests",
                Guid.NewGuid().ToString("N"));

            try
            {
                var firstScene = EditorSceneManager.OpenScene(
                    "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                    OpenSceneMode.Additive);
                try
                {
                    var controller = firstScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                        .Single();
                    var camera = firstScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardCameraController>(true))
                        .Single();
                    controller.ConfigurePersistence(saveRoot, true);
                    controller.Initialize();
                    Assert.That(controller.Move(1), Is.True);
                    ResolveVisibleEvent(controller);
                    Assert.That(controller.ResolveDemonstrationProgress(5), Is.True);

                    Assert.That(controller.Session.Phase, Is.EqualTo(GamePhase.Results));
                    Assert.That(controller.Session.Result.WinnerId, Is.EqualTo(new PlayerId("sandbox-player-1")));
                    Assert.That(controller.Session.Result.ReasonCode, Is.EqualTo("SANDBOX_INNER_GOAL_REACHED"));
                    Assert.That(camera.State, Is.EqualTo(BoardCameraState.Results));
                }
                finally
                {
                    EditorSceneManager.CloseScene(firstScene, true);
                }

                var restoredScene = EditorSceneManager.OpenScene(
                    "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                    OpenSceneMode.Additive);
                try
                {
                    var restored = restoredScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                        .Single();
                    var restoredCamera = restoredScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BoardCameraController>(true))
                        .Single();
                    restored.ConfigurePersistence(saveRoot, true);
                    restored.Initialize();

                    Assert.That(restored.Session.Phase, Is.EqualTo(GamePhase.Results));
                    Assert.That(restored.Session.Result.WinnerId, Is.EqualTo(new PlayerId("sandbox-player-1")));
                    Assert.That(restoredCamera.State, Is.EqualTo(BoardCameraState.Results));
                }
                finally
                {
                    EditorSceneManager.CloseScene(restoredScene, true);
                }
            }
            finally
            {
                if (Directory.Exists(saveRoot))
                    Directory.Delete(saveRoot, true);
            }
        }

        [Test]
        public void MasterPanelUndoesProgressionWithoutErasingAuditAndExportsPlaytest()
        {
            var saveRoot = Path.Combine(
                Path.GetTempPath(),
                "MoneyRank-SandboxMasterUxTests",
                Guid.NewGuid().ToString("N"));
            var scene = EditorSceneManager.OpenScene(
                "Assets/_MoneyRank/Scenes/DevSandbox.unity",
                OpenSceneMode.Additive);

            try
            {
                var controller = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                    .Single();
                controller.ConfigurePersistence(saveRoot, false);
                controller.Initialize();
                Assert.That(controller.Move(1), Is.True);
                ResolveVisibleEvent(controller);
                Assert.That(controller.ResolveDemonstrationProgress(2), Is.True);
                var actionCountBeforeUndo = controller.Session.ActionHistory.Records.Count;

                Assert.That(controller.ControlPanel.UndoLastAction(), Is.True);

                Assert.That(controller.Session.Phase, Is.EqualTo(GamePhase.ApplyEffects));
                Assert.That(controller.Session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("sandbox-player-1")));
                Assert.That(
                    controller.Session.TryGetInnerBoardState(
                        new PlayerId("sandbox-player-1"),
                        out var restoredInner),
                    Is.True);
                Assert.That(restoredInner.Position, Is.Zero);
                Assert.That(controller.Session.ActionHistory.Records.Count, Is.GreaterThan(actionCountBeforeUndo));
                Assert.That(
                    controller.Session.ActionHistory.Records.Last().TypeId,
                    Is.EqualTo("action.undone"));
                Assert.That(controller.UndoJournal.Records, Has.Count.EqualTo(1));

                Assert.That(controller.ControlPanel.ExportPlaytest(), Is.True);
                Assert.That(Directory.Exists(controller.LastExportDirectory), Is.True);
                Assert.That(Directory.GetFiles(controller.LastExportDirectory, "*.json"), Has.Length.EqualTo(1));
                Assert.That(Directory.GetFiles(controller.LastExportDirectory, "*.csv"), Has.Length.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (Directory.Exists(saveRoot))
                    Directory.Delete(saveRoot, true);
            }
        }

        private static void ResolveVisibleEvent(BoardSandboxController controller)
        {
            controller.EventPanel.RequestOption("continue");
            Assert.That(controller.ActivePreview, Is.Not.Null);
            Assert.That(controller.EventPanel.Model.CanConfirm, Is.True);
            controller.EventPanel.RequestConfirmation();
            Assert.That(controller.EventPanel.Model.IsConfirmed, Is.True);
        }
    }
}
