using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using Backend.NetworkDeviceManager;
using Backend.CredentialManager;
using System.Text.Json.Serialization;

namespace DarthGoose.Frontend
{
    /// <summary>
    /// Stores all data pertaining to the UIElements of a specific object
    /// Includes funtions that control interaction with UIObjects
    /// Also enables serialization of important fields
    /// </summary>
    class UIDevice
    {
        // Sets the size of the snap grid
        private static int gridCubeWidth = 50;
        private static int gridCubeHeight = 50;

        // Stores the System.Windows.Controls.Label object that the user sees on the network map
        // Is ignored during serialization
        [JsonIgnore]
        public Label image { get; set; }
        // Stores the UIDs of the devices the object is connected to
        // Included in serialization
        public List<string> connections { get; set; }
        // Stores the System.Windows.Shapes.Line objects that visually connect the object to others
        // Is ignored during serialization
        [JsonIgnore]
        public List<Line> cables { get; set; }
        // Stores a DeviceDetails object that represents the device detailsz window for this specific device
        // Is ignored during serialization
        [JsonIgnore]
        public DeviceDetails deviceMenu { get; set; }
        // Stores the unique identifier for this UIObject
        // Is used during serialization
        public string uid { get; private set; }

        // Holds the current command the user has typed into the ssh terminal
        // Not used during serialization
        protected string currentCommand = "";
        // If true then the user has finished entering their command into the terminal textblock
        // Not used during serialization
        protected bool commandComplete = false;
        // Holds the TextBlock object for the caption that appears below a device
        // Is not used during serialization
        protected TextBlock caption;

        //If true the user is moving/ dragging the object
        //Ignored during serialization
        private bool _drag;
        //Defines the location of the device on the networkmap
        //Is used during serialization
        [JsonInclude]
        private int[] _currentLocation;
        //Defines the type of device this is; See OnInsertDeviceClick for more information about the string format
        // Used during serialzation
        [JsonInclude]
        private string _deviceType;

        /// <summary>
        /// Standard Class Constructor
        /// </summary>
        /// <param name="image" Type="System.Windows.Controls.Label"></param>
        /// <param name="connections" Type="List<string>"></param>
        /// <param name="cables" Type="List<System.Windows.Shapes.Line"></param>
        /// <param name="uid" Type="string"></param>
        /// <param name="deviceType" Type="string">
        ///     more information on string format in OnInsertDeviceClick found in Main.cs
        /// </param>
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

        /// <summary>
        /// JSON Constructor
        /// </summary>
        /// <param name="deviceType" Type="string"></param>
        /// <param name="location" Type="int[]">
        ///  location[0] = X Coordiante
        ///  location[1] = Y Coordinate
        /// </param>
        /// <param name="connections" Type="List<string>"></param>
        /// <param name="uid" Type="string uid"></param>
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

        /// <summary>
        /// Executes when the user presses their left mouse button on the image
        /// Sets the drag variable eqaul to True if the user has only clicked once
        /// Opens the deviceMenu if the user double clicks
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="MouseButtonEventArgs"></param>
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

        /// <summary>
        /// Executes when the user moves the mouse
        /// if the drag variable has been set to true
        ///     gets the new device location based on the nearest grid location to mouse
        ///     if the new location is within the bounds of the network map
        ///         moves the UIObject and redraws the connections to match the new location
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="MouseEventArgs"></param>
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

        /// <summary>
        /// Triggers when the user stops holding the left click button down
        /// Sets the drag variable to false
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="MouseButtonEventArgs"></param>
        private void DeviceMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine("Done");
            _drag = false;
        }

