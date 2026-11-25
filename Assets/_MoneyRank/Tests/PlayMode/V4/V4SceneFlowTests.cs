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
    /// Modal and panel flows of the real V4 scene (hospital, athlete ability, fiscal panel, undo, end screen).
    /// The state is prepared directly; every decision is then taken through the scene's own buttons.
    /// </summary>
    public sealed class V4SceneFlowTests
    {
        private V4Bootstrap _boot;
        private string _sessionId;

        private static Button[] Buttons(string text) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Where(b => b.gameObject.activeInHierarchy)
            .Where(b => { var t = b.GetComponentInChildren<Text>(); return t != null && t.text == text; })
            .OrderBy(b => b.transform.position.x).ToArray();

        private static bool Click(string text, int nth = 1)
        {
            var found = Buttons(text).Where(b => b.interactable).ToArray();
            if (found.Length < nth) return false;
            found[nth - 1].onClick.Invoke();
            return true;
        }

        private static bool Sees(string fragment) => Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .Any(t => t.gameObject.activeInHierarchy && t.text.Contains(fragment));

        private static IEnumerator Frames(int n = 2) { for (var i = 0; i < n; i++) yield return null; }

        private IEnumerator WaitFor(System.Func<bool> condition, float seconds = 10f)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(condition(), Is.True, "timed out waiting for the animation/command");
            yield return Frames();
        }

        [UnitySetUp]
        public IEnumerator StartAMatch()
        {
            _sessionId = null;
            yield return SceneManager.LoadSceneAsync("MoneyRankV4");
            yield return Frames();
            _boot = Object.FindAnyObjectByType<V4Bootstrap>();
            Assert.That(Click("INICIAR PARTIDA"), Is.True);
            yield return Frames();
            _sessionId = _boot.Session.S.SessionId;
        }

        [TearDown]
        public void DeleteTheTestSave()
        {
            if (_sessionId == null) return;
            var store = new MoneyRank.Infrastructure.V4.V4SaveStore(Path.Combine(V4Session.RootDirectory, "Saves"));
            foreach (var f in new[] { store.PathFor(_sessionId), store.PathFor(_sessionId) + ".bak" }) if (File.Exists(f)) File.Delete(f);
        }

        /// <summary>Rolls 1+2 from three steps before the house so the current player lands exactly there.</summary>
        private IEnumerator LandOn(string houseId)
        {
            var session = _boot.Session;
            session.Engine.CurrentPlayer.Position = (session.Content.House(houseId).UnityIndex + 33) % 36;
            _boot.Hud.DiceSource = () => (1, 2);
            Assert.That(Click("LANÇAR DADOS"), Is.True);
            yield return WaitFor(() => !_boot.Hud.Busy && session.S.Phase != V4Phase.AwaitRoll);
        }

        private IEnumerator SelectAndConfirm(string optionId)
        {
            var index = _boot.Session.S.Pending.OptionIds.IndexOf(optionId);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), optionId + " is offered");
            Assert.That(Click("Selecionar", index + 1), Is.True);
            yield return Frames();
            Assert.That(Click("CONFIRMAR"), Is.True);
            yield return Frames();
        }

        [UnityTest]
        public IEnumerator HospitalModalOnlyEnablesAffordableTreatmentsAndAppliesTheChosenOne()
        {
            var s = _boot.Session.S;
            var me = s.Players[0];
            me.HospitalPending = true;                                  // admitted as soon as this action finishes
            yield return LandOn("H04");
            yield return SelectAndConfirm("H04_O02");

            Assert.That(s.Pending.Kind, Is.EqualTo(DecisionKind.HospitalModality));
            Assert.That(Sees("Internação: escolha a modalidade"), Is.True);
            var choices = Buttons("Escolher");
            Assert.That(choices.Length, Is.EqualTo(2));
            Assert.That(choices[0].interactable, Is.False, "the public fund is empty, so public treatment is unavailable");
            Assert.That(choices[1].interactable, Is.True);

            var wallet = me.Wallet;
            var cost = s.Pending.Amount2;
            choices[1].onClick.Invoke();
            yield return Frames();
            Assert.That(s.Pending, Is.Null);
            Assert.That(me.Wallet, Is.EqualTo(wallet - cost));
            Assert.That(me.HospitalTurnsRemaining, Is.GreaterThan(0));
            Assert.That(Sees("HOSPITAL: perde"), Is.True, "the player card shows the hospital status");
            Assert.That(_boot.Session.Engine.TotalCash(), Is.EqualTo(28260));
        }

        [UnityTest]
        public IEnumerator AthleteAbilityModalAdvancesTwoStepsAndMarksTheAbilityAsUsed()
        {
            var s = _boot.Session.S;
            var me = s.Players[0];
            me.ProfessionId = V4Engine.AbilityAtleta;
            yield return LandOn("H03");
            yield return SelectAndConfirm("H03_O03");                   // completes a positive bar

            Assert.That(s.Pending.Kind, Is.EqualTo(DecisionKind.AbilityChoice));
            Assert.That(Sees("Fôlego Extra (Atleta)"), Is.True);
            Assert.That(Click("Avançar duas casas neste ciclo"), Is.True);
            yield return Frames();
            Assert.That(me.AbilityUsedThisYear, Is.True);
            Assert.That(me.Pawns[0].RouteIndex, Is.EqualTo(1), "from base the first step enters index 0, the second reaches index 1");
            Assert.That(Sees("Habilidade do ano: usada"), Is.True);
        }

        [UnityTest]
        public IEnumerator FiscalPanelListsTheEvasionAndUndoReopensTheDecision()
        {
            var s = _boot.Session.S;
            var me = s.Players[0];
            Assert.That(Click("Fiscal"), Is.True);
            yield return Frames();
            Assert.That(Sees("Painel fiscal (pendências)"), Is.True);
            Assert.That(Sees("sem pendências"), Is.True);
            Assert.That(Click("Fechar"), Is.True);
            yield return Frames();
            Assert.That(Sees("Painel fiscal (pendências)"), Is.False);

            yield return LandOn("H25");
            var wallet = me.Wallet;
            yield return SelectAndConfirm("H25_O01");                   // pays the tax
            Assert.That(me.Wallet, Is.LessThan(wallet));
            var fund = s.FundCash;
            Assert.That(fund, Is.GreaterThan(0));

            Assert.That(Click("Desfazer"), Is.True);
            yield return Frames();
            var now = _boot.Session.S;
            Assert.That(now.Players[0].Wallet, Is.EqualTo(wallet), "undo restores the wallet");
            Assert.That(now.FundCash, Is.EqualTo(0));
            Assert.That(now.Pending, Is.Not.Null, "the decision is open again");
            Assert.That(Sees("Última ação desfeita"), Is.True);
            Assert.That(Sees("PRÉVIA"), Is.True, "the decision modal is shown again");

            Assert.That(Click("Voltar ao tabuleiro"), Is.True);
            yield return Frames();
            Assert.That(Click("Histórico"), Is.True);
            yield return Frames();
            Assert.That(Sees("Painel fiscal"), Is.False);
            // the pending decision stays pending while the master reads the history
            Assert.That(Sees("Histórico de eventos"), Is.True);
            Assert.That(Sees("(desfeito)"), Is.True);
            Assert.That(_boot.Session.Engine.TotalCash(), Is.EqualTo(28260));
        }

        [UnityTest]
        public IEnumerator CompletingTheFourthPawnShowsTheEndScreenAndNewMatchReturnsToSetup()
        {
            var s = _boot.Session.S;
            foreach (var pawn in s.Players[0].Pawns) pawn.Completed = true;
            yield return LandOn("H04");
            yield return SelectAndConfirm("H04_O02");

            Assert.That(s.Phase, Is.EqualTo(V4Phase.GameEnd));
            Assert.That(s.WinnerId, Is.EqualTo("p1"));
            Assert.That(Sees("Vitória de Jogador 1!"), Is.True);
            Assert.That(Sees("peões 4/4"), Is.True);
            Assert.That(Click("Nova partida"), Is.True);
            yield return Frames();
            Assert.That(_boot.Session, Is.Null);
            Assert.That(Buttons("INICIAR PARTIDA").Length, Is.EqualTo(1));
            Assert.That(Sees("Vitória de"), Is.False, "the end screen is gone");
        }
    }
}
