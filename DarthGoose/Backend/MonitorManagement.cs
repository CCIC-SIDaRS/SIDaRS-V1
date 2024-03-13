using System.Diagnostics;
using System;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Windows;
using System.Windows.Controls;

namespace Backend.MonitorManager
{
    class MonitorSystem
    {
        private ILiveDevice _sniffingDevice { get; set; }
        public MonitorSystem(ILiveDevice sniffingDevice)
        {
            _sniffingDevice = sniffingDevice;
            SetupCapture();
        }

        private void SetupCapture()
        {
            // Register our handler function to the 'packet arrival' event
            _sniffingDevice.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);

            // Open the device for capturing
            Thread sniffing = new Thread(new ThreadStart(sniffing_Proccess));
            sniffing.IsBackground = true;
            sniffing.Start();
        }

        private void device_OnPacketArrival(object sender, PacketCapture e)
        {
            DateTime time = e.Header.Timeval.Date;
            int len = e.Data.Length;
            RawCapture rawPacket = e.GetPacket();
            Debug.WriteLine("{0}:{1}:{2},{3} Len={4}",
                time.Hour, time.Minute, time.Second, time.Millisecond, len);
            Debug.WriteLine(rawPacket.ToString());
        }

        private void sniffing_Proccess()
        {
            // Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            _sniffingDevice.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);

            // Start the capturing process
            
            _sniffingDevice.Capture();
        }
    }
}