using LogicUnit.Data;
using LogicUnit.Interface;
using NetworkService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogicUnit
{
    public interface IEnvProxy
    {
        bool IsStarted { get; }
        bool IsEnded { get; }
        void Ready();
        void Start();
    }

    public class EVPEnvProxy : IEnvProxy
    {
        public bool IsStarted { get; private set; }
        public bool IsEnded { get; private set; }

        public static EVPEnvProxy Create(IBoard board, AILevel level)
        {
            return new EVPEnvProxy(board, level, "a"); ;
        }

        public void Ready()
        {
            judgeProxy.Enable();
            for (int i = 0; i < tokens.Count; i++)
            {
                shareProxy.PassByToken(tokens[i], ActionType.Join, null);
                shareProxy.PassByToken(tokens[i], ActionType.Ready, null);
            }
        }

        public void Start()
        {
        }

        private EVPEnvProxy(IBoard board, AILevel level, string token)
        {
            this.judgeProxy = new JudgeProxy();
            this.board = board;
            this.judgeProxy.onAttach();
            //Attach proxy
            shareProxy = new LocalProxy(onRelay);
            this.judgeProxy.AttachProxy(shareProxy);
            //Subscribe event
            board.LatticClick += onLatticClick;
            //Generate token and store it.
            userToken = token;
            envToken = TokenHelper.NewToken();
            this.tokens = new List<string>(2);
            tokens.Add(envToken);
            tokens.Add(token);
            deep = 20;
            switch (level)
            {
                case AILevel.Advanced:
                    break;
                case AILevel.Intermediate:
                    deep = 10;
                    break;
                case AILevel.Elementary:
                    deep = 1;
                    break;
            }
        }

        private void onLatticClick(int x, int y)
        {
            shareProxy.PassByToken(userToken, ActionType.Input, new IntPoint(x, y));
        }

        private void onRelay(object sender, Message msg)
        {
            System.Diagnostics.Debug.WriteLine("......\nRelay --> " + msg.Type);
            if (msg.Type == MessageType.Judge)
                onJudge(sender, msg.Content as object[]);
        }

        private void onJudge(object sender, object[] data)
        {
            int size = isValid(data);
            if (size < 2) return;

            string token = (string)data[0];
            JudgeCode code = (JudgeCode)data[1];
            switch (code)
            {
                case JudgeCode.Started:
                    onStateJudge(true);
                    break;
                case JudgeCode.Ended:
                    onStateJudge(false);
                    break;
                case JudgeCode.Joined:
                    int boxId = judgeProxy.GetBoxId(token);
                    if (token == envToken)
                        envBoxId = boxId;
                    else userBoxId = boxId;
                    break;
                case JudgeCode.Actived:
                    activedToken = token;
                    analysis(activedToken);
                    break;
                case JudgeCode.Inputed:
                    if (size != 3) break;
                    var input = (InputAction)data[2];
                    IntPoint p = (IntPoint)input.Data;
                    board?.DrawChess(p.X, p.Y, tokens.IndexOf(token));
                    break;
            }
            System.Diagnostics.Debug.WriteLine("[Judge]-[" + code + "] " + token);
        }

        private void onStateJudge(bool started)
        {
            this.IsStarted = started;
            this.IsEnded = !started;
            System.Diagnostics.Debug.WriteLine("State " + started);
        }

        private void analysis(string actived)
        {
            if (actived == envToken)
            {
                if(analyzer == null)
                {
                    var map = judgeProxy.JudgeUnit.GetMap();
                    analyzer = new Analyzer.AlphaBetaMaxMinAnalyzer(map, envBoxId, userBoxId);
                }
                Task.Run(async () =>
                {
                    long delay = (new Random()).Next(200, 1000);
                    var sw = new Stopwatch();
                    IntPoint p = default(IntPoint);
                    sw.Start();
                    p = analyzer.Analysis(judgeProxy.JudgeUnit.CurrentData(), deep);
                    sw.Stop();
                    long used = sw.ElapsedMilliseconds;
                    if (used < delay)
                        await Task.Delay((int)(delay - used));
                    System.Diagnostics.Debug.WriteLine(p);
                    shareProxy.PassByToken(envToken, ActionType.Input, p);
                });
            }
        }

        private int isValid(object[] objs)
        {
            if (objs != null)
            {
                return objs.Length;
            }
            else return 0;
        }

        private IBoard board;
        private JudgeProxy judgeProxy;
        private LocalProxy shareProxy;
        private IAnalyzer<int[,], IntPoint> analyzer;
        private int deep = 0;
        private int userBoxId;
        private int envBoxId;
        private List<string> tokens;
        private string userToken;
        private string envToken;
        private string activedToken;
    }

    public class PVPEnvProxy : IEnvProxy
    {
        public bool IsStarted { get; private set; }
        public bool IsEnded { get; private set; }

        public static PVPEnvProxy Create(IBoard board)
        {
            return new PVPEnvProxy(board, new List<string>() { "a", "b" }); ;
        }

        public void Ready()
        {
            judgeProxy.Enable();
            for (int i = 0; i < tokens.Count; i++)
            {
                shareProxy.PassByToken(tokens[i], ActionType.Join, null);
                shareProxy.PassByToken(tokens[i], ActionType.Ready, null);
            }
        }
        
        public void Start()
        {
        }

        private PVPEnvProxy(IBoard board, List<string> tokens)
        {
            this.judgeProxy = new JudgeProxy();
            this.board = board;
            this.judgeProxy.onAttach();
            shareProxy = new LocalProxy(onRelay);
            this.judgeProxy.AttachProxy(shareProxy);
            this.tokens = tokens;
            board.LatticClick += onLatticClick;
        }

        private void onLatticClick(int x, int y)
        {
            shareProxy.PassByToken(activedToken, ActionType.Input, new IntPoint(x, y));
        }

        private void onRelay(object sender, Message msg)
        {
            System.Diagnostics.Debug.WriteLine("......\nRelay --> " + msg.Type);
            if (msg.Type == MessageType.Judge)
                onJudge(sender, msg.Content as object[]);
        }

        private void onJudge(object sender, object[] data)
        {
            int size = isValid(data);
            if (size < 2) return;

            string token = (string)data[0];
            JudgeCode code = (JudgeCode)data[1];

            if(code == JudgeCode.Started)
            {
                onStateJudge(true);
            }else if (code == JudgeCode.Ended)
            {
                onStateJudge(false);
            }else if(code == JudgeCode.Actived)
            {
                this.activedToken = token;
            }
            if (size == 3 && code == JudgeCode.Inputed)
            {
                var input = (InputAction)data[2];
                IntPoint p = (IntPoint)input.Data;
                board?.DrawChess(p.X, p.Y, tokens.IndexOf(token));
            }
            System.Diagnostics.Debug.WriteLine("[Judge]-[" + code + "] " + token);
        }

        private void onStateJudge(bool started)
        {
            this.IsStarted = started;
            this.IsEnded = !started;
            System.Diagnostics.Debug.WriteLine("State " + started);
        }

        private int isValid(object[] objs)
        {
            if (objs != null)
            {
                return objs.Length;
            }
            else return 0;
        }
        
        private IBoard board;
        private JudgeProxy judgeProxy;
        private LocalProxy shareProxy;
        private List<string> tokens;
        private string activedToken;
    }

    public class OnlineEnvProxy : ProxyCenter, IEnvProxy
    {
        public bool IsStarted { get; private set; }
        public bool IsEnded { get; private set; }

        public static OnlineEnvProxy Create(IBoard board, string outip, int outport, int inport)
        {
            return new OnlineEnvProxy(board, "r", outip, outport, inport);
        }

        public void Ready()
        {
            this.Enable();
            remote.EnableOut(outip, outport);
        }

        public void Start()
        {
            remote.RelayAction(ActionType.Join, null);
            remote.RelayAction(ActionType.Ready, null);
        }
        
        private OnlineEnvProxy(IBoard board, string localToken, string outip, int outport, int inport)
        {
            this.outip = outip;
            this.inport = inport;
            this.outport = outport;
            
            this.board = board;
            remote = new RemoteProxy(new Connector());
            this.AttachProxy(remote);
            remote.EnableIn(System.Net.IPAddress.Any, inport);
            board.LatticClick += onLatticClick;
        }

        protected override void onMessage(Message msg)
        {
            base.onMessage(msg);
            System.Diagnostics.Debug.WriteLine("......\nRelay --> " + msg.Type);
            if (msg.Type == MessageType.Judge)
                onJudge(msg.Content as object[]);
        }

        private void onLatticClick(int x, int y)
        {
            remote.RelayAction(ActionType.Input, new IntPoint(x, y));
        }

        private void onJudge(object[] data)
        {
            int size = isValid(data);
            if (size < 2) return;

            string token = (string)data[0];
            JudgeCode code = (JudgeCode)data[1];

            if (code == JudgeCode.Started)
            {
                onStateJudge(true);
            }
            else if (code == JudgeCode.Ended)
            {
                onStateJudge(false);
            }
            else if (code == JudgeCode.Actived)
            {
                isActived = token == remote.Token;
            }
            if (size == 3 && code == JudgeCode.Inputed)
            {
                var input = (InputAction)data[2];
                IntPoint p = (IntPoint)input.Data;
                board?.DrawChess(p.X, p.Y, token == remote.Token ? 1 : 2);
            }
            System.Diagnostics.Debug.WriteLine("[Judge]-[" + code + "] " + token);
        }

        private void onStateJudge(bool started)
        {
            this.IsStarted = started;
            this.IsEnded = !started;
            System.Diagnostics.Debug.WriteLine("State " + started);
        }

        private int isValid(object[] objs)
        {
            if (objs != null)
            {
                return objs.Length;
            }
            else return 0;
        }
        
        private IBoard board;
        private RemoteProxy remote;
        private bool isActived;
        private string outip;
        private int outport;
        private int inport;
    }

    public class OnlineHostEnvProxy : IEnvProxy
    {
        public bool IsStarted { get; private set; }
        public bool IsEnded { get; private set; }

        public static OnlineHostEnvProxy Create(IBoard board, string outip, int outport, int inport)
        {
            return new OnlineHostEnvProxy(board, outip, outport, inport);
        }

        public void Ready()
        {
            judgeProxy.Enable();
            remote.EnableOut(outip, outport);
        }

        public void Start()
        {
            local.PassAction(ActionType.Join, null);
            local.PassAction(ActionType.Ready, null);
        }


        private OnlineHostEnvProxy(IBoard board, string outip, int outport, int inport)
        {
            this.outip = outip;
            this.inport = inport;
            this.outport = outport;

            this.judgeProxy = new JudgeProxy();
            this.board = board;
            this.judgeProxy.onAttach();
            local = new LocalProxy(onRelay);
            remote = new RemoteProxy(new Connector());
            remote.EnableIn(System.Net.IPAddress.Any, inport);
            this.judgeProxy.AttachProxy(local);
            this.judgeProxy.AttachProxy(remote);
            board.LatticClick += onLatticClick;
        }
        
        private void onLatticClick(int x, int y)
        {
            local.PassAction(ActionType.Input, new IntPoint(x, y));
        }

        private void onRelay(object sender, Message msg)
        {
            System.Diagnostics.Debug.WriteLine("......\nRelay --> " + msg.Type);
            if (msg.Type == MessageType.Judge)
                onJudge(sender, msg.Content as object[]);
        }

        private void onJudge(object sender, object[] data)
        {
            int size = isValid(data);
            if (size < 2) return;

            string token = (string)data[0];
            JudgeCode code = (JudgeCode)data[1];

            if (code == JudgeCode.Started)
            {
                onStateJudge(true);
            }
            else if (code == JudgeCode.Ended)
            {
                onStateJudge(false);
            }
            else if (code == JudgeCode.Actived)
            {
                isActived = token == local.Token;
            }
            if (size == 3 && code == JudgeCode.Inputed)
            {
                var input = (InputAction)data[2];
                IntPoint p = (IntPoint)input.Data;
                board?.DrawChess(p.X, p.Y, token == local.Token ? 1 : 2);
            }
            System.Diagnostics.Debug.WriteLine("[Judge]-[" + code + "] " + token);
        }

        private void onStateJudge(bool started)
        {
            this.IsStarted = started;
            this.IsEnded = !started;
            System.Diagnostics.Debug.WriteLine("State " + started);
        }

        private int isValid(object[] objs)
        {
            if (objs != null)
            {
                return objs.Length;
            }
            else return 0;
        }
        

        private IBoard board;
        private JudgeProxy judgeProxy;
        private LocalProxy local;
        private RemoteProxy remote;
        private bool isActived;
        private string outip;
        private int outport;
        private int inport;
    }

    internal static class ProxyExtension
    {
        internal static void PassAction(this LocalProxy proxy, ActionType type, object data)
        {
            proxy.Pass(action(proxy.Token, type, data));
        }

        internal static void PassByToken(this LocalProxy proxy, string token, ActionType type, object data)
        {
            proxy.Pass(action(token, type, data));
        }

        internal static void RelayAction(this RemoteProxy proxy, ActionType type, object data)
        {
            proxy.Relay(action(proxy.Token, type, data));
        }

        private static Message action(string token, ActionType type, object data)
        {
            return Message.CreateMessage(token, new InputAction(type, data), MessageType.Action);
        }
    }
}
