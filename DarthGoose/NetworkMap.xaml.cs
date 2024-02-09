using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DarthGoose
{
    /// <summary>
    /// Interaction logic for NetworkMap.xaml
    /// </summary>
    public partial class NetworkMap : Page
    {
        bool drag;
        Point startPoint;
        public NetworkMap()
        {
            InitializeComponent();
        }
        private void InsertRouterClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitMap = new BitmapImage();
            bitMap.BeginInit();
            bitMap.UriSource = new Uri(@"C:\Users\skier\Documents\Code\SIDaRS\SIDaRS-Frontend\DarthGoose\Images\Router.png");
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
            MainCanvas.Children.Add(image);
            
        }
        private void DeviceMouseDown(object sender, MouseButtonEventArgs e)
        {
            drag = true;
            startPoint = Mouse.GetPosition(MainCanvas);
        }

        private void DeviceMouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                Image draggedRectangle = (Image)sender;
                Point newPoint = Mouse.GetPosition(MainCanvas);
                double left = Canvas.GetLeft(draggedRectangle) + (newPoint.X - startPoint.X);
                double top = Canvas.GetTop(draggedRectangle) + (newPoint.Y - startPoint.Y);
                Debug.WriteLine(MainCanvas.Width);
                if ((left + draggedRectangle.Width) < MainCanvas.Width && (top + draggedRectangle.Height) < MainCanvas.Height)
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
