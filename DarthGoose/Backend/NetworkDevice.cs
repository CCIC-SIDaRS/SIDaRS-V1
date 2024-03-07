using System.Text.Json.Serialization;
using Backend.CredentialManager;

namespace Backend.NetworkDeviceManager
{
    class NetworkDevice
    {
        public string name { get; private set; }
        public string v4address { get; private set; }
        [JsonIgnore]
        public TerminalManager terminal { get; private set; }
        [JsonInclude]
        private Credentials credentials { get; set; }
        [JsonInclude]
        private string assetsDir { get; set; }
        private TerminalManager.ReadCallback readCallback { get; set; }

        public NetworkDevice(string name, string v4address, Credentials credentials, string assetsDir, TerminalManager.ReadCallback readCallback)
        {
            this.name = name;
            this.v4address = v4address;
            this.credentials = credentials;
            this.assetsDir = assetsDir;
            this.readCallback = readCallback;

            Task task = new Task(() => { this.terminal = new TerminalManager(this.assetsDir, this.v4address, ManagementProtocol.SSH, this.credentials, this.readCallback); });
            task.Start();
        }

        [JsonConstructor]
        public NetworkDevice (string name, string v4address, Credentials credentials, string assetsDir)
        {
            this.name = name;
            this.v4address = v4address;
            this.credentials = credentials;
            this.assetsDir = assetsDir;
        }

        public void SetCallBack(TerminalManager.ReadCallback readCallback)
        {
            this.readCallback = readCallback;

            Task task = new Task(() => { this.terminal = new TerminalManager(this.assetsDir, this.v4address, ManagementProtocol.SSH, this.credentials, this.readCallback); });
            task.Start();
        }

        public void ChangeName(string name)
        {
            this.name = name;
        }

        public void ChangeAddress(string address)
        {
            this.v4address = address;
        }
    }
}
