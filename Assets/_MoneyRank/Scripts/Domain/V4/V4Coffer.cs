using System;
using System.Collections.Generic;
using System.Globalization;

namespace MoneyRank.Domain.V4
{
    public enum CofferLinkStatus { Disconnected = 0, Reconnecting = 1, Connected = 2 }

    /// <summary>Transport to the physical coffer display (HANDOFF §13). Implementations must never block the main thread.</summary>
    public interface ICofferDisplayTransport
    {
        CofferLinkStatus Status { get; }
        /// <summary>Queue one protocol line (without newline). Returns false if the link is down.</summary>
        bool TrySend(string line);
        /// <summary>Dequeue a reply line received from the display (e.g. "ACK|3"), or null.</summary>
        string TryReceive();
    }

    /// <summary>Used when no display is connected: the app keeps working and shows the balance on screen.</summary>
    public sealed class NullCofferTransport : ICofferDisplayTransport
    {
        public CofferLinkStatus Status => CofferLinkStatus.Disconnected;
        public bool TrySend(string line) => false;
        public string TryReceive() => null;
    }

    /// <summary>
    /// Wire protocol: Unity sends <c>COFRE|seq|balance</c>, Arduino answers <c>ACK|seq</c> or <c>ERR|seq</c>.
    /// Always the absolute balance; duplicates are harmless; the latest sequence wins. Pure and testable.
    /// </summary>
    public static class CofferProtocol
    {
        public const int BaudRate = 115200;

        public static string FormatBalance(int seq, int balance)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            return "COFRE|" + seq.ToString(CultureInfo.InvariantCulture) + "|" + balance.ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryParseReply(string line, out bool ack, out int seq)
        {
            ack = false; seq = 0;
            if (string.IsNullOrEmpty(line)) return false;
            var parts = line.Trim().Split('|');
            if (parts.Length != 2) return false;
            if (parts[0] != "ACK" && parts[0] != "ERR") return false;
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out seq)) return false;
            ack = parts[0] == "ACK";
            return true;
        }

        /// <summary>What the Arduino sketch renders: "COFRE  1234 UMR".</summary>
        public static string DisplayText(int balance) => "COFRE  " + balance.ToString(CultureInfo.InvariantCulture) + " UMR";
    }

    /// <summary>
    /// Keeps the display in sync with <c>FundCash</c>: sends the absolute balance after setup, every change, restore, undo
    /// and reconnect; resends until ACK (lost ACK is safe because the balance is absolute and idempotent).
    /// </summary>
    public sealed class CofferDisplaySync
    {
        private readonly ICofferDisplayTransport _transport;
        private readonly int _resendAfterTicks;
        private int _seq;
        private int _pendingSeq = -1;
        private int _pendingBalance = -1;
        private int _ticksSinceSend;
        private bool _wasConnected;

        public int LastAckedBalance { get; private set; } = -1;
        public int LastAckedSeq { get; private set; } = -1;
        public CofferLinkStatus Status => _transport.Status;

        public CofferDisplaySync(ICofferDisplayTransport transport, int resendAfterTicks = 20)
        {
            _transport = transport ?? new NullCofferTransport();
            _resendAfterTicks = Math.Max(1, resendAfterTicks);
        }

        /// <summary>Call whenever the authoritative balance may have changed (cheap: only sends when it differs from the last ACK).</summary>
        public void Publish(int balance)
        {
            if (balance == LastAckedBalance && _pendingSeq < 0) return;
            if (_pendingSeq >= 0 && balance == _pendingBalance) return;
            _pendingSeq = ++_seq;
            _pendingBalance = balance;
            _ticksSinceSend = _resendAfterTicks; // send on next Tick
        }

        /// <summary>Force a fresh absolute send (after restore, undo or reconnect).</summary>
        public void Resync(int balance)
        {
            LastAckedBalance = -1;
            _pendingSeq = ++_seq;
            _pendingBalance = balance;
            _ticksSinceSend = _resendAfterTicks;
        }

        /// <summary>Drive from the main loop; never blocks.</summary>
        public void Tick()
        {
            var connected = _transport.Status == CofferLinkStatus.Connected;
            if (connected && !_wasConnected && _pendingBalance >= 0) { _pendingSeq = ++_seq; _ticksSinceSend = _resendAfterTicks; }
            _wasConnected = connected;

            string line;
            while ((line = _transport.TryReceive()) != null)
            {
                bool ack; int seq;
                if (!CofferProtocol.TryParseReply(line, out ack, out seq)) continue;
                if (ack && seq == _pendingSeq) { LastAckedBalance = _pendingBalance; LastAckedSeq = seq; _pendingSeq = -1; }
                else if (!ack && seq == _pendingSeq) _ticksSinceSend = _resendAfterTicks; // ERR -> resend
            }
            if (_pendingSeq < 0 || !connected) return;
            if (_ticksSinceSend >= _resendAfterTicks)
            {
                if (_transport.TrySend(CofferProtocol.FormatBalance(_pendingSeq, _pendingBalance))) _ticksSinceSend = 0;
            }
            else _ticksSinceSend++;
        }
    }
}
