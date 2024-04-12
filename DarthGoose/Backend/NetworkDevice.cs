using System.Text.Json.Serialization;
using Backend.CredentialManager;

namespace Backend.NetworkDeviceManager
{   /// <summary>
    /// Defines Network Devices and allows them to be used by the front end
    /// </summary>
    class NetworkDevice
    {/// <summary>
    /// Sets up properties for storing information about a network device/handled during JSON serialization
    /// </summary>
        public string name { get; private set; }
        public string v4address { get; private set; }
        [JsonIgnore]
        public TerminalManager terminal { get; private set; }
        [JsonInclude]
        private Credentials credentials { get; set; }
        [JsonInclude]
        private string assetsDir { get; set; }
        private TerminalManager.ReadCallback readCallback { get; set; }
    /// <summary>
    /// Sets up a constructor for the NetworkDevice class
    /// </summary>
        public NetworkDevice(string name, string v4address, Credentials credentials, string assetsDir, TerminalManager.ReadCallback readCallback)
        {
            this.name = name;
            this.v4address = v4address;
            this.credentials = credentials;
            this.assetsDir = assetsDir;
            this.readCallback = readCallback;
            /// <summary>
            /// Creates a task to initilize the terminal asynchronously, and starts the task 
            /// </summary>
            Task task = new Task(() => { this.terminal = new TerminalManager(this.assetsDir, this.v4address, ManagementProtocol.SSH, this.credentials, this.readCallback); });
            task.Start();
        }
        /// <summary>
        /// Instatiates NetworkDevice objects during deserialization from JSON data
        /// </summary>
        [JsonConstructor]
        public NetworkDevice (string name, string v4address, Credentials credentials, string assetsDir)
        {
            this.name = name;
            this.v4address = v4address;
            this.credentials = credentials;
            this.assetsDir = assetsDir;
        }
        /// <summary>
        /// Callback function that reads terminal data is updated dynamically,and terminal manager is initalized asynchronously
        /// </summary>
        public void SetCallBack(TerminalManager.ReadCallback readCallback)
        {
            this.readCallback = readCallback;

            Task task = new Task(() => { this.terminal = new TerminalManager(this.assetsDir, this.v4address, ManagementProtocol.SSH, this.credentials, this.readCallback); });
            task.Start();
        }
        /// <summary>
        /// The device name can be changed during execution 
        /// </summary>
        public void ChangeName(string name)
        {
            this.name = name;
        }
        /// <summary>
        /// The address name can be changed during execution
        /// </summary>
        public void ChangeAddress(string address)
        {
            this.v4address = address;
        }
    }
}
