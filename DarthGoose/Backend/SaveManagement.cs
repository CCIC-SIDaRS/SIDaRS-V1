using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Backend.CredentialManager;
using DarthGoose.Frontend;
using System.Windows.Shapes;
using System.IO.Packaging;
using System.Diagnostics;


namespace Backend.SaveManager
{
     static class SaveSystem
     {
        public static void Save(string saveFile, UINetDevice[] netDevices, EndpointDevice[] endpointDevices, Credentials masterCredentials)
        {
            
            Dictionary<string, object> saveDict = new();
            saveDict["MasterCredentials"] = JsonSerializer.Serialize(masterCredentials);

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
            
        }
    }
}