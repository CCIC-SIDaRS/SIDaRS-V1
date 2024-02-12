using DarthGoose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.IO;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.Runtime.InteropServices;

namespace FrontEnd
{ 
    public class FrontEndManager
    {
        private MainWindow _mainWindow;
        private LoginPage _loginPage = new();
        private NetworkMap _networkMap = new();
        private Point _windowSize;
        private Dictionary<Image, Device> _devices = new();
        public FrontEndManager(MainWindow window)
        {
            _mainWindow = window;
            _mainWindow.MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            _mainWindow.MainFrame.Navigate(_loginPage);
            _windowSize = new Point(_mainWindow.Width, _mainWindow.Height);
            _mainWindow.SizeChanged += OnWindowSizeChanged;
            _mainWindow.Closing += new CancelEventHandler(MainWindowClosing);

            _loginPage.LoginButton.Click += new RoutedEventHandler(OnLoginEnter);
            _loginPage.LoginButton.IsDefault = true;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _windowSize = new Point(e.NewSize.Width, e.NewSize.Height);
        }

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void SetupNetworkMap()
        {
            _networkMap.GooseSupport.Click += new RoutedEventHandler(GetGooseSupport);
            _networkMap.InsertRouter.Click += new RoutedEventHandler(InsertDeviceClick);
            _networkMap.InsertFirewall.Click += new RoutedEventHandler(InsertDeviceClick);
            _networkMap.InsertHub.Click += new RoutedEventHandler(InsertDeviceClick);
            _networkMap.InsertSwitch.Click += new RoutedEventHandler(InsertDeviceClick);
            _networkMap.InsertEndPoint.Click += new RoutedEventHandler(InsertDeviceClick);
            _networkMap.InsertServer.Click += new RoutedEventHandler(InsertDeviceClick);
            _networkMap.InsertConnection.Click += new RoutedEventHandler(OnInsertConnection);
            _mainWindow.MainFrame.Navigate(_networkMap);
        }

        private void OnLoginEnter(object sender, RoutedEventArgs e)
        {
            _loginPage = null;
            SetupNetworkMap();
        }

