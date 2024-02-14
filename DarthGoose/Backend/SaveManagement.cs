/*using System.IO;
using System.Text.Json;
using Backend.CredentialManager;
using Backend.NetworkDeviceManager;


namespace SaveManager
{
    // Need to deserialize the uids and find common connections
     static class SaveSystem
     {
        public static void Save(string saveFile, NetworkDevice[] networkDevices, Credentials masterCredentials)
        {
            
            Dictionary<string, object> saveDict = new();
            saveDict["MasterCredentials"] = masterCredentials.Save();

            List<string> serializedNetDevices = new();
            foreach (NetworkDevice netDevice in networkDevices)
            {
                serializedNetDevices.Add(netDevice.Save());
            }

            saveDict["NetworkDevices"] = serializedNetDevices;
            Console.WriteLine(JsonSerializer.Serialize(saveDict));
            File.WriteAllText(saveFile, JsonSerializer.Serialize(saveDict));
        }
        public static void Load(string saveFile, out NetworkDevice[] networkDevices, out Credentials masterCredentials)
        {
            string data = File.ReadAllText(saveFile);
            Dictionary<string, string> dict = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
            masterCredentials = new Credentials (JsonSerializer.Deserialize<Dictionary<string,string>>(dict["MasterCredentials"]));
            List<string> serializedDevices = JsonSerializer.Deserialize<List<string>>(dict["NetworkDevices"]); 
            List<NetworkDevice> tempDevices = new();
            foreach (string device in serializedDevices)
            {
                tempDevices.Add(new NetworkDevice(JsonSerializer.Deserialize<Dictionary<string,object>>(device)));
            }
            for (int i = 0; i < tempDevices.Count; i++)
            {
                List<string> uids = JsonSerializer.Deserialize<List<string>>(JsonSerializer.Deserialize<Dictionary<string, string>>(serializedDevices[i])["connections"]);
                List<NetworkDevice> thisConnections = new();
                foreach (NetworkDevice device in tempDevices)
                {
                    if (uids.Contains(device.name))
                    {
                        thisConnections.Add(device);
                    }
                }
                tempDevices[i].SetConnections(thisConnections);
            }
            networkDevices = tempDevices.ToArray();
        }
     }
}*/