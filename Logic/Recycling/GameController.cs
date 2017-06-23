using LogicUnit.Data;
using LogicUnit.Interface;
using NetworkService;
using System;

namespace LogicUnit
{   
    public class GameController : IDisposable
    {
        public bool IsOnline { get; private set; }
        public bool IsAttached { get; private set; }

        public event MessageReceivedEventHandler OnNewMessage;
        public IConfirmer<string> Confirmer { get; set; }
        public IMessageNotifier Notifier { get; set; }
        public SenseInfo SenseInfo { get; set; }
        
        private Connector connector;
        private IBoard board;
        
        private string localToken;
        private bool isHost = false;
        private GameMode mode;

        public GameController(GameMode mode, IBoard board, Player host = null)
        {
            this.board = board;
            this.mode = mode;
            this.localToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
            connector = new Connector(localToken);
        }

        //Tach part
        public void Attach()
        {
            if(board != null)
                board.LatticClick += onBoadLatticClick;
            connector.MessageReceived += onMessageReceived;
            connector.ConnectionReady += onConnectionReady;
            IsAttached = true;
        }

        public void Detach()
        {
            IsAttached = false;
            
            if (board != null)
                board.LatticClick -= onBoadLatticClick;
            connector.MessageReceived -= onMessageReceived;
            connector.ConnectionReady -= onConnectionReady;
        }
        

        //Receive judge message handlers
        private void onStarted()
        {
            SenseInfo?.OnStart();
            Notifier?.Notify("Game is started.", DateTime.Now.ToString());
        }

        private void onEnded()
        {
            SenseInfo?.OnEnded();
        }
        
        private void onActivedChanged(string token)
        {
            SenseInfo?.ChangedActived(token);
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
        
        private void onJudgeAccepted(string token, InputAction action)
        {
            Notifier?.Notify("[" + token + "] " + action.Type.ToString(), action.Data == null ? "Null" : action.Data.ToString());

            if (action.Type == ActionType.Input)
            {
                var data = (IntPoint)action.Data;
                board?.DrawChess(data.X, data.Y, 0);
            }
            else if (action.Type == ActionType.Undo)
            {
                var data = (IntPoint)action.Data;
                board?.RemoveChess(data.X, data.Y);
            }
        }
        
        //Receive message handlers
        private void onMessageReceived(Message msg)
        {
            //Notifier?.Notify("Message", msg.Type.ToString());
            switch (msg.Type)
            {
                case MessageType.Message:
                    Notifier?.Notify("Message", "New message\r\n" + msg.ToString());
                    break;
                case MessageType.Fallback:
                    checkObjects(msg.Content as object[], 3, 
                        objs => handFallback(objs[0], objs[1], objs[2]));
                    break;
                case MessageType.Judge:
                    checkObjects(msg.Content as object[], 3,
                        objs => handJudge(objs[0], objs[1], objs[3]));
                    break;
            }
        }

        private void checkObjects(object[] objs, int length, Action<object[]> doThing)
        {
            if (objs != null && objs.Length == length)
                doThing.Invoke(objs);
        }
        
        private void handJudge(object tokenObj, object data, object extra)
        {
            if(data is JudgeCode && tokenObj is string)
            {
                var token = (string)tokenObj;
                var code = (JudgeCode)data;
                switch (code)
                {
                    case JudgeCode.Actived:
                        onActivedChanged(token);
                        break;
                    case JudgeCode.MarkFirst:
                        onFirstChanged(token);
                        break;
                    case JudgeCode.Joined:
                        onPlayerJoined(token);
                        break;
                    case JudgeCode.Leave:
                        onPlayerLeave(token);
                        break;
                    case JudgeCode.Accepted:
                        if(extra != null && extra is InputAction)
                            onJudgeAccepted(token, (InputAction)extra);
                        break;
                }
            }
        }
        
        private void handFallback(object type, object data, object handResult)
        {
            ActionType uptype;
            bool upresult;
            if (type is ActionType)
            {
                uptype = (ActionType)type;
                upresult = handResult is bool && (bool)handResult;
                if (!upresult)
                    System.Diagnostics.Debug.WriteLine(uptype + " is fail.");

                if(uptype == ActionType.Join && upresult)
                {
                    sendRemoteAction(ActionType.AskHost, null);
                    System.Diagnostics.Debug.WriteLine("Joined fallback.");
                }
                else if(uptype == ActionType.AskHost)
                {
                    if(upresult && data is string)
                    {
                        string token = (string)data;
                        sendRemoteAction(ActionType.Ready, null);
                        System.Diagnostics.Debug.WriteLine("AskHost fallback.");
                    }
                }
            }
        }


        //Send message
        private void sendRemoteMessage(Message msg)
        {
            connector.SendMessage(MessageType.Message, msg);
        }

        private void sendRemoteAction(ActionType type, object data)
        {
            if (isHost)
            {
                OnNewMessage?.Invoke(Message.CreateMessage(localToken, new InputAction(type, data), MessageType.Action));
            }else connector.SendMessage(MessageType.Action, new InputAction(type, data));
        }
        
        //Send input message
        private void onBoadLatticClick(int x, int y)
        {
            sendRemoteAction(ActionType.Input, new IntPoint(x, y));
        }
        
        private void onConnectionReady(bool isConnected)
        {
            IsOnline = isConnected;
            System.Diagnostics.Debug.WriteLine("ConnectionReady " + isConnected);
        }
        
        public void Dispose()
        {
            Confirmer = null;
            Notifier = null;
            SenseInfo = null;
            if (connector != null)
            {
                connector.Disconnct();
                connector.StopListen();
                connector = null;
            }
        }
        
    }
}
