using System.Diagnostics;
using System;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
namespace Backend.MonitorManager
{
    struct Packet
    {
        public Packet(string sourceAddress, string destinationAddress, string protocol, DateTime arrivalTime)
        {
            this.sourceAddress = sourceAddress;
            this.destinationAddress = destinationAddress;
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
            _sniffingDevice.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
        }

        public void StartCapture()
        {
            // Open the device for capturing
            Thread sniffing = new Thread(new ThreadStart(sniffing_Proccess));
            sniffing.IsBackground = true;
            sniffing.Start();
            new Task(PacketClean).Start();
        }

        private void device_OnPacketArrival(object sender, PacketCapture e)
        {
            RawCapture rawPacket = e.GetPacket();
            PacketDotNet.Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            if (packet is PacketDotNet.EthernetPacket eth)
            {
                PacketDotNet.IPPacket ip = packet.Extract<PacketDotNet.IPPacket>();
                if (ip != null)
                {
                    DateTime time = DateTime.Now;
                    PacketAnalysis.addPacket(new Packet(ip.SourceAddress.ToString(), ip.DestinationAddress.ToString(), ip.Protocol.ToString(), time));
                }
            }
        }

        private void PacketClean()
        {
            while (true)
            {
                Task task = new Task(PacketAnalysis.lifeExpiration);
                task.Start();
                task.Wait();
                //Debug.WriteLine(PacketAnalysis.packetDict.Count());
            }
        }

        private void sniffing_Proccess()
        {
            int readTimeoutMilliseconds = 1000;
            _sniffingDevice.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);
            _sniffingDevice.Capture();
        }
    }
    static class PacketAnalysis
    {
        public static ConcurrentDictionary<IPAddress, List<Packet>> packetDict = new ConcurrentDictionary<IPAddress, List<Packet>>();

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
            ConcurrentDictionary<IPAddress, List<Packet>> dictCopy = packetDict;
            foreach (KeyValuePair<IPAddress, List<Packet>> packets in dictCopy)
            {
                if (packets.Value.Count <= 0)
                {
                    packetDict.TryRemove(packets);
                    continue;
                }
                var packetsCopy = packets.Value;
                for(int i = 0; i < packets.Value.Count - 1; i++)
                {
                    //Debug.WriteLine(DateTime.Now <= packets.Value[i].arrivalTime.Add(TTL));
                    //Debug.WriteLine(DateTime.Now >= packets.Value[i].arrivalTime.Add(TTL));
                    if (DateTime.Now >= packets.Value[i].arrivalTime.Add(TTL))
                    {
                        packetsCopy.RemoveAt(i);
                        //Debug.WriteLine("Removed");
                    }
                }
                packetDict[packets.Key] = packetsCopy;
            }
        }
    }
}