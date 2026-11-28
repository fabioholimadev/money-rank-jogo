#if UNITY_EDITOR_WIN
using MoneyRank.Infrastructure.V4;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class WindowsSerialCofferTransportTests
    {
        [TestCase(@"\Device\BthModem0", true)]
        [TestCase(@"\Device\bthmodem12", true)]
        [TestCase(@"\Device\Serial2", false)]
        [TestCase(@"\Device\USBSER000", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void BluetoothVirtualPortsAreRecognizedByDevicePath(string devicePath, bool expected)
        {
            Assert.AreEqual(expected, WindowsSerialCofferTransport.IsBluetoothDevice(devicePath));
        }
    }
}
#endif
