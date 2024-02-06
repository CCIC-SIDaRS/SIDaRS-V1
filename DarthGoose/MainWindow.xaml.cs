using System.Text;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LoginPage _loginPage = new();
        private NetworkMap _networkMap = new();

        public MainWindow()
        {
            InitializeComponent();

            this.MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            this.MainFrame.Navigate(_loginPage);

            _loginPage.LoginButton.Click += new RoutedEventHandler(OnLoginEnter);
            _loginPage.LoginButton.IsDefault = true;
        }

        private void OnLoginEnter(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("DO YOU REALLY WANT TO LOG INTO THIS?", "Error", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                this.MainFrame.Navigate(_networkMap);
            } else
            {
                this.Close();
            }
        }
    }
}