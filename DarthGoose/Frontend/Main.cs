using System.Windows.Navigation;
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
using SharpPcap;
using Microsoft.Win32;
using System.Windows.Input;
using System.Diagnostics.Metrics;

namespace DarthGoose.Frontend
{
    /// <summary>
    /// Controls dthe UIElements for the main window and the device setup window
    /// </summary>
    static class FrontendManager
    {
        /// <summary>
        /// declares public variables for use whena interfacing with the front end control logic
        /// and objects
        /// </summary>
        public static bool connecting = false;
        public static MainWindow mainWindow;
        public static NetworkMap networkMap = new();
        public static Point windowSize;
        public static Dictionary<string, UIDevice> devices = new();
        public static Users masterCredentials;
        public static MonitorSystem? packetCapture = null;
        public static CaptureDeviceList captureDevices = CaptureDeviceList.Instance;

        /// <summary>
        /// decalres private variables for use in local processes
        /// </summary>
        private static LoginPage _loginPage = new();
        private static CreateAccountPage _createAccPage = new();
        private static DeviceSetup _deviceSetupWindow = new();
        private static string? _saveFile = null;

        /// <summary>
        /// Runs on startup to setup the main application and the login page
        /// </summary>
        /// <param name="window" Type="MainWindow"></param>
        public static void FrontendMain(MainWindow window)
        {
            mainWindow = window;
            mainWindow.MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            mainWindow.MainFrame.Navigate(_loginPage);
            windowSize = new Point(mainWindow.Width, mainWindow.Height);
            mainWindow.SizeChanged += OnWindowSizeChanged;
            mainWindow.Closing += new CancelEventHandler(MainWindowClosing);
            _loginPage.LoginButton.Click += new RoutedEventHandler(OnLoginEnter);
            _loginPage.CreateAccountButton.Click += new RoutedEventHandler(NavCreateNewAccount);
            _loginPage.LoginUsername.MouseLeftButtonDown += new MouseButtonEventHandler(OnTextBoxSelected);
            _loginPage.LoginUsername.Focus();
            _loginPage.LoginButton.IsDefault = true;

            _createAccPage.LoginButton.Click += new RoutedEventHandler(NavLogin);
            _createAccPage.CreateButton.Click += new RoutedEventHandler(OnCreateAccount);
            _createAccPage.CreateUsername.MouseLeftButtonDown += new MouseButtonEventHandler(OnTextBoxSelected);
            _createAccPage.CreateUsername.Focus();
            _createAccPage.CreateButton.IsDefault = true;

            mainWindow.MainFrame.Navigate(_loginPage);
        }

        /// <summary>
        /// executes whenever the user, or the program, changes the size of the window
        /// updates the window size for use with bounding
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="SizeChangedEventArgs"></param>
        private static void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            windowSize = new Point(e.NewSize.Width, e.NewSize.Height);
        }

        /// <summary>
        /// executes when the main window izs cliosec
        /// ensures the application is fully shutdown when closed
        /// </summary>
        /// <param name="sender" Type="Object"></param>
        /// <param name="e" Type="CancelEventArgs"></param>
        private static void MainWindowClosing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
   
        /// <summary>
        /// Prepares the mainwindow for use with the network map
        /// Adds even handlers for the ui objects in the network map
        /// Gathers he availible network devices for capture and adds them to a combobox
        /// navigates to the network map page
        /// </summary>
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
            networkMap.SaveAs.Click += new RoutedEventHandler(OnSaveAsClick);
            networkMap.Load.Click += new RoutedEventHandler(OnLoadClick);
            networkMap.CancelConnection.Click += new RoutedEventHandler(OnCancelConnection);
            networkMap.SidePanelToggle.Click += new RoutedEventHandler(OnSidePanelToggleClick);
            networkMap.SidePanelCloseButton.Click += new RoutedEventHandler(OnSidePanelCloseClick);
            networkMap.StartCaptureButton.Click += new RoutedEventHandler(OnStartCaptureClick);
            networkMap.StopCaptureButton.Click += new RoutedEventHandler(OnStopCaptureClick);
            networkMap.UpdateIDSSettingsButton.Click += new RoutedEventHandler(OnUpdateIDSSettingsClick);
            _deviceSetupWindow.FinishedSetup.Click += new RoutedEventHandler(OnFinishedSetup);
            networkMap.CaptureDeviceDropDown.SelectionChanged += new SelectionChangedEventHandler(OnCaptureDeviceSelectionChanged);

