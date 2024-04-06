using System.Diagnostics;
using System;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Net;
using Backend.ThreadSafety;
using System.Security;
using System.CodeDom.Compiler;
namespace Backend.MonitorManager
{
    struct Packet
    {
        public Packet(IPAddress destinationAddress, string protocol, DateTime arrivalTime, int packetSize)
        {
            this.destinationAddress = destinationAddress;
            this.protocol = protocol;
            this.arrivalTime = arrivalTime;
            this.packetSize = packetSize;
        }
        public IPAddress destinationAddress { get; }
        public string protocol { get; }
        public DateTime arrivalTime { get; }
        public int packetSize { get; }

    }

    struct Host
    {
        public Host(IPAddress address)
        {
            this.address = address;
            this.packets = new List<Packet>();
            this.packetCount = 0;
            this.trafficContributed = 0;
            this.trafficContributedReset = DateTime.Now;
        }
        public IPAddress address { get; }
        // This is in BYTES!!!!!!
        public int trafficContributed { get; set; }
        public DateTime trafficContributedReset { get; set; }
        public int packetCount { get; set;  }
        public List<Packet> packets { get; set; }
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
            new Task(() =>
            {
                PacketDotNet.Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                if (packet is PacketDotNet.EthernetPacket eth)
                {
                    PacketDotNet.IPPacket ip = packet.Extract<PacketDotNet.IPPacket>();
                    if (ip != null)
                    {
                        int packetSize = 0;
                        if (ip.PayloadPacket.PayloadData != null)
                        {
                            packetSize = ip.PayloadPacket.PayloadData.Length;
                        }
                        DateTime time = DateTime.Now;
                        PacketAnalysis.addPacket(new Packet(ip.DestinationAddress, ip.Protocol.ToString(), time, packetSize), ip.SourceAddress);
                    }
                }
            }).Start();
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
            if(_sniffingDevice.Description.ToLower().Contains("wireless"))
            {
                _sniffingDevice.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);
            }else
            {
                _sniffingDevice.Open(DeviceModes.None, readTimeoutMilliseconds);
            }
            _sniffingDevice.Capture();
        }
    }
    static class PacketAnalysis
    {
        public static ConcurrentList<Host> hosts = new ConcurrentList<Host>();

        public static void addPacket(Packet packet, IPAddress sourceAddress)
        {
            int host = -1;
            for (int i = 0; i < hosts.Count; i++)
            {
                if (hosts[i].address == sourceAddress)
                {
                    host = i;
                    break;
                }
            }
            if (host == -1)
            {
                Host addingHost = new Host(sourceAddress);
                addingHost.packets.Add(packet);
                addingHost.packetCount++;
                Debug.WriteLine(packet.packetSize);
                addingHost.trafficContributed += packet.packetSize;
                hosts.Add(addingHost);
            }else
            {
                Host modifyingHost = hosts[host];
                modifyingHost.packets.Add(packet);
                modifyingHost.packetCount++;
                Debug.WriteLine(packet.packetSize);
                modifyingHost.trafficContributed += packet.packetSize;
                hosts[host] = modifyingHost;
            }
        }

        public static void lifeExpiration()
        {
            TimeSpan packetLife = new TimeSpan(0, 0, 30);
            TimeSpan trafficContributedLife = new TimeSpan(0, 10, 0);
            ConcurrentList<Host> hostsCopy = hosts;
            if(hosts.Count > 0)
            {
                //Debug.WriteLine(hostsCopy.OrderByDescending(m => m.trafficContributed).First().packets.Count);
            }
            Parallel.ForEach(hostsCopy, host =>
            {
                if (host.packets.Count <= 0)
                {
                    hostsCopy.Remove(host);
                    return;
                }
                if(DateTime.Now >= host.trafficContributedReset.Add(trafficContributedLife))
                {
                    host.trafficContributedReset = DateTime.Now;
                    host.trafficContributed = 0;
                }
                List<Packet> packetsCopy = host.packets;
                for (int i = 0; i < host.packets.Count - 1; i++)
                {
                    //Debug.WriteLine(DateTime.Now <= packets.Value[i].arrivalTime.Add(TTL));
                    //Debug.WriteLine(DateTime.Now >= packets.Value[i].arrivalTime.Add(TTL));
                    if (DateTime.Now >= host.packets[i].arrivalTime.Add(packetLife))
                    {
                        packetsCopy.RemoveAt(i);
                        host.packetCount--;
                        continue;
                        //Debug.WriteLine("Removed");
                    }
                }
                host.packets = packetsCopy;
            });
        }
    }
}