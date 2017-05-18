using GameLogic.Data;
using NetworkService;
using System.Collections.Generic;

namespace GameLogic
{
    public delegate void LatticClickEventHandler(int x, int y);

    public interface IBoard
    {
        event LatticClickEventHandler LatticClick;
        void DrawChess(int x, int y, int color);
        void RemoveChess(int x, int y);
    }

    public interface InformationBoad
    {
        void ChangedActived(string token);
        void OnStart();
        void OnEnded();
        void OnModeChange();
    }

    public interface IMessageNotifier
    {
        void Notify(string title, string content);
    }
    
    public class GameController
    {
        public GameMode Mode { get; private set; }
        public bool IsOnline { get; private set; }
        public bool IsAttached { get; private set; }
        public IConfirmer<string> Confirmer { get; set; }
        public IMessageNotifier Notifier { get; set; }
        public InformationBoad InfoBoad { get; set; }

        private Dictionary<string, RemotePlayer> remotePlayers;
        private MainLogicUnit logicUnit;
        private Connector connector;
        private IBoard board;
        private string connectIp = "127.0.0.1";
        private int port = 8500;

        public GameController(LogicControls controls, IBoard board)
        {
            logicUnit = new MainLogicUnit(controls);
            remotePlayers = new Dictionary<string, RemotePlayer>();
            connector = new Connector();
            this.board = board;
        }

        public bool SwitchMode(GameMode mode, object extra)
        {
            if (mode == this.Mode) return false;
            if (logicUnit.IsStarted)
            {
                if (Confirmer?.Confirm("Game is started.End?") == true)
                {
                    logicUnit.End();
                }
                else return false;
            }
            GameMode oldMode = this.Mode;
            GameMode newMode = mode;
            this.Mode = newMode;
            onGameModeChanged(oldMode, newMode, extra);
            return true;
        }

        //Tach part
        public void Attach()
        {
            if(board != null)
                board.LatticClick += onBoadLatticClick;
            connector.MessageReceived += onRemoteMessageReceived;
            connector.ConnectStateChanged += onConnectStateChanged;
            logicUnit.Accepted += onLogicUnitAccepted;
            logicUnit.PlayerJoined += onPlayerJoined;
            logicUnit.PlayerLeave += onPlayerLeave;
            logicUnit.FirstChanged += onFirstChanged;
            logicUnit.ActivedChanged += onActivedChanged;
            logicUnit.Started += onStarted;
            logicUnit.Ended += onEnded;

            logicUnit.Attach();
            IsAttached = true;
        }

        public void Detach()
        {
            IsAttached = false;

            logicUnit.Detach();
            if (board != null)
                board.LatticClick -= onBoadLatticClick;
            connector.MessageReceived -= onRemoteMessageReceived;
            connector.ConnectStateChanged -= onConnectStateChanged;
            logicUnit.Accepted -= onLogicUnitAccepted;
            logicUnit.PlayerJoined -= onPlayerJoined;
            logicUnit.PlayerLeave -= onPlayerLeave;
            logicUnit.FirstChanged -= onFirstChanged;
            logicUnit.ActivedChanged -= onActivedChanged;
            logicUnit.Started -= onStarted;
            logicUnit.Ended -= onEnded;
        }


        //Join leave orider
        public bool Join(Player player)
        {
            return logicUnit.Join(player);
        }

        public bool Leave(string token)
        {
            return logicUnit.Leave(token);
        }

        public bool SignAsFirst(string token)
        {
            return logicUnit.SignAsFirst(token);
        }

        //Start end
        public bool Start()
        {
            return IsAttached && logicUnit.Start();
        }

        public void End()
        {
            if (IsAttached)
                logicUnit.End();
        }


        private void onStarted()
        {
            InfoBoad?.OnStart();
        }

        private void onEnded()
        {
            InfoBoad?.OnEnded();
        }


        private void onActivedChanged(string token)
        {
            InfoBoad?.ChangedActived(token);
        }

        private void onFirstChanged(string token)
        {
            Notifier?.Notify("Notification", "Player set as first.");
        }

        private void onPlayerJoined(string token)
        {
            Notifier?.Notify("Notification", "New player joined.");
        }

        private void onPlayerLeave(string token)
        {
            Notifier?.Notify("Notification", "Player leave.");
        }


        private void onGameModeChanged(GameMode oldMode, GameMode newMode, object extra)
        {
            if(oldMode == GameMode.PvPOnline)
            {
                connector.Disconnct();
                connector.StopListen();
            }else if(newMode == GameMode.PvPOnline)
            {
                if(extra is string)
                    connectIp = (string)extra;
                if (string.IsNullOrEmpty(connectIp))
                {
                    Notifier?.Notify("Warning", "Connect IP is not valid.");
                }else
                {
                    connector.ListenTo(System.Net.IPAddress.Any, port);
                    connector.Connect(connectIp, port);
                }
            }
            InfoBoad?.OnModeChange();
        }


        private void sendRemoteMessage(Message msg)
        {
            connector.SendMessage(MessageType.Message, msg);
        }

        private void onRemoteMessageReceived(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.Message:
                    Notifier?.Notify("Message", "New message\r\n" + msg.ToString());
                    break;
                case MessageType.Fallback:
                    var data = msg.Content as object[];
                    if(data != null && data.Length == 2 )
                        handFallBack(data[0], data[1]);
                    break;
                case MessageType.Action:
                    handRemoteAction(msg.Token, msg.Content);
                    break;
            }
        }


        private void sendRemoteAction(InputAction action)
        {
            connector.SendMessage(MessageType.Action, action);
            confirmAction = action;
        }

        private void handRemoteAction(string remoteToken, object content)
        {
            if (content != null && content is InputAction)
            {
                var action = (InputAction)content;
                bool handResult = false;
                if (action.Type == ActionType.Join)
                {
                    if (!remotePlayers.ContainsKey(remoteToken))
                        remotePlayers.Add(remoteToken, new RemotePlayer(remoteToken));
                    handResult = logicUnit.Join(remotePlayers[remoteToken]);
                }
                else if (remotePlayers.ContainsKey(remoteToken))
                {
                    string localToken = remotePlayers[remoteToken].Token;
                    handResult = logicUnit.HandInput(localToken, action);
                }
                sendFallback(action.Type, handResult);
            }
        }


        private void sendFallback(ActionType type, bool handResult)
        {
            connector.SendMessage(MessageType.Fallback, new object[]
            {
                type, handResult
            });
        }

        private void handFallBack(object type, object handResult)
        {
            ActionType uptype;
            bool upresult;
            if (type is ActionType && confirmAction != null)
            {
                uptype = (ActionType)type;
                upresult = handResult is bool && (bool)handResult;
                if (confirmAction.Type == uptype)
                {

                }
            }
        }


        private void onBoadLatticClick(int x, int y)
        {
            if (logicUnit.IsStarted && !logicUnit.Actived.IsVirtual())
                logicUnit.Actived.Input(new Point(x, y));
        }
        
        private void onLogicUnitAccepted(string token, InputAction action)
        {
            if (!remotePlayers.ContainsKey(token))
                sendRemoteAction(action);
        }
        
        private void onConnectStateChanged(bool isConnected)
        {
            IsOnline = isConnected;
        }
        
        private InputAction confirmAction;
    }
}