            if (captureDevices.Count < 1)
            {
                MessageBox.Show("No viable capture devices were found on this machine");
            }else
            {
                foreach (ILiveDevice dev in captureDevices)
                {
                    networkMap.CaptureDeviceDropDown.Items.Add(dev.Description);
                }
            }

            mainWindow.MainFrame.Navigate(networkMap);
        }

        /// <summary>
        /// Executes when the user presses enter or clicks the logon button on the login page
        /// reads existing uernames and passwords from the Users.sidars file
        /// checks to make sure an account exists
        /// reads from the username box and ensures something has been entered
        /// reads from the password box and hashes the password using the same algorithm used to encrypt the passwords before storing them
        /// finds the username in the list of existing accounts and compares the stored password with the hashed password from the password box
        /// resets if passwords do not match
        /// uses a different hashing algorithm to set the master password for the device passwords
        /// </summary>
        /// <param name="sender" Type="Object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnLoginEnter(object sender, RoutedEventArgs e)
        {
            Users[]? allCreds = SaveSystem.LoadUsers(@".\Backend\Assets\Users.sidars");
            if (allCreds == null)
            {
                MessageBox.Show("Something went wrong, please make an account or contact goose support");
            }
            foreach (Users cred in allCreds)
            {
                if (cred.GetUsername() == _loginPage.LoginUsername.Text && cred.GetPassword() == System.Text.Encoding.Unicode.GetString(SymmetricEncryption.Hash(_loginPage.LoginPassword.Password, "DARTHGOOSE!!!!")))
                {
                    masterCredentials = new Users(_loginPage.LoginUsername.Text, _loginPage.LoginPassword.Password, false);
                    SymmetricEncryption.SetMaster(_loginPage.LoginPassword.Password, _loginPage.LoginUsername.Text);
                    _loginPage = null;
                    _createAccPage = null;
                    SetupNetworkMap();
                    return;
                }
            }
            MessageBox.Show("Invalid Username or Password.");
            _loginPage.LoginUsername.Text = "";
            _loginPage.LoginPassword.Password = "";
        }

        /// <summary>
        /// executes when the user clicks the create account button on the create account page
        /// reads from the users.sidars file, the username box, and the password box
        /// checks the username against the existing usernames to make sure it doesn't already exist
        /// hashes and stores the username and passwords
        /// set the master using the same method as above
        /// naivgates to the networkmap
        /// </summary>
        /// <param name="sender" Type="Object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnCreateAccount(object sender, RoutedEventArgs e)
        {
            if(_createAccPage.CreateUsername.Text != "" && _createAccPage.CreatePassword.Password != "" && _createAccPage.CreatePassword.Password == _createAccPage.ConfirmPassword.Password)
            {
                Users[]? allCreds = SaveSystem.LoadUsers(@".\Backend\Assets\Users.sidars");
                foreach (Users cred in allCreds)
                {
                    if (cred.GetUsername() == _createAccPage.CreateUsername.Text)
                    {
                        MessageBox.Show("This username already exists. Log in instead.");
                        return;
                    }
                }

                SymmetricEncryption.SetMaster(_createAccPage.CreatePassword.Password, _createAccPage.CreateUsername.Text);
                masterCredentials = new Users(_createAccPage.CreateUsername.Text, _createAccPage.CreatePassword.Password, false);
                allCreds = allCreds.Concat([masterCredentials]).ToArray();
                SaveSystem.SaveUsers(allCreds, @".\Backend\Assets\Users.sidars");

                _loginPage = null;
                _createAccPage = null;
                SetupNetworkMap();
            } else
            {
                MessageBox.Show("YOU FOOL. YOU DID SOMETHING WRONG.");
            }
        }

        /// <summary>
        /// executes when the user clicks the create account button on the login page
        /// navigates to cthe create account page from the login page
        /// </summary>
        /// <param name="sender" Type="Object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void NavCreateNewAccount(object sender, RoutedEventArgs e)
        {
            mainWindow.MainFrame.Navigate(_createAccPage);
            mainWindow.Width = 800;
        }

        /// <summary>
        /// executes when the user clicks the login instead button on the create account page
        /// navigates from the create account page to the login page
        /// </summary>
        /// <param name="sender" Type="Object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void NavLogin(object sender, RoutedEventArgs e)
        {
            mainWindow.MainFrame.Navigate(_loginPage);
        }

        /// <summary>
        /// executes when the user selects the goose support button in the network map
        /// Navigates to our technical support website
        /// </summary>
        /// <param name="sender" Type="Object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void GetGooseSupport(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("GOOSE SUPPORT STARTING...");
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://uploads.dailydot.com/2019/10/Untitled_Goose_Game_Honk.jpeg",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Defines a flag to determine whether the user is still setting up a device or or has finished
        /// </summary>
        private static bool _finishedSetup = false;

        /// <summary>
        /// Executes whenever the user selects any device in the insert menu other then a connection
        /// Determines which menu item was selected before defining lables, captions, and uids for the new device
        /// Calls the CreateLabel function to produce the visual components of the device on the network map canvas
        /// Prompts the user to input relavent information in the DeviceSetup window if it is a managed device and then uses that formation to create a UINetDevice object to manage that device
        /// If the device is not managed it autogenerates a name and an unconfigured ipaddress to create an EnpointDevice object
        /// </summary>
        /// <param name="sender" Type="Object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
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

        /// <summary>
        /// Draws devices onto the screen
        /// Switches on deviceType to determine which image to select from
        /// creates a label consisting of a stack panel which has the image and the caption attached and returns that as outs
        /// </summary>
        /// <param name="deviceType" Type="string">
        /// must be one of the following: InsertRouter, InsertSwitch, InsertUnmanagedSwitch, InsertFirewall, InsertServer, InsertEndPoint
        /// </param>
        /// <param name="location" Type="int[]">
        /// location[0] = XCoordinate, location[1] = YCoordinate
        /// </param>
        /// <param name="label" Type="out System.Windows.Controls.Label">
        /// Label object will have children of:
        /// System.Windows.Controls.StackPanel which will have children of:
        /// System.Windows.Controls.Image
        /// System.Windows.Control.TextBlock
        /// </param>
        /// <param name="caption" Type="out System.Windows.Controls.TextBlock">
        /// parented to a stack panel
        /// </param>
        /// <param name="uid" type="out string">
        /// a unique idenifier for the object consisting of the time it was originally created and the hashcode for the object
        /// </param>
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
            caption.TextAlignment = TextAlignment.Center;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(caption);

            label.Content = stackPanel;

            Canvas.SetLeft(label, location[0]);
            Canvas.SetTop(label, location[1]);

            uid = DateTime.Now.ToString() + "-" + label.GetHashCode().ToString();

            networkMap.MainCanvas.Children.Add(label);
        }

        /// <summary>
        /// Executes when the user clicks the submit button on the DeviceSetupPage
        /// sets the finished setup flag for the InsertDeviceClick function
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnFinishedSetup(object sender, RoutedEventArgs e)
        {
            _finishedSetup = true;
        }

        /// <summary>
        /// A buffer containing the devices that the user has selected to connect
        /// </summary>
        private static List<Label> devicesToBeConnected = new();

        /// <summary>
        /// executes when a user inserts a connection
        /// sets the connecting flag equal to true to begin the connection loop
        /// sets the connection specific ui elements to be visible
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnInsertConnection(object sender, RoutedEventArgs e)
        {
            connecting = true;
            networkMap.InfoText.Text = "Connecting Devices: 0 of 2 Devices Selected";
            networkMap.CancelConnection.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// executes whenever a user clicks the cancel button during a connection 
        /// resets the connection process and hides the connection ui elements
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnCancelConnection(object sender, RoutedEventArgs e)
        {
            connecting = false;
            devicesToBeConnected.Clear();
            networkMap.InfoText.Text = string.Empty;
            networkMap.CancelConnection.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Executes when the user selects the save button on the network map
        /// calls the SaveSystem.Save function on a previously opened file
        /// Provides a message if no file has been opened previously
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (_saveFile == null)
            {
                MessageBox.Show("Please load a file or save as before trying to save");
            }else
            {
                var netDevices = new List<UINetDevice>();
                var endDevices = new List<EndpointDevice>();
                foreach (UIDevice device in devices.Values)
                {
                    if (device.GetType() == typeof(UINetDevice))
                    {
                        netDevices.Add(device as UINetDevice);
                    }
                    else
                    {
                        endDevices.Add(device as EndpointDevice);
                    }
                }

                SaveSystem.Save(_saveFile, netDevices.ToArray(), endDevices.ToArray());
            }
        }

        /// <summary>
        /// Executes when the user selects the load button on the network map
        /// Creates an open file dialog and then calls the SaveSystem.Load function on the selected file
        /// sets the save file variable to the save button can be used
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnLoadClick (object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "*sidars configuration files (*.sidars)|*.sidars";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                SaveSystem.Load(dialog.FileName);
                _saveFile = dialog.FileName;
            }
        }

        /// <summary>
        /// Executes when a user selects the save as button
        /// prompts the user to select and existing file or create a new one and then calls the SaveSystem.Save function on the selected file
        /// sets the save file variable so the save button can be used
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnSaveAsClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "*sidars configuration files (*.sidars)|*.sidars";
            bool? result = saveFileDialog.ShowDialog();
            if(result == true)
            {
                var netDevices = new List<UINetDevice>();
                var endDevices = new List<EndpointDevice>();
                foreach (UIDevice device in devices.Values)
                {
                    if (device.GetType() == typeof(UINetDevice))
                    {
                        netDevices.Add(device as UINetDevice);
                    }
                    else
                    {
                        endDevices.Add(device as EndpointDevice);
                    }
                }
                SaveSystem.Save(saveFileDialog.FileName, netDevices.ToArray(), endDevices.ToArray());
                _saveFile = saveFileDialog.FileName;
            }
        }

        /// <summary>
        /// Executes when the user selects the arrow button on the side of the network map
        /// Changes the columnspan definitions to move UI elements away from the side panel
        /// Makes the side panel visible
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnSidePanelToggleClick(object sender, RoutedEventArgs e)
        {
            networkMap.SidePanelToggle.Visibility = Visibility.Hidden;
            networkMap.DragBorder.SetValue(Grid.ColumnSpanProperty, 1);
            networkMap.DragBorder.SetValue(Grid.ColumnProperty, 1);
            networkMap.SideMenuBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Executes when the user selects the x button on the side panel
        /// hides the side panel
        /// resets column definitions so that ui elements use the whole screen again
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnSidePanelCloseClick(object sender, RoutedEventArgs e)
        {
            networkMap.SidePanelToggle.Visibility = Visibility.Visible;
            networkMap.DragBorder.SetValue(Grid.ColumnSpanProperty, 2);
            networkMap.DragBorder.SetValue(Grid.ColumnProperty, 0);
            networkMap.SideMenuBorder.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Stars a new capture session with the selected device assuming the user has actually made a selection
        /// </summary>
        /// <param name="sender" Type="object"></param>
        /// <param name="e" Type="RoutedEventArgs"></param>
        private static void OnCaptureDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = networkMap.CaptureDeviceDropDown.SelectedIndex;
            if (selectedIndex == -1)
            {
                MessageBox.Show("Please select a network device");
            }
            else if (packetCapture == null)
            {
                packetCapture = new MonitorSystem(captureDevices[selectedIndex]);
            }else
            {
                packetCapture.ChangeCaptureDevice(captureDevices[selectedIndex]);
            }
        }

        /// <summary>
        /// Executes when the user selects the start capture button
        /// Starts the packet capture (and IDS) process(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnStartCaptureClick(object sender, RoutedEventArgs e)
        {
            if(networkMap.CaptureDeviceDropDown.SelectedIndex == -1 || packetCapture == null)
            {
                MessageBox.Show("Please select a network device before attempting to start capture");
            }else
            {
                packetCapture.StartCapture();
            }
        }

        /// <summary>
        /// Executes when the user selecrs the stop capture button
        /// Stops the packet capture (and IDS) process(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnStopCaptureClick(object sender, RoutedEventArgs e)
        {
            if (networkMap.CaptureDeviceDropDown.SelectedIndex == -1 || packetCapture == null)
            {
                MessageBox.Show("Please select a network device and start capture before attempting to stop capture");
            }else
            {
                packetCapture.StopCapture();
            }
        }

        /// <summary>
        /// Executes when the user presses the Update IDS settings button
        /// Attempts to update all IDS setting and sends a message to the user if a bad value is supplied
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnUpdateIDSSettingsClick(object sender, RoutedEventArgs e)
        {
            if(int.TryParse(networkMap.ExpansionThesholdTextBox.Text, out var valueI))
            {
                PacketAnalysis.expansionThreshhold = valueI;
            }else
            {
                MessageBox.Show("The Expansion Threshold value was not updated due to an improper value being supplied");
            }
            if(int.TryParse(networkMap.ViolationThresholdTextBox.Text, out valueI))
            {
                PacketAnalysis.offenseThreshold = valueI;
            }else
            {
                MessageBox.Show("The Violation Threshold value was not updated due to an improper value being supplied");
            }
            if(float.TryParse(networkMap.EWMAWeightBox.Text, out var valueF))
            {
                PacketAnalysis.alpha = valueF;
            }else
            {
                MessageBox.Show("The Exponentially Weighted Moving Average Weight value was not updated due to an improper value being supplied");
            }
            if(float.TryParse(networkMap.RateRatioMaxTextBox.Text, out valueF))
            {
                PacketAnalysis.ratioLimitMax = valueF;
            }else
            {
                MessageBox.Show("The Rate Ratio Max value was not updated due to an improper value being supplied");
            }
            if(float.TryParse(networkMap.RateRatioMinTextBox.Text, out valueF))
            {
                PacketAnalysis.ratioLimitMin = valueF;
            }else
            {
                MessageBox.Show("The Rate Ratio Max value was not updated due to an improper value being supplied");
            }
        }

        /// <summary>
        /// Makes sure a text box is focuses when a user presses their mouse down on the area it occupies
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnTextBoxSelected(object sender, MouseButtonEventArgs e)
        {
            TextBox? box = sender as TextBox;
            if (box != null)
            {
                box.Focus();
            }
        }

        /// <summary>
        /// Collects the uids of the devices to be connected
        /// Adds the keys of the device being connected to to each devices UIDevice object if 2 devices have been selected
        /// If 2 devices have been selected then it also reset the UI from the connection state
        /// If only one device has been selected then it updates the info text on the bottom of the screen
        /// </summary>
        /// <param name="sender" Type="System.Windows.Control.Label"></param>
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

        /// <summary>
        /// If this is a new connection (existingConnection = null):
        ///     Creates a new System.Windows.Shapes.Line object for the connection
        ///     Childs the line object to the ConnectionCanvas
        /// If a connection already exists (existing connection != null)
        ///     sets the current line object to the existing connection line
        /// Draws the line between the center of the 2 devices based on the positions of the label objects provided
        /// </summary>
        /// <param name="connectedDevices" Type="List<Label>"></param>
        /// <param name="connectedUids" Type="List<string>"></param>
        /// <param name="existingConnection" Type="System.Windows.Shapes.Line">
        /// only used for when a device is moved on screen and an existing line/ connection needs to be redrawn
        /// </param>
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

            //Point dev1Location = device1.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0,0));
            //Point dev2Location = device1.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));

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
