using System;
using System.Collections;
using System.IO;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Presentation.Board;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MoneyRank.Tests.PlayMode
{
    public sealed class DevSandboxPlayModeTests
    {
        [UnityTest]
        public IEnumerator AnimatedMoveOpensEventAndConfirmedProgressionChangesTurn()
        {
            var saveRoot = Path.Combine(
                Path.GetTempPath(),
                "MoneyRank-PlayModeTests",
                Guid.NewGuid().ToString("N"));
            var scene = default(Scene);
            BoardSandboxController controller = null;
            void ConfigureLoadedScene(Scene loadedScene, LoadSceneMode mode)
            {
                if (!string.Equals(loadedScene.name, "DevSandbox", StringComparison.Ordinal))
                    return;
                scene = loadedScene;
                controller = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<BoardSandboxController>(true))
                    .Single();
                controller.ConfigurePersistence(saveRoot, false);
            }
            SceneManager.sceneLoaded += ConfigureLoadedScene;

            try
            {
                yield return SceneManager.LoadSceneAsync("DevSandbox", LoadSceneMode.Additive);
                yield return null;

                Assert.That(controller.Session, Is.Not.Null);
                Assert.That(controller.Session.Phase, Is.EqualTo(GamePhase.AwaitMove));
                Assert.That(controller.Move(2), Is.True);

                var remainingFrames = 300;
                while (controller.IsAnimating && remainingFrames-- > 0)
                    yield return null;
                yield return null;

                Assert.That(remainingFrames, Is.GreaterThan(0), "Piece animation did not finish in time.");
                Assert.That(controller.Session.Phase, Is.EqualTo(GamePhase.AwaitChoice));
                Assert.That(controller.EventPanel.Model.EventId, Is.EqualTo("sandbox-investment-01"));

                controller.EventPanel.RequestOption("continue");
                Assert.That(controller.EventPanel.Model.CanConfirm, Is.True);
                controller.EventPanel.RequestConfirmation();
                Assert.That(controller.Session.Phase, Is.EqualTo(GamePhase.ApplyEffects));
                Assert.That(controller.ResolveDemonstrationProgress(1), Is.True);

                Assert.That(controller.Session.Phase, Is.EqualTo(GamePhase.AwaitMove));
                Assert.That(controller.Session.CurrentPlayer.Id, Is.EqualTo(new PlayerId("sandbox-player-2")));
                Assert.That(controller.Session.EventHistory.Records, Has.Count.EqualTo(1));
                Assert.That(controller.Session.ActionHistory.Records, Is.Not.Empty);
            }
            finally
            {
                SceneManager.sceneLoaded -= ConfigureLoadedScene;
                if (scene.IsValid() && scene.isLoaded)
                    SceneManager.UnloadSceneAsync(scene);
                if (Directory.Exists(saveRoot))
                    Directory.Delete(saveRoot, true);
            }
        }
    }
}
