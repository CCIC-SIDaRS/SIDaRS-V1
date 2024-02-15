using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Backend.CredentialManager;

namespace Backend.NetworkDeviceManager
{
    class NetworkDevice
    {
        public string name { get; private set; }
        public string? v4address { get; private set; }
        public TerminalManager terminal { get; private set; }

        private Credentials _credentials { get; set; }
        private string _assetsDir { get; set; }
        private TerminalManager.ReadCallback _readCallback { get; set; }

        public NetworkDevice(string name, string v4address = null, Credentials credentials, string assetsDir, TerminalManager.ReadCallback readCallback, string uid = null)
        {
            this.name = name;
            this.v4address = v4address;
            this._credentials = credentials;
            this._assetsDir = assetsDir;
            this._readCallback = readCallback;

            this.terminal = new TerminalManager(this._assetsDir, this.v4address, ManagementProtocol.SSH, this._credentials, this._readCallback);
        }

        public NetworkDevice(Dictionary<string, object> serializedData)
        {
            this.name = (string)serializedData[nameof(name)];
            this.v4address = (string)serializedData[nameof(v4address)];
            this._assetsDir = (string)serializedData[nameof(_assetsDir)];
            this._credentials = new Credentials(JsonSerializer.Deserialize<Dictionary<string,string>>((string)serializedData[nameof(_credentials)]));
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
                    properties[prop.Name] = this._credentials.Save();
                    continue;
                }
                properties[prop.Name] = prop.GetValue(this);
            }
            return Regex.Replace (JsonSerializer.Serialize(properties), @"[^\u0000-\u007F]+", string.Empty);
        }
    }
}
