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
        private bool _drag;
        private Point _startPoint;
        public NetworkMap()
        {
            InitializeComponent();
        }
        private void InsertRouterClick(object sender, RoutedEventArgs e)
        {
            Rectangle blueRectangle = new Rectangle();
            blueRectangle.Height = 100;
            blueRectangle.Width = 200;
            SolidColorBrush blueBrush = new SolidColorBrush();
            blueBrush.Color = Colors.Blue;
            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;
            blueRectangle.StrokeThickness = 4;
            blueRectangle.Stroke = blackBrush;
            blueRectangle.Fill = blueBrush;
            blueRectangle.MouseDown += DeviceMouseDown;
            blueRectangle.MouseUp += DeviceMouseUp;
            blueRectangle.MouseMove += DeviceMouseMove;
            NetworkGrid.Children.Add(blueRectangle);
        }
        private void DeviceMouseDown(object sender, MouseEventArgs e)
        {
            _drag = true;
            _startPoint = Mouse.GetPosition(this);
        }
        private void DeviceMouseUp(object sender, MouseEventArgs e)
        {
            if (_drag)
            {
                Debug.WriteLine("Dragging");
                Rectangle draggedRectangle = (Rectangle)sender;
                Point nextPoint = Mouse.GetPosition(this);
                double left = Canvas.GetLeft(draggedRectangle);
                double top = Canvas.GetTop(draggedRectangle);
                Canvas.SetLeft(draggedRectangle, left + (nextPoint.X - _startPoint.X));
                Canvas.SetTop(draggedRectangle, top + (nextPoint.Y - _startPoint.Y));

                _startPoint = nextPoint;
            }

        }
        private void DeviceMouseMove(object sender, MouseEventArgs e)
        {
            _drag = false;
        }
    }
}
