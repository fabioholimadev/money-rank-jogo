using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoneyRank.Domain.V4;
using MoneyRank.Infrastructure.V4;
using UnityEngine;

namespace MoneyRank.V4.Presentation
{
    /// <summary>
    /// Glue between the pure engine and the scene: runs commands, autosaves atomically after every confirmed change,
    /// keeps the coffer display synchronized and tells the views to refresh.
    /// </summary>
    public sealed class V4Session
    {
        public V4Engine Engine { get; private set; }
        public V4SaveStore Store { get; }
        public CofferDisplaySync Coffer { get; }
        public string ExportDirectory { get; }
        public string LastSaveError { get; private set; }
        public string LastMessage { get; private set; }
        public event Action Changed;

        public V4State S => Engine.State;
        public V4ContentSet Content => Engine.Content;

        public static string RootDirectory => Path.Combine(Application.persistentDataPath, "MoneyRankV4");

        public V4Session(V4Engine engine, ICofferDisplayTransport transport = null)
        {
            Engine = engine;
            Store = new V4SaveStore(Path.Combine(RootDirectory, "Saves"));
            ExportDirectory = Path.Combine(RootDirectory, "Exports");
            Coffer = new CofferDisplaySync(transport ?? new NullCofferTransport());
            Coffer.Resync(S.FundCash);
        }

        public static V4Session StartNew(IList<V4PlayerSetup> players, V4Config config, out string error, ICofferDisplayTransport transport = null)
        {
            var engine = new V4Engine(V4Assets.Content);
            var r = engine.Setup(players, config);
            error = r.Ok ? null : r.Error;
            if (!r.Ok) return null;
            var session = new V4Session(engine, transport);
            session.Persist();
            return session;
        }

        public static V4Session Resume(string sessionId, out string error, ICofferDisplayTransport transport = null)
        {
            var store = new V4SaveStore(Path.Combine(RootDirectory, "Saves"));
            var loaded = store.Load(sessionId);
            if (!loaded.Ok) { error = loaded.Error; return null; }
            if (loaded.State.ContentHash != V4Assets.Content.ContentHash)
            {
                error = "O conteúdo mudou desde este save (hash diferente). O save foi arquivado; inicie uma nova partida.";
                store.Archive(sessionId);
                return null;
            }
            error = loaded.RecoveredFromBackup ? "Save principal corrompido: restaurado do backup." : null;
            return new V4Session(new V4Engine(V4Assets.Content, loaded.State), transport);
        }

        /// <summary>Saved session ids, oldest first (the last one is the most recent).</summary>
        public static List<string> NewSaveList()
        {
            var store = new V4SaveStore(Path.Combine(RootDirectory, "Saves"));
            return store.ListSessionIds().OrderBy(id => File.GetLastWriteTimeUtc(store.PathFor(id))).ToList();
        }

        public static string NewCommandId() => "u" + Guid.NewGuid().ToString("N");

        /// <summary>Runs a command, autosaves on success and refreshes everything that depends on state.</summary>
        public V4Result Exec(Func<V4Engine, V4Result> command)
        {
            var result = command(Engine);
            if (result.Ok && !result.Duplicate)
            {
                Persist();
                Coffer.Publish(S.FundCash);
            }
            LastMessage = result.Ok ? (result.Events.Count > 0 ? string.Join(" ", result.Events) : LastMessage) : result.Error;
            Changed?.Invoke();
            return result;
        }

        public V4Result Undo(string reason)
        {
            var result = Exec(e => e.Undo(NewCommandId(), reason));
            if (result.Ok) Coffer.Resync(S.FundCash);
            return result;
        }

        public void Persist()
        {
            string error;
            Store.Save(S, out error);
            LastSaveError = error;
        }

        public IReadOnlyList<string> Export()
        {
            return V4Exporter.Export(S, ExportDirectory, "r" + S.History.Count);
        }

        public void Tick() => Coffer.Tick();
    }
}