        private void GetGooseSupport(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("GOOSE SUPPORT STARTING...");
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://uploads.dailydot.com/2019/10/Untitled_Goose_Game_Honk.jpeg",
                UseShellExecute = true
            });
        }

        private void InsertDeviceClick(object sender, RoutedEventArgs e)
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
            Image image = new Image();
            image.Source = bitMap;
            image.Width = 100;
            image.Height = 100;
            image.MouseDown += DeviceMouseDown;
            image.MouseMove += DeviceMouseMove;
            image.MouseUp += DeviceMouseUp;
            Canvas.SetLeft(image, 20);
            Canvas.SetTop(image, 20);
            _networkMap.MainCanvas.Children.Add(image);
            _devices[image] = new Device(image, new List<Image>(), new List<Line>());

        }

        private bool drag;
        private Point startPoint;
        private void DeviceMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2)
            {
                drag = true;
                startPoint = Mouse.GetPosition(_networkMap.MainCanvas);
            } else
            {
                _devices[(Image)sender].deviceMenu.Show();
            }
        }

        private void DeviceMouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                Image draggedRectangle = (Image)sender;
                Point newPoint = Mouse.GetPosition(_networkMap.MainCanvas);
                double left = Canvas.GetLeft(draggedRectangle) + (newPoint.X - startPoint.X);
                double top = Canvas.GetTop(draggedRectangle) + (newPoint.Y - startPoint.Y);
                if ((left + draggedRectangle.Width) < _windowSize.X && left > 0 && (top + draggedRectangle.Height) < _windowSize.Y && top >= _networkMap.TopMenu.Height)
                {
                    Canvas.SetLeft(draggedRectangle, left);
                    Canvas.SetTop(draggedRectangle, top);
                    startPoint = newPoint;
                    for (int i = 0; i < _devices[draggedRectangle].connections.Count; i++)
                    {
                        drawConnection(new List<Image>() { draggedRectangle, _devices[draggedRectangle].connections[i] }, _devices[draggedRectangle].cables[i]);
                    }
                }
            }
        }

        private void DeviceMouseUp(object sender, MouseButtonEventArgs e)
        {
            drag = false;
        }
        private List<Image> devicesToBeConnected = new();
        private void OnInsertConnection(object sender, RoutedEventArgs e)
        {
            foreach(KeyValuePair<Image, Device> device in _devices)
            {
                device.Value.image.MouseDown -= DeviceMouseDown;
                device.Value.image.MouseUp -= DeviceMouseUp;
                device.Value.image.MouseDown += AddToPendingConnections;
            }
        }

        private void AddToPendingConnections(object sender, MouseButtonEventArgs e)
        {
            devicesToBeConnected.Add((Image)sender);
            if (devicesToBeConnected.Count() == 2)
            {
                _devices[devicesToBeConnected[0]].connections.Add(devicesToBeConnected[1]);
                _devices[devicesToBeConnected[1]].connections.Add(devicesToBeConnected[0]);
                drawConnection(devicesToBeConnected);
                devicesToBeConnected.Clear();
                exitConnectionMode();
            }
        }

        private void drawConnection(List<Image> connectedDevices, Line existingConnection = null)
        {
            Line line;
            if (existingConnection == null)
            {
                line = new Line();
                Thickness thickness = new Thickness(101, -11, 362, 250);
                line.Margin = thickness;
                line.Visibility = System.Windows.Visibility.Visible;
                line.StrokeThickness = 2;
                line.Stroke = System.Windows.Media.Brushes.White;
            }
            else
            {
                line = existingConnection;
            }
            Image device1 = connectedDevices[0];
            Image device2 = connectedDevices[1];
            Point dev1Location = new Point(Canvas.GetLeft(device1), Canvas.GetTop(device1));
            Point dev2Location = new Point(Canvas.GetLeft(device2), Canvas.GetTop(device2));

            line.X1 = dev1Location.X - (device1.Width / 2);
            line.Y1 = dev1Location.Y + (device1.Height / 2);

            line.X2 = dev2Location.X - (device2.Width / 2);
            line.Y2 = dev2Location.Y + (device2.Height / 2);

            if (existingConnection == null)
            {
                _networkMap.ConnectionCanvas.Children.Add(line);
                _devices[device1].cables.Add(line);
                _devices[device2].cables.Add(line);
            }
        }
        private void exitConnectionMode()
        {
            foreach (KeyValuePair<Image,Device> device in _devices)
            {
                device.Value.image.MouseDown -= AddToPendingConnections;
                device.Value.image.MouseUp += DeviceMouseUp;
                device.Value.image.MouseDown += DeviceMouseDown;
            }
        }
        
    }
    class Device
    {
        public Image image { get; set; }
        public List<Image> connections { get; set; }
        public List<Line> cables { get; set; }
        public DeviceDetails deviceMenu { get; set; }
        private bool shiftDown { get; set; }

        private static Dictionary<Key, string> keyboard = new Dictionary<Key, string>
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
            // Add more key-value pairs for other keys as needed
        };
        private static Dictionary<Key, string> shiftedKeyboard = new Dictionary<Key, string>
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
        // Add more key-value pairs for other keys as needed
    };
        public Device (Image image, List<Image> connections, List<Line> cables)
        {
            this.image = image;
            this.connections = connections;
            this.cables = cables;
            this.deviceMenu = new DeviceDetails();
            deviceMenu.Closing += new CancelEventHandler(OnClosing);
            deviceMenu.KeyDown += KeyDown;
            deviceMenu.KeyUp += KeyUp;
        }
        
        private void OnClosing(object s, CancelEventArgs e)
        {
            Window sender = (Window) s;
            e.Cancel = true;
            sender.Hide();
        }
        private void KeyDown(object sender, KeyEventArgs e)
        {
            bool foundKey = false;
            string key = "";

            try
            {
                if (shiftDown) { 
                    key = shiftedKeyboard[e.Key];
                } else
                {
                    key = keyboard[e.Key];
                }
                foundKey = true;
            } catch (KeyNotFoundException) { }

            if (foundKey)
            {
                deviceMenu.TerminalTextBox.Text += key;
            } else if(e.Key == Key.Back && deviceMenu.TerminalTextBox.Text.Length > 0)
            {
                deviceMenu.TerminalTextBox.Text = deviceMenu.TerminalTextBox.Text.Substring(0, deviceMenu.TerminalTextBox.Text.Length - 1);
            } else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shiftDown = true;
            }
        }
        private void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shiftDown = false;
            }
        }
    }
}
