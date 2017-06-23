using LogicUnit;
using LogicUnit.Data;
using NetworkService;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
    public partial class MainWindow : Window, IDisposable
    {
        private GameController controller;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            /*
            MainLogicUnit master = new MainLogicUnit(new LogicControls(2, 2, 24));
            Player p1 = new Player() { Name = "p1" };
            Player p2 = new Player() { Name = "p2" };
            master.Start();
            
            if (!master.IsAttached)
                master.Attach();
            if (!master.Join(p1))
                System.Diagnostics.Debug.WriteLine("p1 join fail.");
            if(!master.Join(p2))
                System.Diagnostics.Debug.WriteLine("p2 join fail.");
            var connector = new Connector();

            connector.ListenTo(System.Net.IPAddress.Any, 8500);
            connector.Connect("127.0.0.1", 8500);
            connector.HeartbeatStarted += onHeartbeatStarted;
            connector.HeartbeatFail += onHeartbeatFail;
            connector.MessageReceived += onMessageReceived;

            Task.Run(async () =>
            {
                while (connector.IsConnected)
                {
                    await Task.Delay(5000);
                    connector.SendObject(Message.CreateMessage("fjaiweurqop", new InputAction(ActionType.Input, new DataPoint(0,0,2))));
                }
            });*/

            /*Player p1 = new LocalPlayer() { Name = "p1" };
            controller = new GameController(GameMode.PvP, board, p1);
            controller.Notifier = new ConsoleNotifier();

            if (!controller.IsAttached)
                controller.Attach();*/
        }
        private IEnvProxy env;

        private void onHeartbeatStarted()
        {
            System.Diagnostics.Debug.WriteLine("Heartbeat started.");
        }

        private void onHeartbeatFail()
        {
            System.Diagnostics.Debug.WriteLine("Heartbeat fail.");
        }

        private void onMessageReceived(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.Message:
                    System.Diagnostics.Debug.WriteLine("\r\nNew message\r\n" + msg.ToString());
                    break;
            }
        }

        private void ReadyBtnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(port.Text)) return;
            if (string.IsNullOrEmpty(port2.Text)) return;
            int inport, outport;
            if (int.TryParse(port.Text, out inport) &&
                int.TryParse(port2.Text, out outport))
            {
                bool isHost = sender == readyHostBtn;
                env = EVPEnvProxy.Create(board, AILevel.Elementary);
                return;
                if (isHost)
                    env = OnlineHostEnvProxy.Create(board,  "127.0.0.1", outport, inport);
                else
                    env = OnlineEnvProxy.Create(board,  "127.0.0.1", outport, inport);
            }
        }

        private void ConnectBtnClick(object sender, RoutedEventArgs e)
        {
            env.Ready();
        }

        private void StartBtnClick(object sender, RoutedEventArgs e)
        {
            env.Start();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}