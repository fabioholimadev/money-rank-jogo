using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MoneyRank.Domain.V4;

namespace MoneyRank.Infrastructure.V4
{
    public sealed class V4LoadResult
    {
        public bool Ok;
        public string Error;
        public bool RecoveredFromBackup;
        public V4State State;
    }

    /// <summary>
    /// Atomic local saves (tmp + replace + .bak) of the authoritative V4 state. Failure to write never touches
    /// the in-memory state; the caller keeps the engine state and may retry.
    /// </summary>
    public sealed class V4SaveStore
    {
        private readonly string _root;
        public V4SaveStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory)) throw new ArgumentException("Save root cannot be empty.", nameof(rootDirectory));
            _root = Path.GetFullPath(rootDirectory);
        }

        public string PathFor(string sessionId) => Path.Combine(_root, Sanitize(sessionId) + ".v4save.json");

        public static string Sanitize(string id)
        {
            var sb = new StringBuilder();
            foreach (var ch in id ?? string.Empty) sb.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_');
            if (sb.Length == 0) throw new ArgumentException("Empty session id.");
            return sb.ToString();
        }

        public bool Save(V4State state, out string error)
        {
            error = null;
            var path = PathFor(state.SessionId);
            var tmp = path + ".tmp";
            var bak = path + ".bak";
            try
            {
                Directory.CreateDirectory(_root);
                File.WriteAllText(tmp, StateJson.Write(state), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(tmp, path, bak, true);
                else File.Move(tmp, path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is ArgumentException)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* best effort */ }
                error = ex.Message;
                return false;
            }
        }

        public V4LoadResult Load(string sessionId)
        {
            var path = PathFor(sessionId);
            var main = TryRead(path);
            if (main.Ok) return main;
            var backup = TryRead(path + ".bak");
            if (backup.Ok) { backup.RecoveredFromBackup = true; return backup; }
            return new V4LoadResult { Ok = false, Error = File.Exists(path) || File.Exists(path + ".bak") ? "Save corrompido e sem backup válido." : "Save não encontrado." };
        }

        private static V4LoadResult TryRead(string path)
        {
            if (!File.Exists(path)) return new V4LoadResult { Ok = false, Error = "missing" };
            try
            {
                var state = StateJson.Read<V4State>(File.ReadAllText(path, Encoding.UTF8));
                if (state == null || state.Players == null || string.IsNullOrEmpty(state.SessionId)) return new V4LoadResult { Ok = false, Error = "invalid" };
                return new V4LoadResult { Ok = true, State = state };
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is IOException || ex is ArgumentException || ex is NullReferenceException)
            {
                return new V4LoadResult { Ok = false, Error = ex.Message };
            }
        }

        public IReadOnlyList<string> ListSessionIds()
        {
            if (!Directory.Exists(_root)) return new string[0];
            return Directory.GetFiles(_root, "*.v4save.json").Select(f => Path.GetFileName(f).Replace(".v4save.json", string.Empty)).OrderBy(x => x).ToList();
        }

        /// <summary>Incompatible/old saves are archived (never deleted) so a new session can start (HANDOFF §3).</summary>
        public string Archive(string sessionId)
        {
            var path = PathFor(sessionId);
            if (!File.Exists(path)) return null;
            var dir = Path.Combine(_root, "archive");
            Directory.CreateDirectory(dir);
            var target = Path.Combine(dir, Path.GetFileName(path) + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
            File.Move(path, target);
            return target;
        }
    }

    /// <summary>Analytical export: separate CSV tables with IDs plus a full JSON snapshot. No personal data beyond nicknames.</summary>
    public static class V4Exporter
    {
        public static IReadOnlyList<string> Export(V4State s, string directory, string revision = null)
        {
            Directory.CreateDirectory(directory);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var prefix = Path.Combine(directory, "v4-" + V4SaveStore.Sanitize(s.SessionId) + "-" + stamp + (revision != null ? "-" + revision : string.Empty));
            var files = new List<string>();

            files.Add(Write(prefix + "-session.csv", new[] { "session_id", "rules_version", "content_hash", "difficulty", "mode", "rounds", "year", "phase", "winner", "end_reason", "bank", "cofre" },
                new[] { new[] { s.SessionId, s.RulesVersion, s.ContentHash, s.Config.DifficultyId, s.Config.Mode, I(s.CompletedRounds), I(s.YearIndex), s.Phase.ToString(), s.WinnerId, s.EndReason, I(s.BankCash), I(s.FundCash) } }));

            files.Add(Write(prefix + "-players.csv", new[] { "session_id", "player_id", "profession", "color", "wallet", "reserve", "salary", "level", "properties", "vehicle", "eliminated", "pawns_done", "k", "i", "c", "t", "co", "d" },
                s.Players.Select(p => new[]
                {
                    s.SessionId, p.Id, p.ProfessionId, p.Color, I(p.Wallet), I(p.ReserveBalance), I(p.Salary), I(p.Level), I(p.Properties), p.HasVehicle ? "1" : "0", p.Eliminated ? "1" : "0",
                    I(p.Pawns.Count(x => x.Completed)),
                    p.Attributes[0].Progress.ToString(CultureInfo.InvariantCulture), p.Attributes[1].Progress.ToString(CultureInfo.InvariantCulture), p.Attributes[2].Progress.ToString(CultureInfo.InvariantCulture),
                    p.Attributes[3].Progress.ToString(CultureInfo.InvariantCulture), p.Attributes[4].Progress.ToString(CultureInfo.InvariantCulture), p.Attributes[5].Progress.ToString(CultureInfo.InvariantCulture)
                })));

            files.Add(Write(prefix + "-events.csv", new[] { "session_id", "seq", "round", "year", "player_id", "kind", "house_id", "option_id", "detail", "undone", "origin" },
                s.History.Select(h => new[] { s.SessionId, I(h.Seq), I(h.Round), I(h.Year), h.PlayerId, h.Kind, h.HouseId, h.OptionId, h.Detail, h.Undone ? "1" : "0", s.Config.Mode })));

            files.Add(Write(prefix + "-ledger.csv", new[] { "session_id", "seq", "round", "year", "from", "to", "amount", "reason", "ref", "from_before", "from_after", "to_before", "to_after", "reversed" },
                s.Ledger.Select(l => new[] { s.SessionId, I(l.Seq), I(l.Round), I(l.Year), l.From, l.To, I(l.Amount), l.Reason, l.Ref, I(l.FromBefore), I(l.FromAfter), I(l.ToBefore), I(l.ToAfter), l.Reversed ? "1" : "0" })));

            files.Add(Write(prefix + "-fiscal.csv", new[] { "session_id", "player_id", "record_id", "kind", "year", "principal", "outstanding", "status" },
                s.Players.SelectMany(p => p.Fiscal.Select(f => new[] { s.SessionId, p.Id, f.Id, f.Kind, I(f.SourceYear), I(f.Principal), I(f.Outstanding), f.Status }))));

            files.Add(Write(prefix + "-rng.csv", new[] { "session_id", "decision_id", "kind", "value" },
                s.Rng.Select(r => new[] { s.SessionId, r.DecisionId, r.Kind, r.Value })));

            var snapshot = prefix + "-snapshot.json";
            File.WriteAllText(snapshot, StateJson.Write(s), new UTF8Encoding(false));
            files.Add(snapshot);
            return files;
        }

        private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);

        private static string Write(string path, string[] header, IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.Append('﻿');
            sb.Append(string.Join(",", header.Select(Quote))).Append("\r\n");
            foreach (var r in rows) sb.Append(string.Join(",", r.Select(Quote))).Append("\r\n");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        private static string Quote(string v)
        {
            if (v == null) return string.Empty;
            return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }
    }
}
