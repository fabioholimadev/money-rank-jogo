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
    /// <summary>
    /// Renders the real scene and saves screenshots to Logs/shots so the visual design can be reviewed from the command line.
    /// Only asserts that the frames are not blank; the images are the evidence.
    /// </summary>
    [Category("Visual")]
    public sealed class V4VisualCaptureTests
    {
        private static string ShotDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "shots"));

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

        /// <summary>Renders the main camera (with the UI canvas switched to camera space) into a PNG; works headless in batch mode.</summary>
        private static IEnumerator Shot(string name, int width = 1600, int height = 900)
        {
            yield return null;
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToList();
            var modes = canvases.Select(c => c.renderMode).ToList();
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 1f; }
            yield return null; yield return null;       // lets the CanvasScaler and the camera adapt to the target aspect
            cam.Render();
            cam.targetTexture = previous;
            for (var i = 0; i < canvases.Count; i++) canvases[i].renderMode = modes[i];
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(ShotDir);
            File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), tex.EncodeToPNG());
            var px = tex.GetPixels32();
            var distinct = px.Where((p, i) => i % 997 == 0).Select(p => (p.r / 16) * 256 + (p.g / 16) * 16 + (p.b / 16)).Distinct().Count();
            Object.Destroy(tex); rt.Release(); Object.Destroy(rt);
            Assert.That(distinct, Is.GreaterThan(6), name + " looks blank");
        }

        private static IEnumerator Wait(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        [UnityTest]
        public IEnumerator CaptureSetupBoardDiceCoinAndCorners()
        {
            string sessionId = null;
            yield return SceneManager.LoadSceneAsync("MoneyRankV4");
            yield return Wait(0.6f);
            var boot = Object.FindAnyObjectByType<V4Bootstrap>();
            try
            {
                yield return Shot("cap_01_setup");
                Assert.That(Click("INICIAR PARTIDA"), Is.True);
                yield return Wait(0.7f);
                sessionId = boot.Session.S.SessionId;
                yield return Shot("cap_02_board");

                // corners close-ups from the default camera side
                boot.SetView(0f, 48f, 9.5f, new Vector3(-4.2f, 0f, 4.2f)); yield return Wait(0.4f); yield return Shot("cap_03_corner_start");
                boot.SetView(0f, 48f, 9.5f, new Vector3(4.2f, 0f, 4.2f)); yield return Wait(0.4f); yield return Shot("cap_04_corner_hospital");
                boot.SetView(0f, 48f, 9.5f, new Vector3(4.2f, 0f, -4.2f)); yield return Wait(0.4f); yield return Shot("cap_05_corner_direct");
                boot.SetView(0f, 48f, 9.5f, new Vector3(-4.2f, 0f, -4.2f)); yield return Wait(0.4f); yield return Shot("cap_06_corner_cofre");
                boot.SetView(0f, 50f, 26f, new Vector3(0f, 0f, 0.8f)); yield return Wait(0.4f); yield return Shot("cap_a1_view_50_26");
                boot.SetView(0f, 44f, 28f, new Vector3(0f, 0f, 2.6f)); yield return Wait(0.4f); yield return Shot("cap_a2_view_44_28");
                boot.ResetView();
                yield return Wait(0.3f);

                boot.Hud.DiceSource = () => (3, 4);
                Assert.That(Click("LANÇAR DADOS"), Is.True);
                yield return Wait(0.85f); yield return Shot("cap_07_dice_flying");
                yield return Wait(1.0f); yield return Shot("cap_08_dice_landed");
                var spins = 0;
                while (boot.Session.S.Phase == V4Phase.AwaitRoll && spins++ < 200) yield return null;
                yield return Wait(0.5f); yield return Shot("cap_09_after_dice");
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

        /// <summary>Same screens at 4:3 (tablet), 16:9 (720p/1080p) with four players, to review layout by eye.</summary>
        [UnityTest]
        public IEnumerator CaptureLayoutsAcrossResolutions()
        {
            var sizes = new[] { (1024, 768), (1280, 720), (1920, 1080) };
            string sessionId = null;
            yield return SceneManager.LoadSceneAsync("MoneyRankV4");
            yield return Wait(0.5f);
            var boot = Object.FindAnyObjectByType<V4Bootstrap>();
            try
            {
                Assert.That(Click("4"), Is.True);
                yield return Wait(0.3f);
                foreach (var (w, h) in sizes) yield return Shot("res_" + w + "x" + h + "_1_setup", w, h);

                Assert.That(Click("INICIAR PARTIDA"), Is.True);
                yield return Wait(0.7f);
                sessionId = boot.Session.S.SessionId;
                foreach (var (w, h) in sizes) yield return Shot("res_" + w + "x" + h + "_2_board", w, h);

                boot.Session.Engine.CurrentPlayer.Position = 0;     // 0 + 3 -> H04
                boot.Hud.DiceSource = () => (1, 2);
                Click("LANÇAR DADOS");
                var guard = 0; while (boot.Session.S.Phase != V4Phase.AwaitDecision && guard++ < 900) yield return null;
                yield return Wait(0.3f);
                Click("Selecionar", 2); yield return Wait(0.3f);
                foreach (var (w, h) in sizes) yield return Shot("res_" + w + "x" + h + "_3_decision", w, h);

                Click("Voltar ao tabuleiro"); yield return Wait(0.2f);
                Click("Fiscal"); yield return Wait(0.3f);
                foreach (var (w, h) in sizes) yield return Shot("res_" + w + "x" + h + "_4_fiscal", w, h);
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
        public IEnumerator CaptureCoinFlip()
        {
            string sessionId = null;
            yield return SceneManager.LoadSceneAsync("MoneyRankV4");
            yield return Wait(0.5f);
            var boot = Object.FindAnyObjectByType<V4Bootstrap>();
            try
            {
                Click("INICIAR PARTIDA");
                yield return Wait(0.6f);
                sessionId = boot.Session.S.SessionId;
                boot.Session.S.Players[0].Position = 21;
                boot.Hud.DiceSource = () => (1, 2);
                Click("LANÇAR DADOS");
                var guard = 0; while (boot.Session.S.Phase != V4Phase.AwaitDecision && guard++ < 600) yield return null;
                yield return Wait(0.4f);
                Click("Selecionar", 2); yield return Wait(0.3f);
                Click("CONFIRMAR"); yield return Wait(0.5f);
                yield return Shot("cap_10_coin_modal");
                boot.Hud.CoinSource = () => false;
                Click("LANÇAR MOEDA");
                yield return Wait(0.8f); yield return Shot("cap_11_coin_flying");
                yield return Wait(1.1f); yield return Shot("cap_12_coin_landed");
                var spins = 0; while (boot.Session.S.Pending != null && boot.Session.S.Pending.Kind == DecisionKind.TaxCoin && spins++ < 600) yield return null;
                yield return Wait(0.5f); yield return Shot("cap_13_after_coin");
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
    }
}
