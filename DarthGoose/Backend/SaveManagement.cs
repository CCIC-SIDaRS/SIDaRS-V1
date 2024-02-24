using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Backend.CredentialManager;
using System.Text.RegularExpressions;
using Backend.NetworkDeviceManager;
using DarthGoose.Frontend;


namespace Backend.SaveManager
{
    // Need to deserialize the uids and find common connections
     static class SaveSystem
     {
        public static void Save(string saveFile, UINetDevice[] netDevices, EndpointDevice[] endpointDevices, Credentials masterCredentials)
        {
            
            Dictionary<string, object> saveDict = new();
            saveDict["MasterCredentials"] = masterCredentials.Save();

            List<string> serializedNetDevices = new();
            foreach (UINetDevice netDevice in netDevices)
            {
                serializedNetDevices.Add(netDevice.Save());
            }
            foreach (EndpointDevice endpointDevice in endpointDevices)
            {
                serializedNetDevices.Add(endpointDevice.Save());
            }

            saveDict["NetworkDevices"] = serializedNetDevices;
            // Debug.WriteLine(JsonSerializer.Serialize(saveDict));
            File.WriteAllText(saveFile, JsonSerializer.Serialize(saveDict));
        }
        /*public static void Load(string saveFile, out NetworkDevice[] networkDevices, out Credentials masterCredentials)
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
        }*/
     }
}