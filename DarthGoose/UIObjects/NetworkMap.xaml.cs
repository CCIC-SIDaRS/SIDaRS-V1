using DarthGoose.Frontend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DarthGoose
{
    /// <summary>
    /// Interaction logic for NetworkMap.xaml
    /// </summary>
    public partial class NetworkMap : Page
    {
        public NetworkMap()
        {
            InitializeComponent();
            MyViewModel vm = new MyViewModel();
            IDSGraph.Points = vm.myModel.points;
            if(vm.currentSecond >= (GraphViewer.Width / 5))
            {
                
                GraphViewer.ScrollToHorizontalOffset((vm.currentSecond * 5) -  GraphViewer.Width);
            }
        }

    }
    public class MyViewModel
    {

        public int currentSecond = 0;
        //Random rd = new Random();
        public PointCollection LtPoint = new PointCollection();

        public MyModel myModel { get; set; } = new MyModel();
        public MyViewModel()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Start();

            myModel = new MyModel()
            {
                points = LtPoint,
                ColorName = "White"
            };
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            currentSecond++;
            double x = currentSecond * 5;
            double y = FrontendManager.packetAnalyzer.lastPacketRatio;
            LtPoint.Add(new Point(x, y * 10));
        }
    }

    public class MyModel
    {
        public PointCollection points { get; set; } = new PointCollection();

        public string ColorName { get; set; }
    }
}
