using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain.V4
{
    /// <summary>Outcome of applying a batch of attribute deltas to one player.</summary>
    public sealed class AttributeOutcome
    {
        public readonly int[] Cycles = new int[AttributeIds.Count];
        public bool HospitalTriggered;
        public int PositiveCycles => Cycles[0] + Cycles[1] + Cycles[2];
        public int NegativeCycles => Cycles[3] + Cycles[4] + Cycles[5];
    }

    /// <summary>Attribute bars, multipliers and cycles (V4 §4, HANDOFF §7). Exact decimals, no float.</summary>
    public static class V4Attributes
    {
        /// <summary>
        /// Apply signed base deltas. Order is deterministic: reductions first, then gains of negative attributes,
        /// then gains of positive attributes. Gains are multiplied by profession and difficulty factors;
        /// reductions are never multiplied (multiply_reductions=false) and never create credit or undo cycles.
        /// </summary>
        public static AttributeOutcome Apply(PlayerState p, decimal[] baseDeltas, ProfessionDefinition profession,
            DifficultyDefinition difficulty, RulesParameters rules)
        {
            var outcome = new AttributeOutcome();
            var threshold = (decimal)rules.AttributeThreshold;

            // 1) reductions
            for (var i = 0; i < AttributeIds.Count; i++)
            {
                if (baseDeltas[i] >= 0m) continue;
                var delta = baseDeltas[i];
                if (rules.MultiplyReductions) delta *= profession.Multipliers[i] * difficulty.MultiplierFor((AttributeId)i);
                var bar = p.Attributes[i];
                bar.Progress = Math.Max(0m, bar.Progress + delta);
            }

            // 2) gains: negative attributes first (recuos), then positive (avancos)
            foreach (var i in new[] { 3, 4, 5, 0, 1, 2 })
            {
                if (baseDeltas[i] <= 0m) continue;
                var attr = (AttributeId)i;
                var effective = baseDeltas[i] * profession.Multipliers[i] * difficulty.MultiplierFor(attr);
                var bar = p.Attributes[i];
                var total = bar.Progress + effective;
                var cycles = (int)Math.Floor(total / threshold);
                bar.Progress = total - cycles * threshold;
                bar.LifetimeCycles += cycles;
                outcome.Cycles[i] += cycles;
                if (cycles > 0 && AttributeIds.IsHealth(attr))
                {
                    bar.HealthCyclesSinceDischarge += cycles;
                    if (bar.HealthCyclesSinceDischarge >= rules.HealthCyclesToHospital)
                    {
                        if (p.HospitalTurnsRemaining == 0 && !p.HospitalPending) outcome.HospitalTriggered = true;
                        p.HospitalPending = true;
                    }
                }
            }
            return outcome;
        }

        /// <summary>Discharge: zero Toxina and Cortisol bars and their hospital cycle counters; keep everything else.</summary>
        public static void Discharge(PlayerState p)
        {
            foreach (var a in new[] { AttributeId.Toxina, AttributeId.Cortisol })
            {
                var bar = p.Attributes[(int)a];
                bar.Progress = 0m;
                bar.HealthCyclesSinceDischarge = 0;
            }
        }
    }

    public sealed class LudoMoveResult
    {
        public bool Moved;
        public int FromIndex;
        public int ToIndex;
        public bool PawnCompleted;
        public bool Discarded;                       // surplus beyond goal or retreat in base (no-op)
        public readonly List<string> CapturedPlayerIds = new List<string>();
    }

    /// <summary>Internal Ludo track: four pawns in strict order, routes per color, capture and checkpoints (V4 §5).</summary>
    public static class V4Ludo
    {
        public static int GoalIndex(V4ContentSet c, string color) => c.RouteOf(color).Count - 1;

        public static void NormalizeActive(PlayerState p)
        {
            p.ActivePawn = -1;
            for (var i = 0; i < p.Pawns.Length; i++)
                if (!p.Pawns[i].Completed) { p.ActivePawn = i; return; }
        }

        public static bool AllCompleted(PlayerState p) => p.Pawns.All(x => x.Completed);

        /// <summary>One concession of <paramref name="steps"/> advance. Capture only at the final cell.</summary>
        public static LudoMoveResult Advance(V4ContentSet c, PlayerState p, IEnumerable<PlayerState> others, int steps)
        {
            var result = new LudoMoveResult();
            NormalizeActive(p);
            if (p.ActivePawn < 0 || steps <= 0) { result.Discarded = true; return result; }
            var pawn = p.Pawns[p.ActivePawn];
            var route = c.RouteOf(p.Color);
            var goal = route.Count - 1;
            result.FromIndex = pawn.RouteIndex;
            // from base (-1) one step lands on the entry (0)
            var target = pawn.RouteIndex + steps;
            if (target >= goal)
            {
                target = goal;
                pawn.Completed = true;
                result.PawnCompleted = true;
            }
            pawn.RouteIndex = target;
            if (target > pawn.MaxIndex) pawn.MaxIndex = target;
            result.ToIndex = target;
            result.Moved = true;

            if (!pawn.Completed)
            {
                var cell = route[target];
                if (cell.IsShared && !cell.Protected)
                {
                    foreach (var q in others)
                    {
                        if (q == p || q.Eliminated) continue;
                        var qRoute = c.RouteOf(q.Color);
                        var captured = false;
                        foreach (var qp in q.Pawns)
                        {
                            if (qp.Completed || qp.RouteIndex < 0) continue;
                            var qCell = qRoute[qp.RouteIndex];
                            if (!qCell.IsShared || qCell.GlobalCell != cell.GlobalCell) continue;
                            qp.RouteIndex = CaptureReturn(q, qp);
                            captured = true;
                        }
                        if (captured) result.CapturedPlayerIds.Add(q.Id);
                    }
                }
            }
            NormalizeActive(p);
            return result;
        }

        /// <summary>Best personal checkpoint already reached by that pawn, never ahead of its current cell; else base.</summary>
        public static int CaptureReturn(PlayerState owner, PawnState pawn)
        {
            var best = -1;
            foreach (var cp in owner.Checkpoints)
                if (cp <= pawn.MaxIndex && cp < pawn.RouteIndex && cp > best) best = cp;
            return best;
        }

        /// <summary>One negative cycle: step back one; base is a no-op; completed pawns are never reopened.</summary>
        public static LudoMoveResult Retreat(PlayerState p)
        {
            var result = new LudoMoveResult();
            NormalizeActive(p);
            if (p.ActivePawn < 0) { result.Discarded = true; return result; }
            var pawn = p.Pawns[p.ActivePawn];
            result.FromIndex = pawn.RouteIndex;
            if (pawn.RouteIndex <= -1) { result.Discarded = true; result.ToIndex = -1; return result; }
            pawn.RouteIndex -= 1;
            result.ToIndex = pawn.RouteIndex;
            result.Moved = true;
            return result;
        }

        public static string CheckpointBlockReason(V4ContentSet c, PlayerState p)
        {
            if (p.CheckpointRights <= 0) return "Sem direito de checkpoint disponível.";
            NormalizeActive(p);
            if (p.ActivePawn < 0) return "Todos os peões já concluíram.";
            var pawn = p.Pawns[p.ActivePawn];
            if (pawn.RouteIndex < 0) return "O peão ativo está na base.";
            var cell = c.RouteOf(p.Color)[pawn.RouteIndex];
            if (!cell.IsShared) return "Checkpoint só é permitido em casa compartilhada.";
            if (cell.Protected) return "Casa protegida ou de entrada não aceita checkpoint.";
            if (p.Checkpoints.Contains(pawn.RouteIndex)) return "Já existe checkpoint nesta posição.";
            return null;
        }

        public static bool TryInstallCheckpoint(V4ContentSet c, PlayerState p)
        {
            if (CheckpointBlockReason(c, p) != null) return false;
            p.Checkpoints.Add(p.Pawns[p.ActivePawn].RouteIndex);
            p.Checkpoints.Sort();
            p.CheckpointRights -= 1;
            return true;
        }
    }
}
