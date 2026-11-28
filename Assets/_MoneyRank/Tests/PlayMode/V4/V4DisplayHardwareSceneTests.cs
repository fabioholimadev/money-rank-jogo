using System.Collections;
using System.IO;
using System.Linq;
using MoneyRank.V4.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MoneyRank.Tests.PlayMode.V4
{
    /// <summary>
    /// Real scene + real Arduino: "Detectar display" must find the coffer display on whatever COM port it is and the side
    /// panel must show the balance the display confirmed. Explicit: needs the Arduino with Docs/Hardware/cofre_display.
    /// </summary>
    [Explicit("Needs the Arduino + OLED with the coffer sketch on a USB port"), Category("Hardware")]
    public sealed class V4DisplayHardwareSceneTests
    {
        private static bool Click(string text)
        {
            var b = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.gameObject.activeInHierarchy && x.interactable && x.GetComponentInChildren<Text>() != null && x.GetComponentInChildren<Text>().text == text);
            if (b == null) return false;
            b.onClick.Invoke();
            return true;
        }

        private static string Find(string fragment) => Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .Where(t => t.gameObject.activeInHierarchy && t.text.Contains(fragment)).Select(t => t.text).FirstOrDefault();

        [UnityTest]
        public IEnumerator DetectButtonFindsTheDisplayAndShowsTheConfirmedBalance()
        {
            yield return SceneManager.LoadSceneAsync("MoneyRankV4");
            yield return null; yield return null;
            var boot = Object.FindAnyObjectByType<V4Bootstrap>();
            Assert.That(Click("INICIAR PARTIDA"), Is.True);
            yield return null;
            var sessionId = boot.Session.S.SessionId;
            try
            {
                Click("Fechar");                                   // the rules panel opens at match start
                Assert.That(Click("Detectar display"), Is.True);
                var until = Time.realtimeSinceStartup + 25f;
                while (Find("✓ mostrando") == null && Time.realtimeSinceStartup < until) yield return null;
                var status = Find("Display (") ?? Find("Procurando") ?? Find("Display do cofre");
                TestContext.WriteLine("status: " + status + " | toast: " + (Find("encontrado") ?? Find("não respondeu") ?? "—"));
                Assert.That(Find("✓ mostrando"), Is.Not.Null, "the display must confirm the balance; status was: " + status);
                Assert.That(boot.Session.Coffer.IsConfirmed, Is.True);
                Assert.That(boot.Session.Coffer.LastAckedBalance, Is.EqualTo(boot.Session.S.FundCash));
            }
            finally
            {
                var store = new MoneyRank.Infrastructure.V4.V4SaveStore(Path.Combine(V4Session.RootDirectory, "Saves"));
                foreach (var f in new[] { store.PathFor(sessionId), store.PathFor(sessionId) + ".bak" }) if (File.Exists(f)) File.Delete(f);
            }
        }
    }
}
