using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.CredentialManager;
using SSHBackend;
using System.Windows;
using System.Windows.Media.Animation;

namespace Backend.NetworkDeviceManager
{
    enum ManagementProtocol
    {
        SSH = 0,
        SSHNoExe = 1,
        SNMP = 2
    }

    class TerminalManager
    {
        public delegate void ReadCallback(string output);
        public bool connectionEstablished { get; set; }
        private string assetsDir { get; set; }
        private static Dictionary<string, object> catalystCommands { get; set; }
        private string v4address { get; set; }
        private ManagementProtocol protocol { get; set; }
        private Credentials credentials { get; set; }
        private SSHManager? sshManager { get; set; }
        private ReadCallback readCallback { get; set; }

        public TerminalManager(string assetsDir, string v4address, ManagementProtocol protocol, Credentials credentials, ReadCallback readCallback)
        {
            this.assetsDir = assetsDir;
            catalystCommands = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(this.assetsDir + @"\CiscoCommandTree.json")) ?? new Dictionary<string, object>();
            this.v4address = v4address;
            this.credentials = credentials;
            this.readCallback = readCallback;

            if (protocol == ManagementProtocol.SSH)
            {
                sshManager = new SSHManager(this.v4address, this.credentials, this.readCallback);
                AttemptConnection();
            }
        }

        public void AttemptConnection()
        {
            try
            {
                sshManager.Connect();
                var task = new Task(() => { sshManager.ExecuteExecChannel("ls"); });

                readCallback("Attempting SSH (Exec)...");

                task.Wait(TimeSpan.FromSeconds(5));

                if (connectionEstablished)
                {
                    this.protocol = ManagementProtocol.SSH;
                }
                else
                {
                    readCallback("SSH (Exec) unavailable\nAttempting SSHNoExec...");
                    this.protocol = ManagementProtocol.SSHNoExe;
                    sshManager.sshType = ManagementProtocol.SSHNoExe;
                }
                sshManager.Disconnect();
                connectionEstablished = true;
            }
            catch
            {
                MessageBox.Show(this.v4address + " could not be connected to using ssh,\n network management features will be disabled for this device until the address is corrected or the device is connected");
                connectionEstablished = false;
            }
        }

        public static string CiscoCommandCompletion(string[] currentCommand)
        {
            IEnumerable<string> matchingValues;

            if (currentCommand.Length > 1)
            {
                Dictionary<string, object> currentCommandDictionary = catalystCommands;
                try
                {
                    for (int i = 0; i <= currentCommand.Length - 2; i++)
                    {
                        currentCommandDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(currentCommandDictionary[currentCommand[i]].ToString()) ?? new Dictionary<string, object>();
                    }
                    matchingValues = currentCommandDictionary.Keys
                                    .Where(x => x.StartsWith(currentCommand[currentCommand.Length - 1]));
                }
                catch (KeyNotFoundException ex)
                {
                    throw new KeyNotFoundException(ex.ToString());
                }
                catch (Exception ex)
                {
                    throw new Exception("It errored: " + ex.ToString());
                }
            }
            else
            {
                matchingValues = catalystCommands.Keys
                                    .Where(x => x.StartsWith(currentCommand[0]));
            }
            matchingValues = from arrElement in matchingValues.Distinct()
                             orderby arrElement.Length
                             select arrElement;
            string[] updatedCommand = currentCommand;
            updatedCommand[currentCommand.Length - 1] = matchingValues.ToArray()[0];
            return string.Join(" ", updatedCommand);
        }

        public void SendCommand(string command)
        {
            if ((int)protocol <= 1)
            {
                if (protocol == ManagementProtocol.SSH)
                {
                    sshManager.ExecuteExecChannel(command);
                }
                else if (protocol == ManagementProtocol.SSHNoExe)
                {
                    sshManager.ExecuteShellStream(command);
                }
                else
                {
                    this.readCallback("Failed to send SSH command.");
                }
            }
            else
            {
                throw new Exception("SSH MUST EXIST YOU FOOL");
            }
        }
        public void Connect()
        {
            if ((int)protocol <= 1 && connectionEstablished)
            {
                sshManager.Connect();
            }else if (!connectionEstablished)
            {
                MessageBox.Show("Please connect the device before attempting to establish a connection");
            }
            else
            {
                throw new Exception("FU");
            }
        }
        public void Disconnect()
        {
            if ((int)protocol <= 1)
            {
                sshManager.Disconnect();
            }
            else
            {
                throw new Exception("FU");
            }
        }

        public void checkConnection(string output)
        {
            connectionEstablished = true;
        }
    }
}
