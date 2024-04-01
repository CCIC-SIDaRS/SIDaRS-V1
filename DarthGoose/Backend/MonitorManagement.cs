using System.Diagnostics;
using System;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Net;

namespace Backend.MonitorManager
{
    struct Packet
    {
        public Packet(string sourcePacket, string destinationPacket, string protocol, DateTime arrivalTime)
        {
            this.sourceAddress = sourcePacket;
            this.destinationAddress = destinationPacket;
            this.protocol = protocol;
            this.arrivalTime = arrivalTime;
        }
        public string sourceAddress { get; }
        public string destinationAddress { get; }
        public string protocol { get; }
        public DateTime arrivalTime { get; }

    }
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
            RawCapture rawPacket = e.GetPacket();
            PacketDotNet.Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            DateTime time = DateTime.Now;
            PacketAnalysis.addPacket(new Packet(packet.Extract<PacketDotNet.IPPacket>().SourceAddress.ToString(), packet.Extract<PacketDotNet.IPPacket>().DestinationAddress.ToString(), packet.Extract<PacketDotNet.IPPacket>().Protocol.ToString(), time));
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
    static class PacketAnalysis
    {
        public static Dictionary<IPAddress, List<Packet>> packetDict = new Dictionary<IPAddress, List<Packet>>();

        public static void addPacket(Packet packet)
        {
            if (packetDict.ContainsKey(IPAddress.Parse(packet.sourceAddress)))
            {
                packetDict[IPAddress.Parse(packet.sourceAddress)].Add(packet);
            }else
            {
                packetDict[IPAddress.Parse(packet.sourceAddress)] = new List<Packet>() { packet };
            }
        }

        public static void lifeExpiration()
        {
            TimeSpan TTL = new TimeSpan(0, 0, 30);
            foreach (List<Packet> packets in packetDict.Values)
            {
                foreach (Packet packet in packets)
                {
                    if (DateTime.Now <= packet.arrivalTime.Add(TTL))
                    {
                        
                    }
                }
            }
        }
    }
}