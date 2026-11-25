using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>Helpers shared by the V4 engine tests. Drives the engine only through its public commands.</summary>
    public sealed class V4Game
    {
        public V4Engine Engine;
        private int _cmd;
        public V4State S => Engine.State;
        public PlayerState P(int i) => S.Players[i];
        public PlayerState Cur => Engine.CurrentPlayer;

        public static readonly string[] Prof = { "P01", "P02", "P03", "P04", "P05", "P06" };
        public const string Professora = "P01", Medica = "P02", Economista = "P03", Atleta = "P04", Empreendedora = "P05", Motorista = "P06";

        public static V4Game New(string[] professions, string difficulty = "B1_medio", string mode = "local")
        {
            var g = new V4Game { Engine = new V4Engine(V4TestContent.Content) };
            var setup = professions.Select((pr, i) => new V4PlayerSetup { Id = "p" + (i + 1), Name = "J" + (i + 1), ProfessionId = pr, Color = V4Engine.Colors[i] }).ToList();
            var r = g.Engine.Setup(setup, new V4Config { DifficultyId = difficulty, Mode = mode }, "test-session");
            Assert.That(r.Ok, Is.True, r.Error);
            return g;
        }

        public string Cmd() => "c" + (++_cmd);

        public V4Result Roll(int die1, int die2) => Engine.RegisterRoll(Cmd(), Cur.Id, die1, die2);

        /// <summary>Puts the current player three steps before the house and rolls 1+2 (not doubles) to land exactly there.</summary>
        public V4Result Land(string houseId)
        {
            var idx = V4TestContent.Content.House(houseId).UnityIndex;
            Cur.Position = (idx + 33) % 36;
            return Roll(1, 2);
        }

        public V4Result Choose(string optionId) => Engine.ChooseOption(Cmd(), S.Pending.Id, S.Pending.PlayerId, optionId);
        public V4Result Coin(bool happy) => Engine.RegisterCoin(Cmd(), S.Pending.Id, S.Pending.PlayerId, happy);
        public V4Result Hospital(string modality) => Engine.ChooseHospital(Cmd(), S.Pending.Id, S.Pending.PlayerId, modality);

        /// <summary>Test-only cash setup that preserves the 28.260 identity (moves coins bank &lt;-&gt; wallet).</summary>
        public void SetWallet(int playerIndex, int amount)
        {
            var p = P(playerIndex);
            var diff = amount - p.Wallet;
            p.Wallet = amount;
            S.BankCash -= diff;
            Assert.That(S.BankCash, Is.GreaterThanOrEqualTo(0));
        }

        public void SetFund(int amount)
        {
            var diff = amount - S.FundCash;
            S.FundCash = amount;
            S.BankCash -= diff;
        }

        /// <summary>Plays neutral turns (decisions get their last option) until it is the given player's turn.</summary>
        public void TurnOf(int playerIndex)
        {
            var guard = 0;
            while (S.Phase != V4Phase.GameEnd && !(S.Phase == V4Phase.AwaitRoll && Cur.Id == P(playerIndex).Id) && guard++ < 50)
            {
                if (S.Phase == V4Phase.AwaitDecision) { AnswerPending(); continue; }
                Assert.That(Land("H01").Ok, Is.True);
            }
        }

        /// <summary>Answers whatever is pending in the cheapest neutral way (pay taxes, happy coin, first hospital option).</summary>
        public void AnswerPending()
        {
            var d = S.Pending;
            V4Result r;
            switch (d.Kind)
            {
                case DecisionKind.TaxCoin:
                case DecisionKind.FiscalCoin: r = Coin(true); break;
                case DecisionKind.HospitalModality: r = Hospital(d.OptionIds[0]); break;
                case DecisionKind.AbilityChoice: r = Engine.ChooseAbility(Cmd(), d.Id, d.OptionIds[d.OptionIds.Count - 1]); break;
                default:
                    var isTax = d.Tag != null && d.Tag.StartsWith("tax:");
                    r = Choose(isTax ? d.OptionIds[0] : d.OptionIds[d.OptionIds.Count - 1]);
                    break;
            }
            Assert.That(r.Ok, Is.True, r.Error);
        }

        public void AssertIdentity() => Assert.That(Engine.TotalCash(), Is.EqualTo(28260));

        /// <summary>Plays neutral turns (landing on H01) until the target number of completed rounds, paying IR when asked.</summary>
        public void PlayNeutralUntilRounds(int rounds, bool payIr = true)
        {
            var guard = 0;
            // keep going until the rounds are done AND any annual close (IR decisions) has fully finished
            while ((S.CompletedRounds < rounds || S.Phase != V4Phase.AwaitRoll) && S.Phase != V4Phase.GameEnd && guard++ < 200)
            {
                if (S.Phase == V4Phase.AwaitDecision)
                {
                    var d = S.Pending;
                    if (d.Kind == DecisionKind.Choice && d.Tag != null && d.Tag.StartsWith("tax:"))
                        Assert.That(Choose(d.OptionIds[payIr ? 0 : d.OptionIds.Count - 1]).Ok, Is.True);
                    else Assert.Fail("Unexpected decision " + d.Kind + " while playing neutral turns");
                    continue;
                }
                if (S.Phase == V4Phase.AwaitRoll) { var r = Land("H01"); Assert.That(r.Ok, Is.True, r.Error); continue; }
                Assert.Fail("Unexpected phase " + S.Phase);
            }
        }

        public static string FirstSharedGlobalWithDifferentIndex(V4ContentSet c, string colorA, string colorB, bool wantProtected, out int idxA, out int idxB)
        {
            var a = c.RouteOf(colorA); var b = c.RouteOf(colorB);
            foreach (var ca in a.Where(x => x.IsShared && x.Protected == wantProtected))
            {
                var cb = b.FirstOrDefault(x => x.IsShared && x.GlobalCell == ca.GlobalCell);
                if (cb != null && cb.Index != ca.Index) { idxA = ca.Index; idxB = cb.Index; return ca.GlobalCell; }
            }
            idxA = idxB = -1;
            return null;
        }
    }
}
