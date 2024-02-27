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
        public static void Load(string saveFile)
        {
            foreach(UIDevice device in FrontendManager.devices.Values)
            {
                 device.DestroyDevice();
            }
            FrontendManager.devices.Clear();
            string data = File.ReadAllText(saveFile);
            Dictionary<string, object> dict = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
            FrontendManager.masterCredentials = new Credentials(JsonSerializer.Deserialize<Dictionary<string, string>>(dict["MasterCredentials"].ToString()));
            SymmetricEncryption.SetMaster("PhatWalrus123");
            List<string> serializedDevices = JsonSerializer.Deserialize<List<string>>(dict["NetworkDevices"].ToString());
            List<Label> deserializedDevices = new();
            foreach (string device in serializedDevices)
            {
                Dictionary<string, object> deviceInfo = JsonSerializer.Deserialize<Dictionary<string,object>>(device);
                Debug.WriteLine(deviceInfo);
                string managementStyle;
                Label image;
                TextBlock imageLabel;
                createLabel(deviceInfo["_deviceType"].ToString(), JsonSerializer.Deserialize<int[]>(deviceInfo["location"].ToString()), out managementStyle, out image, out imageLabel);
                deserializedDevices.Add(image);
                if(managementStyle == "managed")
                {
                    Dictionary<string, object> networkDevice = JsonSerializer.Deserialize<Dictionary<string, object>>(deviceInfo["networkDevice"].ToString());
                    Dictionary<string, string> credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(networkDevice["credentials"].ToString());
                    FrontendManager.devices[image] = new UINetDevice(image, new List<Label>(), new List<Line>(), networkDevice["name"].ToString(), networkDevice["v4address"].ToString(), new Credentials(credentials["_username"], credentials["_password"]), @".\Backend\Assets", deviceInfo["_deviceType"].ToString(), deviceInfo["uid"].ToString());
                    imageLabel.Text = networkDevice["name"].ToString() + "\n" + networkDevice["v4address"].ToString();
                }
                else if(managementStyle == "unmanaged")
                {
                    FrontendManager.devices[image] = new EndpointDevice(image, new List<Label>(), new List<Line>(), deviceInfo["v4Address"].ToString(), deviceInfo["name"].ToString(), deviceInfo["_deviceType"].ToString(), deviceInfo["uid"].ToString());
                    imageLabel.Text = deviceInfo["name"].ToString() + "\n" + deviceInfo["v4Address"].ToString();
                }
                else if (managementStyle == "FU")
                {
                    throw new Exception("FU");
                }
            }
            for(int i = 0; i < serializedDevices.Count(); i++)
            {
                List<string> connections = JsonSerializer.Deserialize<List<string>>(JsonSerializer.Deserialize<Dictionary<string, object>>(serializedDevices[i])["connections"].ToString());
                foreach (string uid in connections)
                {
                    foreach(KeyValuePair<Label,UIDevice> uiDevice in FrontendManager.devices)
                    {
                        if (uiDevice.Value.uid == uid)
                        {
                            FrontendManager.drawConnection(new List<Label> { uiDevice.Key, deserializedDevices[i] });
                            uiDevice.Value.connections.Add(deserializedDevices[i]);
                            FrontendManager.devices[deserializedDevices[i]].connections.Add(uiDevice.Key);
                        }
                    }
                }
            }
        }

        private static void createLabel(string deviceType, int[] location, out string managementStyle, out Label image, out TextBlock imageLabel)
        {
            BitmapImage bitMap = new BitmapImage();
            bitMap.BeginInit();
            switch (deviceType)
            {
                case "InsertRouter":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Router.png"));
                    managementStyle = "managed";
                    break;
                case "InsertSwitch":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Switch.png"));
                    managementStyle = "managed";
                    break;
                case "InsertUnmanagedSwitch":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Switch.png"));
                    managementStyle = "unmanaged";
                    break;
                case "InsertFirewall":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Firewall.png"));
                    managementStyle = "managed";
                    break;
                case "InsertServer":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Server.png"));
                    managementStyle = "unmanaged";
                    break;
                case "InsertEndPoint":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Endpoint.png"));
                    managementStyle = "unmanaged";
                    break;
                default:
                    managementStyle = "FU";
                    break;
            }
            bitMap.EndInit();

            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
            label.Width = 125;
            label.Height = 150;
            label.Foreground = new SolidColorBrush(Colors.White);
            label.HorizontalContentAlignment = HorizontalAlignment.Left;
            label.VerticalContentAlignment = VerticalAlignment.Top;

            Image picture = new Image();
            picture.HorizontalAlignment = HorizontalAlignment.Left;
            picture.VerticalAlignment = VerticalAlignment.Top;
            picture.Source = bitMap;
            picture.Width = 100;
            picture.Height = 100;

            TextBlock textBlock = new TextBlock();
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(picture);
            stackPanel.Children.Add(textBlock);

            label.Content = stackPanel;

            Canvas.SetLeft(label, location[0]);
            Canvas.SetTop(label, location[1]);

            FrontendManager.networkMap.MainCanvas.Children.Add(label);
            image = label;
            imageLabel = textBlock;
        }
    }
}