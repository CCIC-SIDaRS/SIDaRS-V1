using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Xml.Serialization;
using CredentialManager;
using DarthGoose;

namespace NetworkDeviceManager
{
    class NetworkDevice
    {
        public string name { get; private set; }
        public string v4address { get; private set; }
        public int[] uiLocation {  get; private set; }
        public List<NetworkDevice> connections { get; private set; }
        public TerminalManager terminal { get; private set; }
        private Credentials credentials { get; set; }
        private string assetsDir { get; set; }
        private TerminalManager.ReadCallback readCallback { get; set; }
        public string uid { get; private set; }

        public NetworkDevice(string name, string v4address, int[] uiLocation, List<NetworkDevice> connections, Credentials credentials, string assetsDir, TerminalManager.ReadCallback readCallback, string uid = null)
        {
            this.name = name;
            this.v4address = v4address;
            this.uiLocation = uiLocation;
            this.connections = connections;
            this.credentials = credentials;
            this.assetsDir = assetsDir;
            this.readCallback = readCallback;
            if (uid != null)
            {
                this.uid = uid;
            } else
            {
                this.uid = DateTime.Now.ToString() + "-" + this.GetHashCode().ToString();
            }

            this.terminal = new TerminalManager(this.assetsDir, this.v4address, ManagementProtocol.SSH, this.credentials, this.readCallback);
        }
        public NetworkDevice(Dictionary<string, object> serializedData)
        {
            this.name = (string)serializedData[nameof(name)];
            this.v4address = (string)serializedData[nameof(v4address)];
            this.uiLocation = (int[])serializedData[nameof(uiLocation)];
            this.uid = (string)serializedData[nameof(uid)];
            this.assetsDir = (string)serializedData[nameof(assetsDir)];
            this.credentials = new Credentials(JsonSerializer.Deserialize<Dictionary<string,string>>((string)serializedData[nameof(credentials)]));
        }
        public string Save()
        {
            Dictionary<string, object> properties = new();
            foreach (PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Console.WriteLine(prop.Name);
                //TEMP!!!!!
                if (prop.Name.ToLower() == "readcallback" || prop.Name.ToLower() == "terminal")
                {
                    continue;
                } else if (prop.Name.ToLower() == "credentials")
                {
                    properties[prop.Name] = this.credentials.Save();
                    continue;
                } else if (prop.Name.ToLower() == "connections") {
                    List<string> uids = [];
                    foreach (NetworkDevice netDevice in this.connections)
                    {
                        uids.Add(netDevice.uid);
                    }
                    properties[prop.Name] = uids;
                    continue;
                }
                properties[prop.Name] = prop.GetValue(this);
            }
            return Regex.Replace (JsonSerializer.Serialize(properties), @"[^\u0000-\u007F]+", string.Empty);
        }
        public void SetConnections(List<NetworkDevice> connections)
        {
            this.connections = connections;
        }
    }
}
