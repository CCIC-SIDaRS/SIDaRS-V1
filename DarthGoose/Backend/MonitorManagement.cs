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
using PacketDotNet;
using System.Text.Json.Serialization;
using System.Diagnostics.Eventing.Reader;
using System.Printing;
using System.Windows.Data;
using System.ComponentModel.Design.Serialization;
using System.Runtime.CompilerServices;
namespace Backend.MonitorManager
{
    class Node
    {
        public NodeRecord? parentRecord { get; set; }
        public Node? previousNode { get; set; }
        public Node? nextNode { get; set; }

        public NodeRecord[] records = new NodeRecord[256];

    }
    class NodeRecord
    {
        public ExponentiallyWeightedMovingAverage fromRate { get; set; }
        public ExponentiallyWeightedMovingAverage toRate { get; set; }
        public ExponentiallyWeightedMovingAverage ratioAverage { get; set; }
        public Node? child { get; set; }
        public NodeRecord(float alpha)
        {
            fromRate = new ExponentiallyWeightedMovingAverage(alpha);
            toRate = new ExponentiallyWeightedMovingAverage(alpha);
            ratioAverage = new ExponentiallyWeightedMovingAverage(alpha);
        }
    }
    class MonitorSystem
    {
        public IPAddress gateway = IPAddress.Parse("192.168.1.1");
        private ILiveDevice _sniffingDevice { get; set; }
        private Task _packetClean { get; set; }
        private bool _stopClean = false;
        private bool _captureRunning = false;
        public MonitorSystem(ILiveDevice sniffingDevice)
        {
            _sniffingDevice = sniffingDevice;
            _sniffingDevice.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
            _packetClean = new Task(PacketClean);
        }

        public void ChangeCaptureDevice(ILiveDevice device)
        {
            StopCapture();
            _sniffingDevice = device;
            StartCapture();
        }

        public void StartCapture()
        {
            // Open the device for capturing
            Thread sniffing = new Thread(new ThreadStart(sniffing_Proccess));
            sniffing.IsBackground = true;
            sniffing.Start();
            //_packetClean.Start();
            _captureRunning = true;
        }

        public void StopCapture()
        {
            if(!_captureRunning)
            {
                return;
            }
            _stopClean = true;
            _sniffingDevice.StopCapture();
            _captureRunning = false;
        }

        private void device_OnPacketArrival(object sender, PacketCapture e)
        {
            RawCapture rawPacket = e.GetPacket();
            int packetSize = e.Data.Length;

            // Debug.WriteLine(packetSize);
            Task.Run(() =>
            {
                Packet packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                IPPacket ip = packet.Extract<IPPacket>();
                if(ip != null)
                {
                    if (ip.DestinationAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6 && ip.SourceAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) 
                    {
                        PacketAnalysis.addPacket(ip);
                        //Debug.WriteLine("source " + ip.SourceAddress + " destination: " + ip.DestinationAddress + " protocol: " + ip.Protocol);
                    }
                }
                //PacketAnalysis.lifeExpiration();
            });
        }

        private void PacketClean()
        {
            //while (!_stopClean)
            //{
            //    Task task = new Task(PacketAnalysis.lifeExpiration);
            //    task.Start();
            //    task.Wait();
            //    //Debug.WriteLine(PacketAnalysis.packetDict.Count());
            //}
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
            //Debug.WriteLine(_sniffingDevice);
            _sniffingDevice.Capture();
        }
    }
    static class PacketAnalysis
    {
        //public static ConcurrentList<Host> hosts = new ConcurrentList<Host>();
        // This will be reset whenever we clear the packet counter

        // Will be reset after x amount of time
        private static int _foldTime = 30000;

        // This represents entries for all addresses
        private static Node _rootNode = new Node();

        private static int expansionThreshhold = 10; // Packets per Millisecond

