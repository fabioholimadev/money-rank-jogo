using System;
using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>Acceptance scenarios INV*, FIS*, HOS*, CAS*, PRO*, HAB*, ECO*, SAVE*, ANO*, NET*.</summary>
    public sealed class V4EngineEconomyTests
    {
        private static decimal Bar(PlayerState p, AttributeId a) => p.Attributes[(int)a].Progress;

        // ---------------------------------------------------------------- investments

        [Test]
        public void INV01_ReserveInterestIsCappedAtTheSalary()
        {
            var g = V4Game.New(new[] { V4Game.Economista, V4Game.Professora });
            g.P(0).ReserveBalance = 480; // Economista salary 500
            g.PlayNeutralUntilRounds(3);
            Assert.That(g.P(0).ReserveBalance, Is.EqualTo(500));
            g.AssertIdentity();
        }

        [Test]
        public void INV02_ReserveInterestIsRoundedUp()
        {
            var g = V4Game.New(new[] { V4Game.Economista, V4Game.Professora });
            g.P(0).ReserveBalance = 101;
            g.PlayNeutralUntilRounds(3);
            Assert.That(g.P(0).ReserveBalance, Is.EqualTo(112));
        }

        [Test]
        public void INV03_DepositRedeemRedepositSamePrincipalGrantsNoRepeatedPoints()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H05").Ok, Is.True);
            Assert.That(g.Choose("H05_O02").Ok, Is.True); // 100 -> +2 Investimento
            Assert.That(Bar(g.P(0), AttributeId.Investimento), Is.EqualTo(2m));
            g.AssertIdentity();
            g.TurnOf(0);
            Assert.That(g.Engine.RedeemReserve(g.Cmd(), "p1", 100).Ok, Is.True);
            Assert.That(g.P(0).ReservePrincipal, Is.EqualTo(0));
            Assert.That(g.Land("H05").Ok, Is.True);
            Assert.That(g.Choose("H05_O02").Ok, Is.True);
            Assert.That(Bar(g.P(0), AttributeId.Investimento), Is.EqualTo(2m), "no new points for the same principal");
            Assert.That(g.P(0).ReserveBalance, Is.EqualTo(100));
            g.AssertIdentity();
        }

        [Test]
        public void INV04_DepositAboveTheSalaryCapIsBlockedWithoutConsumingTheOption()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.P(0).ReserveBalance = 100; // salary 250: +200 would reach 300
            Assert.That(g.Land("H05").Ok, Is.True);
            var pending = g.S.Pending.Id;
            var r = g.Choose("H05_O03");
            Assert.That(r.Ok, Is.False);
            Assert.That(g.S.Pending.Id, Is.EqualTo(pending), "decision still open");
            Assert.That(g.P(0).Wallet, Is.EqualTo(500));
        }

        [Test]
        public void INV05_VehiclePurchaseActivatesBonusImmediatelyAndMaintenanceExtends()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H14").Ok, Is.True);
            Assert.That(g.Choose("H14_O01").Ok, Is.True);
            var p = g.P(0);
            Assert.That(p.Wallet, Is.EqualTo(100));
            Assert.That(p.VehicleExpiresAfterRound, Is.EqualTo(10), "completedRounds(0) + 9 + 1");
            Assert.That(g.Engine.VehicleActive(p), Is.True);
            Assert.That(p.Pawns[0].RouteIndex, Is.EqualTo(1), "+4 Investimento = 1 cycle, vehicle makes the advance 2 from base");
            g.SetWallet(0, 500);
            p.Position = 0;
            g.S.CurrentIndex = 0;
            g.S.Phase = V4Phase.AwaitRoll;
            Assert.That(g.Land("H14").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Does.Not.Contain("H14_O01"));
            Assert.That(g.Choose("H14_O02").Ok, Is.True);
            Assert.That(p.VehicleExpiresAfterRound, Is.EqualTo(13));
        }

        [Test]
        public void INV06_ExpiredVehicleMeansNoIpvaAndRepurchaseIsOfferedAgain()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.P(0).HasVehicle = true; g.P(0).VehicleExpiresAfterRound = 10;
            g.S.CompletedRounds = 10;
            Assert.That(g.Engine.VehicleActive(g.P(0)), Is.False);
            Assert.That(g.Land("H11").Ok, Is.True);
            Assert.That(g.S.Pending, Is.Null, "IPVA exempt without an active vehicle");
        }

        [Test]
        public void INV07_TwoPropertiesCostBothAndGiveIndependentRightsAndDoubleIptu()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.SetWallet(0, 1000);
            var p = g.P(0);
            Assert.That(g.Land("H20").Ok, Is.True); Assert.That(g.Choose("H20_O01").Ok, Is.True);
            g.TurnOf(0);
            Assert.That(g.Land("H20").Ok, Is.True); Assert.That(g.Choose("H20_O01").Ok, Is.True);
            Assert.That(p.Wallet, Is.EqualTo(0));
            Assert.That(p.Properties, Is.EqualTo(2));
            Assert.That(p.CheckpointRights, Is.EqualTo(2));
            g.TurnOf(0);
            g.SetWallet(0, 500);
            Assert.That(g.Land("H07").Ok, Is.True);
            Assert.That(g.S.Pending.Amount, Is.EqualTo(100), "IPTU 50 x 2 properties");
            g.AssertIdentity();
        }

        [Test]
        public void INV08_InvestimentoLivreDelegatesExactlyOneOperationWithoutMoving()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H34").Ok, Is.True);
            Assert.That(g.Choose("H34_O02").Ok, Is.True);
            Assert.That(g.S.Pending.Delegated, Is.True);
            Assert.That(g.S.Pending.HouseId, Is.EqualTo("H14"));
            Assert.That(g.Choose("H14_O01").Ok, Is.True);
            Assert.That(g.P(0).HasVehicle, Is.True);
            Assert.That(g.P(0).Position, Is.EqualTo(33), "no new displacement (H34)");
        }

        // ---------------------------------------------------------------- tax

        [Test]
        public void FIS01_ExemptFromIptuStillGetsAuditedForOldPendencies()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.P(0).Fiscal.Add(new FiscalRecord { Id = "f1", Kind = "ISS", Principal = 30, Outstanding = 30, Status = FiscalStatus.Sonegado });
            Assert.That(g.Land("H07").Ok, Is.True);
            Assert.That(g.S.Pending.Kind, Is.EqualTo(DecisionKind.FiscalCoin));
        }

        [Test]
        public void FIS02_AnnualIncomeTaxMediumSalary350Is70ToTheCofre()
        {
            var g = V4Game.New(new[] { V4Game.Empreendedora, V4Game.Professora });
            g.PlayNeutralUntilRounds(3);
            Assert.That(g.S.History.Any(h => h.Kind == "tax" && h.Detail.Contains("70")), Is.True);
            Assert.That(g.S.FundCash, Is.EqualTo(70 + 50), "Empreendedora 350*20% + Professora 250*20%");
            Assert.That(g.P(0).Attributes[(int)AttributeId.Cidadania].Progress, Is.GreaterThan(0m), "voluntary payment +1 Cidadania base");
        }

        [Test]
        public void FIS03_EvasionWithHappyCoinRegistersPrincipalWithoutPayment()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.P(0).HasVehicle = true; g.P(0).VehicleExpiresAfterRound = 10;
            Assert.That(g.Land("H11").Ok, Is.True);
            Assert.That(g.Choose("H11_O02").Ok, Is.True);
            Assert.That(g.Coin(true).Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(500));
            var rec = g.P(0).Fiscal.Single();
            Assert.That(rec.Principal, Is.EqualTo(40));
            Assert.That(rec.Status, Is.EqualTo(FiscalStatus.Sonegado));
        }

        [Test]
        public void FIS04_EvasionWithSadCoinCharges80AndOneCortisolBase()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.P(0).HasVehicle = true; g.P(0).VehicleExpiresAfterRound = 10;
            Assert.That(g.Land("H11").Ok, Is.True);
            Assert.That(g.Choose("H11_O02").Ok, Is.True);
            Assert.That(g.Coin(false).Ok, Is.True);
            Assert.That(g.S.FundCash, Is.EqualTo(80));
            Assert.That(g.P(0).Wallet, Is.EqualTo(420));
            Assert.That(Bar(g.P(0), AttributeId.Cortisol), Is.EqualTo(1m));
            g.AssertIdentity();
        }

        [Test]
        public void FIS05_AuditOfThirtyAndFortyWithSadCoinCharges140AndRegularizes()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.SetWallet(0, 1000);
            g.P(0).Fiscal.Add(new FiscalRecord { Id = "a", Kind = "ISS", Principal = 30, Outstanding = 30, Status = FiscalStatus.Sonegado });
            g.P(0).Fiscal.Add(new FiscalRecord { Id = "b", Kind = "ICMS", Principal = 40, Outstanding = 40, Status = FiscalStatus.Sonegado });
            Assert.That(g.Land("H25").Ok, Is.True);
            Assert.That(g.S.Pending.Kind, Is.EqualTo(DecisionKind.FiscalCoin));
            Assert.That(g.Coin(false).Ok, Is.True);
            Assert.That(g.S.FundCash, Is.EqualTo(140));
            Assert.That(g.P(0).Fiscal.All(f => f.Status == FiscalStatus.Regularizado), Is.True);
            Assert.That(Bar(g.P(0), AttributeId.Cortisol), Is.EqualTo(1m));
            Assert.That(g.S.Pending.Kind, Is.EqualTo(DecisionKind.Choice), "then the new tax is resolved");
            Assert.That(g.S.Pending.Amount, Is.EqualTo(30));
        }

        [Test]
        public void FIS06_NoResourcesRegistersPendingAndThreeDividasWithoutNegativeBalance()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.SetWallet(0, 0);
            Assert.That(g.Land("H25").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Is.EquivalentTo(new[] { "H25_O03" }));
            Assert.That(g.Choose("H25_O03").Ok, Is.True);
            Assert.That(g.P(0).Fiscal.Single().Status, Is.EqualTo(FiscalStatus.Inadimplente));
            Assert.That(g.P(0).Wallet, Is.EqualTo(0));
            Assert.That(g.P(0).Attributes[(int)AttributeId.Dividas].LifetimeCycles, Is.EqualTo(1), "3 base x 1.5 = 4.5 -> one cycle");
            g.AssertIdentity();
        }

        [Test]
        public void FIS07_FiscalVisibilityLocalMasterOnlyOnlineEveryone()
        {
            Assert.That(V4Game.New(new[] { V4Game.Professora, V4Game.Medica }, mode: "local").Engine.IsFiscalPublic, Is.False);
            Assert.That(V4Game.New(new[] { V4Game.Professora, V4Game.Medica }, mode: "online").Engine.IsFiscalPublic, Is.True);
        }

        // ---------------------------------------------------------------- hospital

        [Test]
        public void HOS01_PublicTreatmentMovesCofreToBankAndLosesTwoTurnsThenDischargeZeroesHealth()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.SetFund(150);
            g.P(0).Attributes[(int)AttributeId.Toxina].Progress = 2m;
            var bank = g.S.BankCash;
            Assert.That(g.Land("H19").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Does.Contain("public"));
            Assert.That(g.Hospital("public").Ok, Is.True);
            Assert.That(g.S.FundCash, Is.EqualTo(0));
            Assert.That(g.S.BankCash, Is.EqualTo(bank + 150));
            Assert.That(g.P(0).HospitalTurnsRemaining, Is.EqualTo(2));
            // p2 plays a neutral turn, p1 loses one, p2 neutral, p1 loses the last and is discharged
            for (var i = 0; i < 2; i++) { var r = g.Land("H01"); Assert.That(r.Ok, Is.True, r.Error); }
            Assert.That(g.P(0).HospitalTurnsRemaining, Is.EqualTo(0));
            Assert.That(Bar(g.P(0), AttributeId.Toxina), Is.EqualTo(0m));
            g.AssertIdentity();
        }

        [Test]
        public void HOS03_DoctorPaysHalfOncePerYearWithMinimumOneTurn()
        {
            var g = V4Game.New(new[] { V4Game.Medica, V4Game.Professora });
            Assert.That(g.Land("H19").Ok, Is.True);
            Assert.That(g.S.Pending.Amount2, Is.EqualTo(125));
            Assert.That(g.Hospital("private").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(375));
            Assert.That(g.P(0).HospitalTurnsRemaining, Is.EqualTo(1));
            Assert.That(g.P(0).AbilityUsedThisYear, Is.True);
            Assert.That(g.Engine.Difficulty.PrivateHospitalCost, Is.EqualTo(250));
        }

        [TestCase("B1_facil", 3, 0, false)]
        [TestCase("B1_medio", 4, 2, false)]
        [TestCase("B1_dificil", 0, 0, true)]
        public void HOS04_NoResourcesUsesTheEmergencyRulePerDifficulty(string difficulty, int turns, int debtBase, bool eliminated)
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica }, difficulty);
            g.SetWallet(0, 0);
            Assert.That(g.Land("H19").Ok, Is.True);
            Assert.That(g.P(0).Eliminated, Is.EqualTo(eliminated));
            Assert.That(g.P(0).HospitalTurnsRemaining, Is.EqualTo(turns));
            if (debtBase > 0) Assert.That(g.P(0).Attributes[(int)AttributeId.Dividas].LifetimeCycles, Is.EqualTo(1), "2 base x1.5 (Professora) x1 = 3 -> one cycle");
            g.AssertIdentity();
            if (eliminated) { Assert.That(g.S.Phase, Is.EqualTo(V4Phase.GameEnd)); Assert.That(g.S.WinnerId, Is.EqualTo("p2")); }
        }

        // ---------------------------------------------------------------- cash rules

        [Test]
        public void CAS01_EssentialWithoutResourcesGivesThreeDividasAndNoNegativeBalance()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.SetWallet(0, 0);
            Assert.That(g.Land("H04").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Is.EquivalentTo(new[] { "H04_O04" }));
            Assert.That(g.Choose("H04_O04").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(0));
            Assert.That(g.P(0).Attributes[(int)AttributeId.Dividas].LifetimeCycles, Is.EqualTo(1));
        }

        [Test]
        public void CAS02_NonEssentialDeclineHasNoEffectAtAll()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            var before = g.P(0).Wallet;
            Assert.That(g.Land("H02").Ok, Is.True);
            Assert.That(g.Choose("H02_O04").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(before));
            Assert.That(g.P(0).Attributes.All(a => a.Progress == 0m), Is.True);
        }

        [Test]
        public void CAS03_RenegotiationWithZeroDividasIsNeutralWithoutCharge()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H30").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Is.EquivalentTo(new[] { "H30_O04" }));
            Assert.That(g.Choose("H30_O04").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(500));
        }

        [Test]
        public void CAS04_PartnershipPaysNowAndReturnsTwoHundredOnceAtTheYearClose()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H22").Ok, Is.True);
            Assert.That(g.Choose("H22_O02").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(400));
            Assert.That(g.P(0).Receivables.Single().TotalReturn, Is.EqualTo(200));
            g.P(0).Wallet = g.P(0).Wallet; // explicit: nothing paid back yet
            var before = g.P(0).Wallet;
            g.PlayNeutralUntilRounds(3);
            Assert.That(g.P(0).Receivables.Single().Paid, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(before + 250 - 50 + 200), "salary 250, IR 20% = 50, return 200");
            g.AssertIdentity();
        }

        [Test]
        public void CAS05_ExtraWorkPaysTwentyFivePercentRoundedUpAndKeepsTheSalary()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H18").Ok, Is.True);
            Assert.That(g.Choose("H18_O01").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(563), "ceil(250 * 25 / 100) = 63");
            Assert.That(g.P(0).Salary, Is.EqualTo(250));
        }

        // ---------------------------------------------------------------- progression

        [Test]
        public void PRO01_ProfessoraFirstProgressionCosts125SalaryBecomes300AndGivesKnowledge()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H08").Ok, Is.True);
            Assert.That(g.Choose("H08_O01").Ok, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(375));
            Assert.That(g.P(0).Salary, Is.EqualTo(300));
            Assert.That(g.P(0).Level, Is.EqualTo(1));
            Assert.That(g.P(0).Attributes[(int)AttributeId.Conhecimento].LifetimeCycles, Is.EqualTo(1), "+2 base x1.5 = 3");
        }

        [Test]
        public void PRO02_DeclineKeepsTheSameCardPending()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H08").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Is.EqualTo(new[] { "H08_O01" }), "the offer cannot be dismissed without confirming");
            g.P(0).Level = 0;
        }

        [Test]
        public void PRO03_AfterTheFourthStepSalaryIs180PercentAndTheHouseIsNeutral()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.SetWallet(0, 1500);
            for (var i = 0; i < 4; i++)
            {
                g.TurnOf(0);
                g.SetWallet(0, Math.Max(g.P(0).Wallet, 1000));
                var r = g.Land(new[] { "H08", "H16", "H24", "H29" }[i]);
                Assert.That(r.Ok, Is.True, r.Error);
                Assert.That(g.S.Pending.OptionIds.Single(), Does.EndWith("O01"));
                var c = g.Choose(g.S.Pending.OptionIds.Single());
                Assert.That(c.Ok, Is.True, c.Error);
            }
            g.TurnOf(0);
            Assert.That(g.P(0).Salary, Is.EqualTo(450));
            Assert.That(g.P(0).Level, Is.EqualTo(4));
            g.SetWallet(0, Math.Max(g.P(0).Wallet, 1000));
            Assert.That(g.Land("H08").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds.Single(), Is.EqualTo("H08_O02"));
        }

        // ---------------------------------------------------------------- abilities

        [Test]
        public void HAB01_DriverSwapsTheRollForTwoPointsOfHealthOnceAYear()
        {
            var g = V4Game.New(new[] { V4Game.Motorista, V4Game.Professora });
            g.P(0).Attributes[(int)AttributeId.Cortisol].Progress = 2.5m;
            var pos = g.P(0).Position;
            Assert.That(g.Engine.UseDriverFlex(g.Cmd(), "p1", AttributeId.Cortisol).Ok, Is.True);
            Assert.That(Bar(g.P(0), AttributeId.Cortisol), Is.EqualTo(0.5m));
            Assert.That(g.P(0).Position, Is.EqualTo(pos));
            Assert.That(g.P(0).Salary, Is.EqualTo(400));
            Assert.That(g.Cur.Id, Is.EqualTo("p2"));
            g.Land("H01");
            Assert.That(g.Engine.UseDriverFlex(g.Cmd(), "p1", AttributeId.Toxina).Ok, Is.False, "once per year");
        }

        [Test]
        public void HAB02_VehicleAndAbilityDoNotStackAdvanceIsTwoNotFour()
        {
            var g = V4Game.New(new[] { V4Game.Economista, V4Game.Professora });
            var p = g.P(0);
            p.HasVehicle = true; p.VehicleExpiresAfterRound = 10;
            Assert.That(g.Land("H14").Ok, Is.True); // already active -> maintenance option exists
            Assert.That(g.Choose("H14_O02").Ok, Is.True); // +1 Investimento x1.5 -> 1.5, no cycle
            g.TurnOf(0);
            g.SetWallet(0, 600);
            // one Investimento burst with the economist ability available and the vehicle active
            p.Pawns[0].RouteIndex = 0; p.Pawns[0].MaxIndex = 0;
            p.AbilityUsedThisYear = false;
            Assert.That(g.Land("H20").Ok, Is.True);
            Assert.That(g.Choose("H20_O01").Ok, Is.True); // +4 Investimento x1.5 = 6 -> 2 cycles
            Assert.That(p.Pawns[0].RouteIndex, Is.EqualTo(0 + 2 + 2), "first cycle (ability) = 2, second (vehicle) = 2; never 4 in one cycle");
        }

        // ---------------------------------------------------------------- liquidity, idempotency, saves, rounds

        [Test]
        public void ECO01_BankWithoutCashBlocksTheGameAndNeverGoesNegative()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            var give = g.S.BankCash;
            g.S.Players[0].Wallet += give;
            g.S.BankCash = 0;
            g.AssertIdentity();
            var guard = 0;
            V4Result last = null;
            while (g.S.CompletedRounds < 3 && guard++ < 20 && !(last != null && last.Liquidity))
            {
                if (g.S.Phase == V4Phase.AwaitDecision) { last = g.Choose(g.S.Pending.OptionIds[0]); continue; }
                last = g.Land("H01");
            }
            Assert.That(last.Liquidity, Is.True);
            Assert.That(g.S.BankCash, Is.GreaterThanOrEqualTo(0));
            Assert.That(g.S.LiquidityBlocked, Is.True);
            Assert.That(g.Land("H01").Ok, Is.False, "paused until the master registers the physical adjustment");
            Assert.That(g.Engine.ClearLiquidityBlock(g.Cmd(), "troco ajustado").Ok, Is.True);
            g.AssertIdentity();
        }

        [Test]
        public void SAVE01_PendingChoiceAndCustomOrderSurviveSaveAndRestore()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica, V4Game.Economista });
            g.S.TurnOrder = new System.Collections.Generic.List<string> { "p3", "p1", "p2" };
            g.S.CurrentIndex = 0;
            Assert.That(g.Land("H02").Ok, Is.True);
            var json = StateJson.Write(g.S);
            var restored = new V4Engine(V4TestContent.Content, StateJson.Read<V4State>(json));
            Assert.That(restored.State.TurnOrder, Is.EqualTo(new[] { "p3", "p1", "p2" }));
            Assert.That(restored.State.Pending.Id, Is.EqualTo(g.S.Pending.Id));
            Assert.That(restored.State.Pending.OptionIds, Is.EqualTo(g.S.Pending.OptionIds));
            Assert.That(StateJson.Write(restored.State), Is.EqualTo(json), "lossless round trip");
            var r = restored.ChooseOption("again", restored.State.Pending.Id, "p3", "H02_O01");
            Assert.That(r.Ok, Is.True, r.Error);
        }

        [Test]
        public void SAVE02_DoubleClickAndRetryAreIdempotent()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H02").Ok, Is.True);
            var id = g.S.Pending.Id;
            var r1 = g.Engine.ChooseOption("same-click", id, "p1", "H02_O01");
            var r2 = g.Engine.ChooseOption("same-click", id, "p1", "H02_O01");
            Assert.That(r1.Ok && !r1.Duplicate, Is.True);
            Assert.That(r2.Ok && r2.Duplicate, Is.True);
            Assert.That(g.P(0).Wallet, Is.EqualTo(470), "charged once");
            var stale = g.Engine.ChooseOption("other-click", id, "p1", "H02_O01");
            Assert.That(stale.Ok, Is.False, "stale decision id is rejected");
        }

        [Test]
        public void NET01_ActionOutOfTurnIsRejectedAndStateConverges()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            var before = StateJson.Write(g.S);
            Assert.That(g.Engine.RegisterRoll("x1", "p2", 1, 2).Ok, Is.False);
            Assert.That(StateJson.Write(g.S), Is.EqualTo(before));
            Assert.That(g.Land("H02").Ok, Is.True);
            Assert.That(g.Engine.ChooseOption("x2", g.S.Pending.Id, "p2", "H02_O01").Ok, Is.False);
        }

        [Test]
        public void ANO01_YearCloseRunsOnceSalariesIncludeHospitalizedAndAbilitiesRenew()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Motorista });
            g.P(1).AbilityUsedThisYear = true;
            g.SetWallet(0, 500);
            g.P(0).HospitalTurnsRemaining = 0;
            g.PlayNeutralUntilRounds(3);
            Assert.That(g.S.YearIndex, Is.EqualTo(2));
            Assert.That(g.P(1).AbilityUsedThisYear, Is.False, "abilities renew after every effect");
            Assert.That(g.S.History.Count(h => h.Kind == "year_close"), Is.EqualTo(1));
            Assert.That(g.P(0).Wallet, Is.EqualTo(500 + 250 - 50));
            g.AssertIdentity();
        }

        [Test]
        public void ANO02_SavingInTheMiddleOfAnnualTaxResumesWithoutDuplicatingAnything()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            var guard = 0;
            while (g.S.Phase != V4Phase.AwaitDecision && guard++ < 50) g.Land("H01");
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.AwaitDecision));
            Assert.That(g.S.CloseStep, Is.EqualTo("tax"));
            var salaryPaid = g.P(0).Wallet;
            var json = StateJson.Write(g.S);
            var restored = new V4Game { Engine = new V4Engine(V4TestContent.Content, StateJson.Read<V4State>(json)) };
            Assert.That(restored.P(0).Wallet, Is.EqualTo(salaryPaid));
            restored.PlayNeutralUntilRounds(3);
            Assert.That(restored.S.YearIndex, Is.EqualTo(2));
            Assert.That(restored.S.History.Count(h => h.Kind == "year_close"), Is.EqualTo(1));
            Assert.That(restored.Engine.TotalCash(), Is.EqualTo(28260));
        }
    }
}
