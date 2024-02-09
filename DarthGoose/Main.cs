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

namespace FrontEnd
{
    public class FrontEndManager
    {
        private MainWindow _mainWindow;
        private LoginPage _loginPage = new();
        private NetworkMap _networkMap = new();
        private Point _windowSize;
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
                    bitMap.UriSource = new Uri(Path.Combine(Directory.GetCurrentDirectory(), @"Images\Router.png"));
                    break;
                case "InsertSwitch":
                    bitMap.UriSource = new Uri(Path.Combine(Directory.GetCurrentDirectory(), @"Images\Switch.png"));
                    break;
                case "InsertHub":
                    bitMap.UriSource = new Uri(Path.Combine(Directory.GetCurrentDirectory(), @"Images\Switch.png"));
                    break;
                case "InsertFirewall":
                    bitMap.UriSource = new Uri(Path.Combine(Directory.GetCurrentDirectory(), @"Images\Firewall.png"));
                    break;
                case "InsertServer":
                    bitMap.UriSource = new Uri(Path.Combine(Directory.GetCurrentDirectory(), @"Images\Server.png"));
                    break;
                case "InsertEndPoint":
                    bitMap.UriSource = new Uri(Path.Combine(Directory.GetCurrentDirectory(), @"Images\Endpoint.png"));
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
                if ((left + draggedRectangle.Width) < _windowSize.X && (top + draggedRectangle.Height) < _windowSize.Y)
                {
                    Canvas.SetLeft(draggedRectangle, left);
                    Canvas.SetTop(draggedRectangle, top);
                    startPoint = newPoint;
                }
            }
        }

        private void DeviceMouseUp(object sender, MouseButtonEventArgs e)
        {
            drag = false;
        }
    }
}