        public static void addPacket(IPPacket packet)
        {
            
            byte[] sourceAddress = packet.SourceAddress.GetAddressBytes();
            byte[] destinationAddress = packet.DestinationAddress.GetAddressBytes();
            Node currentBase = _rootNode;
            NodeRecord? currentSourceNodeRecord = null;
            NodeRecord? currentDestinationNodeRecord = null;
            bool SourceComplete = false;
            bool DestinationComplete = false;
            int deepestSourceLevel = 0;
            int deepestDestinationLevel = 0;
            for (int i = 0; i < 4; i++)
            {
                if(!SourceComplete)
                {
                    byte currentSourceByte = sourceAddress[i];
                    currentSourceNodeRecord = _rootNode.records[currentSourceByte];

                    if (currentSourceNodeRecord == null)
                    {
                        currentSourceNodeRecord = new NodeRecord(0.01f);
                        currentBase.records[currentSourceByte] = currentSourceNodeRecord;
                    }
                    currentSourceNodeRecord.fromRate.AddPacketToRateList();
                    float currentAverage = Convert.ToSingle(currentSourceNodeRecord.toRate.exponentialMovingAverage / currentSourceNodeRecord.fromRate.exponentialMovingAverage);
                    if(currentAverage > 0 && currentAverage != float.PositiveInfinity)
                    {
                        currentSourceNodeRecord.ratioAverage.AddValueToRateList(currentAverage);
                    }
                    if((currentSourceNodeRecord.ratioAverage.exponentialMovingAverage > 2.3 || currentSourceNodeRecord.ratioAverage.exponentialMovingAverage < 0.3) 
                        && currentSourceNodeRecord.ratioAverage.exponentialMovingAverage > 0)
                    {
                        Debug.WriteLine("Source " + packet.SourceAddress.ToString() + " " + packet.DestinationAddress.ToString() + " " + currentSourceNodeRecord.ratioAverage.exponentialMovingAverage);
                    }
                    
                    if (currentSourceNodeRecord.child == null)
                    {
                        SourceComplete = true;
                    }else
                    {
                        currentBase = currentSourceNodeRecord.child;
                        deepestSourceLevel++;
                    }
                }
                if(!DestinationComplete)
                {
                    byte currentDestinationByte = destinationAddress[i];
                    currentDestinationNodeRecord = _rootNode.records[currentDestinationByte];
                    if (currentDestinationNodeRecord == null && !DestinationComplete)
                    {
                        currentDestinationNodeRecord = new NodeRecord(0.01f);
                        currentBase.records[currentDestinationByte] = currentDestinationNodeRecord;
                    }
                    currentDestinationNodeRecord.toRate.AddPacketToRateList();
                    float currentAverage = Convert.ToSingle(currentDestinationNodeRecord.toRate.exponentialMovingAverage / currentDestinationNodeRecord.fromRate.exponentialMovingAverage);
                    if(currentAverage > 0 && currentAverage != float.PositiveInfinity)
                    {
                        currentDestinationNodeRecord.ratioAverage.AddValueToRateList(currentAverage);
                    }
                    if ((currentDestinationNodeRecord.ratioAverage.exponentialMovingAverage > 2.3 || currentDestinationNodeRecord.ratioAverage.exponentialMovingAverage < 0.3)
                        && currentDestinationNodeRecord.ratioAverage.exponentialMovingAverage > 0)
                    {
                        Debug.WriteLine("Destination " + packet.SourceAddress.ToString() + " " + packet.DestinationAddress.ToString() + " " + currentDestinationNodeRecord.ratioAverage.exponentialMovingAverage);
                    }
                    //Debug.WriteLine(currentDestinationNodeRecord.ratioAverage.exponentialMovingAverage);
                    if (currentDestinationNodeRecord.child == null)
                    {
                        DestinationComplete = true;
                    }else
                    {
                        currentBase = currentDestinationNodeRecord.child;
                        deepestDestinationLevel++;
                    }
                }
                if(DestinationComplete && SourceComplete)
                {
                    break;
                }
            }
            if(currentDestinationNodeRecord.toRate.exponentialMovingAverage >= expansionThreshhold && deepestDestinationLevel < 3) 
            {
                //Debug.WriteLine("Destination Node Increase");
                currentDestinationNodeRecord.child = new Node();
                currentDestinationNodeRecord.child.parentRecord = currentDestinationNodeRecord;
            }
            if(currentSourceNodeRecord.fromRate.exponentialMovingAverage >= expansionThreshhold && deepestSourceLevel < 3)
            {
                //Debug.WriteLine("Source Node Increase");
                currentSourceNodeRecord.child = new Node();
                currentSourceNodeRecord.child.parentRecord = currentSourceNodeRecord;
            }
        }

        public static void fold()
        {
            Node currentRoot = _rootNode;
            for(int i = 0; i < 0; i++)
            {

            }
        }
    }
    class ExponentiallyWeightedMovingAverage
    {
        private float _alpha { get; set; }

        // packets/millisecond
        private ConcurrentList<float> RateList = new ConcurrentList<float>();
        private long _packetCount = 0;
        private long _someTimeInThePast = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        private DateTime lastReset = DateTime.Now;
        private TimeSpan timeToReset = new TimeSpan(0, 0, 30);

        public double exponentialMovingAverage { get
            {
                if (DateTime.Now > lastReset.Add(timeToReset))
                {
                    //Debug.WriteLine("Something");
                    for (int i = RateList.Count / 2; i < RateList.Count - 1; i++)
                    {
                        try
                        {
                            RateList.RemoveAt(i);
                        }catch(IndexOutOfRangeException)
                        {
                            break;
                        }
                    }
                }
                return RateList
                    .DefaultIfEmpty()
                    .Aggregate(RateList.FirstOrDefault(),
                    (ema, nextRate) => _alpha * nextRate + (1 - _alpha) * ema);
            } }

        public ExponentiallyWeightedMovingAverage(float alpha)
        {
            _alpha = alpha;
            //new Task(Clean).Start();
        }

        public void AddPacketToRateList()
        {
            try
            {
                _packetCount++;
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                float value = _packetCount / (now / _someTimeInThePast);
                RateList.Add(value);
            }catch(DivideByZeroException) 
            {
                return;
            }
        }

        public void AddValueToRateList(float value)
        {
            RateList.Add(value);
        }
    }
}