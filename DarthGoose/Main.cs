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
using Microsoft.VisualBasic;
using System.Security.Policy;

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

            _loginPage.LoginButton.Click += new RoutedEventHandler(OnLoginEnter);
            _loginPage.LoginButton.IsDefault = true;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _windowSize = new Point(e.NewSize.Width, e.NewSize.Height);
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
            // bitMap.UriSource = new Uri(@"C:\Users\skier\Documents\Code\SIDaRS\SIDaRS-Frontend\DarthGoose\Images\Router.png");
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
            drag = true;
            startPoint = Mouse.GetPosition(_networkMap.MainCanvas);
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
            Debug.WriteLine("Adding something");
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
                line.StrokeThickness = 4;
                line.Stroke = System.Windows.Media.Brushes.White;
            }else
            {
                line = existingConnection;
            }
            Image device1 = connectedDevices[0];
            Image device2 = connectedDevices[1];
            Point dev1Location = new Point(Canvas.GetLeft(device1), Canvas.GetTop(device1));
            Point dev2Location = new Point(Canvas.GetLeft(device2), Canvas.GetTop(device2));
            bool above = (dev1Location.Y < dev2Location.Y);
            bool left = (dev1Location.X < dev2Location.X);
            
            if (left && above)
            {
                line.X1 = dev1Location.X; // + device1.Width;
                line.X2 = dev2Location.X - device2.Width;
                line.Y1 = dev1Location.Y + device1.Height;
                line.Y2 = dev2Location.Y;
            }else if (above && !left)
            {
                line.X1 = dev1Location.X - device1.Width;
                line.X2 = dev2Location.X;
                line.Y1 = dev1Location.Y + device1.Height;
                line.Y2 = dev2Location.Y;
            }else if (!above && left)
            {
                line.X1 = dev1Location.X;
                line.X2 = dev2Location.X - device2.Width;
                line.Y1 = dev1Location.Y;
                line.Y2 = dev2Location.Y + device2.Height;
            }else if (!above && !left)
            {
                line.X1 = dev1Location.X - device1.Width;
                line.X2 = dev2Location.X;
                line.Y1 = dev1Location.Y;
                line.Y2 = dev2Location.Y + device2.Height;
            }
            if (existingConnection == null)
            {
                _networkMap.MainCanvas.Children.Add(line);
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
        public Device (Image image, List<Image> connections, List<Line> cables)
        {
            this.image = image;
            this.connections = connections;
            this.cables = cables;
        }
    }
}
