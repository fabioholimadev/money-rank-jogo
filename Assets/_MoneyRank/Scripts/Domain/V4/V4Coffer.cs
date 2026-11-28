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

    public enum CofferReplyKind { Ack = 0, Err = 1, Ready = 2 }

    /// <summary>
    /// Wire protocol with the Arduino (ASCII, one line per frame, 115200 baud 8N1). Every frame ends with <c>*HH</c>, a CRC-8
    /// (poly 0x07, init 0x00) of everything before the '*', so a corrupted number is rejected instead of shown.
    /// <code>
    /// Unity -> Arduino   COFRE|seq|centavos*HH      absolute balance in whole centavos (integer, never a float)
    /// Arduino -> Unity   ACK|seq|centavos*HH        echoes the value that is now ON the display
    ///                    ERR|seq*HH                 frame rejected (bad format, CRC or range)
    ///                    PRONTO|centavos*HH         the Arduino (re)booted and shows that value (0 = R$0,00)
    /// </code>
    /// The C++ side (<c>Docs/Hardware/cofre_display/cofre_protocol.h</c>) is verified against the same vectors
    /// (<c>protocol_vectors.csv</c>) as this class.
    /// </summary>
    public static class CofferProtocol
    {
        public const int BaudRate = 115200;
        /// <summary>The LCD 16x2 shows the amount after this prefix. DECISION_REQUIRED: confirm "R$" vs "CRIA" (see Docs/OPEN_DECISIONS.md).</summary>
        public const string DisplayPrefix = "R$";
        public const string IdleText = "R$0,00";
        public const long CentsPerUnit = 100;
        /// <summary>Largest value the display accepts: 9.999.999,99 (fits a 32-bit unsigned integer on the Arduino and 16 LCD columns).</summary>
        public const long MaxCents = 999999999L;
        public const int MaxBalance = (int)(MaxCents / CentsPerUnit);

        public static bool IsRepresentable(int balance) => balance >= 0 && balance <= MaxBalance;

        public static long ToCents(int balance)
        {
            if (!IsRepresentable(balance)) throw new ArgumentOutOfRangeException(nameof(balance));
            return balance * CentsPerUnit;
        }

        public static byte Crc8(string text)
        {
            byte crc = 0;
            foreach (var ch in text)
            {
                if (ch > 127) throw new ArgumentException("ASCII only", nameof(text));
                crc ^= (byte)ch;
                for (var i = 0; i < 8; i++) crc = (byte)((crc & 0x80) != 0 ? (crc << 1) ^ 0x07 : crc << 1);
            }
            return crc;
        }

        private static string Seal(string body) => body + "*" + Crc8(body).ToString("X2", CultureInfo.InvariantCulture);

        public static string FormatBalance(int seq, int balance)
        {
            if (seq < 0) throw new ArgumentOutOfRangeException(nameof(seq));
            return FormatCents(seq, ToCents(balance));
        }

        public static string FormatCents(int seq, long cents)
        {
            if (seq < 0 || cents < 0 || cents > MaxCents) throw new ArgumentOutOfRangeException(nameof(cents));
            return Seal("COFRE|" + seq.ToString(CultureInfo.InvariantCulture) + "|" + cents.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>The frames the Arduino sends; here so tests and a simulated display speak exactly the same protocol.</summary>
        public static string FormatAck(int seq, long cents) =>
            Seal("ACK|" + seq.ToString(CultureInfo.InvariantCulture) + "|" + cents.ToString(CultureInfo.InvariantCulture));
        public static string FormatErr(int seq) => Seal("ERR|" + seq.ToString(CultureInfo.InvariantCulture));
        public static string FormatReady(long cents) => Seal("PRONTO|" + cents.ToString(CultureInfo.InvariantCulture));

        private static bool TryParseUInt(string s, long max, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(s) || s.Length > 10) return false;
            if (s.Length > 1 && s[0] == '0') return false;               // canonical form only
            foreach (var ch in s) if (ch < '0' || ch > '9') return false;
            value = long.Parse(s, NumberStyles.None, CultureInfo.InvariantCulture);
            return value <= max;
        }

        /// <summary>Verifies the CRC and returns the body ("KIND|a|b") of a sealed line, or null.</summary>
        private static string Unseal(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            line = line.Trim();
            var star = line.LastIndexOf('*');
            if (star < 0 || line.Length - star != 3) return null;
            foreach (var ch in line) if (ch > 127) return null;
            var body = line.Substring(0, star);
            var hex = line.Substring(star + 1).ToUpperInvariant();
            return hex == Crc8(body).ToString("X2", CultureInfo.InvariantCulture) ? body : null;
        }

        /// <summary>Parses a reply from the Arduino; false for anything malformed or with a wrong CRC.</summary>
        public static bool TryParseReply(string line, out CofferReplyKind kind, out int seq, out long cents)
        {
            kind = CofferReplyKind.Err; seq = 0; cents = 0;
            var body = Unseal(line);
            if (body == null) return false;
            var parts = body.Split('|');
            long a, b;
            if (parts[0] == "ACK" && parts.Length == 3 && TryParseUInt(parts[1], int.MaxValue, out a) && TryParseUInt(parts[2], MaxCents, out b))
            { kind = CofferReplyKind.Ack; seq = (int)a; cents = b; return true; }
            if (parts[0] == "ERR" && parts.Length == 2 && TryParseUInt(parts[1], int.MaxValue, out a))
            { kind = CofferReplyKind.Err; seq = (int)a; return true; }
            if (parts[0] == "PRONTO" && parts.Length == 2 && TryParseUInt(parts[1], MaxCents, out b))
            { kind = CofferReplyKind.Ready; cents = b; return true; }
            return false;
        }

        /// <summary>"R$28.260,00": pt-BR separators built by integer arithmetic only (identical to the C++ formatter).</summary>
        public static string FormatMoney(long cents)
        {
            if (cents < 0 || cents > MaxCents) throw new ArgumentOutOfRangeException(nameof(cents));
            var units = (cents / CentsPerUnit).ToString(CultureInfo.InvariantCulture);
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < units.Length; i++)
            {
                if (i > 0 && (units.Length - i) % 3 == 0) sb.Append('.');
                sb.Append(units[i]);
            }
            return DisplayPrefix + sb + "," + (cents % CentsPerUnit).ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>What the LCD's amount line shows for this balance.</summary>
        public static string DisplayText(int balance) => FormatMoney(ToCents(balance));
    }

    /// <summary>
    /// Keeps the display in sync with <c>FundCash</c>: sends the absolute balance after setup, every change, restore, undo
    /// and reconnect; resends until ACK (lost ACK is safe because the balance is absolute and idempotent).
    /// </summary>
    public sealed class CofferDisplaySync
    {
        private ICofferDisplayTransport _transport;
        private readonly int _resendAfterTicks;
        private int _seq;
        private int _pendingSeq = -1;
        private int _pendingBalance = -1;
        private int _ticksSinceSend;
        private bool _wasConnected;

        public int LastAckedBalance { get; private set; } = -1;
        public int LastAckedSeq { get; private set; } = -1;
        /// <summary>Diagnostics: replies dropped for a bad format/CRC, ACKs that echoed a different number, balances the display cannot show.</summary>
        public int RejectedReplies { get; private set; }
        public int MismatchedAcks { get; private set; }
        public int UnrepresentableBalances { get; private set; }
        public CofferLinkStatus Status => _transport.Status;
        /// <summary>The display answered with a valid frame since the port was (re)opened (proves it is the coffer sketch).</summary>
        public bool DisplayAnswered { get; private set; }
        /// <summary>The display confirmed the current balance (nothing pending).</summary>
        public bool IsConfirmed => DisplayAnswered && LastAckedBalance >= 0 && _pendingSeq < 0;
        /// <summary>Ticks spent connected without any valid reply (to tell "port open but no Arduino answering").</summary>
        public int TicksWithoutReply { get; private set; }

        public CofferDisplaySync(ICofferDisplayTransport transport, int resendAfterTicks = 20)
        {
            _transport = transport ?? new NullCofferTransport();
            _resendAfterTicks = Math.Max(1, resendAfterTicks);
        }

        /// <summary>Swap the transport at runtime (e.g. the master picked a COM port). Resends the last known balance.</summary>
        public void SetTransport(ICofferDisplayTransport transport)
        {
            _transport = transport ?? new NullCofferTransport();
            _wasConnected = false;
            DisplayAnswered = false;
            TicksWithoutReply = 0;
            if (_pendingBalance >= 0 || LastAckedBalance >= 0) Resync(_pendingBalance >= 0 ? _pendingBalance : LastAckedBalance);
        }

        /// <summary>Call whenever the authoritative balance may have changed (cheap: only sends when it differs from the last ACK).</summary>
        public void Publish(int balance)
        {
            if (!CofferProtocol.IsRepresentable(balance)) { UnrepresentableBalances++; return; }   // never throws into the game loop
            if (balance == LastAckedBalance && _pendingSeq < 0) return;
            if (_pendingSeq >= 0 && balance == _pendingBalance) return;
            _pendingSeq = ++_seq;
            _pendingBalance = balance;
            _ticksSinceSend = _resendAfterTicks; // send on next Tick
        }

        /// <summary>Force a fresh absolute send (after restore, undo or reconnect).</summary>
        public void Resync(int balance)
        {
            if (!CofferProtocol.IsRepresentable(balance)) { UnrepresentableBalances++; return; }
            LastAckedBalance = -1;
            _pendingSeq = ++_seq;
            _pendingBalance = balance;
            _ticksSinceSend = _resendAfterTicks;
        }

        /// <summary>Drive from the main loop; never blocks.</summary>
        public void Tick()
        {
            var connected = _transport.Status == CofferLinkStatus.Connected;
            if (connected && !_wasConnected)
            {
                DisplayAnswered = false;
                TicksWithoutReply = 0;
                if (_pendingBalance < 0 && LastAckedBalance >= 0) _pendingBalance = LastAckedBalance;   // reconnect: prove the link again
                if (_pendingBalance >= 0) { _pendingSeq = ++_seq; _ticksSinceSend = _resendAfterTicks; }
            }
            _wasConnected = connected;
            if (connected && !DisplayAnswered) TicksWithoutReply++;

            string line;
            while ((line = _transport.TryReceive()) != null)
            {
                CofferReplyKind kind; int seq; long cents;
                if (!CofferProtocol.TryParseReply(line, out kind, out seq, out cents)) { RejectedReplies++; continue; }
                DisplayAnswered = true;
                TicksWithoutReply = 0;
                if (kind == CofferReplyKind.Ack && seq == _pendingSeq)
                {
                    if (cents == CofferProtocol.ToCents(_pendingBalance)) { LastAckedBalance = _pendingBalance; LastAckedSeq = seq; _pendingSeq = -1; }
                    else { MismatchedAcks++; _ticksSinceSend = _resendAfterTicks; }      // the display shows another number: send again
                }
                else if (kind == CofferReplyKind.Err && seq == _pendingSeq) _ticksSinceSend = _resendAfterTicks;
                else if (kind == CofferReplyKind.Ready)                                   // the Arduino (re)booted and shows R$0,00
                {
                    var current = _pendingBalance >= 0 ? _pendingBalance : LastAckedBalance;
                    if (current >= 0) Resync(current);
                }
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
