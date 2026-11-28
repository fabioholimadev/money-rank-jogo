using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>
    /// The numbers sent to the Arduino LCD must arrive exactly. The same vectors file (protocol_vectors.csv) is checked here (C#)
    /// and by Docs/Hardware/cofre_display/tests/test_cofre_protocol.cpp (the real cofre_protocol.h that runs on the Arduino).
    /// </summary>
    public sealed class V4CofferProtocolTests
    {
        private sealed class FakeTransport : ICofferDisplayTransport
        {
            public CofferLinkStatus Status { get; set; } = CofferLinkStatus.Connected;
            public readonly List<string> Sent = new List<string>();
            public readonly Queue<string> Replies = new Queue<string>();
            public bool TrySend(string line) { if (Status != CofferLinkStatus.Connected) return false; Sent.Add(line); return true; }
            public string TryReceive() => Replies.Count > 0 ? Replies.Dequeue() : null;
        }

        [Test]
        public void LINK01_ConfirmedOnlyAfterTheDisplayAcknowledgesTheBalance()
        {
            var t = new FakeTransport();
            var sync = new CofferDisplaySync(t, resendAfterTicks: 3);
            sync.Publish(176);
            sync.Tick();
            Assert.That(t.Sent.Count, Is.EqualTo(1), "the balance is sent");
            Assert.That(sync.DisplayAnswered, Is.False, "port open is not proof that the display is there");
            Assert.That(sync.IsConfirmed, Is.False);
            var seq = int.Parse(t.Sent[0].Split('|')[1]);
            t.Replies.Enqueue(CofferProtocol.FormatAck(seq, CofferProtocol.ToCents(176)));
            sync.Tick();
            Assert.That(sync.DisplayAnswered && sync.IsConfirmed, Is.True);
            Assert.That(sync.LastAckedBalance, Is.EqualTo(176));
        }

        [Test]
        public void LINK02_SilentPortCountsTicksWithoutReplyAndReconnectProvesTheLinkAgain()
        {
            var t = new FakeTransport();
            var sync = new CofferDisplaySync(t, resendAfterTicks: 3);
            sync.Publish(0);
            for (var i = 0; i < 10; i++) sync.Tick();
            Assert.That(sync.TicksWithoutReply, Is.EqualTo(10), "no Arduino answering on this port");

            t.Replies.Enqueue(CofferProtocol.FormatAck(int.Parse(t.Sent[t.Sent.Count - 1].Split('|')[1]), 0));
            sync.Tick();
            Assert.That(sync.IsConfirmed, Is.True);

            var other = new FakeTransport();
            sync.SetTransport(other);
            Assert.That(sync.IsConfirmed, Is.False, "a new port must answer again");
            sync.Tick();
            Assert.That(other.Sent.Count, Is.EqualTo(1), "the confirmed balance is resent on the new port");
        }

        private static string VectorsPath => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Docs", "Hardware", "cofre_display", "protocol_vectors.csv"));

        private static List<string[]> Vectors()
        {
            var rows = File.ReadAllLines(VectorsPath).Skip(1).Where(l => l.Length > 0).Select(l => l.Split(';')).ToList();
            foreach (var r in rows) Assert.That(r.Length, Is.EqualTo(4), string.Join(";", r));
            return rows;
        }

        [Test]
        public void CRC01_StandardCheckValueAndSharedVectorsMatchTheCppSide()
        {
            Assert.That(CofferProtocol.Crc8("123456789"), Is.EqualTo((byte)0xF4), "CRC-8 poly 0x07 check value");
            var rows = Vectors();
            Assert.That(rows.Count, Is.GreaterThan(40));
            foreach (var r in rows.Where(r => r[0] == "crc"))
                Assert.That(CofferProtocol.Crc8(r[1]).ToString("X2"), Is.EqualTo(r[3]), r[1]);
        }

        [Test]
        public void CRC02_MoneyTextAndSealedFramesAreIdenticalToTheSharedVectors()
        {
            foreach (var r in Vectors())
            {
                switch (r[0])
                {
                    case "money": Assert.That(CofferProtocol.FormatMoney(long.Parse(r[1])), Is.EqualTo(r[3]), r[1]); break;
                    case "cmd": Assert.That(CofferProtocol.FormatCents(int.Parse(r[1]), long.Parse(r[2])), Is.EqualTo(r[3])); break;
                    case "ack": Assert.That(CofferProtocol.FormatAck(int.Parse(r[1]), long.Parse(r[2])), Is.EqualTo(r[3])); break;
                    case "err": Assert.That(CofferProtocol.FormatErr(int.Parse(r[1])), Is.EqualTo(r[3])); break;
                    case "ready": Assert.That(CofferProtocol.FormatReady(long.Parse(r[1])), Is.EqualTo(r[3])); break;
                }
            }
        }

        [Test]
        public void CRC03_RepliesFromTheArduinoAreParsedExactlyAndBadOnesRejected()
        {
            foreach (var r in Vectors())
            {
                CofferReplyKind kind; int seq; long cents;
                switch (r[0])
                {
                    case "ack":
                        Assert.That(CofferProtocol.TryParseReply(r[3], out kind, out seq, out cents), Is.True, r[3]);
                        Assert.That(kind, Is.EqualTo(CofferReplyKind.Ack)); Assert.That(seq, Is.EqualTo(int.Parse(r[1]))); Assert.That(cents, Is.EqualTo(long.Parse(r[2])));
                        break;
                    case "err":
                        Assert.That(CofferProtocol.TryParseReply(r[3], out kind, out seq, out cents), Is.True, r[3]);
                        Assert.That(kind, Is.EqualTo(CofferReplyKind.Err)); Assert.That(seq, Is.EqualTo(int.Parse(r[1])));
                        break;
                    case "ready":
                        Assert.That(CofferProtocol.TryParseReply(r[3], out kind, out seq, out cents), Is.True, r[3]);
                        Assert.That(kind, Is.EqualTo(CofferReplyKind.Ready)); Assert.That(cents, Is.EqualTo(long.Parse(r[1])));
                        break;
                    case "badreply":
                        Assert.That(CofferProtocol.TryParseReply(r[1], out kind, out seq, out cents), Is.False, "[" + r[1] + "]");
                        break;
                }
            }
        }

        [Test]
        public void CRC04_AnySingleCharacterChangeInAReplyIsRejected()
        {
            var good = CofferProtocol.FormatAck(1234, 2826000);
            for (var i = 0; i < good.Length; i++)
                for (var c = 32; c < 127; c++)
                {
                    if (good[i] == (char)c) continue;
                    if (i >= good.Length - 2 && char.ToUpperInvariant((char)c) == good[i]) continue;   // 'a' and 'A' are the same hex digit
                    var bad = good.Substring(0, i) + (char)c + good.Substring(i + 1);
                    CofferReplyKind kind; int seq; long cents;
                    Assert.That(CofferProtocol.TryParseReply(bad, out kind, out seq, out cents), Is.False, bad);
                }
        }

        [Test]
        public void CRC05_BalanceIsSentAsWholeCentavosAndIdleTextIsZero()
        {
            Assert.That(CofferProtocol.IdleText, Is.EqualTo("R$0,00"));
            Assert.That(CofferProtocol.DisplayText(0), Is.EqualTo(CofferProtocol.IdleText));
            Assert.That(CofferProtocol.DisplayText(28260), Is.EqualTo("R$28.260,00"), "starting money of a 4-player game");
            Assert.That(CofferProtocol.FormatBalance(7, 1234), Does.StartWith("COFRE|7|123400*"));
            Assert.That(CofferProtocol.ToCents(CofferProtocol.MaxBalance), Is.EqualTo(999999900L), "9.999.999 whole CRIA Coin; the display limit is 999999999 centavos");
            Assert.Throws<ArgumentOutOfRangeException>(() => CofferProtocol.FormatBalance(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CofferProtocol.FormatBalance(1, CofferProtocol.MaxBalance + 1));
            // every representable balance in a wide range keeps its exact digits (no rounding anywhere)
            for (var b = 0; b <= 60000; b += 7)
            {
                var text = CofferProtocol.DisplayText(b);
                Assert.That(text.Length, Is.LessThanOrEqualTo(16));
                Assert.That(int.Parse(text.Substring(2, text.Length - 5).Replace(".", "")), Is.EqualTo(b));
                Assert.That(text.EndsWith(",00"), Is.True);
            }
        }

        [Test]
        public void CRC06_AckThatEchoesAnotherNumberIsNotAcceptedAndTheBalanceIsSentAgain()
        {
            var t = new FakeTransport();
            var sync = new CofferDisplaySync(t, 2);
            sync.Publish(150); sync.Tick();
            var seq = int.Parse(t.Sent[0].Split('|')[1]);
            t.Replies.Enqueue(CofferProtocol.FormatAck(seq, 12345));       // the LCD shows something else
            sync.Tick();
            Assert.That(sync.LastAckedBalance, Is.EqualTo(-1));
            Assert.That(sync.MismatchedAcks, Is.EqualTo(1));
            sync.Tick();
            Assert.That(t.Sent.Count, Is.EqualTo(2), "sent again after the wrong echo");
            t.Replies.Enqueue(CofferProtocol.FormatAck(seq, 15000));
            sync.Tick();
            Assert.That(sync.LastAckedBalance, Is.EqualTo(150));
        }

        [Test]
        public void CRC07_CorruptedRepliesAreCountedAndNeverAcknowledge()
        {
            var t = new FakeTransport();
            var sync = new CofferDisplaySync(t, 2);
            sync.Publish(150); sync.Tick();
            var seq = int.Parse(t.Sent[0].Split('|')[1]);
            t.Replies.Enqueue("ACK|" + seq + "|15000");                    // no CRC
            t.Replies.Enqueue(CofferProtocol.FormatAck(seq, 15000).Replace("15000", "15001"));
            t.Replies.Enqueue("lixo");
            sync.Tick();
            Assert.That(sync.RejectedReplies, Is.EqualTo(3));
            Assert.That(sync.LastAckedBalance, Is.EqualTo(-1));
        }

        [Test]
        public void CRC08_ArduinoRebootAnnouncementMakesTheCurrentBalanceBeSentAgain()
        {
            var t = new FakeTransport();
            var sync = new CofferDisplaySync(t, 2);
            sync.Publish(150); sync.Tick();
            t.Replies.Enqueue(CofferProtocol.FormatAck(int.Parse(t.Sent[0].Split('|')[1]), 15000));
            sync.Tick();
            Assert.That(sync.LastAckedBalance, Is.EqualTo(150));
            t.Replies.Enqueue(CofferProtocol.FormatReady(0));              // the board just powered on showing R$0,00
            sync.Tick();
            Assert.That(t.Sent.Count, Is.EqualTo(2));
            Assert.That(t.Sent[1], Does.Contain("|15000*"));
            Assert.That(sync.LastAckedBalance, Is.EqualTo(-1), "not confirmed until the new ACK arrives");
        }

        [Test]
        public void CRC09_ABalanceTheDisplayCannotShowNeverThrowsIntoTheGame()
        {
            var t = new FakeTransport();
            var sync = new CofferDisplaySync(t, 2);
            Assert.DoesNotThrow(() => sync.Publish(-1));
            Assert.DoesNotThrow(() => sync.Publish(CofferProtocol.MaxBalance + 1));
            Assert.DoesNotThrow(() => sync.Resync(int.MaxValue));
            sync.Tick();
            Assert.That(sync.UnrepresentableBalances, Is.EqualTo(3));
            Assert.That(t.Sent, Is.Empty);
        }
    }
}
