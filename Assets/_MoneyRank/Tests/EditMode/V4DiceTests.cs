using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>User rule: two dice for movement; equal dice roll again, at most 3 extra rolls, then the turn passes.</summary>
    public sealed class V4DiceTests
    {
        private static V4Game NewGame() => V4Game.New(new[] { V4Game.Professora, V4Game.Medica });

        [Test]
        public void DICE01_MovementIsTheSumOfTwoDiceAndInvalidValuesAreRejectedWithoutChange()
        {
            var g = NewGame();
            Assert.That(g.Engine.Rules.DiceCount, Is.EqualTo(2));
            Assert.That(g.Engine.Rules.DoublesExtraRollsMax, Is.EqualTo(3));
            g.Cur.Position = 0;
            Assert.That(g.Roll(3, 4).Ok, Is.True);
            Assert.That(g.P(0).Position, Is.EqualTo(7));
            Assert.That(g.S.LastDie1, Is.EqualTo(3));
            Assert.That(g.S.LastDie2, Is.EqualTo(4));
            Assert.That(g.S.RollAgain, Is.False);
            var before = StateJson.Write(g.S);
            Assert.That(g.Roll(0, 4).Ok, Is.False);
            Assert.That(g.Roll(4, 0).Ok, Is.False);
            Assert.That(g.Roll(7, 1).Ok, Is.False);
            Assert.That(g.Roll(1, 9).Ok, Is.False);
            Assert.That(StateJson.Write(g.S), Is.EqualTo(before));
        }

        [Test]
        public void DICE02_DoublesGiveTheSamePlayerAnotherRollAfterTheHouseIsResolved()
        {
            var g = NewGame();
            g.Cur.Position = 34;                       // 1+1 -> H01 (neutral)
            Assert.That(g.Roll(1, 1).Ok, Is.True);
            Assert.That(g.Cur.Id, Is.EqualTo("p1"), "same player rolls again");
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));
            Assert.That(g.S.ExtraRollsUsed, Is.EqualTo(1));
            Assert.That(g.S.History.Any(h => h.Kind == "roll_again"), Is.True);
            g.Cur.Position = 33;                       // 33 + 1 + 2 = 36 -> H01 (neutral)
            Assert.That(g.Roll(1, 2).Ok, Is.True);     // not doubles -> turn passes
            Assert.That(g.Cur.Id, Is.EqualTo("p2"));
            Assert.That(g.S.ExtraRollsUsed, Is.EqualTo(0), "counter resets for the next turn");
        }

        [Test]
        public void DICE03_AtMostThreeExtraRollsThenTheTurnPassesEvenOnDoubles()
        {
            var g = NewGame();
            for (var i = 0; i < 4; i++)
            {
                Assert.That(g.Cur.Id, Is.EqualTo("p1"), "still player 1 before roll " + (i + 1));
                g.Cur.Position = 34;                   // keep landing on the neutral H01
                var r = g.Roll(1, 1);
                Assert.That(r.Ok, Is.True, r.Error);
            }
            Assert.That(g.Cur.Id, Is.EqualTo("p2"), "4th double (3 extras already used) does not grant another roll");
            Assert.That(g.S.History.Count(h => h.Kind == "roll_again"), Is.EqualTo(3));
        }

        [Test]
        public void DICE04_TheExtraRollComesOnlyAfterTheDecisionOfTheHouseIsConfirmed()
        {
            var g = NewGame();
            g.Cur.Position = 35;                       // 35 + 1 + 1 = 37 -> index 1 (H02, non essential)
            Assert.That(g.Roll(1, 1).Ok, Is.True);
            Assert.That(g.S.Pending, Is.Not.Null);
            Assert.That(g.S.RollAgain, Is.True);
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.AwaitDecision), "must decide first");
            Assert.That(g.Choose("H02_O04").Ok, Is.True);
            Assert.That(g.Cur.Id, Is.EqualTo("p1"));
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));
        }

        [Test]
        public void DICE05_NoExtraRollWhenTheTurnEndsInHospital()
        {
            var g = NewGame();
            g.Cur.Position = 16;                       // 16 + 1 + 1 = 18 -> H19 direct to hospital
            Assert.That(g.Roll(1, 1).Ok, Is.True);
            Assert.That(g.S.Pending.Kind, Is.EqualTo(DecisionKind.HospitalModality));
            Assert.That(g.Hospital("private").Ok, Is.True);
            Assert.That(g.Cur.Id, Is.EqualTo("p2"), "hospitalised player does not roll again");
            Assert.That(g.S.RollAgain, Is.False);
        }

        [Test]
        public void DICE06_PendingExtraRollSurvivesSaveAndRestore()
        {
            var g = NewGame();
            g.Cur.Position = 35;
            g.Roll(1, 1);                              // H02 pending + RollAgain
            var json = StateJson.Write(g.S);
            var restored = new V4Game { Engine = new V4Engine(V4TestContent.Content, StateJson.Read<V4State>(json)) };
            Assert.That(restored.S.RollAgain, Is.True);
            Assert.That(restored.S.ExtraRollsUsed, Is.EqualTo(1));
            Assert.That(restored.Engine.ChooseOption("restored-choice", restored.S.Pending.Id, restored.S.Pending.PlayerId, "H02_O04").Ok, Is.True);
            Assert.That(restored.Cur.Id, Is.EqualTo("p1"));
            Assert.That(restored.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));
        }

        [Test]
        public void DICE07_UndoRestoresTheExtraRollFlag()
        {
            var g = NewGame();
            g.Cur.Position = 34;
            g.Roll(1, 1);
            Assert.That(g.S.ExtraRollsUsed, Is.EqualTo(1));
            Assert.That(g.Engine.Undo(g.Cmd(), "teste").Ok, Is.True);
            Assert.That(g.S.ExtraRollsUsed, Is.EqualTo(0));
            Assert.That(g.S.RollAgain, Is.False);
            Assert.That(g.S.Phase, Is.EqualTo(V4Phase.AwaitRoll));
        }
    }
}
