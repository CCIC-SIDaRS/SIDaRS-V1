using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Backend.CredentialManager;
using Backend.CredentialManager;

namespace Backend.NetworkDeviceManager
{
    class NetworkDevice
    {
        public string name { get; private set; }
        public string v4address { get; private set; }
        public TerminalManager terminal { get; private set; }
        private Credentials credentials { get; set; }
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

        public void ChangeName(string name)
        {
            this.name = name;
        }

        public void ChangeAddress(string address)
        {
            this.v4address = address;
        }

        public string Save()
        {
            // Will serialize all fields except the terminal and the readCallBackFunction

            var tempDict = new Dictionary<string, object>();

            foreach (PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.Name != "terminal" && prop.Name != "readCallback" && prop.Name != "credentials")
                {
                    tempDict[prop.Name] = prop.GetValue(this);
                }else if (prop.Name == "credentials")
                {
                    tempDict[prop.Name] = credentials.Save();
                }
            }
            return JsonSerializer.Serialize(tempDict);
        }
    }
}
