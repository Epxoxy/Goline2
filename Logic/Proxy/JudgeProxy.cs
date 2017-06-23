using LogicUnit.Data;
using NetworkService;
using System.Collections.Generic;
using System.Linq;

namespace LogicUnit
{
    public class JudgeProxy : ProxyCenter
    {
        private JudgeUnit judge;
        private Dictionary<string, VirtualPlayer> virtualPlayers;
        private Dictionary<string, string> tokens;
        internal JudgeUnit JudgeUnit => judge;

        public JudgeProxy() : this(new Restriction(2, 2, 24)) { }
        public JudgeProxy(Restriction rest)
        {
            judge = new JudgeUnit(rest);
            virtualPlayers = new Dictionary<string, VirtualPlayer>();
            tokens = new Dictionary<string, string>();
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
        
        protected override void onMessage(Message msg)
        {
            if(msg != null && msg.Type == MessageType.Action)
                handAction(msg.Token, msg.Content);
        }

        //Hand actions from player
        private void handAction(string token, object content)
        {
            if (content != null && content is InputAction)
            {
                var action = (InputAction)content;
                System.Diagnostics.Debug.WriteLine(".....\n[JudgeProxy] <-- " + action.Type);

                bool isAccepted = false;
                object dataFallback = null;
                //Hand join action
                //Create virtual player and hand by judge
                if (action.Type == ActionType.Join)
                {
                    VirtualPlayer player = null;
                    if (!virtualPlayers.ContainsKey(token))
                    {
                        player = new VirtualPlayer(token);
                        virtualPlayers.Add(token, player);
                    }
                    else player = virtualPlayers[token];
                    
                    isAccepted = judge.Join(player);
                }
                //Get host token fallback
                else if (action.Type == ActionType.AskHost)
                {
                    dataFallback = judge.First.Token;
                    isAccepted = true;
                }
                //Hand other actions
                else if (virtualPlayers.ContainsKey(token))
                {
                    isAccepted = judge.HandInput(virtualPlayers[token].Token, action);
                }
                //Send fallback to player
                Unicast(token, Message.CreateMessage(token, new object[]{
                    action.Type, dataFallback, isAccepted
                }, MessageType.Fallback));
            }
        }
        
        private void onJudged(string token, JudgeCode code, object extra)
        {
            token = findCastToken(token);
            var data = new List<object>() { token, code};
            if (extra != null) data.Add(extra);

            BroadcastJudgeMsg(token, data.ToArray());
        }

        internal int GetBoxId(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                foreach(var p in virtualPlayers)
                {
                    if(p.Value.OriginToken  == token)
                        return judge.GetBoxId(p.Value.Token);
                }
            }
            return -1;
        }

        private void BroadcastJudgeMsg(string token, object[] data)
        {
            Broadcast(Message.CreateMessage(token, data, MessageType.Judge));
        }

        private string findCastToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                if (!tokens.ContainsKey(token))
                {
                    var player = findPlayer(token);
                    if (player != null)
                    {
                        tokens.Add(token, player.OriginToken);
                        return player.OriginToken;
                    }
                }
                else return tokens[token];
            }
            return string.Empty;
        }

        private VirtualPlayer findPlayer(string token)
        {
            foreach (var pair in virtualPlayers)
            {
                if (pair.Value.Token == token)
                    return pair.Value;
            }
            return null;
        }
    }
}
