using GameLogic.Data;
using NetworkService;
using System.Collections.Generic;

namespace GameLogic
{
    public delegate void LatticClickEventHandler(int x, int y);

    public interface IBoad
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
    }

    public interface MessageNotifier
    {
        void Notify(string title, string content);
    }
    
    public class GameController
    {
        public GameMode Mode { get; private set; }
        public bool IsOnline { get; private set; }
        public bool IsAttached { get; private set; }
        public MessageNotifier Notifier { get; set; }
        public InformationBoad InfoBoad { get; set; }

        private Dictionary<string, RemotePlayer> remotePlayers;
        private MainLogicUnit logicUnit;
        private Connector connector;
        private IBoad boad;

        public GameController(LogicControls controls, IBoad boad)
        {
            logicUnit = new MainLogicUnit(controls);
            remotePlayers = new Dictionary<string, RemotePlayer>();
            connector = new Connector();
        }

        public bool SwitchMode(GameMode mode)
        {
            if (mode == this.Mode) return false;
            if (logicUnit.IsStarted)
                logicUnit.End();
            this.Mode = mode;
            onGameModeChanged();
            return true;
        }

        public void Attach()
        {
            if(boad != null)
                boad.LatticClick += onBoadLatticClick;
            connector.MessageReceived += onRemoteMessageReceived;
            logicUnit.Accepted += onLogicUnitAccepted;
            logicUnit.PlayerJoined += onPlayerJoined;
            logicUnit.PlayerLeave += onPlayerLeave;
            logicUnit.FirstChanged += onFirstChanged;
            logicUnit.ActivedChanged += onActivedChanged;
            logicUnit.Started += onStarted;
            logicUnit.Ended += onEnded;

            IsAttached = true;
        }

        public void Detach()
        {
            IsAttached = false;

            logicUnit.Detach();
            if (boad != null)
                boad.LatticClick -= onBoadLatticClick;
            connector.MessageReceived -= onRemoteMessageReceived;
            logicUnit.Accepted -= onLogicUnitAccepted;
            logicUnit.PlayerJoined -= onPlayerJoined;
            logicUnit.PlayerLeave -= onPlayerLeave;
            logicUnit.FirstChanged -= onFirstChanged;
            logicUnit.ActivedChanged -= onActivedChanged;
            logicUnit.Started -= onStarted;
            logicUnit.Ended -= onEnded;
        }

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

        private void onGameModeChanged()
        {
        }

        private void onRemoteMessageReceived(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.Message:
                    System.Diagnostics.Debug.WriteLine("\r\nNew message\r\n" + msg.ToString());
                    break;
                case MessageType.Action:
                    if (msg.Content != null && msg.Content is InputAction)
                    {
                        var action = (InputAction)msg.Content;
                        bool handResult = false;
                        if (action.Type == ActionType.Join)
                        {
                            if (!remotePlayers.ContainsKey(msg.Token))
                                remotePlayers.Add(msg.Token, new RemotePlayer(msg.Token));
                            handResult = logicUnit.Join(remotePlayers[msg.Token]);
                        }
                        else if (remotePlayers.ContainsKey(msg.Token))
                        {
                            string token = remotePlayers[msg.Token].Token;
                            handResult = logicUnit.HandInput(token, action);
                        }
                        connector.SendMessage(MessageType.Fallback, new object[]
                        {
                            action.Type, handResult
                        });
                    }
                    break;
            }
        }

        private void onBoadLatticClick(int x, int y)
        {
            if (!logicUnit.IsStarted) return;
            if (!logicUnit.Actived.IsVirtual())
            {
                logicUnit.Actived.Input(new Point(x, y));
            }
        }
        
        private void onLogicUnitAccepted(string token, InputAction action)
        {

        }

    }
}
