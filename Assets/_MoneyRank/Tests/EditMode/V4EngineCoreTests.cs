using System.Collections.Generic;
using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>Acceptance scenarios SET*, MOV*, ATR*, LUD* from Docs/V4/05_validacao/cenarios_aceite.csv.</summary>
    public sealed class V4EngineCoreTests
    {
        private static PlayerState Bare(string professionId, string color = "verde")
        {
            var p = new PlayerState { Id = "x", Name = "X", ProfessionId = professionId, Color = color };
            for (var i = 0; i < p.Pawns.Length; i++) p.Pawns[i] = new PawnState();
            for (var i = 0; i < p.Attributes.Length; i++) p.Attributes[i] = new AttributeState();
            return p;
        }

        private static decimal[] D(int k = 0, int i = 0, int c = 0, int t = 0, int co = 0, int d = 0)
            => new decimal[] { k, i, c, t, co, d };

        private static AttributeOutcome Apply(PlayerState p, decimal[] deltas, string difficulty = "B1_medio")
        {
            var c = V4TestContent.Content;
            return V4Attributes.Apply(p, deltas, c.Profession(p.ProfessionId), c.Difficulty(difficulty), c.Rules);
        }

        // ---------------------------------------------------------------- setup

        [Test]
        public void SET01_FourPlayersStartWithTheContractedState()
        {
            var g = V4Game.New(new[] { "P01", "P02", "P03", "P04" });
            Assert.That(g.S.Players.Select(p => p.Wallet), Is.All.EqualTo(500));
            Assert.That(g.S.BankCash, Is.EqualTo(26260));
            Assert.That(g.S.FundCash, Is.EqualTo(0));
            Assert.That(g.S.Players.SelectMany(p => p.Pawns).Count(x => x.RouteIndex == -1 && !x.Completed), Is.EqualTo(16));
            Assert.That(g.S.Players.SelectMany(p => p.Attributes).All(a => a.Progress == 0m && a.LifetimeCycles == 0), Is.True);
            Assert.That(g.S.Players.All(p => p.Position == 0), Is.True);
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));
            g.AssertIdentity();
        }

        [Test]
        public void SET02_InvalidSetupIsBlockedAndValidSetupPersists()
        {
            var engine = new V4Engine(V4TestContent.Content);
            var dup = new[]
            {
                new V4PlayerSetup { Id = "a", ProfessionId = "P01", Color = "verde" },
                new V4PlayerSetup { Id = "b", ProfessionId = "P01", Color = "azul" }
            };
            Assert.That(engine.Setup(dup, new V4Config()).Ok, Is.False);
            Assert.That(engine.State.Players.Count, Is.EqualTo(0));
            var badColor = new[]
            {
                new V4PlayerSetup { Id = "a", ProfessionId = "P01", Color = "verde" },
                new V4PlayerSetup { Id = "b", ProfessionId = "P02", Color = "verde" }
            };
            Assert.That(engine.Setup(badColor, new V4Config()).Ok, Is.False);
            Assert.That(engine.Setup(dup.Take(1).ToList(), new V4Config()).Ok, Is.False, "solo only as demonstration");
            var ok = new[]
            {
                new V4PlayerSetup { Id = "a", ProfessionId = "P01", Color = "verde" },
                new V4PlayerSetup { Id = "b", ProfessionId = "P02", Color = "azul" }
            };
            Assert.That(engine.Setup(ok, new V4Config()).Ok, Is.True);
            Assert.That(engine.State.Players.Count, Is.EqualTo(2));
            Assert.That(engine.Setup(ok, new V4Config()).Ok, Is.False, "cannot set up twice");
        }

        // ---------------------------------------------------------------- movement

        [Test]
        public void MOV01_H36PlusOneLandsOnStartWithoutSalary()
        {
            var g = V4Game.New(new[] { "P01", "P02" });
            g.Cur.Position = 33;
            var wallet = g.Cur.Wallet;
            var year = g.S.YearIndex;
            Assert.That(g.Roll(1, 2).Ok, Is.True);
            Assert.That(g.P(0).Position, Is.EqualTo(0));
            Assert.That(g.P(0).Wallet, Is.EqualTo(wallet));
            Assert.That(g.S.YearIndex, Is.EqualTo(year));
        }

        [Test]
        public void MOV02_InvalidDieIsRejectedWithoutChangingState()
        {
            var g = V4Game.New(new[] { "P01", "P02" });
            var before = StateJson.Write(g.S);
            Assert.That(g.Roll(0, 3).Ok, Is.False);
            Assert.That(g.Roll(3, 0).Ok, Is.False);
            Assert.That(g.Roll(7, 1).Ok, Is.False);
            Assert.That(g.Roll(1, 7).Ok, Is.False);
            Assert.That(StateJson.Write(g.S), Is.EqualTo(before));
        }

        [Test]
        public void MOV03_PassingTheHospitalDoesNotInternAndOnlyDestinationTriggers()
        {
            var g = V4Game.New(new[] { "P01", "P02" });
            g.Cur.Position = 7; // H08 -> 1+2 passes H10 and lands on H11 (IPVA, exempt)
            Assert.That(g.Roll(1, 2).Ok, Is.True);
            Assert.That(g.P(0).HospitalTurnsRemaining, Is.EqualTo(0));
            Assert.That(g.P(0).HospitalPending, Is.False);
            Assert.That(g.P(0).Position, Is.EqualTo(10));
            Assert.That(g.S.Pending, Is.Null, "IPVA without vehicle is exempt: no event, turn passes");
        }

        [Test]
        public void MOV04_DirectToHospitalTeleportsToH10AndAsksForTreatmentWithoutSecondEvent()
        {
            var g = V4Game.New(new[] { "P01", "P02" });
            Assert.That(g.Land("H19").Ok, Is.True);
            Assert.That(g.S.Pending.Kind, Is.EqualTo(DecisionKind.HospitalModality));
            Assert.That(g.P(0).Position, Is.EqualTo(9));
            Assert.That(g.S.Pending.OptionIds, Is.EquivalentTo(new[] { "private", "stay" }), "cofre starts at zero: private, or stay without paying (playtest option)");
            Assert.That(g.Hospital("private").Ok, Is.True);
            Assert.That(g.P(0).HospitalTurnsRemaining, Is.EqualTo(1));
            Assert.That(g.P(0).Wallet, Is.EqualTo(250));
            Assert.That(g.P(0).Position, Is.EqualTo(9));
            Assert.That(g.Cur.Id, Is.EqualTo("p2"), "turn passes on");
            g.AssertIdentity();
        }

        // ---------------------------------------------------------------- attributes

        [Test]
        public void ATR01_MediumNoModifierPlusFourInvestimentoMakesOneCycleRemainderOne()
        {
            var p = Bare("P01");
            var o = Apply(p, D(i: 4));
            Assert.That(o.Cycles[(int)AttributeId.Investimento], Is.EqualTo(1));
            Assert.That(p.Attributes[(int)AttributeId.Investimento].Progress, Is.EqualTo(1m));
        }

        [Test]
        public void ATR02_EasyProfessoraPlusOneConhecimentoIsThreeEffectivePoints()
        {
            var p = Bare("P01");
            var o = Apply(p, D(k: 1), "B1_facil");   // 1 x Professora 2 x Fácil 2 = 4 (integer multipliers since the playtest)
            Assert.That(o.Cycles[(int)AttributeId.Conhecimento], Is.EqualTo(1));
            Assert.That(p.Attributes[(int)AttributeId.Conhecimento].Progress, Is.EqualTo(1m));
        }

        [Test]
        public void ATR03_ReductionIsFlooredAtZeroAndKeepsCyclesAndPosition()
        {
            var p = Bare("P01");
            p.Attributes[(int)AttributeId.Cortisol].Progress = 0.75m;
            p.Attributes[(int)AttributeId.Cortisol].LifetimeCycles = 1;
            p.Pawns[0].RouteIndex = 7;
            Apply(p, D(co: -3));
            Assert.That(p.Attributes[(int)AttributeId.Cortisol].Progress, Is.EqualTo(0m));
            Assert.That(p.Attributes[(int)AttributeId.Cortisol].LifetimeCycles, Is.EqualTo(1));
            Assert.That(p.Pawns[0].RouteIndex, Is.EqualTo(7));
        }

        [Test]
        public void ATR04_TwoToxinaCyclesAndTwoCortisolCyclesDoNotIntern()
        {
            var p = Bare("P01");
            Apply(p, D(t: 6, co: 6));
            Assert.That(p.Attributes[(int)AttributeId.Toxina].HealthCyclesSinceDischarge, Is.EqualTo(2));
            Assert.That(p.Attributes[(int)AttributeId.Cortisol].HealthCyclesSinceDischarge, Is.EqualTo(2));
            Assert.That(p.HospitalPending, Is.False);
        }

        [Test]
        public void ATR05_ThirdCortisolCycleTriggersHospitalOnceWithOneRetreatPerCycle()
        {
            var p = Bare("P01");
            var o = Apply(p, D(co: 9));
            Assert.That(o.Cycles[(int)AttributeId.Cortisol], Is.EqualTo(3));
            Assert.That(o.HospitalTriggered, Is.True);
            Assert.That(p.HospitalPending, Is.True);
            var again = Apply(p, D(co: 0));
            Assert.That(again.HospitalTriggered, Is.False);
        }

        [Test]
        public void ATR06_ThreeDividasBarsRetreatThreeTimesAndNeverIntern()
        {
            var g = V4Game.New(new[] { "P03", "P02" });
            var p = g.P(0);
            p.Pawns[0].RouteIndex = 10; p.Pawns[0].MaxIndex = 10;
            g.SetWallet(0, 0);
            // Economista Dividas multiplier 1 (integer since the playtest): 9 points = 3 bars
            var o = V4Attributes.Apply(p, new decimal[] { 0, 0, 0, 0, 0, 9 }, g.Engine.ProfessionOf(p), g.Engine.Difficulty, g.Engine.Rules);
            Assert.That(o.Cycles[(int)AttributeId.Dividas], Is.EqualTo(3));
            Assert.That(p.HospitalPending, Is.False);
        }

        [Test]
        public void ReductionsAreNotMultipliedAndGainsAreExact()
        {
            var p = Bare("P05"); // Empreendedora Cortisol x2 (integer since the playtest)
            Apply(p, D(co: 1));                 // 1 * 2 * Médio 1 = 2 whole points
            Assert.That(p.Attributes[(int)AttributeId.Cortisol].Progress, Is.EqualTo(2m));
            Apply(p, D(co: -1));                // reductions are not multiplied: exactly one point
            Assert.That(p.Attributes[(int)AttributeId.Cortisol].Progress, Is.EqualTo(1m));
        }

        // ---------------------------------------------------------------- ludo

        private static (V4ContentSet c, PlayerState green, PlayerState red) Pair()
        {
            var c = V4TestContent.Content;
            var green = Bare("P01", "verde"); green.Id = "g";
            var red = Bare("P02", "vermelho"); red.Id = "r";
            return (c, green, red);
        }

        [Test]
        public void LUD01_FirstPositiveCycleOnlyReleasesTheFirstPawn()
        {
            var (c, green, red) = Pair();
            var r = V4Ludo.Advance(c, green, new[] { green, red }, 1);
            Assert.That(r.Moved, Is.True);
            Assert.That(green.Pawns[0].RouteIndex, Is.EqualTo(0), "one step from base lands on the entry");
            Assert.That(green.Pawns.Skip(1).All(x => x.RouteIndex == -1), Is.True);
        }

        [Test]
        public void LUD02_SurplusBeyondTheGoalIsDiscardedNotCarriedToTheNextPawn()
        {
            var (c, green, red) = Pair();
            green.Pawns[0].RouteIndex = 55;
            var r = V4Ludo.Advance(c, green, new[] { green, red }, 2);
            Assert.That(r.PawnCompleted, Is.True);
            Assert.That(green.Pawns[0].Completed, Is.True);
            Assert.That(green.Pawns[0].RouteIndex, Is.EqualTo(56));
            Assert.That(green.Pawns[1].RouteIndex, Is.EqualTo(-1));
            Assert.That(green.ActivePawn, Is.EqualTo(1));
        }

        [Test]
        public void LUD03_FourthPawnCompletedMeansVictoryAfterResolution()
        {
            var g = V4Game.New(new[] { "P01", "P02" });
            var p = g.P(0);
            for (var i = 0; i < 3; i++) { p.Pawns[i].Completed = true; p.Pawns[i].RouteIndex = 56; }
            p.Pawns[3].RouteIndex = 55; p.Pawns[3].MaxIndex = 55;
            p.ActivePawn = 3;
            Assert.That(g.Land("H03").Ok, Is.True);
            Assert.That(g.Choose("H03_O03").Ok, Is.True); // +3 Conhecimento (x1.5) -> a cycle -> advance
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.GameEnd));
            Assert.That(g.S.WinnerId, Is.EqualTo("p1"));
        }

        [Test]
        public void LUD04_CaptureBeforeAnyCheckpointSendsThePawnToBase()
        {
            var (c, green, red) = Pair();
            int gi, ri;
            var cell = V4Game.FirstSharedGlobalWithDifferentIndex(c, "verde", "vermelho", false, out gi, out ri);
            Assert.That(cell, Is.Not.Null);
            red.Pawns[0].RouteIndex = ri; red.Pawns[0].MaxIndex = ri;
            green.Pawns[0].RouteIndex = gi - 1; green.Pawns[0].MaxIndex = gi - 1;
            var r = V4Ludo.Advance(c, green, new[] { green, red }, 1);
            Assert.That(r.CapturedPlayerIds, Is.EquivalentTo(new[] { "r" }));
            Assert.That(red.Pawns[0].RouteIndex, Is.EqualTo(-1));
        }

        [Test]
        public void LUD05_CaptureAfterTwoCheckpointsReturnsToTheMostAdvancedReachedOne()
        {
            var (c, green, red) = Pair();
            int gi, ri;
            V4Game.FirstSharedGlobalWithDifferentIndex(c, "verde", "vermelho", false, out gi, out ri);
            red.Checkpoints.AddRange(new[] { 3, ri - 1 });
            red.Pawns[0].RouteIndex = ri; red.Pawns[0].MaxIndex = ri;
            green.Pawns[0].RouteIndex = gi - 1;
            V4Ludo.Advance(c, green, new[] { green, red }, 1);
            Assert.That(red.Pawns[0].RouteIndex, Is.EqualTo(ri - 1), "returns to best checkpoint, never forward");
        }

        [Test]
        public void LUD06_ProtectedAndPrivateCellsNeverCapture()
        {
            var (c, green, red) = Pair();
            int gi, ri;
            var cell = V4Game.FirstSharedGlobalWithDifferentIndex(c, "verde", "vermelho", true, out gi, out ri);
            Assert.That(cell, Is.Not.Null, "there must be shared protected cells");
            red.Pawns[0].RouteIndex = ri; red.Pawns[0].MaxIndex = ri;
            green.Pawns[0].RouteIndex = gi - 1;
            var r = V4Ludo.Advance(c, green, new[] { green, red }, 1);
            Assert.That(r.CapturedPlayerIds, Is.Empty);
            Assert.That(red.Pawns[0].RouteIndex, Is.EqualTo(ri));
            // private corridors of different colours have distinct global ids
            var greenPrivate = c.RouteOf("verde").Where(x => x.Kind == "privado").Select(x => x.GlobalCell);
            var redPrivate = c.RouteOf("vermelho").Where(x => x.Kind == "privado").Select(x => x.GlobalCell);
            Assert.That(greenPrivate.Intersect(redPrivate), Is.Empty);
        }

        [Test]
        public void LUD07_NormalRegressionMayDropBelowCheckpointAndNeverReopensACompletedPawn()
        {
            var (c, green, red) = Pair();
            green.Checkpoints.Add(10);
            green.Pawns[0].RouteIndex = 10; green.Pawns[0].MaxIndex = 10;
            V4Ludo.Retreat(green);
            Assert.That(green.Pawns[0].RouteIndex, Is.EqualTo(9));
            green.Pawns[0].RouteIndex = 56; green.Pawns[0].Completed = true;
            V4Ludo.Retreat(green);
            Assert.That(green.Pawns[0].Completed, Is.True);
            Assert.That(green.Pawns[0].RouteIndex, Is.EqualTo(56));
            Assert.That(green.Pawns[1].RouteIndex, Is.EqualTo(-1), "retreat in base is a no-op");
        }
    }
}
