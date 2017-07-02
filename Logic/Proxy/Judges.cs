using LogicUnit.Data;
using NetworkService;
using System.Collections.Generic;
using System.Linq;

namespace LogicUnit
{
    public class Judges : BridgeBase
    {
        private JudgeUnit judge;
        //Strore originToken - judgeToken
        private Dictionary<string, string> toJudge;
        private Dictionary<string, string> toProxy;
        internal JudgeUnit JudgeUnit => judge;
        private object lockHelper = new object();

        public Judges() : this(new MapFormation(2, 2, 24)) { }
        public Judges(MapFormation rest)
        {
            judge = new JudgeUnit(rest);
            toJudge = new Dictionary<string, string>();
            toProxy = new Dictionary<string, string>();
        }

        public void onAttach()
        {
            judge.OnJudged += onJudged;
            judge.Attach();
        }

        public void onDetach()
        {
            judge.Detach();
            judge.OnJudged -= onJudged;
        }

        public void Reset()
        {
            judge.Reset();
        }
        
        protected override void onMessage(Message msg)
        {
            if(msg != null && msg.Type == MessageType.Action)
            {
                lock (lockHelper)
                {
                    handAction(msg.Token, msg.Content);
                }
            }
        }

        internal int GetBoxId(string proxyToken)
        {
            if (!string.IsNullOrEmpty(proxyToken))
            {
                if (toProxy.ContainsValue(proxyToken))
                {
                    return judge.GetBoxId(toProxy.FirstOrDefault(p => p.Value == proxyToken).Key);
                }
            }
            return -1;
        }

        //Hand actions from player
        private void handAction(string proxyToken, object content)
        {
            if (content != null && content is InputAction)
            {
                var action = (InputAction)content;
                System.Diagnostics.Debug.WriteLine(".....\n[JudgeProxy] <-- " + action.Type);
                System.Diagnostics.Debug.WriteLine(".....\n[JudgeProxy] <-- " + proxyToken);

                bool accepted = false;
                object fallback = null;
                //Hand join action
                //Create virtual player and hand by judge
                if (action.Type == ActionType.Join)
                {
                    if (!toJudge.ContainsKey(proxyToken))
                    {
                        toJudge.Add(proxyToken, string.Empty);
                    }
                    jToken = string.Empty;
                    pToken = proxyToken;
                    accepted = judge.Join(ref jToken);
                    toJudge[proxyToken] = jToken;
                    jToken = string.Empty;
                    pToken = string.Empty;
                }/*else if(action.Type == ActionType.Undo)
                {
                    string active = findCastToken(judge.ActiveToken);
                    //Send fallback to player
                    Broadcast(Message.CreateMessage(active, new object[] {
                        action.Type, proxyToken
                    }, MessageType.Action));
                }*/
                //Hand other actions
                else if (toJudge.ContainsKey(proxyToken))
                {
                    if(action.Type == ActionType.Ready)
                    {

                    }
                    accepted = judge.HandInput(toJudge[proxyToken], action);
                }
                //Send fallback to player
                /*Broadcast(Message.CreateMessage(proxyToken, new object[]{
                    action.Type, fallback, accepted
                }, MessageType.Fallback));*/
            }
        }
        
        private void onJudged(string judgeToken, JudgeCode code, object extra)
        {
            string proxyToken = findCastToken(judgeToken);
            var data = new List<object>() {code};
            if (extra != null) data.Add(extra);

            BroadcastJudgeMsg(proxyToken, data.ToArray());
        }

        private void BroadcastJudgeMsg(string proxyToken, object[] data)
        {
            Broadcast(Message.CreateMessage(proxyToken, data, MessageType.Judge));
        }

        private string findCastToken(string judgeToken)
        {
            if (!string.IsNullOrEmpty(judgeToken))
            {
                if (!toProxy.ContainsKey(judgeToken))
                {
                    if(judgeToken == jToken && !string.IsNullOrEmpty(pToken))
                    {
                        toProxy.Add(judgeToken, pToken);
                        return pToken;
                    }
                    else
                    {
                        var pair = toJudge.FirstOrDefault(p => p.Value == judgeToken);
                        if (!default(KeyValuePair<string, string>).Equals(pair))
                        {
                            toProxy.Add(judgeToken, pair.Key);
                            return pair.Key;
                        }
                    }
                }
                else return toProxy[judgeToken];
            }
            return string.Empty;
        }

        private string jToken;
        private string pToken;
    }
}
