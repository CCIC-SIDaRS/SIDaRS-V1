﻿using System.Windows.Navigation;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Shapes;
using System.ComponentModel;
using Backend.CredentialManager;
using DarthGoose.UIObjects;
using System.Windows.Media;
using Backend.SaveManager;
using Backend.MonitorManager;

namespace DarthGoose.Frontend
{
    static class FrontendManager
    {
        public static bool connecting = false;
        public static MainWindow mainWindow;
        public static NetworkMap networkMap = new();
        public static Point windowSize;
        public static Dictionary<string, UIDevice> devices = new();

        private static LoginPage _loginPage = new();
        
        private static DeviceSetup _deviceSetupWindow = new();
        public static Credentials masterCredentials;

        public static void FrontendMain(MainWindow window)
        {
            mainWindow = window;
            mainWindow.MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            mainWindow.MainFrame.Navigate(_loginPage);
            windowSize = new Point(mainWindow.Width, mainWindow.Height);
            mainWindow.SizeChanged += OnWindowSizeChanged;
            mainWindow.Closing += new CancelEventHandler(MainWindowClosing);
            _loginPage.LoginButton.Click += new RoutedEventHandler(OnLoginEnter);
            _loginPage.CreateAccountButton.Click += new RoutedEventHandler(OnCreateNewAccount);
            _loginPage.LoginButton.IsDefault = true;

            // new MonitorSystem("192.168.1.110");
        }

        private static void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            windowSize = new Point(e.NewSize.Width, e.NewSize.Height);
        }

        private static void MainWindowClosing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private static void SetupNetworkMap()
        {
            mainWindow.Top = SystemParameters.PrimaryScreenHeight / 2 - 360;
            mainWindow.Left = SystemParameters.PrimaryScreenWidth / 2 - 640;
            mainWindow.Height = 720;
            mainWindow.Width = 1280;
            mainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

            networkMap.GooseSupport.Click += new RoutedEventHandler(GetGooseSupport);
            networkMap.InsertRouter.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertFirewall.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertUnmanagedSwitch.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertSwitch.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertEndPoint.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertServer.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertConnection.Click += new RoutedEventHandler(OnInsertConnection);
            networkMap.Save.Click += new RoutedEventHandler(OnSaveClick);
            networkMap.Load.Click += new RoutedEventHandler(OnLoadClick);
            networkMap.CancelConnection.Click += new RoutedEventHandler(OnCancelConnection);
            _deviceSetupWindow.FinishedSetup.Click += new RoutedEventHandler(OnFinishedSetup);

            mainWindow.MainFrame.Navigate(networkMap);
        }

        private static void OnLoginEnter(object sender, RoutedEventArgs e)
        {
            SymmetricEncryption.SetMaster("PhatWalrus123");
            if (_loginPage.LoginTitle.Text == "Welcome")
            {
                _loginPage = null;
                SetupNetworkMap();
            }else if(_loginPage.LoginTitle.Text == "Create New Account")
            {
                SymmetricEncryption.SetMaster(_loginPage.LoginPassword.Password);
                masterCredentials = new Credentials(_loginPage.LoginUsername.Text, _loginPage.LoginPassword.Password, false);
            }
        }

        private static void OnCreateNewAccount(object sender, RoutedEventArgs e)
        {
            _loginPage.LoginTitle.Text = "Create New Account";
        }

