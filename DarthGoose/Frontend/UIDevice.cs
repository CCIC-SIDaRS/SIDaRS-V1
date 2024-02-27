using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using Backend.NetworkDeviceManager;
using Backend.CredentialManager;
using System.Reflection;
using System.Diagnostics;
using System.Xml.Linq;
using System.Windows.Threading;
using System.Printing;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace DarthGoose.Frontend
{
    class UIDevice
    {
        private static int gridCubeWidth = 50;
        private static int gridCubeHeight = 50;
        
        public Label image { get; set; }
        public List<Label> connections { get; set; }
        public List<Line> cables { get; set; }
        public DeviceDetails deviceMenu { get; set; }
        public string uid { get; private set; }

        protected readonly List<string> serializeable = new() { nameof(uid) };
        protected string currentCommand = "";
        protected bool commandComplete = false;

        private bool _drag;

        public UIDevice(Label image, List<Label> connections, List<Line> cables, string uid = null)
        {
            this.image = image;
            this.connections = connections;
            this.cables = cables;
            this.deviceMenu = new DeviceDetails();
            if (uid is null)
            {
                this.uid = DateTime.Now.ToString() + "-" + this.GetHashCode().ToString();
            }else
            {
                this.uid = uid;
            }
            this.image.MouseDown += DeviceMouseDown;
            this.image.MouseMove += DeviceMouseMove;
            this.image.MouseUp += DeviceMouseUp;

            FrontendManager.networkMap.MainCanvas.MouseMove += DeviceMouseMove;
            FrontendManager.networkMap.MainCanvas.MouseUp += DeviceMouseUp;

            deviceMenu.Closing += new CancelEventHandler(OnClosing);
            deviceMenu.DeleteDevice.Click += new RoutedEventHandler(OnDeleteDevice);
        }

        private void DeviceMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (FrontendManager.connecting)
            {
                FrontendManager.AddToPendingConnections(this.image);
            }
            else if (e.ClickCount < 2)
            {
                _drag = true;
            }
            else
            {
                this.deviceMenu.Show();
            }
        }

        private void DeviceMouseMove(object sender, MouseEventArgs e)
        {
            if (_drag)
            {
                Label draggedRectangle = this.image;
                Point newPoint = Mouse.GetPosition(FrontendManager.networkMap.MainCanvas);

                double left = Math.Round((newPoint.X - (draggedRectangle.Width / 2)) / gridCubeWidth)  * gridCubeWidth;
                double top = Math.Round((newPoint.Y - (draggedRectangle.Height / 2)) / gridCubeHeight) * gridCubeHeight;
                if (left + draggedRectangle.Width < FrontendManager.windowSize.X && left > 0 && top + draggedRectangle.Height < FrontendManager.windowSize.Y && top >= -FrontendManager.networkMap.TopMenu.Height)
                {
                    //Debug.WriteLine(newPoint.X);
                    Canvas.SetLeft(draggedRectangle, left);
                    Canvas.SetTop(draggedRectangle, top);
                    for (int i = 0; i < this.connections.Count; i++)
                    {
                        FrontendManager.drawConnection(new List<Label>() { draggedRectangle, this.connections[i] }, this.cables[i]);
                    }
                }
            }
        }

        private void DeviceMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine("Done");
            _drag = false;
        }

        private void OnClosing(object s, CancelEventArgs e)
        {
            Window sender = (Window)s;
            e.Cancel = true;
            sender.Hide();
        }

        private void OnDeleteDevice(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this device\nThis action will delete all data associated with this device", "SIDaRS", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FrontendManager.devices.Remove(image);
                FrontendManager.networkMap.MainCanvas.Children.Remove(image);
                deviceMenu.Close();
            }
        }
    }
    class EndpointDevice : UIDevice
    {
        public string v4Address { get; private set; }
        public string name { get; private set; }

        private string _deviceType { get; set; }
        private List<string> _serializeable = new() { nameof(name), nameof(v4Address), nameof(_deviceType) };

        public EndpointDevice(Label image, List<Label> connections, List<Line> cables, string v4Address, string name, string deviceType, string uid = null) : base(image, connections, cables, uid)
        {
            this.v4Address = v4Address;
            this.name = name;
            deviceMenu.Name.Text = name;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.TextChanged += OnAddressChanged;
            deviceMenu.SshTerminal.Visibility = Visibility.Hidden;
            _deviceType = deviceType;
            _serializeable.AddRange(serializeable);
        }

        private void OnNameChange(object sender, TextChangedEventArgs e)
        {
            name = deviceMenu.Name.Text;
            image.Content = name + "\n" + v4Address;
        }

        private void OnAddressChanged(object sender, TextChangedEventArgs e)
        {
            v4Address = deviceMenu.V4Address.Text;
            image.Content = name + "\n" + v4Address;
        }

        public string Save()
        {
            // Will serialize the device type, name (if configured), and v4Address (if configured) COMPLETE
            // will serialize the UID of each device that it is connected to COMPLETE
            // will serialize its current corrdinates COMPLETE

            var tempDict = new Dictionary<string, object>();

            foreach(PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (_serializeable.Contains(prop.Name))
                {
                    tempDict[prop.Name] = prop.GetValue(this);
                }
            }
            
            var tempList = new List<string>();
            foreach(Label device in connections)
            {
                tempList.Add(FrontendManager.devices[device].uid);
            }
            tempDict["connections"] = tempList;
            tempDict["location"] = new List<int>() { (int)Canvas.GetLeft(image), (int)Canvas.GetTop(image) };

            return JsonSerializer.Serialize(tempDict);
        }
    }

    class UINetDevice : UIDevice
    {
        private NetworkDevice _networkDevice { get; set; }
        // private readonly Task _terminalTask = new Task(RunTerminal);
        private string _deviceType { get; set; }
        private List<string> _serializeable = new() { nameof(_deviceType) };

        private bool _shiftDown { get; set; }
        private string lastCommand = string.Empty;

        private static readonly Dictionary<Key, string> _keyboard = new Dictionary<Key, string>
        {
            {Key.A, "a"},
            {Key.B, "b"},
            {Key.C, "c"},
            {Key.D, "d"},
            {Key.E, "e"},
            {Key.F, "f"},
            {Key.G, "g"},
            {Key.H, "h"},
            {Key.I, "i"},
            {Key.J, "j"},
            {Key.K, "k"},
            {Key.L, "l"},
            {Key.M, "m"},
            {Key.N, "n"},
            {Key.O, "o"},
            {Key.P, "p"},
            {Key.Q, "q"},
            {Key.R, "r"},
            {Key.S, "s"},
            {Key.T, "t"},
            {Key.U, "u"},
            {Key.V, "v"},
            {Key.W, "w"},
            {Key.X, "x"},
            {Key.Y, "y"},
            {Key.Z, "z"},
            {Key.D0, "0"},
            {Key.D1, "1"},
            {Key.D2, "2"},
            {Key.D3, "3"},
            {Key.D4, "4"},
            {Key.D5, "5"},
            {Key.D6, "6"},
            {Key.D7, "7"},
            {Key.D8, "8"},
            {Key.D9, "9"},
            {Key.NumPad0, "0"},
            {Key.NumPad1, "1"},
            {Key.NumPad2, "2"},
            {Key.NumPad3, "3"},
            {Key.NumPad4, "4"},
            {Key.NumPad5, "5"},
            {Key.NumPad6, "6"},
            {Key.NumPad7, "7"},
            {Key.NumPad8, "8"},
            {Key.NumPad9, "9"},
            {Key.OemTilde, "`"},
            {Key.OemMinus, "-"},
            {Key.OemPlus, "="},
            {Key.OemOpenBrackets, "["},
            {Key.OemCloseBrackets, "]"},
            {Key.OemPipe, "\\"},
            {Key.OemSemicolon, ";"},
            {Key.OemQuotes, "'"},
            {Key.OemComma, ","},
            {Key.OemPeriod, "."},
            {Key.OemQuestion, "/"},
            {Key.Space, " "}
        };
        private static readonly Dictionary<Key, string> _shiftedKeyboard = new Dictionary<Key, string>
        {
            {Key.A, "A"},
            {Key.B, "B"},
            {Key.C, "C"},
            {Key.D, "D"},
            {Key.E, "E"},
            {Key.F, "F"},
            {Key.G, "G"},
            {Key.H, "H"},
            {Key.I, "I"},
            {Key.J, "J"},
            {Key.K, "K"},
            {Key.L, "L"},
            {Key.M, "M"},
            {Key.N, "N"},
            {Key.O, "O"},
            {Key.P, "P"},
            {Key.Q, "Q"},
            {Key.R, "R"},
            {Key.S, "S"},
            {Key.T, "T"},
            {Key.U, "U"},
            {Key.V, "V"},
            {Key.W, "W"},
            {Key.X, "X"},
            {Key.Y, "Y"},
            {Key.Z, "Z"},
            {Key.D0, ")"},
            {Key.D1, "!"},
            {Key.D2, "@"},
            {Key.D3, "#"},
            {Key.D4, "$"},
            {Key.D5, "%"},
            {Key.D6, "^"},
            {Key.D7, "&"},
            {Key.D8, "*"},
            {Key.D9, "("},
            {Key.NumPad0, "0"},
            {Key.NumPad1, "1"},
            {Key.NumPad2, "2"},
            {Key.NumPad3, "3"},
            {Key.NumPad4, "4"},
            {Key.NumPad5, "5"},
            {Key.NumPad6, "6"},
            {Key.NumPad7, "7"},
            {Key.NumPad8, "8"},
            {Key.NumPad9, "9"},
            {Key.OemTilde, "~"},
            {Key.OemMinus, "_"},
            {Key.OemPlus, "+"},
            {Key.OemOpenBrackets, "{"},
            {Key.OemCloseBrackets, "}"},
            {Key.OemPipe, "|"},
            {Key.OemSemicolon, ":"},
            {Key.OemQuotes, "\""},
            {Key.OemComma, "<"},
            {Key.OemPeriod, ">"},
            {Key.OemQuestion, "?"},
            {Key.Space, " "},
        };

        public UINetDevice(Label image, List<Label> connections, List<Line> cables, string name, string v4Address, Credentials credentials, string assetsDir, string deviceType, string uid = null) : base(image, connections, cables, uid)
        {
            _networkDevice = new NetworkDevice(name, v4Address, credentials, assetsDir, ReadCallback);
            deviceMenu.Name.Text = name;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.V4Address.TextChanged += OnAddressChange;
            deviceMenu.DeviceDetailsTabs.SelectionChanged += OnTabChanged;
            _deviceType = deviceType;
            _serializeable.AddRange(serializeable);


            deviceMenu.KeyDown += KeyDown;
            deviceMenu.KeyUp += KeyUp;
        }
        // This should probably be changed so that there is a confirmation but that's Roman's problem :)
        private void OnNameChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeName(deviceMenu.Name.Text);
            image.Content = deviceMenu.Name.Text + "\n" + deviceMenu.V4Address.Text;
        }
        private void OnAddressChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeAddress(deviceMenu.V4Address.Text);
            image.Content = deviceMenu.Name.Text + "\n" + deviceMenu.V4Address.Text;
        }
        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (deviceMenu.DeviceDetailsTabs.SelectedIndex == 2)
            {
                if (_networkDevice.terminal is null)
                {
                    MessageBox.Show("Please wait for the connection to be completed before access the ssh terminal");
                } else
                {
                    _networkDevice.terminal.Connect();
                }
            } else if (deviceMenu.DeviceDetailsTabs.SelectedIndex != 2 && _networkDevice.terminal is not null)
            {
                _networkDevice.terminal.Disconnect();
            }
        }

        public void ReadCallback(string input)
        {
            Application.Current.Dispatcher.Invoke(() => { 
                Debug.WriteLine(input);
                //if (lastCommand != "" && input.Contains(lastCommand))
                //{
                //    return;
                //}
                deviceMenu.TerminalTextBox.Text += input; 
                deviceMenu.TerminalScroller.ScrollToBottom();
            });
        }

        public string Save()
        {
            // Will serialize the network device object, uid, device type, uids of connected devices, and current coordinates in the grid
            var tempDict = new Dictionary<string, object>();

            foreach(PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (_serializeable.Contains(prop.Name))
                {
                    tempDict[prop.Name] = prop.GetValue(this);
                }
            }

            var tempList = new List<string>();
            foreach (Label device in connections)
            {
                tempList.Add(FrontendManager.devices[device].uid);
            }
            tempDict["connections"] = tempList;
            tempDict["location"] = new List<int>() { (int)Canvas.GetLeft(image), (int)Canvas.GetTop(image) };
            tempDict["networkDevice"] = _networkDevice.Save();

            return JsonSerializer.Serialize(tempDict);
        }

        private void KeyDown(object sender, KeyEventArgs e)
        {
            bool foundKey = false;
            string key = "";

            try
            {
                if (_shiftDown)
                {
                    key = _shiftedKeyboard[e.Key];
                }
                else
                {
                    key = _keyboard[e.Key];
                }
                foundKey = true;
            }
            catch (KeyNotFoundException) { }

            if (foundKey)
            {
                deviceMenu.TerminalTextBox.Text += key;
                currentCommand += key;
            }
            else if (e.Key == Key.Back && deviceMenu.TerminalTextBox.Text.Length > 0 && currentCommand.Length > 0)
            {
                deviceMenu.TerminalTextBox.Text = deviceMenu.TerminalTextBox.Text.Substring(0, deviceMenu.TerminalTextBox.Text.Length - 1);
                currentCommand = currentCommand.Substring(0, currentCommand.Length - 1);
            }
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _shiftDown = true;
            }
            else if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                _networkDevice.terminal.SendCommand(currentCommand);
                lastCommand = currentCommand;
                currentCommand = string.Empty;
            }
            else if (e.Key == Key.Tab)
            {
                string completedCommand = TerminalManager.CiscoCommandCompletion(currentCommand.Split(" "));
                deviceMenu.TerminalTextBox.Text = deviceMenu.TerminalTextBox.Text.Substring(0, deviceMenu.TerminalTextBox.Text.Length - currentCommand.Length);
                currentCommand = completedCommand;
                deviceMenu.TerminalTextBox.Text += completedCommand;
            }
        }

        private void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _shiftDown = false;
            }
        }
    }
}