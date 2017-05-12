using Logic;
using System;
using System.Collections.Generic;
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

namespace Goline2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            GameMaster master = new GameMaster(new LogicControls() { PlayerLimits = 2 });
            Player p1 = new Player() { Name = "p1" };
            Player p2 = new Player() { Name = "p2" };
            if (!master.IsAttached)
                master.Attach();
            if (!master.Join(p1))
                System.Diagnostics.Debug.WriteLine("p1 join fail.");
            if(!master.Join(p2))
                System.Diagnostics.Debug.WriteLine("p2 join fail.");
        }
    }
}
