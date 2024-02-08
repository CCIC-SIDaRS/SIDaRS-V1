using DarthGoose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using System.Windows;

namespace FrontEnd
{
    public class FrontEndManager
    {
        private MainWindow _mainWindow;
        private LoginPage _loginPage = new();
        private NetworkMap _networkMap = new();
        public FrontEndManager(MainWindow window)
        {
            _mainWindow = window;
            _mainWindow.MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            _mainWindow.MainFrame.Navigate(_loginPage);

            _loginPage.LoginButton.Click += new RoutedEventHandler(OnLoginEnter);
            _loginPage.LoginButton.IsDefault = true;
        }

        private void OnLoginEnter(object sender, RoutedEventArgs e)
        {
            _mainWindow.MainFrame.Navigate(_networkMap);
        }
    }
}
