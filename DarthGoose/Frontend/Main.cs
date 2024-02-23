using System.Windows.Navigation;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.IO;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using Backend.CredentialManager;
using DarthGoose.UIObjects;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace DarthGoose.Frontend
{
    public static class FrontendManager
    {
        public static bool connecting = false;
        public static MainWindow mainWindow;
        public static NetworkMap networkMap = new();
        public static Point windowSize;

        private static LoginPage _loginPage = new();
        private static Dictionary<Label, UIDevice> _devices = new();
        private static DeviceSetup _deviceSetupWindow = new();
        private static Credentials _masterCredentials;

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
            networkMap.GooseSupport.Click += new RoutedEventHandler(GetGooseSupport);
            networkMap.InsertRouter.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertFirewall.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertHub.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertSwitch.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertEndPoint.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertServer.Click += new RoutedEventHandler(InsertDeviceClick);
            networkMap.InsertConnection.Click += new RoutedEventHandler(OnInsertConnection);
            _deviceSetupWindow.FinishedSetup.Click += new RoutedEventHandler(OnFinishedSetup);
            mainWindow.MainFrame.Navigate(networkMap);
        }

        private static void OnLoginEnter(object sender, RoutedEventArgs e)
        {
            if(_loginPage.LoginTitle.Text == "Login")
            {
                _loginPage = null;
                SetupNetworkMap();
            }else if(_loginPage.LoginTitle.Text == "Create New Account")
            {
                SymmetricEncryption.SetMaster(_loginPage.LoginPassword.Password);
                _masterCredentials = new Credentials(_loginPage.LoginUsername.Text, _loginPage.LoginPassword.Password, false);
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
            BitmapImage bitMap = new BitmapImage();
            bitMap.BeginInit();
            switch (deviceType.Name)
            {
                case "InsertRouter":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Router.png"));
                    break;
                case "InsertSwitch":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Switch.png"));
                    break;
                case "InsertHub":
                    bitMap.UriSource = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"Images\Hub.png"));
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

            //Image image = new Image();
            //image.Source = bitMap;
            //image.Width = 100;
            //image.Height = 100;

            Label label = new Label();
            label.Background = new ImageBrush(bitMap);
            label.Width = 100;
            label.Height = 100;
            label.Foreground = new SolidColorBrush(Colors.White);
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            label.VerticalContentAlignment = VerticalAlignment.Bottom;

            Canvas.SetLeft(label, 20);
            Canvas.SetTop(label, 20);

            networkMap.MainCanvas.Children.Add(label);

            if (deviceType.Name == "InsertRouter" || deviceType.Name == "InsertSwitch" || deviceType.Name == "InsertFirewall")
            {
                SymmetricEncryption.SetMaster("PhatWalrus123");
                _deviceSetupWindow.Show();
                while (!_finishedSetup)
                {
                    await Task.Delay(25);
                    // Debug.WriteLine("Something");
                }
                // Debug.WriteLine(_deviceSetupWindow.SetupSSHPasswordBox.Password);
                _devices[label] = new UINetDevice(label, new List<Label>(), new List<Line>(), _deviceSetupWindow.SetupNameBox.Text, _deviceSetupWindow.SetupV4AddressBox.Text, new Backend.CredentialManager.Credentials(_deviceSetupWindow.SetupSSHUsernameBox.Text, _deviceSetupWindow.SetupSSHPasswordBox.Password, false), @".\Backend\Assets");
                label.Content = _deviceSetupWindow.SetupNameBox.Text + "\n" + _deviceSetupWindow.SetupV4AddressBox.Text;
                _deviceSetupWindow.Close();
                _finishedSetup = false;
            }else
            {
                _devices[label] = new EndpointDevice(label, new List<Label>(), new List<Line>(), "Not Configured", deviceType.Name + _devices.Count());
                label.Content = deviceType.Name + _devices.Count() + "\nNot Configured";
            }
        }

        private static void OnFinishedSetup(object sender, RoutedEventArgs e)
        {
            _finishedSetup = true;
        }

        private static List<Label> devicesToBeConnected = new();
        private static void OnInsertConnection(object sender, RoutedEventArgs e)
        {
            connecting = true;
        }

        public static void AddToPendingConnections(Label sender)
        {
            devicesToBeConnected.Add(sender);
            if (devicesToBeConnected.Count() == 2)
            {
                _devices[devicesToBeConnected[0]].connections.Add(devicesToBeConnected[1]);
                _devices[devicesToBeConnected[1]].connections.Add(devicesToBeConnected[0]);
                drawConnection(devicesToBeConnected);
                devicesToBeConnected.Clear();
                connecting = false;
            }
        }

        public static void drawConnection(List<Label> connectedDevices, Line existingConnection = null)
        {
            Line line;
            if (existingConnection == null)
            {
                line = new Line();
                Thickness thickness = new Thickness(101, -11, 362, 250);
                line.Margin = thickness;
                line.Visibility = Visibility.Visible;
                line.StrokeThickness = 2;
                line.Stroke = System.Windows.Media.Brushes.White;
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
                _devices[device1].cables.Add(line);
                _devices[device2].cables.Add(line);
            }
        }
    }
}
