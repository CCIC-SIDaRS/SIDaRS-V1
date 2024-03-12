using System.Diagnostics;
using System;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Windows;

namespace Backend.MonitorManager
{
    class MonitorSystem
    {

        public MonitorSystem(string monitorAddress)
        {
            //_monitorAddress = IPAddress.Parse(monitorAddress);
            // _captureSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

            SetupCapture();
        }

        public static void SetupCapture()
        {
            // Print SharpPcap version
            var ver = Pcap.SharpPcapVersion;
            Debug.WriteLine("SharpPcap {0}, Example3.BasicCap.cs", ver);

            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // If no devices were found print an error
            if (devices.Count < 1)
            {
                Debug.WriteLine("No devices were found on this machine");
                return;
            }

            Debug.WriteLine("");
            Debug.WriteLine("The following devices are available on this machine:");
            Debug.WriteLine("----------------------------------------------------");
            Debug.WriteLine("");

            int i = 0;

            // Print out the devices
            foreach (var dev in devices)
            {
                /* Description */
                Debug.WriteLine("{0}) {1} {2}", i, dev.Name, dev.Description);
                i++;
            }

            Debug.WriteLine("");
            Debug.Write("-- Please choose a device to capture: ");
            i = 4;

            using var device = devices[i];

            // Register our handler function to the 'packet arrival' event
            device.OnPacketArrival +=
                new PacketArrivalEventHandler(device_OnPacketArrival);

            // Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: readTimeoutMilliseconds);

            Debug.WriteLine("");
            Debug.WriteLine("-- Listening on {0} {1}, hit 'Enter' to stop...",
                device.Name, device.Description);

            // Start the capturing process
            device.StartCapture();
            Debug.WriteLine(device.Started.ToString());
        }

        private static void device_OnPacketArrival(object sender, PacketCapture e)
        {
            var time = e.Header.Timeval.Date;
            var len = e.Data.Length;
            var rawPacket = e.GetPacket();
            Debug.WriteLine("{0}:{1}:{2},{3} Len={4}",
                time.Hour, time.Minute, time.Second, time.Millisecond, len);
            MessageBox.Show(rawPacket.ToString());
        }
    }
}