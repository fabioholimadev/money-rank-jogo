#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MoneyRank.Domain.V4;

namespace MoneyRank.Infrastructure.V4
{
    /// <summary>
    /// Serial link to the coffer display (115200 8N1, newline-terminated lines) using the Win32 API directly,
    /// because the project's .NET Standard 2.1 profile does not expose System.IO.Ports.
    /// A background thread owns the port: it never blocks the main thread, reconnects automatically and
    /// reports the link status. The game keeps working when the display is absent.
    /// </summary>
    public sealed class WindowsSerialCofferTransport : ICofferDisplayTransport, IDisposable
    {
        private readonly string _portName;
        private readonly ConcurrentQueue<string> _outgoing = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        private Thread _thread;
        private volatile bool _run;
        private volatile int _status = (int)CofferLinkStatus.Disconnected;

        public string PortName => _portName;
        public CofferLinkStatus Status => (CofferLinkStatus)_status;

        public WindowsSerialCofferTransport(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName)) throw new ArgumentException("COM port is required.", nameof(portName));
            _portName = portName.Trim().ToUpperInvariant();
            _run = true;
            _status = (int)CofferLinkStatus.Reconnecting;
            _thread = new Thread(Worker) { IsBackground = true, Name = "CofferSerial-" + _portName };
            _thread.Start();
        }

        /// <summary>
        /// COM ports currently known to Windows (QueryDosDevice; Registry is not available in this profile).
        /// Real serial/USB ports come first and Bluetooth virtual ports (\Device\BthModem*) last, because the
        /// picker only shows the first few entries and a PC often has several Bluetooth ports before the Arduino.
        /// </summary>
        public static IReadOnlyList<string> ListPorts()
        {
            var ports = new List<string>();
            var bluetooth = new List<string>();
            var buffer = new StringBuilder(512);
            for (var i = 1; i <= 64; i++)
            {
                var name = "COM" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                try
                {
                    buffer.Clear();
                    if (QueryDosDevice(name, buffer, buffer.Capacity) == 0) continue;
                    if (IsBluetoothDevice(buffer.ToString())) bluetooth.Add(name);
                    else ports.Add(name);
                }
                catch (Exception) { break; }
            }
            ports.AddRange(bluetooth);
            return ports;
        }

        /// <summary>Only real serial/USB ports (no Bluetooth virtual ports): where an Arduino can be.</summary>
        public static IReadOnlyList<string> ListCandidatePorts()
        {
            var ports = new List<string>();
            var buffer = new StringBuilder(512);
            for (var i = 1; i <= 64; i++)
            {
                var name = "COM" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                try
                {
                    buffer.Clear();
                    if (QueryDosDevice(name, buffer, buffer.Capacity) != 0 && !IsBluetoothDevice(buffer.ToString())) ports.Add(name);
                }
                catch (Exception) { break; }
            }
            return ports;
        }

        /// <summary>True for the device path of a Bluetooth virtual COM port (e.g. "\Device\BthModem0").</summary>
        public static bool IsBluetoothDevice(string devicePath) =>
            !string.IsNullOrEmpty(devicePath) && devicePath.IndexOf("BthModem", StringComparison.OrdinalIgnoreCase) >= 0;

        public bool TrySend(string line)
        {
            if (Status != CofferLinkStatus.Connected || string.IsNullOrEmpty(line)) return false;
            _outgoing.Enqueue(line);
            return true;
        }

        public string TryReceive()
        {
            string line;
            return _incoming.TryDequeue(out line) ? line : null;
        }

        public void Dispose()
        {
            _run = false;
            var t = _thread;
            _thread = null;
            if (t != null && t.IsAlive) t.Join(1500);
            _status = (int)CofferLinkStatus.Disconnected;
        }

        // ------------------------------------------------------------------ worker

        private void Worker()
        {
            while (_run)
            {
                IntPtr handle = IntPtr.Zero;
                try
                {
                    _status = (int)CofferLinkStatus.Reconnecting;
                    handle = Open(_portName);
                    if (handle == IntPtr.Zero || handle == INVALID) { Sleep(2000); continue; }
                    string drop; while (_outgoing.TryDequeue(out drop)) { }
                    _status = (int)CofferLinkStatus.Connected;
                    Pump(handle);
                }
                catch (Exception) { /* fall through to reconnect */ }
                finally
                {
                    if (handle != IntPtr.Zero && handle != INVALID) CloseHandle(handle);
                    _status = (int)(_run ? CofferLinkStatus.Reconnecting : CofferLinkStatus.Disconnected);
                }
                Sleep(1500);
            }
        }

        private void Sleep(int ms)
        {
            for (var waited = 0; _run && waited < ms; waited += 50) Thread.Sleep(50);
        }

        private void Pump(IntPtr handle)
        {
            var rx = new StringBuilder();
            var buffer = new byte[256];
            while (_run)
            {
                string line;
                while (_outgoing.TryDequeue(out line))
                {
                    var bytes = Encoding.ASCII.GetBytes(line + "\n");
                    uint written;
                    if (!WriteFile(handle, bytes, (uint)bytes.Length, out written, IntPtr.Zero) || written != bytes.Length)
                        throw new InvalidOperationException("Serial write failed.");
                }
                uint read;
                if (!ReadFile(handle, buffer, (uint)buffer.Length, out read, IntPtr.Zero))
                    throw new InvalidOperationException("Serial read failed.");
                for (var i = 0; i < read; i++)
                {
                    var ch = (char)buffer[i];
                    if (ch == '\n') { var text = rx.ToString().Trim(); rx.Clear(); if (text.Length > 0) _incoming.Enqueue(text); }
                    else if (ch != '\r' && rx.Length < 64) rx.Append(ch);
                }
                Thread.Sleep(15);
            }
        }

        // ------------------------------------------------------------------ Win32

        private static readonly IntPtr INVALID = new IntPtr(-1);
        private const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct DCB
        {
            public uint DCBlength, BaudRate, Flags;
            public ushort wReserved, XonLim, XoffLim;
            public byte ByteSize, Parity, StopBits, XonChar, XoffChar, ErrorChar, EofChar, EvtChar;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMMTIMEOUTS
        {
            public uint ReadIntervalTimeout, ReadTotalTimeoutMultiplier, ReadTotalTimeoutConstant, WriteTotalTimeoutMultiplier, WriteTotalTimeoutConstant;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDevice(string deviceName, StringBuilder target, int max);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetCommState(IntPtr handle, ref DCB dcb);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetCommState(IntPtr handle, ref DCB dcb);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetCommTimeouts(IntPtr handle, ref COMMTIMEOUTS timeouts);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool WriteFile(IntPtr handle, byte[] buffer, uint toWrite, out uint written, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadFile(IntPtr handle, byte[] buffer, uint toRead, out uint read, IntPtr overlapped);

        private static IntPtr Open(string portName)
        {
            var handle = CreateFile(@"\\.\" + portName, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handle == INVALID) return INVALID;
            var dcb = new DCB { DCBlength = (uint)Marshal.SizeOf(typeof(DCB)) };
            if (!GetCommState(handle, ref dcb)) { CloseHandle(handle); return INVALID; }
            dcb.BaudRate = (uint)CofferProtocol.BaudRate;
            dcb.ByteSize = 8; dcb.Parity = 0; dcb.StopBits = 0; // 8N1
            dcb.Flags = 1u;                                      // fBinary only; no flow control
            if (!SetCommState(handle, ref dcb)) { CloseHandle(handle); return INVALID; }
            var timeouts = new COMMTIMEOUTS { ReadIntervalTimeout = uint.MaxValue, ReadTotalTimeoutMultiplier = 0, ReadTotalTimeoutConstant = 0, WriteTotalTimeoutMultiplier = 0, WriteTotalTimeoutConstant = 500 };
            SetCommTimeouts(handle, ref timeouts);
            return handle;
        }
    }
}
#endif
