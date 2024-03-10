using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Threading;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Backend.MonitorManager
{
    class MonitorSystem
    {
        private IPAddress _monitorAddress { get; set; }
        private Socket _captureSocket { get; set; }
        private Thread _captureThread { get; set; }
        private bool _runCapture { get; set; }

        public MonitorSystem(string monitorAddress)
        {
            //_monitorAddress = IPAddress.Parse(monitorAddress);
            // _captureSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

            SetupCapture();
        }

        public void SetupCapture()
        {
            var devices = CaptureDeviceList.Instance;
            for (int i = 0; i < devices.Count; i++)
            {
                Debug.WriteLine(i + "\n" + devices[i].ToString());
            }
            using var device = devices[9];
            device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: 1000);
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
            Debug.WriteLine("Something");
            device.StartCapture();
            Debug.WriteLine("Something");
        }

        public void device_OnPacketArrival(object s, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            Debug.WriteLine(rawPacket.ToString());
            Debug.WriteLine("Something");
        }
    }
}