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
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using System.Net;

namespace DarthGoose.Frontend
{
    class UIDevice
    {
        private static int gridCubeWidth = 50;
        private static int gridCubeHeight = 50;

        [JsonIgnore]
        public Label image { get; set; }
        public List<string> connections { get; set; }
        [JsonIgnore]
        public List<Line> cables { get; set; }
        [JsonIgnore]
        public DeviceDetails deviceMenu { get; set; }
        public string uid { get; private set; }

        protected string currentCommand = "";
        protected bool commandComplete = false;
        protected TextBlock caption;

        private bool _drag;
        [JsonInclude]
        private int[] _currentLocation;
        [JsonInclude]
        private string _deviceType;

        public UIDevice(Label image, List<string> connections, List<Line> cables, string uid, string deviceType)
        {
            this.image = image;
            this._currentLocation = [(int)Canvas.GetLeft(image), (int)Canvas.GetTop(image)];
            this.connections = connections;
            this.cables = cables;
            this.deviceMenu = new DeviceDetails();
            this.uid = uid;
            this._deviceType = deviceType;
            this.image.MouseDown += DeviceMouseDown;
            this.image.MouseMove += DeviceMouseMove;
            this.image.MouseUp += DeviceMouseUp;

            FrontendManager.networkMap.MainCanvas.MouseMove += DeviceMouseMove;
            FrontendManager.networkMap.MainCanvas.MouseUp += DeviceMouseUp;

            deviceMenu.Closing += new CancelEventHandler(OnClosing);
            deviceMenu.DeleteDevice.Click += new RoutedEventHandler(OnDeleteDevice);
        }

        public UIDevice(string deviceType, int[] location, List<string> connections, string uid)
        {
            this.connections = connections;
            TextBlock caption;
            Label label;
            string tempUid;
            FrontendManager.CreateLabel(deviceType, location, out label, out caption, out tempUid);
            this.uid = uid;
            this.image = label;
            this.caption = caption;
            this.cables = new List<Line>();
            this._currentLocation = location;
            this._deviceType = deviceType;

            caption = null;
            label = null;
            tempUid = null;

            this.deviceMenu = new DeviceDetails();
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
                    _currentLocation = [(int)left, (int)top];
                    for (int i = 0; i < this.connections.Count; i++)
                    {
                        FrontendManager.drawConnection(new List<Label>() { draggedRectangle, FrontendManager.devices[this.connections[i]].image }, new List<string>() { this.uid, this.connections[i] }, this.cables[i]);
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
                DestroyDevice();
            }
        }

        public void DestroyDevice()
        {
            FrontendManager.devices.Remove(uid);
            FrontendManager.networkMap.MainCanvas.Children.Remove(image);
            deviceMenu.Close();
        }
    }
    class EndpointDevice : UIDevice
    {
        public string v4Address { get; private set; }
        public string name { get; private set; }


        public EndpointDevice(Label image, List<string> connections, List<Line> cables, string v4Address, string name, string deviceType, string uid = null) : base(image, connections, cables, uid, deviceType)
        {
            this.v4Address = v4Address;
            this.name = name;
            deviceMenu.Name.Text = name;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.TextChanged += OnAddressChanged;
            deviceMenu.SshTerminal.Visibility = Visibility.Hidden;
        }

        [JsonConstructor]
        public EndpointDevice(string _deviceType, int[] _currentLocation, List<string> connections, string v4Address, string uid, string name) : base(_deviceType, _currentLocation, connections, uid)
        {
            this.v4Address = v4Address;
            this.name = name;
            deviceMenu.Name.Text = name;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.TextChanged += OnAddressChanged;
            deviceMenu.SshTerminal.Visibility = Visibility.Hidden;
            caption.Text = name + "\n" + v4Address;
        }

        private void OnNameChange(object sender, TextChangedEventArgs e)
        {
            name = deviceMenu.Name.Text;
            base.caption.Text = name + "\n" + v4Address;
        }

        private void OnAddressChanged(object sender, TextChangedEventArgs e)
        {
            v4Address = deviceMenu.V4Address.Text;
            base.caption.Text = name + "\n" + v4Address;
        }
    }

    class UINetDevice : UIDevice
    {
        [JsonInclude]
        private NetworkDevice _networkDevice { get; set; }
        // private readonly Task _terminalTask = new Task(RunTerminal);

        private bool _shiftDown { get; set; }

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

        public UINetDevice(Label image, List<string> connections, List<Line> cables, string name, string v4Address, Credentials credentials, string assetsDir, string deviceType, string uid = null) : base(image, connections, cables, uid, deviceType)
        {
            _networkDevice = new NetworkDevice(name, v4Address, credentials, assetsDir, ReadCallback);
            deviceMenu.Name.Text = name;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.V4Address.TextChanged += OnAddressChange;
            deviceMenu.DeviceDetailsTabs.SelectionChanged += OnTabChanged;


            deviceMenu.KeyDown += KeyDown;
            deviceMenu.KeyUp += KeyUp;
        }

        [JsonConstructor]
        public UINetDevice(string _deviceType, int[] _currentLocation, List<string> connections, string uid, NetworkDevice _networkDevice) : base(_deviceType, _currentLocation, connections, uid)
        {
            this._networkDevice = _networkDevice;
            deviceMenu.Name.Text = _networkDevice.name;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.Text = _networkDevice.v4address;
            deviceMenu.V4Address.TextChanged += OnAddressChange;
            deviceMenu.DeviceDetailsTabs.SelectionChanged += OnTabChanged;
            deviceMenu.KeyDown += KeyDown;
            deviceMenu.KeyUp += KeyUp;
            caption.Text = _networkDevice.name + "\n" + _networkDevice.v4address;
            _networkDevice.SetCallBack(ReadCallback);
        }
        // This should probably be changed so that there is a confirmation but that's Roman's problem :)
        private void OnNameChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeName(deviceMenu.Name.Text);
            base.caption.Text = deviceMenu.Name.Text + "\n" + deviceMenu.V4Address.Text;
        }
        private void OnAddressChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeAddress(deviceMenu.V4Address.Text);
            base.caption.Text = deviceMenu.Name.Text + "\n" + deviceMenu.V4Address.Text;
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
                
                if (deviceMenu.TerminalScroller.VerticalOffset == deviceMenu.TerminalScroller.ScrollableHeight)
                {
                    deviceMenu.TerminalTextBox.Text += input;
                    deviceMenu.TerminalScroller.ScrollToBottom();
                }
                else
                {
                    deviceMenu.TerminalTextBox.Text += input;
                }
            });
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
                deviceMenu.TerminalTextBox.Text = deviceMenu.TerminalTextBox.Text.Substring(0, deviceMenu.TerminalTextBox.Text.Length - currentCommand.Length);
                _networkDevice.terminal.SendCommand(currentCommand);
                currentCommand = "";
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