        /// <summary>
        /// Executes when the user selects the close button on the devicemenu
        /// Hides the window but keeps it running in the background
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="CancelEventArgs"></param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            Window window = (Window)sender;
            e.Cancel = true;
            window.Hide();
        }

        /// <summary>
        /// Executes when the user selects the delete device button in the device menu
        /// if the user confirms deletion
        ///     removes all references to the object
        /// </summary>
        /// <param name="sender" type="object"></param>
        /// <param name="e" type="RoutedEventArgs"></param>
        private void OnDeleteDevice(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this device\nThis action will delete all data associated with this device", "SIDaRS", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DestroyDevice();
            }
        }

        /// <summary>
        /// removes all references to this object in other areas of the program and actually closes the decvice menu window
        /// </summary>
        public void DestroyDevice()
        {
            FrontendManager.devices.Remove(uid);
            FrontendManager.networkMap.MainCanvas.Children.Remove(image);
            deviceMenu.Close();
        }
    }

    /// <summary>
    /// Stores information and functions for devices that are not managed over the network by SIDaRS
    /// Inherits from the UIDevice class
    /// </summary>
    class EndpointDevice : UIDevice
    {
        // stores the IPAddress (if configured)
        // used during serialization
        public string v4Address { get; private set; }

        // stores the name for the device (if configured)
        // used during serialization
        public string name { get; private set; }

        /// <summary>
        /// Standard class constructor
        /// Also calls the UIDevice constructor
        /// </summary>
        /// <param name="image" Type="System.Windows.Controls.Label"></param>
        /// <param name="connections" Type="List<string>"></param>
        /// <param name="cables" Type="List<System.Windows.Shapes.Line>"></param>
        /// <param name="v4Address" Type="string"></param>
        /// <param name="name" Type="string"></param>
        /// <param name="deviceType" Type="string"></param>
        /// <param name="uid" Type="string"></param>
        public EndpointDevice(Label image, List<string> connections, List<Line> cables, string v4Address, string name, string deviceType, string uid = null) : base(image, connections, cables, uid, deviceType)
        {
            this.v4Address = v4Address;
            this.name = name;
            deviceMenu.Name.Text = name;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.TextChanged += OnAddressChanged;
            deviceMenu.RetryConnection.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// JsonConstructor
        /// Also calls the UIDevice json constructor
        /// </summary>
        /// <param name="_deviceType" Type="string"></param>
        /// <param name="_currentLocation" Type="int[]">
        /// currentLocation[0] = X
        /// currentLocation[1] = y
        /// </param>
        /// <param name="connections" Type="List<string>"></param>
        /// <param name="v4Address" Type="string"></param>
        /// <param name="uid" Type="string"></param>
        /// <param name="name" Type="string"></param>
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

        /// <summary>
        /// Changes the device name both internally and on the label whenever the user changes the text in the DeviceName box in the device menu
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="TextChangedEventArgs"></param>
        private void OnNameChange(object sender, TextChangedEventArgs e)
        {
            name = deviceMenu.Name.Text;
            base.caption.Text = name + "\n" + v4Address;
        }

        /// <summary>
        /// Changes the device ipaddress both internally and on the label whenever the user changes the text in the ipaddress box in the device menu
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="TextChangedEventArgs"></param>
        private void OnAddressChanged(object sender, TextChangedEventArgs e)
        {
            v4Address = deviceMenu.V4Address.Text;
            base.caption.Text = name + "\n" + v4Address;
        }
    }

    /// <summary>
    /// Holds data and functions pertaining to devices that are managed over the network by SIDaRS
    /// Inherits from UIDevice
    /// </summary>
    class UINetDevice : UIDevice
    {
        // contains a network device object specific to this device
        // used during serialization
        [JsonInclude]
        private NetworkDevice _networkDevice { get; set; }

        // Set to true if the shift key is pressed
        // Not used during serialization
        private bool _shiftDown { get; set; }

        // Key Mappings for terminal keyboard
        // Not used during serialzation
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

        // Key mapping for shifted keyboard
        // Not used during serialization
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

        /// <summary>
        /// Standard class constructor
        /// Also calls the Standard UIDevice contructor
        /// </summary>
        /// <param name="image"Type="System.Windows.Controls.Label"></param>
        /// <param name="connections" Type="List<string>"></param>
        /// <param name="cables" Type="List<System.Windows.Shapes.Line>"></param>
        /// <param name="name" Type="string"></param>
        /// <param name="v4Address" Type="string"></param>
        /// <param name="credentials" Type="Backend.CredentialManager.Credentials">
        ///     An object that contains the SSH credentials for the device in question
        /// </param>
        /// <param name="assetsDir" Type="string"></param>
        /// <param name="deviceType" Type="string"></param>
        /// <param name="uid" Type="string"></param>
        public UINetDevice(Label image, List<string> connections, List<Line> cables, string name, string v4Address, Credentials credentials, string assetsDir, string deviceType, string uid = null) : base(image, connections, cables, uid, deviceType)
        {
            _networkDevice = new NetworkDevice(name, v4Address, credentials, assetsDir, ReadCallback);
            deviceMenu.Name.Text = name;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.Text = v4Address;
            deviceMenu.V4Address.TextChanged += OnAddressChange;
            deviceMenu.DeviceDetailsTabs.SelectionChanged += OnTabChanged;
            deviceMenu.RetryConnection.Click += RetryConnectionClick;
            deviceMenu.RetryConnection.Visibility = Visibility.Visible;


            deviceMenu.KeyDown += KeyDown;
            deviceMenu.KeyUp += KeyUp;
        }

        /// <summary>
        /// Json Constructor
        /// Also calls the UIDevice Json constructor
        /// </summary>
        /// <param name="_deviceType" Type="string"></param>
        /// <param name="_currentLocation" Type="int[]">
        /// _currentLocation[0] = X
        /// _currentLocation[1] = Y
        /// </param>
        /// <param name="connections" Type="List<string>"></param>
        /// <param name="uid" Type="string"></param>
        /// <param name="_networkDevice" Type="Backend.NetworkDeviceManager.NetworkDevice"></param>
        [JsonConstructor]
        public UINetDevice(string _deviceType, int[] _currentLocation, List<string> connections, string uid, NetworkDevice _networkDevice) : base(_deviceType, _currentLocation, connections, uid)
        {
            this._networkDevice = _networkDevice;
            deviceMenu.Name.Text = _networkDevice.name;
            deviceMenu.Name.TextChanged += OnNameChange;
            deviceMenu.V4Address.Text = _networkDevice.v4address;
            deviceMenu.V4Address.TextChanged += OnAddressChange;
            deviceMenu.DeviceDetailsTabs.SelectionChanged += OnTabChanged;
            deviceMenu.RetryConnection.Click += RetryConnectionClick;
            deviceMenu.RetryConnection.Visibility = Visibility.Visible;
            deviceMenu.KeyDown += KeyDown;
            deviceMenu.KeyUp += KeyUp;
            caption.Text = _networkDevice.name + "\n" + _networkDevice.v4address;
            _networkDevice.SetCallBack(ReadCallback);
        }
        // This should probably be changed so that there is a confirmation but that's Roman's problem :)
        /// <summary>
        /// Changes the device name both internally and on the label whenever the user changes the text in the DeviceName box in the device menu
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="TextChangedEventArgs"></param>
        private void OnNameChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeName(deviceMenu.Name.Text);
            base.caption.Text = deviceMenu.Name.Text + "\n" + deviceMenu.V4Address.Text;
        }

        /// <summary>
        /// Changes the device ipaddress both internally and on the label whenever the user changes the text in the ipaddress box in the device menu
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="TextChangedEventArgs"></param>
        private void OnAddressChange(object sender, TextChangedEventArgs e)
        {
            _networkDevice.ChangeAddress(deviceMenu.V4Address.Text);
            base.caption.Text = deviceMenu.Name.Text + "\n" + deviceMenu.V4Address.Text;
        }

        /// <summary>
        /// Executes when the user changes the tab in the device menu
        /// If the user changed to the ssh terminal tab it connects to the device terminal
        /// If the user changes from the ssh terminal tab it disconnects from the device terminal
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="SelectionChangedEventArgs"></param>
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

        /// <summary>
        /// Adds text into the temrinal TextBlock and formats it
        /// </summary>
        /// <param name="input" Type="string"></param>
        public void ReadCallback(string input)
        {
            Application.Current.Dispatcher.Invoke(() => { 
                //Debug.WriteLine(input);
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

        /// <summary>
        /// Executes when the user presses a key on the keyboard while using the ssh terminal
        /// Places the character associated with a key in the terminal textblock
        /// If the key is not in the key mappings then it determines if the key is one we care about otherwise the action is discarded
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="KeyEventArgs"></param>
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

        /// <summary>
        /// Triggers when the user stops pressing a key in the terminal
        /// If it isnt the left or right shift key you shouldnt be holding it down and therefore we dont care
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="KeyEventArgs"></param>
        private void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _shiftDown = false;
            }
        }

        /// <summary>
        /// Executes when a user clicks the retry connection button in the device menu
        /// Attempts a connection to the device
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private void RetryConnectionClick (object sender, RoutedEventArgs e)
        {
            _networkDevice.terminal.AttemptConnection();
        }
    }
}