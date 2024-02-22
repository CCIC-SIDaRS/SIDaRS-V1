using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using Backend.NetworkDeviceManager;
using Backend.CredentialManager;
using System.Diagnostics;
using System.Xml.Linq;
using System.Windows.Threading;

namespace DarthGoose.Frontend
{
    class UIDevice
    {
        private static int gridCubeWidth = 50;
        private static int gridCubeHeight = 50;
        
        public Image image { get; set; }
        public List<Image> connections { get; set; }
        public List<Line> cables { get; set; }
        public DeviceDetails deviceMenu { get; set; }
        public string uid { get; private set; }

        protected string currentCommand = "";
        protected bool commandComplete = false;

        private bool _shiftDown { get; set; }
        private bool _drag;
        private Point _startPoint;

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

        public UIDevice(Image image, List<Image> connections, List<Line> cables)
        {
            this.image = image;
            this.connections = connections;
            this.cables = cables;
            this.deviceMenu = new DeviceDetails();
            this.uid = DateTime.Now.ToString() + "-" + this.GetHashCode().ToString();

            this.image.MouseDown += DeviceMouseDown;
            this.image.MouseMove += DeviceMouseMove;
            this.image.MouseUp += DeviceMouseUp;

            FrontendManager.networkMap.MainCanvas.MouseMove += DeviceMouseMove;
            FrontendManager.networkMap.MainCanvas.MouseUp += DeviceMouseUp;

            deviceMenu.Closing += new CancelEventHandler(OnClosing);
            deviceMenu.KeyDown += KeyDown;
            deviceMenu.KeyUp += KeyUp;
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
                _startPoint = Mouse.GetPosition(FrontendManager.networkMap.MainCanvas);
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
                Image draggedRectangle = this.image;
                Point newPoint = Mouse.GetPosition(FrontendManager.networkMap.MainCanvas);

                double left = Math.Round((newPoint.X - (draggedRectangle.Width / 2)) / gridCubeWidth)  * gridCubeWidth;
                double top = Math.Round((newPoint.Y - (draggedRectangle.Height / 2)) / gridCubeHeight) * gridCubeHeight;
                if (left + draggedRectangle.Width < FrontendManager.windowSize.X && left > 0 && top + draggedRectangle.Height < FrontendManager.windowSize.Y && top >= -FrontendManager.networkMap.TopMenu.Height)
                {
                    //Debug.WriteLine(newPoint.X);
                    Canvas.SetLeft(draggedRectangle, left);
                    Canvas.SetTop(draggedRectangle, top);
                    _startPoint = newPoint;
                    for (int i = 0; i < this.connections.Count; i++)
                    {
                        FrontendManager.drawConnection(new List<Image>() { draggedRectangle, this.connections[i] }, this.cables[i]);
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
                commandComplete = true;
            }
        }

        private void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _shiftDown = false;
            }
        }

        public void ReadCallback(string input)
        {
            Application.Current.Dispatcher.Invoke(() => { deviceMenu.TerminalTextBox.Text += input; });
        }
    }
    class EndpointDevice : UIDevice
    {
        public string v4Address { get; private set; }
        public EndpointDevice(Image image, List<Image> connections, List<Line> cables, string v4Address) : base(image, connections, cables)
        {
            this.v4Address = v4Address;
        }
    }

    class UINetDevice : UIDevice
    {
        private NetworkDevice _networkDevice { get; set; }
        // private readonly Task _terminalTask = new Task(RunTerminal);
        public UINetDevice(Image image, List<Image> connections, List<Line> cables, string name, string v4Address, Credentials credentials, string assetsDir) : base(image, connections, cables)
        {
            _networkDevice = new NetworkDevice(name, v4Address, credentials, assetsDir, base.ReadCallback);
            deviceMenu.Name.Text = name;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.V4Address.TextChanged += OnAddressChange;
            deviceMenu.DeviceDetailsTabs.SelectionChanged += OnTabChanged;
        }
        // This should probably be changed so that there is a confirmation but that's Roman's problem :)
        private void OnNameChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeName(deviceMenu.Name.Text);
        }
        private void OnAddressChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeAddress(deviceMenu.V4Address.Text);
        }
        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (deviceMenu.DeviceDetailsTabs.SelectedIndex == 2)
            {
                if(_networkDevice.terminal is null)
                {
                    MessageBox.Show("Please wait for the connection to be completed before access the ssh terminal");
                }else
                {
                    _networkDevice.terminal.Connect();
                    Thread thread = new Thread(RunTerminal);
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
        }

        private async void RunTerminal()
        {
            while(!commandComplete)
            {
                await Task.Delay(25);
            }
            commandComplete = false;
            if (currentCommand == "stop")
            {
                _networkDevice.terminal.Disconnect();
                currentCommand = "";
            }else
            {
                _networkDevice.terminal.SendCommand(currentCommand);
                currentCommand = "";
                RunTerminal();
            }
        }
    }
}