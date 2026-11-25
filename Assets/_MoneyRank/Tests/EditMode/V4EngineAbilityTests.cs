using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>Acceptance scenarios SAVE03, NET02, plus Atleta/Empreendedora abilities and cash-identity guards.</summary>
    public sealed class V4EngineAbilityTests
    {
        [Test]
        public void SAVE03_UndoOfATaxRestoresBalancesAndAttributesButKeepsTheAuditTrail()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H25").Ok, Is.True);
            Assert.That(g.Choose("H25_O01").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(470));
            Assert.That(g.S.FundCash, Is.EqualTo(30));
            Assert.That(g.P(0).Attributes[(int)AttributeId.Cidadania].Progress, Is.GreaterThan(0m));
            var ledgerBefore = g.S.Ledger.Count;
            Assert.That(g.Engine.CanUndo, Is.True);
            Assert.That(g.Engine.Undo(g.Cmd(), "mestre escolheu errado").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(500));
            Assert.That(g.S.FundCash, Is.EqualTo(0));
            Assert.That(g.P(0).Attributes[(int)AttributeId.Cidadania].Progress, Is.EqualTo(0m));
            Assert.That(g.S.Pending, Is.Not.Null, "the tax decision is open again");
            Assert.That(g.S.Ledger.Count, Is.EqualTo(ledgerBefore), "reverted entries remain in the ledger");
            Assert.That(g.S.Ledger.Any(l => l.Reversed), Is.True);
            Assert.That(g.S.History.Any(h => h.Undone), Is.True);
            Assert.That(g.S.History.Any(h => h.Kind == "undo"), Is.True);
            g.AssertIdentity();
            // and the choice can now be made again
            Assert.That(g.Choose("H25_O01").Ok, Is.True);
            Assert.That(g.S.FundCash, Is.EqualTo(30));
        }

        [Test]
        public void UndoIsForbiddenOnline()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica }, mode: "online");
            g.Land("H02");
            Assert.That(g.Engine.Undo(g.Cmd(), "x").Ok, Is.False);
        }

        [Test]
        public void NET02_ReconnectRestoresStateWithoutRerollingOrDuplicatingEffects()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            var r = g.Engine.RegisterRoll("roll-1", "p1", 1, 2);
            Assert.That(r.Ok, Is.True);
            var json = StateJson.Write(g.S);
            var restored = new V4Engine(V4TestContent.Content, StateJson.Read<V4State>(json));
            var again = restored.RegisterRoll("roll-1", "p1", 3, 2);
            Assert.That(again.Ok && again.Duplicate, Is.True, "the same command is a no-op after reconnect");
            Assert.That(StateJson.Write(restored.State), Is.EqualTo(json));
            Assert.That(restored.State.LastDieValue, Is.EqualTo("1+2"));
        }

        [Test]
        public void AthleteChoosesAnExtraStepOnceAndDecliningDoesNotConsumeTheAbility()
        {
            var g = V4Game.New(new[] { V4Game.Atleta, V4Game.Professora });
            Assert.That(g.Land("H03").Ok, Is.True);
            Assert.That(g.Choose("H03_O03").Ok, Is.True); // +3 Conhecimento x1 = one cycle
            Assert.That(g.S.Pending.Kind, Is.EqualTo(DecisionKind.AbilityChoice));
            Assert.That(g.S.Pending.OptionIds, Does.Contain("advance2"));
            var decline = g.Engine.ChooseAbility(g.Cmd(), g.S.Pending.Id, "decline");
            Assert.That(decline.Ok, Is.True, decline.Error);
            Assert.That(g.P(0).AbilityUsedThisYear, Is.False);
            Assert.That(g.P(0).Pawns[0].RouteIndex, Is.EqualTo(0), "normal single step from base");

            g.TurnOf(0);
            g.P(0).Attributes[(int)AttributeId.Cortisol].Progress = 0.5m;
            Assert.That(g.Land("H03").Ok, Is.True);
            Assert.That(g.Choose("H03_O03").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Does.Contain("reduce:cortisol"));
            Assert.That(g.Engine.ChooseAbility(g.Cmd(), g.S.Pending.Id, "advance2").Ok, Is.True);
            Assert.That(g.P(0).AbilityUsedThisYear, Is.True);
            Assert.That(g.P(0).Pawns[0].RouteIndex, Is.EqualTo(2));
        }

        [Test]
        public void EntrepreneurCanSwapAnUnfavourableChoiceOncePerYearButNotTaxes()
        {
            var g = V4Game.New(new[] { V4Game.Empreendedora, V4Game.Professora });
            Assert.That(g.Land("H02").Ok, Is.True);
            var d = g.S.Pending;
            var r = g.Engine.SwapWithEntrepreneur(g.Cmd(), d.Id, "p1", "H02_O02", "H02_O04");
            Assert.That(r.Ok, Is.True, r.Error);
            Assert.That(g.P(0).Wallet, Is.EqualTo(500), "swapped to the decline option");
            Assert.That(g.P(0).AbilityUsedThisYear, Is.True);
            g.TurnOf(0);
            Assert.That(g.Land("H02").Ok, Is.True);
            Assert.That(g.Engine.SwapWithEntrepreneur(g.Cmd(), g.S.Pending.Id, "p1", "H02_O02", "H02_O04").Ok, Is.False, "once per year");
            g.P(0).AbilityUsedThisYear = false;
            g.Choose("H02_O04");
            g.TurnOf(0);
            Assert.That(g.Land("H25").Ok, Is.True);
            Assert.That(g.Engine.SwapWithEntrepreneur(g.Cmd(), g.S.Pending.Id, "p1", "H25_O01", "H25_O02").Ok, Is.False, "taxes are never swappable");
        }

        [Test]
        public void EssentialExpenseCannotBeEscapedAndPreviewDoesNotMutate()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H15").Ok, Is.True);
            Assert.That(g.S.Pending.Mandatory, Is.True);
            Assert.That(g.S.Pending.OptionIds.Any(o => o.EndsWith("O04")), Is.False, "no free escape while a paid option is accessible");
            var before = StateJson.Write(g.S);
            var preview = g.Engine.PreviewOption(g.S.Pending.Id, "p1", "H15_O01");
            Assert.That(preview.Ok, Is.True);
            Assert.That(preview.WalletDelta, Is.EqualTo(-50));
            Assert.That(preview.BankDelta, Is.EqualTo(50));
            Assert.That(StateJson.Write(g.S), Is.EqualTo(before), "preview never changes the real state");
        }

        [Test]
        public void CashIdentityHoldsAcrossAWholeVariedPlaythrough()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica, V4Game.Economista, V4Game.Motorista });
            var houses = new[] { "H02", "H05", "H09", "H12", "H14", "H18", "H22", "H25", "H36", "H31", "H17", "H33", "H23", "H26" };
            var i = 0;
            var guard = 0;
            while (g.S.Phase != V4Phase.GameEnd && g.S.CompletedRounds < 6 && guard++ < 400)
            {
                if (g.S.Phase == V4Phase.AwaitDecision)
                {
                    var d = g.S.Pending;
                    V4Result res;
                    if (d.Kind == DecisionKind.Choice) res = g.Choose(d.OptionIds[(i++) % d.OptionIds.Count]);
                    else if (d.Kind == DecisionKind.TaxCoin || d.Kind == DecisionKind.FiscalCoin) res = g.Coin(i++ % 2 == 0);
                    else if (d.Kind == DecisionKind.HospitalModality) res = g.Hospital(d.OptionIds[0]);
                    else res = g.Engine.ChooseAbility(g.Cmd(), d.Id, d.OptionIds.Last());
                    Assert.That(res.Ok, Is.True, res.Error);
                }
                else
                {
                    var r = g.Land(houses[(i++) % houses.Length]);
                    Assert.That(r.Ok, Is.True, r.Error);
                }
                g.AssertIdentity();
                Assert.That(g.S.Players.All(p => p.Wallet >= 0), Is.True);
                Assert.That(g.S.BankCash, Is.GreaterThanOrEqualTo(0));
                Assert.That(g.S.FundCash, Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(g.S.Ledger.Count, Is.GreaterThan(10));
        }
    }
}
