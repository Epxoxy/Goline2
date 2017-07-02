using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace GoLine
{
    /// <summary>
    /// Interaction logic for MenuPage.xaml
    /// </summary>
    public partial class MenuControl : ItemsControl 
    {
        public MenuControl()
        {
            InitializeComponent();
        }

        private void ExitBtnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SettingBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new SettingPage());
        }

        private void HelpBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new HelpPage());
        }

        private void TopBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new ScorePage());
        }

        private void AboutBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new AboutPage());
        }
    }
}
