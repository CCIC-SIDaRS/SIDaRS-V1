using System.Diagnostics;
using System;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Windows;
using Backend.ThreadSafety;
using PacketDotNet;
namespace Backend.MonitorManager
{
    /// <summary>
    /// Does monitoring
    /// </summary>
    class MonitorSystem
    {
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
            PacketAnalysis.sniffingStart = DateTime.Now;
            //_packetClean.Start();
            _captureRunning = true;
            MessageBox.Show("Capture has started");
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
            MessageBox.Show("Capture has stopped");
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
                    
                    if (ip.DestinationAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6 && ip.SourceAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                    ip.DestinationAddress.ToString() != "224.0.0.251" && ip.SourceAddress.ToString() != "224.0.0.251") 
                    {
                        PacketAnalysis.analyzePacket(ip);
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

    // Values that need new input fields
    // offense threshold
    // expansion threshold
    // EWMA weight (alpha)
    // Rate in/Rate out minimum and maximum

    /// <summary>
    /// Does Intrusion detection
    /// </summary>
    static class PacketAnalysis
    {
        class ExponentiallyWeightedMovingAverage
        {
            private float _alpha { get; set; }

            // packets/millisecond
            private ConcurrentList<float> RateList = new ConcurrentList<float>();
            private long _packetCount = 0;
            private long _someTimeInThePast = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            private DateTime lastReset = DateTime.Now;
            private TimeSpan timeToReset = new TimeSpan(0, 0, 30);

            public double exponentialMovingAverage
            {
                get
                {
                    if (DateTime.Now > lastReset.Add(timeToReset) && RateList.Count > 0)
                    {
                        //Debug.WriteLine("Something");
                        for (int i = 0; i < Math.Floor((float)(RateList.Count - 1) / 2); i++)
                        {
                            if (i >= RateList.Count - 1 || i < 0 || RateList.Count <= 0)
                            {
                                break;
                            }
                            try
                            {
                                RateList.RemoveAt(i);
                            }
                            catch (IndexOutOfRangeException)
                            {
                                break;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }
                    }
                    try
                    {

                        return RateList
                            .DefaultIfEmpty()
                            .Aggregate(RateList.FirstOrDefault(),
                            (ema, nextRate) => _alpha * nextRate + (1 - _alpha) * ema);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return 0;
                    }
                }
            }

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
                }
                catch (DivideByZeroException)
                {
                    return;
                }
            }

            public void AddValueToRateList(float value)
            {
                RateList.Add(value);
            }
        }
        class Node
        {
            public NodeRecord? parentRecord { get; set; }
            public Node? previousNode { get; set; }
            public Node? nextNode { get; set; }

            public NodeRecord[] records = new NodeRecord[256];
            public ExponentiallyWeightedMovingAverage nodeRatioAverage = new ExponentiallyWeightedMovingAverage(0.01f);

        }
        class NodeRecord
        {
            public ExponentiallyWeightedMovingAverage fromRate { get; set; }
            public ExponentiallyWeightedMovingAverage toRate { get; set; }
            public ExponentiallyWeightedMovingAverage ratioAverage { get; set; }
            public Node? child { get; set; }
            public int offenseCount = 0;
            public NodeRecord(float alpha)
            {
                fromRate = new ExponentiallyWeightedMovingAverage(alpha);
                toRate = new ExponentiallyWeightedMovingAverage(alpha);
                ratioAverage = new ExponentiallyWeightedMovingAverage(alpha);
            }
        }

        public static DateTime sniffingStart;
        private static DateTime _lastOffenseReset = DateTime.Now;

        // The bottom of the tree
        private static Node _rootNode = new Node();

        // IDS Settings
        public static int expansionThreshhold = 100; // Packets per Millisecond
        public static int offenseThreshold = 50; // Offenses per _stabilizationPeriod
        public static TimeSpan _stabilizationPeriod = new TimeSpan(0, 0, 30);
        public static float ratioLimitMin = 0.3f; // IncomingPacketRate / OutgoingPacketRate lower limit
        public static float ratioLimitMax = 2.3f; // IncomingPacketRate / OutgoingPacketRate upper limit
        public static float alpha = 0.01f;

        public static void analyzePacket(IPPacket packet)
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
            bool resetOffsense = DateTime.Now > _lastOffenseReset.Add(_stabilizationPeriod);
            for (int i = 0; i < 4; i++)
            {
                if(!SourceComplete)
                {
                    byte currentSourceByte = sourceAddress[i];
                    currentSourceNodeRecord = _rootNode.records[currentSourceByte];

                    if (currentSourceNodeRecord == null)
                    {
                        currentSourceNodeRecord = new NodeRecord(alpha);
                        currentBase.records[currentSourceByte] = currentSourceNodeRecord;
                    }
                    currentSourceNodeRecord.fromRate.AddPacketToRateList();
                    float currentAverage = Convert.ToSingle(currentSourceNodeRecord.toRate.exponentialMovingAverage / currentSourceNodeRecord.fromRate.exponentialMovingAverage);
                    if(currentAverage > 0 && currentAverage != float.PositiveInfinity)
                    {
                        currentSourceNodeRecord.ratioAverage.AddValueToRateList(currentAverage);
                        currentBase.nodeRatioAverage.AddValueToRateList(Convert.ToSingle(currentSourceNodeRecord.ratioAverage.exponentialMovingAverage));
                    }
                    if(resetOffsense)
                    {
                        currentSourceNodeRecord.offenseCount = 0;
                        _lastOffenseReset = DateTime.Now;
                    }
                    if((currentBase.nodeRatioAverage.exponentialMovingAverage > ratioLimitMax || currentBase.nodeRatioAverage.exponentialMovingAverage < ratioLimitMin) 
                        && currentSourceNodeRecord.ratioAverage.exponentialMovingAverage > 0 && DateTime.Now > sniffingStart.Add(_stabilizationPeriod))
                    {
                        currentSourceNodeRecord.offenseCount++;
                    }
                    if(currentSourceNodeRecord.offenseCount > offenseThreshold && currentBase.nodeRatioAverage.exponentialMovingAverage > 0 && deepestSourceLevel == 3)
                    {
                        //Debug.WriteLine("Source " + packet.SourceAddress.ToString() + " " + packet.DestinationAddress.ToString() + " " + currentSourceNodeRecord.ratioAverage.exponentialMovingAverage);
                        MessageBox.Show(packet.SourceAddress.ToString() + " was flagged as being malicous");
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
                        currentDestinationNodeRecord = new NodeRecord(alpha);
                        currentBase.records[currentDestinationByte] = currentDestinationNodeRecord;
                    }
                    currentDestinationNodeRecord.toRate.AddPacketToRateList();
                    float currentAverage = Convert.ToSingle(currentDestinationNodeRecord.toRate.exponentialMovingAverage / currentDestinationNodeRecord.fromRate.exponentialMovingAverage);
                    if(currentAverage > 0 && currentAverage != float.PositiveInfinity)
                    {
                        currentDestinationNodeRecord.ratioAverage.AddValueToRateList(currentAverage);
                        currentBase.nodeRatioAverage.AddValueToRateList(Convert.ToSingle(currentDestinationNodeRecord.ratioAverage.exponentialMovingAverage));
                    }
                    if(resetOffsense)
                    {
                        currentDestinationNodeRecord.offenseCount = 0;
                        _lastOffenseReset = DateTime.Now;
                    }
                    if ((currentBase.nodeRatioAverage.exponentialMovingAverage > ratioLimitMax || currentBase.nodeRatioAverage.exponentialMovingAverage < ratioLimitMin)
                        && currentBase.nodeRatioAverage.exponentialMovingAverage > 0 && DateTime.Now > sniffingStart.Add(_stabilizationPeriod))
                    {
                        //Debug.WriteLine("Added Offense");
                        currentDestinationNodeRecord.offenseCount++;
                    }
                    if(currentDestinationNodeRecord.offenseCount > offenseThreshold && currentBase.nodeRatioAverage.exponentialMovingAverage > 0 && deepestDestinationLevel == 3)
                    {
                        //Debug.WriteLine("Destination " + packet.SourceAddress.ToString() + " " + packet.DestinationAddress.ToString() + " " + currentDestinationNodeRecord.ratioAverage.exponentialMovingAverage);
                        MessageBox.Show(packet.DestinationAddress.ToString() + " was flagged as being malicous");
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
                //if (i == 4) Debug.WriteLine("i is 4");
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
    }
}