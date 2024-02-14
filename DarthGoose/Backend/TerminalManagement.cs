using System.Text.Json;
using System.IO;
using Backend.CredentialManager;
using SSHBackend;

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
        private string _assetsDir { get; set; }
        private Dictionary<string, object> _catalystCommands { get; set; }
        private string _v4address { get; set; }
        private ManagementProtocol _protocol { get; set; }
        private Credentials _credentials { get; set; }
        private SSHManager? _sshManager { get; set; }
        private ReadCallback _readCallback { get; set; }

        public TerminalManager(string assetsDir, string v4address, ManagementProtocol protocol, Credentials credentials, ReadCallback readCallback) 
        {
            this._assetsDir = assetsDir;
            this._catalystCommands = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(this._assetsDir + @"\CiscoCommandTree.json")) ?? new Dictionary<string, object>();
            this._v4address = v4address;
            this._credentials = credentials;
            this._readCallback = readCallback;

            if (protocol == ManagementProtocol.SSH)
            {
                _sshManager = new SSHManager(this._v4address, this._credentials, this._readCallback);
                _sshManager.Connect();
                var task = new Task(() => { _sshManager.ExecuteExecChannel("show version"); });

                readCallback("Attempting SSH (Exec)...");

                if (task.Wait(TimeSpan.FromSeconds(5)))
                {
                    this._protocol = ManagementProtocol.SSH;
                } else
                {
                    readCallback("SSH (Exec) unavailable\nAttempting SSHNoExec...");
                    this._protocol = ManagementProtocol.SSHNoExe;
                    _sshManager.sshType = ManagementProtocol.SSHNoExe;
                }
                _sshManager.Disconnect();
            }
        }

        public string[] CiscoCommandCompletion(string[] currentCommand)
        {
            IEnumerable<string> matchingValues;

            if (currentCommand.Length > 1)
            {
                Dictionary<string, object> currentCommandDictionary = _catalystCommands;
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
                matchingValues = _catalystCommands.Keys
                                    .Where(x => x.StartsWith(currentCommand[0]));
            }

            return matchingValues.ToArray();
        }

        public void SendCommand(string command)
        {
            if ((int) _protocol <= 1)
            {
                if (_protocol == ManagementProtocol.SSH)
                {
                    _sshManager.ExecuteExecChannel(command);
                }else if (_protocol == ManagementProtocol.SSHNoExe)
                {
                    _sshManager.ExecuteShellStream(command);
                }else
                {
                    this._readCallback("Failed to send SSH command.");
                }
            } else
            {
                throw new Exception("SSH MUST EXIST YOU FOOL");
            }
        }
        public void Connect()
        {
            if ((int) _protocol <= 1)
            {
                _sshManager.Connect();
            }else
            {
                throw new Exception("FU");
            }
        }
        public void Disconnect()
        {
            if ((int) _protocol <= 1)
            {
                _sshManager.Disconnect();
            }else
            {
                throw new Exception("FU");
            }
        }
    }
}
