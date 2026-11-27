using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>Rule and data changes requested after the 27/11/2026 playtest (approved by the user).</summary>
    public sealed class V4PlaytestChangesTests
    {
        [Test]
        public void CofreAndDirectToHospitalSwappedPositionsLikeThePhysicalBoard()
        {
            var c = V4TestContent.Content;
            Assert.That(c.HouseAt(18).Id, Is.EqualTo("H28"), "position 19 = Cofre público");
            Assert.That(c.HouseAt(27).Id, Is.EqualTo("H19"), "position 28 = Direto para o hospital");
            Assert.That(c.House("H19").Handler, Is.EqualTo("internar"));
            Assert.That(c.House("H28").Handler, Is.EqualTo("consulta_cofre"));
        }

        [Test]
        public void LandingOnPositionNineteenOpensTheCofreAndTwentyEightSendsToHospital()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            g.Cur.Position = 15;
            Assert.That(g.Roll(1, 2).Ok, Is.True);                // 15 + 3 = 18 -> position 19
            Assert.That(g.S.Pending.HouseId, Is.EqualTo("H28"));
            Assert.That(g.Choose(g.S.Pending.OptionIds.Last()).Ok, Is.True);

            g.Cur.Position = 24;
            Assert.That(g.Roll(1, 2).Ok, Is.True);                // 24 + 3 = 27 -> position 28
            Assert.That(g.S.Pending.Kind, Is.EqualTo(DecisionKind.HospitalModality));
            Assert.That(g.P(1).Position, Is.EqualTo(9), "teleported to the hospital (H10)");
        }

        [Test]
        public void PositiveBarAdvancesThreeCellsAndNegativeBarRetreatsOne()
        {
            var g = V4Game.New(new[] { V4Game.Medica, V4Game.Professora });
            Assert.That(g.Land("H03").Ok, Is.True);
            Assert.That(g.Choose("H03_O03").Ok, Is.True);         // +3 Conhecimento x1 = one bar; +2 Cortisol x2 = 4 = one bar
            Assert.That(g.P(0).Pawns[0].RouteIndex, Is.EqualTo(2), "retreat first (no-op at base), then 3 cells from base");
        }

        [Test]
        public void HospitalAlwaysOffersStayingWithoutPayingExceptOnHard()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica }, "B1_facil");
            g.SetFund(500);
            Assert.That(g.Land("H19").Ok, Is.True);
            Assert.That(g.S.Pending.OptionIds, Is.EquivalentTo(new[] { "public", "private", "stay" }));
            var wallet = g.P(0).Wallet;
            Assert.That(g.Hospital("stay").Ok, Is.True);
            Assert.That(g.P(0).HospitalTurnsRemaining, Is.EqualTo(3), "Fácil emergency recovery: 3 turns");
            Assert.That(g.P(0).Wallet, Is.EqualTo(wallet), "nothing is paid");
            Assert.That(g.P(0).Attributes[(int)AttributeId.Dividas].Progress, Is.EqualTo(0m), "Fácil: no Dívidas");
            g.AssertIdentity();

            var medium = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(medium.Land("H19").Ok, Is.True);
            Assert.That(medium.Hospital("stay").Ok, Is.True);
            Assert.That(medium.P(0).HospitalTurnsRemaining, Is.EqualTo(4));
            Assert.That(medium.P(0).Attributes[(int)AttributeId.Dividas].Progress, Is.EqualTo(1m), "Médio: +2 Dívidas base x Professora 2 = 4 = one bar + 1");

            var hard = V4Game.New(new[] { V4Game.Professora, V4Game.Medica }, "B1_dificil");
            Assert.That(hard.Land("H19").Ok, Is.True);
            Assert.That(hard.S.Pending.OptionIds, Does.Not.Contain("stay"), "staying without paying eliminates on Difícil");
        }

        [Test]
        public void YearCloseSummaryRecordsSalaryIncomeTaxAndPartnershipForEachPlayer()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Land("H22").Ok, Is.True);
            Assert.That(g.Choose("H22_O01").Ok, Is.True);         // partnership 50 -> 100 at the close
            g.PlayNeutralUntilRounds(3, payIr: true);
            var lines = g.S.YearSummaries.Where(l => l.Year == 1).ToList();
            Assert.That(lines.Count, Is.EqualTo(2));
            var prof = lines.Single(l => l.PlayerId == "p1");
            Assert.That(prof.Salary, Is.EqualTo(250));
            Assert.That(prof.IncomeTaxDue, Is.EqualTo(50), "Médio 20% of 250");
            Assert.That(prof.IncomeTaxPaid, Is.EqualTo(50));
            Assert.That(prof.IncomeTaxOutcome, Is.EqualTo("pago"));
            Assert.That(prof.PartnershipReturns, Is.EqualTo(100));
        }

        [Test]
        public void PlayerCanJoinOnlyAtTheStartOfAYearAndStartsFromZero()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
            Assert.That(g.Engine.JoinBlockReason(), Is.Null, "before the first roll of year 1");
            Assert.That(g.Land("H01").Ok, Is.True);
            Assert.That(g.Engine.JoinBlockReason(), Is.Not.Null, "someone already played this year");
            var refused = g.Engine.AddPlayer(g.Cmd(), new V4PlayerSetup { Name = "Ana", ProfessionId = V4Game.Atleta, Color = "azul" });
            Assert.That(refused.Ok, Is.False);

            g.PlayNeutralUntilRounds(3, payIr: true);
            Assert.That(g.S.YearIndex, Is.EqualTo(2));
            Assert.That(g.Engine.JoinBlockReason(), Is.Null, "start of year 2");
            var bank = g.S.BankCash;
            var r = g.Engine.AddPlayer(g.Cmd(), new V4PlayerSetup { Name = "Ana", ProfessionId = V4Game.Atleta, Color = "azul" });
            Assert.That(r.Ok, Is.True, r.Error);
            var ana = g.S.Players.Last();
            Assert.That(ana.Name, Is.EqualTo("Ana"));
            Assert.That(ana.Wallet, Is.EqualTo(500));
            Assert.That(ana.Position, Is.EqualTo(0));
            Assert.That(ana.Salary, Is.EqualTo(300));
            Assert.That(ana.Pawns.All(p => p.RouteIndex == -1), Is.True);
            Assert.That(g.S.TurnOrder.Last(), Is.EqualTo(ana.Id), "plays last");
            Assert.That(g.S.BankCash, Is.EqualTo(bank - 500), "paid by the bank");
            g.AssertIdentity();

            var dup = g.Engine.AddPlayer(g.Cmd(), new V4PlayerSetup { Name = "Bia", ProfessionId = V4Game.Atleta, Color = "amarelo" });
            Assert.That(dup.Ok, Is.False, "profession already in use");
        }

        [Test]
        public void LeavingPlayerReturnsTheWalletAndTheTurnMovesOn()
        {
            var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica, V4Game.Economista });
            g.SetWallet(0, 700);
            g.P(0).ReserveBalance = 100;
            g.P(0).Properties = 1; g.P(0).CheckpointRights = 1;
            g.P(0).Pawns[0].RouteIndex = 4;
            var bank = g.S.BankCash;
            var r = g.Engine.RemovePlayer(g.Cmd(), "p1", "foi embora");
            Assert.That(r.Ok, Is.True, r.Error);
            var p = g.P(0);
            Assert.That(p.Left && p.Eliminated, Is.True);
            Assert.That(p.Wallet, Is.EqualTo(0));
            Assert.That(g.S.BankCash, Is.EqualTo(bank + 700));
            Assert.That(p.ReserveBalance + p.Properties + p.CheckpointRights, Is.EqualTo(0));
            Assert.That(p.Pawns.All(x => x.RouteIndex == -1), Is.True, "pieces leave the board");
            Assert.That(g.Cur.Id, Is.EqualTo("p2"), "the leaving player's turn passes on");
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));
            g.AssertIdentity();

            var last = g.Engine.RemovePlayer(g.Cmd(), "p2", null);
            Assert.That(last.Ok, Is.False, "at least two active players must remain");
        }
    }
}
