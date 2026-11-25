using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>
    /// Randomised full-game simulations of every B1/B2 difficulty configuration. They prove engine robustness
    /// (no exception, cash identity, no negative balance, game progresses) — they are NOT balance evidence and do not
    /// replace human pre-tests with the physical kit (see Docs/V4/05_validacao/roteiro_pretestes.md).
    /// </summary>
    public sealed class V4SimulationTests
    {
        private sealed class Row
        {
            public string Config, Professions; public int Seed, Steps, Rounds, Year; public string Ended, Winner;
            public int Hospital, Eliminated, Captures, Taxes, Evasions, Undone, MinBank, MinFund, Ledger;
        }

        private static Row Play(string difficulty, string[] professions, int seed, int maxSteps)
        {
            var rng = new Random(seed);
            var engine = new V4Engine(V4TestContent.Content);
            var setup = professions.Select((pr, i) => new V4PlayerSetup { Id = "p" + (i + 1), Name = "J" + (i + 1), ProfessionId = pr, Color = V4Engine.Colors[i] }).ToList();
            Assert.That(engine.Setup(setup, new V4Config { DifficultyId = difficulty }, "sim-" + difficulty + "-" + seed).Ok, Is.True);
            var s = engine.State;
            var row = new Row { Config = difficulty, Seed = seed, Professions = string.Join("+", professions), MinBank = int.MaxValue, MinFund = int.MaxValue };
            var step = 0;
            while (engine.State.Phase != V4Phase.GameEnd && step++ < maxSteps)
            {
                s = engine.State;
                V4Result r;
                if (s.LiquidityBlocked) { r = engine.ClearLiquidityBlock("c" + step, "sim"); }
                else if (s.Phase == V4Phase.AwaitRoll)
                {
                    var cur = engine.CurrentPlayer;
                    r = engine.RegisterRoll("c" + step, cur.Id, rng.Next(1, 7));
                }
                else
                {
                    var d = s.Pending;
                    Assert.That(d, Is.Not.Null, "AwaitDecision without pending (seed " + seed + ")");
                    switch (d.Kind)
                    {
                        case DecisionKind.Choice: r = engine.ChooseOption("c" + step, d.Id, d.PlayerId, d.OptionIds[rng.Next(d.OptionIds.Count)]); break;
                        case DecisionKind.TaxCoin:
                        case DecisionKind.FiscalCoin: r = engine.RegisterCoin("c" + step, d.Id, d.PlayerId, rng.Next(2) == 0); break;
                        case DecisionKind.HospitalModality: r = engine.ChooseHospital("c" + step, d.Id, d.PlayerId, d.OptionIds[rng.Next(d.OptionIds.Count)]); break;
                        default: r = engine.ChooseAbility("c" + step, d.Id, d.OptionIds[rng.Next(d.OptionIds.Count)]); break;
                    }
                }
                if (!r.Ok && !r.Liquidity) Assert.Fail("Command failed in simulation " + difficulty + " seed " + seed + " step " + step + ": " + r.Error);
                s = engine.State;
                Assert.That(engine.TotalCash(), Is.EqualTo(28260), "cash identity broke (seed " + seed + ", step " + step + ")");
                Assert.That(s.BankCash, Is.GreaterThanOrEqualTo(0));
                Assert.That(s.FundCash, Is.GreaterThanOrEqualTo(0));
                Assert.That(s.Players.All(p => p.Wallet >= 0 && p.ReserveBalance >= 0), Is.True);
                row.MinBank = Math.Min(row.MinBank, s.BankCash);
                row.MinFund = Math.Min(row.MinFund, s.FundCash);
            }
            s = engine.State;
            row.Steps = step; row.Rounds = s.CompletedRounds; row.Year = s.YearIndex;
            row.Ended = s.Phase == V4Phase.GameEnd ? s.EndReason : "limit";
            row.Winner = s.WinnerId == null ? string.Empty : s.Players.First(p => p.Id == s.WinnerId).ProfessionId;
            row.Hospital = s.History.Count(h => h.Kind == "hospital" && h.OptionId != null);
            row.Eliminated = s.Players.Count(p => p.Eliminated);
            row.Taxes = s.History.Count(h => h.Kind == "tax");
            row.Evasions = s.History.Count(h => h.Kind == "tax" && h.Detail != null && h.Detail.Contains("sonegou"));
            row.Ledger = s.Ledger.Count;
            // round-trip every finished/limit state through the save format
            var json = StateJson.Write(s);
            Assert.That(StateJson.Write(StateJson.Read<V4State>(json)), Is.EqualTo(json));
            return row;
        }

        [Test]
        public void RandomisedGamesOfAllBatteryConfigurationsKeepEveryInvariant()
        {
            var configs = new[] { "B1_facil", "B1_medio", "B1_dificil", "B2_facil", "B2_medio", "B2_dificil" };
            var mixes = new[]
            {
                new[] { "P01", "P02", "P03", "P04" },
                new[] { "P05", "P06", "P01", "P03" },
                new[] { "P02", "P04", "P06" },
                new[] { "P03", "P05" }
            };
            var rows = new List<Row>();
            foreach (var cfg in configs)
                for (var seed = 1; seed <= 12; seed++)
                    rows.Add(Play(cfg, mixes[seed % mixes.Length], 1000 * Array.IndexOf(configs, cfg) + seed, 3500));

            var sb = new StringBuilder("config,professions,seed,steps,rounds,year,ended,winner,hospital_events,eliminated,tax_events,evasions,ledger_entries,min_bank,min_cofre\r\n");
            foreach (var r in rows)
                sb.Append(string.Join(",", r.Config, r.Professions, r.Seed, r.Steps, r.Rounds, r.Year, r.Ended, r.Winner, r.Hospital, r.Eliminated, r.Taxes, r.Evasions, r.Ledger, r.MinBank, r.MinFund)).Append("\r\n");
            var dir = Path.Combine(UnityEngine.Application.dataPath, "..", "Docs", "V4", "05_validacao");
            File.WriteAllText(Path.Combine(dir, "resultado_simulacao.csv"), sb.ToString(), new UTF8Encoding(true));

            Assert.That(rows.Count, Is.EqualTo(72));
            Assert.That(rows.Count(r => r.Ended != "limit"), Is.GreaterThan(0), "at least some simulated games reach an end state");
            Assert.That(rows.All(r => r.MinBank >= 0), Is.True);
            Assert.That(rows.Sum(r => r.Ledger), Is.GreaterThan(1000), "simulations exercised the economy");
        }

        [Test]
        public void SimulatedGamesAreDeterministicForTheSameSeed()
        {
            var a = Play("B1_medio", new[] { "P01", "P02", "P03" }, 77, 800);
            var b = Play("B1_medio", new[] { "P01", "P02", "P03" }, 77, 800);
            Assert.That(b.Steps, Is.EqualTo(a.Steps));
            Assert.That(b.Ledger, Is.EqualTo(a.Ledger));
            Assert.That(b.MinBank, Is.EqualTo(a.MinBank));
            Assert.That(b.Rounds, Is.EqualTo(a.Rounds));
        }
    }
}
