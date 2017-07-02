using LogicUnit.Data;
using LogicUnit.Interface;
using NetworkService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogicUnit
{
    public delegate void StateChanged(JudgeCode code);
    public interface IGameCoreResolver : IDisposable
    {
        event StateChanged StateChanged;
        bool Useful { get; }
        bool IsStarted { get; }
        bool IsEnded { get; }
        bool IsHostActive { get; }
        string HostToken { get; }
        string JoinToken { get; }
        string WinnerToken { get; }
        void Ready();
        void Start();
        void Reset();
        void SetFirst(string token);
        void Undo();
        Task<IntPoint> Tips();
    }

    internal interface IJudgeResolver
    {
        void OnBasicJudge(string token, JudgeCode code);
        void OnJudgeInput(string token, JudgeCode code, IntPoint p);
        void OnAction(ActionType type, object[] content);
        void OnStateJudge(JudgeCode code);
        void RaiseStateChange(JudgeCode code);
    }
    
    internal class ResolverCommon
    {
        public bool IsStarted { get; protected set; }
        public bool IsEnded { get; protected set; } = true;
        public string ActiveToken { get; private set; }
        public string WinnerToken { get; private set; }
        private IJudgeResolver resolver;

        public ResolverCommon(IJudgeResolver resolver)
        {
            this.resolver = resolver;
        }

        public void OnJudge(string token, object[] data)
        {
            int size = ProxyEx.ValidSize(data);
            if (size < 1) return;
            JudgeCode code = (JudgeCode)data[0];

            switch (code)
            {
                case JudgeCode.Started:
                    onStateJudge(code, true);
                    break;
                case JudgeCode.Ended:
                    onStateJudge(code, false);
                    break;
                case JudgeCode.Active:
                    ActiveToken = token;
                    raiseStateChange(code);
                    break;
                case JudgeCode.NewWinner:
                    WinnerToken = token;
                    raiseStateChange(code);
                    break;
                case JudgeCode.Reset:
                    raiseStateChange(code);
                    resolver.OnStateJudge(code);
                    break;
            }
            if (size == 2 && (code == JudgeCode.Input || code == JudgeCode.Undo))
            {
                var input = (InputAction)data[1];
                IntPoint p = (IntPoint)input.Data;
                resolver.OnJudgeInput(token, code, p);
            }
            resolver.OnBasicJudge(token, code);
            Debug.WriteLine("[Judge]-[" + code + "] " + token);
        }

        public void RelayMessage(object sender, Message msg)
        {
            Debug.WriteLine("......\nRelay --> " + msg.Type);
            if (msg.Type == MessageType.Judge)
                OnJudge(msg.Token, msg.Content as object[]);
            else if (msg.Type == MessageType.Action)
            {
                var objs = msg.Content as object[];
                int size = ProxyEx.ValidSize(objs);
                if (size > 0)
                {
                    var type = (ActionType)objs[0];
                    resolver.OnAction(type, objs);
                }
            }
        }
        
        private void onStateJudge(JudgeCode code, bool started)
        {
            IsStarted = started;
            IsEnded = !started;
            if (IsStarted) WinnerToken = string.Empty;
            Debug.WriteLine("State " + started);
            raiseStateChange(code);
            resolver.OnStateJudge(code);
        }

        private void raiseStateChange(JudgeCode code)
        {
            resolver.RaiseStateChange(code);
        }
    }

    internal abstract class CoreResolverBase : IGameCoreResolver, IJudgeResolver
    {
        public event StateChanged StateChanged;
        public bool Useful { get; protected set; } = true;
        public bool IsStarted => resolverCommon.IsStarted;
        public bool IsEnded => resolverCommon.IsEnded;
        public string WinnerToken => resolverCommon.WinnerToken;
        public bool IsHostActive => ActiveToken == HostToken;

        protected string ActiveToken => resolverCommon.ActiveToken;
        public string HostToken { get; protected set; }
        public string JoinToken { get; protected set; }
        protected Judges Judges { get; set; }
        protected IBoard Board { get; set; }
        protected int HostBodId => hostBoxId;
        protected int JoinBodId => joinBoxId;

        private ResolverCommon resolverCommon;

        public CoreResolverBase()
        {
            Judges = new Judges();
            resolverCommon = new ResolverCommon(this);
        }

        public void RaiseStateChange(JudgeCode code)
        {
            StateChanged?.Invoke(code);
        }

        public void OnJudge(string token, object[] data)
        {
            resolverCommon.OnJudge(token, data);
        }

        public virtual void OnBasicJudge(string token, JudgeCode code)
        {
            if (code == JudgeCode.Joined)
            {
                int boxId = Judges.GetBoxId(token);
                if (token == HostToken)
                    hostBoxId = boxId;
                else if(token == JoinToken)
                    joinBoxId = boxId;
            }
        }

        public virtual void OnJudgeInput(string token, JudgeCode code, IntPoint p)
        {

        }

        public virtual void OnAction(ActionType type, object[] content)
        {

        }

        public virtual void OnStateJudge(JudgeCode code)
        {
            switch (code)
            {
                case JudgeCode.Reset:
                    Useful = true;
                    break;
                case JudgeCode.Started:
                    if(Board != null)
                        Board.Dispatcher.Invoke(()=>Board.ClearChess());
                    Useful = true;
                    break;
                case JudgeCode.Ended:
                    Useful = false;
                    break;
            }
        }
        
        protected void RelayMessage(object sender, Message msg)
        {
            Debug.WriteLine("......\nRelay --> " + msg.Type);
            if (msg.Type == MessageType.Judge)
                OnJudge(msg.Token, msg.Content as object[]);
            else if(msg.Type == MessageType.Action)
            {
                var objs = msg.Content as object[];
                int size = ProxyEx.ValidSize(objs);
                if(size > 0)
                {
                    var type = (ActionType)objs[0];
                    OnAction(type, objs);
                }
            }
        }

        public abstract void Ready();
        public abstract void Start();
        public abstract void Undo();
        public virtual void Reset()
        {
            Judges.Reset();
        }

        public abstract void Pass(string token, ActionType type, object data);
        
        public virtual void SetFirst(string token)
        {
            Pass(token, ActionType.First, null);
        }

        public abstract string GetToken(int index);

        public abstract void Dispose();

        public async Task<IntPoint> Tips()
        {
            int envId = IsHostActive ? joinBoxId : hostBoxId;
            int activeId = IsHostActive ? hostBoxId : joinBoxId;
            return await analysisTips(envId, activeId);
        }

        private async Task<IntPoint> analysisTips(int envId, int userId)
        {
            var map = Judges.JudgeUnit.GetMap();
            //User as environment, other part as user
            //Find a good result
            tipsAnalyzer = new Analyzer.AlphaBetaMaxMinAnalyzer(userId, envId);
            IntPoint p = default(IntPoint);
            await Task.Run(() =>
            {
                p = tipsAnalyzer.Analysis(map, deep);
            });
            return p;
        }

        private IAnalyzer<IMap,IntPoint> tipsAnalyzer;
        private int hostBoxId;
        private int joinBoxId;
        private int deep;
    }

    internal class EVPResolver : CoreResolverBase
    {
        internal EVPResolver(IBoard board, AILevel level)
        {
            Board = board;
            Judges.onAttach();
            //Attach proxy
            JoinToken = TokenHelper.NewToken();
            HostToken = TokenHelper.NewToken();
            shareProxy = new LocalProxy(RelayMessage);
            shareProxy.Token = TokenHelper.NewToken();
            Judges.AttachProxy(shareProxy);
            //Subscribe event
            board.LatticClick += onLatticClick;
            //Generate token and store it.
            tokens = new List<string>(2);
            tokens.Add(JoinToken);
            tokens.Add(HostToken);
            setDeep(level);
        }

        public override void Ready()
        {
            Judges.Enable();
            foreach (var token in tokens)
            {
                Pass(token, ActionType.Join, null);
            }
        }

        public override void Start()
        {
            foreach (var token in tokens)
            {
                Pass(token, ActionType.Ready, null);
            }
        }


        public override void OnBasicJudge(string token, JudgeCode code)
        {
            base.OnBasicJudge(token, code);
            if (code == JudgeCode.Active)
            {
                analysis(ActiveToken);
            }
        }
        
        public override void OnJudgeInput(string token, JudgeCode code, IntPoint p)
        {
            Board?.Dispatcher.Invoke(() => {
                Board.DrawChess(p.X, p.Y, IsHostActive);
            });
        }

        public override void OnAction(ActionType type, object[] content)
        {
            /*if(type == ActionType.Undo)
            {
                var token = content[1];
                Pass(JoinToken, ActionType.AllowUndo, token);
            }*/
        }

        public override void Pass(string token, ActionType type, object data)
        {
            shareProxy.PassActionByToken(token, type, data);
        }

        public override void Dispose()
        {
            ProxyEx.Dispose(shareProxy);
        }

        public override string GetToken(int index)
        {
            if (index < tokens.Count) return tokens[index];
            return string.Empty;
        }
        
        private void setDeep(AILevel level)
        {
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
            if (IsEnded) return;
            Pass(HostToken, ActionType.Input, new IntPoint(x, y));
        }
        
        private void analysis(string active)
        {
            if (active == JoinToken)
            {
                IMap map = Judges.JudgeUnit.GetMap();
                if (analyzer == null)
                {
                    analyzer = new Analyzer.AlphaBetaMaxMinAnalyzer(JoinBodId, HostBodId);
                }
                Task.Run(async () =>
                {
                    long delay = (new Random()).Next(200, 1000);
                    var sw = new Stopwatch();
                    IntPoint p = default(IntPoint);
                    sw.Start();
                    p = analyzer.Analysis(map, deep);
                    sw.Stop();
                    long used = sw.ElapsedMilliseconds;
                    if (used < delay)
                        await Task.Delay((int)(delay - used));
                    Debug.WriteLine(p);
                    Pass(JoinToken, ActionType.Input, p);
                });
            }
        }

        public override void Undo()
        {
            Pass(HostToken, ActionType.Undo, null);
        }

        private LocalProxy shareProxy;
        private IAnalyzer<IMap, IntPoint> analyzer;
        private int deep = 0;
        private List<string> tokens;
    }

    internal class PVPResolver : CoreResolverBase
    {
        public override void Ready()
        {
            Judges.Enable();
            foreach (var token in tokens)
                Pass(token, ActionType.Join, null);
        }

        public override void Start()
        {
            foreach (var token in tokens)
                Pass(token, ActionType.Ready, null);
        }


        internal PVPResolver(IBoard board)
        {
            Board = board;

            Judges.onAttach();
            HostToken = TokenHelper.NewToken();
            JoinToken = TokenHelper.NewToken();
            tokens = new List<string>() { HostToken, JoinToken };
            shareProxy = new LocalProxy(RelayMessage);
            shareProxy.Token = HostToken;
            Judges.AttachProxy(shareProxy);
            board.LatticClick += onLatticClick;
        }

        public override void OnJudgeInput(string token, JudgeCode code, IntPoint p)
        {
            base.OnJudgeInput(token, code, p);
            if(code == JudgeCode.Input)
            {
                Board?.Dispatcher.Invoke(()=>Board.DrawChess(p.X, p.Y, IsHostActive));
            }
            else if(code == JudgeCode.Undo)
            {
                Board?.Dispatcher.Invoke(() => Board.RemoveChess(p.X, p.Y));
            }
        }

        public override void Pass(string token, ActionType type, object data)
        {
            shareProxy.PassActionByToken(token, type, data);
        }

        public override string GetToken(int index)
        {
            if (index < tokens.Count) return tokens[index];
            return string.Empty;
        }
        
        public override void Dispose()
        {
            ProxyEx.Dispose(shareProxy);
        }

        public override void Undo()
        {
            Pass(ActiveToken, ActionType.Undo, null);
        }

        private void onLatticClick(int x, int y)
        {
            if (IsEnded) return;
            shareProxy.PassActionByToken(ActiveToken, ActionType.Input, new IntPoint(x, y));
        }

        private LocalProxy shareProxy;
        private List<string> tokens;
    }

    internal class OnlineResolver : BridgeBase, IGameCoreResolver, IJudgeResolver
    {
        public event StateChanged StateChanged;
        public bool IsHostActive => resolverCommon.ActiveToken == remote.Token;
        public string HostToken => remote.Token;
        public string JoinToken { get; private set; }
        public string WinnerToken => resolverCommon.WinnerToken;
        public bool IsStarted => resolverCommon.IsStarted;
        public bool IsEnded => resolverCommon.IsEnded;
        public bool Useful { get; private set; } = true;

        internal OnlineResolver(IBoard board, string outip, int outport, int inport)
        {
            resolverCommon = new ResolverCommon(this);
            this.outip = outip;
            this.inport = inport;
            this.outport = outport;

            this.board = board;
            var token = TokenHelper.NewToken();
            var connector = new Connector(token);
            remote = new RemoteProxy(connector);
            remote.Token = token;
            AttachProxy(remote);

            remote.EnableIn(System.Net.IPAddress.Any, inport);
            board.LatticClick += onLatticClick;
        }

        protected override void onMessage(Message msg)
        {
            base.onMessage(msg);
            Debug.WriteLine("......\nRelay --> " + msg.Type);
            if (msg.Type == MessageType.Judge)
            {
                resolverCommon.OnJudge(msg.Token, msg.Content as object[]);
            }
        }

        public void Ready()
        {
            Enable();
            remote.EnableOut(outip, outport);
            remote.Relay(Message.CreateMessage(remote.Token, "", MessageType.Proxy));
            Useful = remote.Connected;
        }

        public void Start()
        {
            Pass(null,ActionType.Join, null);
            Pass(null, ActionType.Ready, null);
        }

        public void Reset()
        {
            Pass(null, ActionType.Reset, new object());
        }

        public void Pass(string token, ActionType type, object data)
        {
            remote.RelayAction(type, data);
        }

        public void SetFirst(string token)
        {
            Pass(null, ActionType.First, null);
        }

        public string GetToken(int index)
        {
            if (index == 0) return remote.Token;
            return string.Empty;
        }

        public void Dispose()
        {
            if(remote != null)
            {
                remote.Dispose();
                remote = null;
            }
        }


        #region Implement IJudgeResolver

        public void OnBasicJudge(string token, JudgeCode code)
        {
            if(code == JudgeCode.Joined && token != HostToken)
                if (string.IsNullOrEmpty(JoinToken))
                    JoinToken = token;
        }

        public void OnJudgeInput(string token, JudgeCode code, IntPoint p)
        {
            board?.Dispatcher.Invoke(() => board.DrawChess(p.X, p.Y, IsHostActive));
        }

        public void OnAction(ActionType type, object[] content)
        {
        }

        public void OnStateJudge(JudgeCode code)
        {
            if (IsEnded)
                Useful = false;
        }

        public void RaiseStateChange(JudgeCode code)
        {
            StateChanged?.Invoke(code);
        }

        #endregion


        private void onLatticClick(int x, int y)
        {
            if (IsEnded) return;
            Pass(null, ActionType.Input, new IntPoint(x, y));
        }

        public Task<IntPoint> Tips()
        {
            throw new NotImplementedException();
        }

        public void Undo()
        {
            Pass(null, ActionType.Undo, null);
        }

        private ResolverCommon resolverCommon;
        private RemoteProxy remote;
        private IBoard board;
        private string outip;
        private int outport;
        private int inport;
    }

    internal class OnlineHostResolver : CoreResolverBase
    {
        internal OnlineHostResolver(IBoard board, string outip, int outport, int inport)
        {
            this.outip = outip;
            this.inport = inport;
            this.outport = outport;

            HostToken = TokenHelper.NewToken();
            tokens = new List<string>();
            Board = board;
            Judges.onAttach();
            //Create local proxy
            local = new LocalProxy(RelayMessage);
            local.Token = HostToken;
            tokens.Add(HostToken);
            //Create remote proxy
            JoinToken = TokenHelper.NewToken();
            var connector = new Connector(JoinToken);
            remote = new RemoteProxy(connector);
            remote.Token = JoinToken;
            tokens.Add(remote.Token);
            remote.EnableIn(System.Net.IPAddress.Any, inport);
            Judges.AttachProxy(local);
            Judges.AttachProxy(remote);
            Board.LatticClick += onLatticClick;
        }

        public override void OnJudgeInput(string token, JudgeCode code, IntPoint p)
        {
            base.OnJudgeInput(token, code, p);
            Board?.Dispatcher.Invoke(()=>Board.DrawChess(p.X, p.Y, IsHostActive));
        }

        public override void Ready()
        {
            Judges.Enable();
            remote.EnableOut(outip, outport);
        }

        public override void Start()
        {
            Pass(ActionType.Join, null);
            Pass(ActionType.Ready, null);
        }

        public override void Pass(string token, ActionType type, object data)
        {
            local.PassAction(type, data);
        }

        public override string GetToken(int index)
        {
            if (index < tokens.Count) return tokens[index];
            return string.Empty;
        }
        
        public override void Dispose()
        {
            ProxyEx.Dispose(local);
            ProxyEx.Dispose(remote);
        }

        public void Pass(ActionType type, object data)
        {
            local.PassAction(type, data);
        }

        private void onLatticClick(int x, int y)
        {
            if (IsEnded) return;
            local.PassAction(ActionType.Input, new IntPoint(x, y));
        }

        public override void Undo()
        {
            Pass(HostToken, ActionType.Undo, null);
        }

        private List<string> tokens;
        private LocalProxy local;
        private RemoteProxy remote;
        private string outip;
        private int outport;
        private int inport;
    }

    public class GameResolver
    {
        public static IGameCoreResolver BuildEVP(IBoard board, AILevel level)
        {
            return new EVPResolver(board, level);
        }

        public static IGameCoreResolver BuildPVP(IBoard board)
        {
            return new PVPResolver(board);
        }

        public static IGameCoreResolver BuildOnline(IBoard board, string outip, int outport, int inport)
        {
            return new OnlineResolver(board, outip, outport, inport);
        }

        public static IGameCoreResolver BuildOnlineHost(IBoard board, string outip, int outport, int inport)
        {
            return new OnlineHostResolver(board, outip, outport, inport);
        }

    }

    internal static class ProxyEx
    {
        internal static int ValidSize(object[] objs)
        {
            if (objs != null)
            {
                return objs.Length;
            }
            else return 0;
        }

        internal static void PassAction(this LocalProxy proxy, ActionType type, object data)
        {
            proxy.Send(action(proxy.Token, type, data));
        }

        internal static void PassActionByToken(this LocalProxy proxy, string token, ActionType type, object data)
        {
            proxy.SendByToken(token, action(token, type, data));
        }

        internal static void RelayAction(this RemoteProxy proxy, ActionType type, object data)
        {
            proxy.Relay(action(proxy.Token, type, data));
        }

        private static Message action(string token, ActionType type, object data)
        {
            return Message.CreateMessage(token, new InputAction(type, data), MessageType.Action);
        }

        internal static void Dispose(IProxy proxy)
        {
            if(proxy != null)
            {
                proxy.Dispose();
                proxy = null;
            }
        }
    }
}
