using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;
using Backend.CredentialManager;
using DarthGoose.Frontend;
using Microsoft.Win32;
using PacketDotNet.Lldp;


namespace Backend.SaveManager
{
     static class SaveSystem
     {
        public static void SaveUsers(Credentials[] masterCredentials, string assets)
        {
            Debug.WriteLine(masterCredentials.Length);
            File.WriteAllText(assets, JsonSerializer.Serialize(masterCredentials));
        }

        public static Credentials[]? LoadUsers(string assets)
        {
            string file = File.ReadAllText(assets);
            if (File.Exists(assets) && file.Length > 0)
            {
                return JsonSerializer.Deserialize<Credentials[]>(file);
            } else
            {
                return Array.Empty<Credentials>();
            }
            
        }
        public static void Save(string saveFile, UINetDevice[] netDevices, EndpointDevice[] endpointDevices, Credentials masterCredentials)
        {
            Dictionary<string, object> saveDict = new();
            

            List<string> serializedNetDevices = new();
            foreach (UINetDevice netDevice in netDevices)
            {
                serializedNetDevices.Add(JsonSerializer.Serialize(netDevice));
            }
            List<string> serializedEndpointDevices = new();
            foreach (EndpointDevice endpointDevice in endpointDevices)
            {
                serializedEndpointDevices.Add(JsonSerializer.Serialize(endpointDevice));
            }

            saveDict["NetworkDevices"] = serializedNetDevices;
            saveDict["EndpointDevices"] = serializedEndpointDevices;   
            // Debug.WriteLine(JsonSerializer.Serialize(saveDict));
            File.WriteAllText(saveFile, JsonSerializer.Serialize(saveDict));
        }
        public static void Load(string saveFile)
        {
            Dictionary<string, object> saveDict = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(saveFile));

            // FrontendManager.masterCredentials = JsonSerializer.Deserialize<Credentials>(saveDict["MasterCredentials"].ToString());

            foreach (string device in JsonSerializer.Deserialize<List<string>>(saveDict["NetworkDevices"].ToString()))
            {
                UINetDevice tempDevice = JsonSerializer.Deserialize<UINetDevice>(device);
                FrontendManager.devices[tempDevice.uid] = tempDevice;
            }

            foreach(string device in JsonSerializer.Deserialize<List<string>>(saveDict["EndpointDevices"].ToString()))
            {
                EndpointDevice tempDevice = JsonSerializer.Deserialize<EndpointDevice>(device);
                FrontendManager.devices[tempDevice.uid] = tempDevice;
            }

            Dictionary<string, List<string>> connected = new();

            foreach (string device in FrontendManager.devices.Keys)
            {
                connected[device] = new();
                foreach (string connection in FrontendManager.devices[device].connections)
                {
                    try
                    {
                        if (!connected[connection].Contains(device))
                        {
                            connected[device].Add(connection);
                            FrontendManager.drawConnection(new List<Label>() { FrontendManager.devices[device].image, FrontendManager.devices[connection].image }, new List<string>() { device, connection });
                        }
                    }
                    catch (KeyNotFoundException) { }
                }
            }

            connected = null;
        }
    }
}