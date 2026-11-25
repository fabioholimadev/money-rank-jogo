using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoneyRank.Domain.V4;
using MoneyRank.Infrastructure.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>Acceptance scenarios HW*, DATA01 and save robustness.</summary>
    public sealed class V4PersistenceTests
    {
        private sealed class FakeTransport : ICofferDisplayTransport
        {
            public CofferLinkStatus Status { get; set; } = CofferLinkStatus.Disconnected;
            public readonly List<string> Sent = new List<string>();
            public readonly Queue<string> Replies = new Queue<string>();
            public bool TrySend(string line) { if (Status != CofferLinkStatus.Connected) return false; Sent.Add(line); return true; }
            public string TryReceive() => Replies.Count > 0 ? Replies.Dequeue() : null;
        }

        private static string TempDir() => Path.Combine(Path.GetTempPath(), "mr-v4-" + System.Guid.NewGuid().ToString("N"));

        [Test]
        public void HW01_DisconnectedDisplayNeverBlocksAndReconnectSendsTheAbsoluteBalance()
        {
            var t = new FakeTransport();
            var sync = new CofferDisplaySync(t, 3);
            sync.Publish(150);
            for (var i = 0; i < 10; i++) sync.Tick();
            Assert.That(t.Sent, Is.Empty);
            t.Status = CofferLinkStatus.Connected;
            sync.Tick();
            Assert.That(t.Sent.Single(), Does.Contain("|15000*"), "150 CRIA Coin = 15000 centavos");
            Assert.That(t.Sent.Single(), Does.StartWith("COFRE|"));
        }

        [Test]
        public void HW02_LostAckIsResentAndTheDisplayEndsOnTheLatestBalance()
        {
            var t = new FakeTransport { Status = CofferLinkStatus.Connected };
            var sync = new CofferDisplaySync(t, 2);
            sync.Publish(150);
            sync.Tick();
            Assert.That(t.Sent.Count, Is.EqualTo(1));
            for (var i = 0; i < 6; i++) sync.Tick();           // ACK lost -> resent with the same sequence
            Assert.That(t.Sent.Count, Is.GreaterThan(1));
            Assert.That(t.Sent.Distinct().Count(), Is.EqualTo(1));
            var oldSeq = int.Parse(t.Sent.Last().Split('|')[1]);
            sync.Publish(80);                                  // balance changes while waiting
            sync.Tick();
            var newSeq = int.Parse(t.Sent.Last().Split('|')[1]);
            Assert.That(newSeq, Is.Not.EqualTo(oldSeq));
            t.Replies.Enqueue(CofferProtocol.FormatAck(oldSeq, 15000));                // late ACK of the old sequence is ignored
            sync.Tick();
            Assert.That(sync.LastAckedBalance, Is.Not.EqualTo(80));
            t.Replies.Enqueue(CofferProtocol.FormatAck(newSeq, 8000));
            sync.Tick();
            Assert.That(sync.LastAckedBalance, Is.EqualTo(80));
            Assert.That(t.Sent.Last(), Does.Contain("|8000*"));
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [Test]
        public void WindowsSerialTransportListsPortsAndUnavailablePortNeverThrowsOrBlocks()
        {
            Assert.That(WindowsSerialCofferTransport.ListPorts(), Is.Not.Null);
            var transport = new WindowsSerialCofferTransport("COM250");   // surely absent
            try
            {
                Assert.That(transport.Status, Is.Not.EqualTo(CofferLinkStatus.Connected));
                Assert.That(transport.TrySend("COFRE|1|10"), Is.False, "no link: caller keeps playing, balance is shown on screen");
                Assert.That(transport.TryReceive(), Is.Null);
            }
            finally { transport.Dispose(); }
            Assert.That(transport.Status, Is.EqualTo(CofferLinkStatus.Disconnected));
        }
#endif

        [Test]
        public void SwappingTheTransportResendsTheLastBalance()
        {
            var a = new FakeTransport { Status = CofferLinkStatus.Connected };
            var sync = new CofferDisplaySync(a, 2);
            sync.Publish(120); sync.Tick();
            a.Replies.Enqueue(CofferProtocol.FormatAck(int.Parse(a.Sent[0].Split('|')[1]), 12000)); sync.Tick();
            Assert.That(sync.LastAckedBalance, Is.EqualTo(120));
            var b = new FakeTransport { Status = CofferLinkStatus.Connected };
            sync.SetTransport(b);
            sync.Tick();
            Assert.That(b.Sent.Single(), Does.Contain("|12000*"));
        }

        [Test]
        public void SaveRoundTripsAtomicallyAndRecoversFromACorruptedMainFile()
        {
            var dir = TempDir();
            try
            {
                var g = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
                g.Land("H02");
                var store = new V4SaveStore(dir);
                string error;
                Assert.That(store.Save(g.S, out error), Is.True, error);
                Assert.That(store.Save(g.S, out error), Is.True, error); // second save creates the .bak
                var loaded = store.Load(g.S.SessionId);
                Assert.That(loaded.Ok, Is.True);
                Assert.That(StateJson.Write(loaded.State), Is.EqualTo(StateJson.Write(g.S)));
                File.WriteAllText(store.PathFor(g.S.SessionId), "{ corrupted");
                var recovered = store.Load(g.S.SessionId);
                Assert.That(recovered.Ok && recovered.RecoveredFromBackup, Is.True);
                Assert.That(store.ListSessionIds(), Does.Contain(g.S.SessionId));
                Assert.That(store.Archive(g.S.SessionId), Is.Not.Null);
                Assert.That(store.Load(g.S.SessionId).Ok, Is.True, "backup still allows recovery after archiving the main file");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void DATA01_TwoExportedSessionsHaveDistinctIdsFilesAndFullTables()
        {
            var dir = TempDir();
            try
            {
                var a = V4Game.New(new[] { V4Game.Professora, V4Game.Medica });
                var engineB = new V4Engine(V4TestContent.Content);
                engineB.Setup(new[]
                {
                    new V4PlayerSetup { Id = "p1", ProfessionId = "P03", Color = "verde" },
                    new V4PlayerSetup { Id = "p2", ProfessionId = "P04", Color = "azul" }
                }, new V4Config { DifficultyId = "B2_facil" });
                a.Land("H02"); a.Choose("H02_O01");
                var filesA = V4Exporter.Export(a.S, dir, "r1");
                var filesB = V4Exporter.Export(engineB.State, dir, "r1");
                Assert.That(a.S.SessionId, Is.Not.EqualTo(engineB.State.SessionId));
                Assert.That(filesA.Intersect(filesB), Is.Empty);
                Assert.That(filesA.All(File.Exists), Is.True);
                var ledger = File.ReadAllText(filesA.First(f => f.EndsWith("-ledger.csv")));
                Assert.That(ledger, Does.Contain("despesa"));
                var session = File.ReadAllText(filesB.First(f => f.EndsWith("-session.csv")));
                Assert.That(session, Does.Contain("B2_facil"));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
    }
}