        private static void GetGooseSupport(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("GOOSE SUPPORT STARTING...");
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://uploads.dailydot.com/2019/10/Untitled_Goose_Game_Honk.jpeg",
                UseShellExecute = true
            });
        }
        private static bool _finishedSetup = false;
        private static async void InsertDeviceClick(object sender, RoutedEventArgs e)
        {
            MenuItem deviceType = (MenuItem)sender;
            Label label;
            TextBlock textBlock;
            string uid;
            CreateLabel(deviceType.Name, [20, 20], out label, out textBlock, out uid);

            if (deviceType.Name == "InsertRouter" || deviceType.Name == "InsertSwitch" || deviceType.Name == "InsertFirewall")
            {
                _deviceSetupWindow.Show();
                while (!_finishedSetup)
                {
                    await Task.Delay(25);
                    // Debug.WriteLine("Something");
                }
                // Debug.WriteLine(_deviceSetupWindow.SetupSSHPasswordBox.Password);
                devices[uid] = new UINetDevice(label, new List<string>(), new List<Line>(), _deviceSetupWindow.SetupNameBox.Text, _deviceSetupWindow.SetupV4AddressBox.Text, new Backend.CredentialManager.Credentials(_deviceSetupWindow.SetupSSHUsernameBox.Text, _deviceSetupWindow.SetupSSHPasswordBox.Password, false), @".\Backend\Assets", deviceType.Name, uid);
                textBlock.Text = _deviceSetupWindow.SetupNameBox.Text + "\n" + _deviceSetupWindow.SetupV4AddressBox.Text;
                _deviceSetupWindow.Close();
                _finishedSetup = false;
            }else
            {
                devices[uid] = new EndpointDevice(label, new List<string>(), new List<Line>(), "Not Configured", deviceType.Name + devices.Count(), deviceType.Name, uid);
                textBlock.Text = deviceType.Name + devices.Count() + "\nNot Configured";
            }
        }

        public static void CreateLabel(string deviceType, int[] location, out Label label, out TextBlock caption, out string uid)
        {
            BitmapImage bitMap = new BitmapImage();
            bitMap.BeginInit();
            switch (deviceType)
            {
                case "InsertRouter":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Router.png"));
                    break;
                case "InsertSwitch":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Switch.png"));
                    break;
                case "InsertUnmanagedSwitch":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Switch.png"));
                    break;
                case "InsertFirewall":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Firewall.png"));
                    break;
                case "InsertServer":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Server.png"));
                    break;
                case "InsertEndPoint":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Endpoint.png"));
                    break;
            }
            bitMap.EndInit();

            Image image = new Image();
            image.Source = bitMap;
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Width = 100;
            image.Height = 100;

            label = new Label();
            label.Width = 125;
            label.Height = 150;
            label.Foreground = new SolidColorBrush(Colors.White);
            label.Background = new SolidColorBrush(Colors.Black);
            label.HorizontalContentAlignment = HorizontalAlignment.Left;
            label.VerticalContentAlignment = VerticalAlignment.Top;


            caption = new TextBlock();
            caption.HorizontalAlignment = HorizontalAlignment.Center;
            caption.VerticalAlignment = VerticalAlignment.Top;
            caption.TextWrapping = TextWrapping.Wrap;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(caption);

            label.Content = stackPanel;

            Canvas.SetLeft(label, location[0]);
            Canvas.SetTop(label, location[1]);

            uid = DateTime.Now.ToString() + "-" + label.GetHashCode().ToString();

            networkMap.MainCanvas.Children.Add(label);
        }

        private static void OnFinishedSetup(object sender, RoutedEventArgs e)
        {
            _finishedSetup = true;
        }

        private static List<Label> devicesToBeConnected = new();
        private static void OnInsertConnection(object sender, RoutedEventArgs e)
        {
            connecting = true;
            networkMap.InfoText.Text = "Connecting Devices: 0 of 2 Devices Selected";
            networkMap.CancelConnection.Visibility = Visibility.Visible;
        }

        private static void OnCancelConnection(object sender, RoutedEventArgs e)
        {
            connecting = false;
            devicesToBeConnected.Clear();
            networkMap.InfoText.Text = string.Empty;
            networkMap.CancelConnection.Visibility = Visibility.Hidden;
        }

        private static void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var netDevices = new List<UINetDevice>();
            var endDevices = new List<EndpointDevice>();
            foreach (UIDevice device in devices.Values)
            {
                if (device.GetType() ==  typeof(UINetDevice))
                {
                    netDevices.Add(device as UINetDevice);
                }else
                {
                    endDevices.Add(device as EndpointDevice);
                }
            }
            SaveSystem.Save(@".\Backend\Assets\SaveFile.sidars",netDevices.ToArray(), endDevices.ToArray(), new Credentials("walrus","12345678!Aa", false));
        }

        private static void OnLoadClick (object sender, RoutedEventArgs e)
        {
            SaveSystem.Load(@".\Backend\Assets\SaveFile.sidars");
        }

        public static void AddToPendingConnections(Label sender)
        {
            devicesToBeConnected.Add(sender);
            if (devicesToBeConnected.Count() == 2)
            {
                string key1 = devices.FirstOrDefault(x => x.Value.image == devicesToBeConnected[0]).Key;
                string key2 = devices.FirstOrDefault(x => x.Value.image == devicesToBeConnected[1]).Key;
                devices[key1].connections.Add(key2);
                devices[key2].connections.Add(key1);
                drawConnection(devicesToBeConnected, new List<string> { key1, key2 });
                devicesToBeConnected.Clear();
                connecting = false;
                networkMap.InfoText.Text = string.Empty;
                networkMap.CancelConnection.Visibility = Visibility.Hidden;
            }
            else
            {
                networkMap.InfoText.Text = "Connecting Devices: 1 of 2 Devices Selected";
            }
        }

        public static void drawConnection(List<Label> connectedDevices, List<string> connectedUids, Line existingConnection = null)
        {
            Line line;
            if (existingConnection == null)
            {
                line = new Line();
                Thickness thickness = new Thickness(101, -11, 362, 250);
                line.Margin = thickness;
                line.Visibility = Visibility.Visible;
                line.StrokeThickness = 2;
                line.Stroke = Brushes.White;
            }
            else
            {
                line = existingConnection;
            }
            Label device1 = connectedDevices[0];
            Label device2 = connectedDevices[1];
            Point dev1Location = new Point(Canvas.GetLeft(device1), Canvas.GetTop(device1));
            Point dev2Location = new Point(Canvas.GetLeft(device2), Canvas.GetTop(device2));

            line.X1 = dev1Location.X - device1.Width / 2;
            line.Y1 = dev1Location.Y + device1.Height / 2;

            line.X2 = dev2Location.X - device2.Width / 2;
            line.Y2 = dev2Location.Y + device2.Height / 2;

            if (existingConnection == null)
            {
                networkMap.ConnectionCanvas.Children.Add(line);
                devices[connectedUids[0]].cables.Add(line);
                devices[connectedUids[1]].cables.Add(line);
            }
        }
    }
}
