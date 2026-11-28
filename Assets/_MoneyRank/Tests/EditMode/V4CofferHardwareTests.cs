#if UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.Threading;
using MoneyRank.Domain.V4;
using MoneyRank.Infrastructure.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>
    /// Real-hardware check of the game's own serial path (WindowsSerialCofferTransport + CofferDisplaySync) against the
    /// Arduino running Docs/Hardware/cofre_display. Explicit: runs only when selected, with MONEYRANK_COFFER_PORT=COMx set.
    /// </summary>
    [Explicit("Needs the Arduino + display on MONEYRANK_COFFER_PORT"), Category("Hardware")]
    public sealed class V4CofferHardwareTests
    {
        [Test]
        public void GameTransportGetsTheBalanceAcknowledgedByTheDisplay()
        {
            var port = Environment.GetEnvironmentVariable("MONEYRANK_COFFER_PORT");
            if (string.IsNullOrEmpty(port)) Assert.Ignore("Set MONEYRANK_COFFER_PORT (e.g. COM12).");

            using (var transport = new WindowsSerialCofferTransport(port))
            {
                var sync = new CofferDisplaySync(transport, resendAfterTicks: 20);
                foreach (var balance in new[] { 0, 176, 28260 })
                {
                    sync.Publish(balance);
                    var watch = Stopwatch.StartNew();
                    while (sync.LastAckedBalance != balance && watch.ElapsedMilliseconds < 8000)
                    {
                        sync.Tick();
                        Thread.Sleep(30);             // ~33 ticks/s, like the game loop
                    }
                    TestContext.WriteLine(port + " status=" + transport.Status + " balance=" + balance + " acked=" + sync.LastAckedBalance +
                                          " after " + watch.ElapsedMilliseconds + " ms; rejected=" + sync.RejectedReplies + " mismatched=" + sync.MismatchedAcks);
                    Assert.That(sync.LastAckedBalance, Is.EqualTo(balance), "the display must confirm " + balance + " (status " + transport.Status + ")");
                    Thread.Sleep(1200);
                }
            }
        }
    }
}
#endif
