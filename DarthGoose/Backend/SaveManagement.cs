using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;
using Backend.CredentialManager;
using Backend.MonitorManager;
using DarthGoose.Frontend;
using Microsoft.Win32;
using PacketDotNet.Lldp;
using SharpPcap;
using System.Linq;
using System.Windows;


namespace Backend.SaveManager
{
     static class SaveSystem
     {
        /// <summary>
        /// Saves user credentials to a file in JSON
        /// </summary>
        public static void SaveUsers(Users[] masterCredentials, string assets)
        {
            //Debug.WriteLine(masterCredentials.Length);
            File.WriteAllText(assets, JsonSerializer.Serialize(masterCredentials));
        }
        /// <summary>
        /// Reads user credentials from a file, returns as an array of "Users", creates empty array if empty or doesn't exist 
        /// </summary>
        public static Users[]? LoadUsers(string assets)
        {
            string file = File.ReadAllText(assets);
            if (File.Exists(assets) && file.Length > 0)
            {
                return JsonSerializer.Deserialize<Users[]>(file);
            } else
            {
                return Array.Empty<Users>();
            }
            
        }
       /// <summary>
       /// Organizes devices into a dictionary, dictionary is saved as a JSON file
       /// </summary>
        public static void Save(string saveFile, UINetDevice[] netDevices, EndpointDevice[] endpointDevices, PacketAnalysis packetAnalyzer)
        {
            Dictionary<string, object> saveDict = new();

            saveDict["AuthHash"] = SymmetricEncryption.Encrypt(FrontendManager.masterCredentials.GetUsername(), SymmetricEncryption.master);
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
            saveDict["packetAnalyzer"] = JsonSerializer.Serialize(packetAnalyzer);
            // Debug.WriteLine(JsonSerializer.Serialize(saveDict));
            File.WriteAllText(saveFile, JsonSerializer.Serialize(saveDict));
        }
        public static void Load(string saveFile)
        {
            Dictionary<string, object>? saveDict = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(saveFile));

            if(saveDict == null)
            {
                MessageBox.Show("Save file was unreadable");
                return;
            }

            string? authHash = saveDict["AuthHash"].ToString();
            if(authHash == null || SymmetricEncryption.Decrypt(authHash, SymmetricEncryption.master) != FrontendManager.masterCredentials.GetUsername())
            {
                MessageBox.Show("Cannot load this save file either because it does not belong to the account that is currently authenticated or it does not the correct authentication format");
                saveDict = null;
                return;
            }

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
            FrontendManager.packetAnalyzer = JsonSerializer.Deserialize<PacketAnalysis>(saveDict["packetAnalyzer"].ToString());

            connected = null;
        }
    }
}