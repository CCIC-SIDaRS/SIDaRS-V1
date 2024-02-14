using Microsoft.Win32;
using System.Management;
using System.ServiceProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Security;
using Lextm.SharpSnmpLib.Messaging;
using System.Net.Sockets;
using System.Text;

namespace SNMPBackend
{
    class SNMPManager
    {
        public SNMPManager()
        {
            CatchTheWalrus();
        }
        private void StartSNMPTrapService()
        {
            ServiceController controller = new ServiceController("SNMPTrap");
            if (controller.Status == ServiceControllerStatus.Stopped)
                controller.Start();
        }
        public void CatchTheWalrus()
        {
            // TODO: We couldn't catch the walrus, maybe another time..?
            UdpClient udpListener = new UdpClient(162); // SNMP traps are typically sent to port 162

            Console.WriteLine("Listening for SNMP traps. Press any key to exit.");

            while (true)
            {
                // Listen for incoming SNMP traps
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpListener.Receive(ref remoteEndPoint);

                // Process the received SNMP trap data
                string trapMessage = Encoding.ASCII.GetString(receivedData);
                Console.WriteLine($"Received SNMP trap from {remoteEndPoint}: {trapMessage}");
            }
        }
       
    }
}