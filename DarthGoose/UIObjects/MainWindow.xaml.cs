using System.Windows;
using DarthGoose.Frontend;

namespace DarthGoose
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            FrontendManager.FrontendMain(this);
        }
    }
}