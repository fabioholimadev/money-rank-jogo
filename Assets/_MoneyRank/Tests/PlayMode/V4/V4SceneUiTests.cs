using System.Collections;
using System.IO;
using System.Linq;
using MoneyRank.Domain.V4;
using MoneyRank.V4.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MoneyRank.Tests.PlayMode.V4
{
    /// <summary>Drives the real V4 scene through its own buttons (no engine shortcuts) to prove the UI is wired to the rules.</summary>
    public sealed class V4SceneUiTests
    {
        private static bool Click(string text, int nth = 1)
        {
            var found = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(b => b.gameObject.activeInHierarchy && b.interactable)
                .Where(b => { var t = b.GetComponentInChildren<Text>(); return t != null && t.text == text; })
                .OrderBy(b => b.transform.position.x).ToList();
            if (found.Count < nth) return false;
            found[nth - 1].onClick.Invoke();
            return true;
        }

        private static IEnumerator Frames(int n = 2) { for (var i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator StartMatchRollAndConfirmAnEssentialExpenseThroughTheUi()
        {
            string sessionId = null;
            yield return SceneManager.LoadSceneAsync("MoneyRankV4");
            yield return Frames();
            var boot = Object.FindAnyObjectByType<V4Bootstrap>();
            Assert.That(boot, Is.Not.Null);
            Assert.That(boot.Session, Is.Null, "starts on the setup screen");
            try
            {
                Assert.That(Click("INICIAR PARTIDA"), Is.True);
                yield return Frames();
                Assert.That(boot.Session, Is.Not.Null);
                sessionId = boot.Session.S.SessionId;
                Assert.That(boot.Session.S.Players.Count, Is.EqualTo(2));
                Assert.That(boot.Session.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));

                Assert.That(Click("3"), Is.True, "dice button for 3");   // H01 + 3 = H04 (essential)
                yield return Frames();
                Assert.That(boot.Session.S.Phase, Is.EqualTo(V4Phase.AwaitDecision));
                Assert.That(boot.Session.S.Pending.HouseId, Is.EqualTo("H04"));
                Assert.That(boot.Session.S.Pending.Mandatory, Is.True);

                // select the 2nd card (preview only), then confirm
                Assert.That(Click("Selecionar", 2), Is.True);
                yield return Frames();
                Assert.That(boot.Session.S.Players[0].Wallet, Is.EqualTo(500), "preview must not apply anything");
                Assert.That(Click("CONFIRMAR"), Is.True);
                yield return Frames();
                Assert.That(boot.Session.S.Players[0].Wallet, Is.EqualTo(450), "H04_O02 costs 50");
                Assert.That(boot.Session.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));
                Assert.That(boot.Session.Engine.CurrentPlayer.Id, Is.EqualTo("p2"));
                Assert.That(boot.Session.Engine.TotalCash(), Is.EqualTo(28260));
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("Carteira 450")), Is.True, "HUD shows the new balance");

                // save/restore in the middle of a pending decision keeps the very same decision
                Assert.That(Click("1"), Is.True);                         // p2: H01 + 1 = H02 (non essential)
                yield return Frames();
                var pendingId = boot.Session.S.Pending.Id;
                string error;
                var resumed = V4Session.Resume(sessionId, out error);
                Assert.That(resumed, Is.Not.Null, error);
                Assert.That(resumed.S.Pending.Id, Is.EqualTo(pendingId));
                Assert.That(resumed.S.Pending.OptionIds, Is.EqualTo(boot.Session.S.Pending.OptionIds));
                Assert.That(Click("Voltar ao tabuleiro"), Is.True);
                yield return Frames();
                Assert.That(boot.Session.S.Pending, Is.Not.Null, "going back does not decline: the decision is still pending");
                Assert.That(Click("Abrir decisão"), Is.True);
                yield return Frames();
                Assert.That(Click("Selecionar", 4), Is.True);             // "Recusar"
                yield return Frames();
                Assert.That(Click("CONFIRMAR"), Is.True);
                yield return Frames();
                Assert.That(boot.Session.S.Pending, Is.Null);
                Assert.That(boot.Session.S.Players[1].Wallet, Is.EqualTo(500), "declining costs nothing");
            }
            finally
            {
                if (sessionId != null)
                {
                    var store = new MoneyRank.Infrastructure.V4.V4SaveStore(Path.Combine(V4Session.RootDirectory, "Saves"));
                    foreach (var f in new[] { store.PathFor(sessionId), store.PathFor(sessionId) + ".bak" }) if (File.Exists(f)) File.Delete(f);
                }
            }
        }

        [UnityTest]
        public IEnumerator SetupDoesNotAllowTheSameProfessionTwice()
        {
            yield return SceneManager.LoadSceneAsync("MoneyRankV4");
            yield return Frames();
            var boot = Object.FindAnyObjectByType<V4Bootstrap>();
            // "Professora" is player 1's choice: in the second row that button is disabled, so the first match is the only clickable one
            var clickable = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Count(b => b.gameObject.activeInHierarchy && b.interactable && b.GetComponentInChildren<Text>() != null && b.GetComponentInChildren<Text>().text == "Professora");
            Assert.That(clickable, Is.EqualTo(1));
            Assert.That(boot.Session, Is.Null);
        }
    }
}
