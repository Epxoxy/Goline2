using LogicUnit.Data;
using LogicUnit.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicUnit
{
    public delegate void JudgeEventHandler(string token, JudgeCode code, object extra = null);

    public class JudgeUnit
    {
        public event JudgeEventHandler OnJudged;

        public TimeSpan Elapsed { get; private set; }

        public Player First { get; private set; }
        public Player Actived { get; private set; }
        public Player Winner { get; private set; }
        public Player Next => Actived == null ? null : Actived.Next;
        public Player Front => Actived == null ? null : Actived.Front;

        public bool IsStarted { get; private set; }
        public bool IsAttached { get; private set; }

        private Dictionary<string, Player> players { get; set; }
        private Dictionary<string, PlayerExtra> extras { get; set; }
        private Restriction restriction;
        private DataBox dataBox;
        private object lockHelper = new object();

        public JudgeUnit(Restriction rst)
        {
            this.restriction = rst;
            dataBox = new DataBox(rst.EntryCopy(), rst.Allow, rst.NotAllow);
        }

        public IMap GetMap()
        {
            return restriction;
        }

        //When gameMaster is attach,you can join a valid player
        //before game start
        public bool Join(Player player)
        {
            if (player == null || !IsAttached || IsStarted)
                return false;
            lock (lockHelper)
            {
                if (players.ContainsValue(player)) return true;
                if (players.Count >= restriction.MaxPlayer) return false;
                string token = TokenHelper.NewToken();
                player.Token = token;

                if (players.Count > 0)
                {
                    var first = players.First().Value;
                    var last = players.Last().Value;
                    last.Next = player;
                    player.Front = last;
                    player.Next = first;
                }
                players.Add(token, player);
                extras.Add(token, new PlayerExtra()
                {
                    BoxId = players.Count + 1
                });
                player.Judge = this;
                player.IsAttached = true;
                System.Diagnostics.Debug.WriteLine($"Player[{player.Name}] Joined. Token[{token}]");
                raiseJudged(token, JudgeCode.Joined);
                if (players.Count == 1)
                    SignAsFirst(player.Token);

                return true;
            }
        }

        public bool Leave(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsAttached || IsStarted || players.Count < 1)
                return false;
            lock (lockHelper)
            {
                if (players.ContainsKey(token))
                {
                    var player = players[token];
                    players.Remove(token);
                    extras.Remove(token);
                    player.Judge = null;

                    var front = player.Front;
                    var next = player.Next;
                    front.Next = next;

                    System.Diagnostics.Debug.WriteLine($"Player[{player.Name}] Leave.");
                    raiseJudged(token, JudgeCode.Leave);
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Token[{token}] Valid Fail.");
                    return false;
                }
            }
        }

        public bool GiveUp(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsStarted || players.Count < 1) return false;
            lock (lockHelper)
            {
                if (players.ContainsKey(token))
                {
                    var player = players[token];
                    if (player.IsActive)
                    {
                        var watch = extras[token];
                        extras.Remove(token);
                        watch.Dispose();
                    }
                    players.Remove(token);
                    player.Judge = null;

                    var front = player.Front;
                    var next = player.Next;
                    front.Next = next;

                    if (IsStarted && players.Count == 1)
                        onWinnerAppend(front.Token);
                    else
                        tryActive(next);

                    System.Diagnostics.Debug.WriteLine($"Player[{player.Name}] Leave.");
                    raiseJudged(token, JudgeCode.GiveUp);
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Token[{token}] Valid Fail.");
                    return false;
                }
            }
        }

        public bool SignAsFirst(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsAttached || IsStarted 
                || !players.ContainsKey(token))
                return false;
            First = players[token];
            raiseJudged(token, JudgeCode.MarkFirst);
            return true;
        }

        public void Attach()
        {
            IsAttached = true;
            players = new Dictionary<string, Player>();
            extras = new Dictionary<string, PlayerExtra>();
        }

        public void Detach()
        {
            players.Clear();
            foreach(var ex in extras.Values)
                ex.Dispose();
            extras.Clear();
            players = null;
            extras = null;
        }
        
        public bool Ready(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsAttached || IsStarted || players.Count < 1)
                return false;
            if (extras.ContainsKey(token))
            {
                if (!extras[token].IsReady)
                {
                    extras[token].IsReady = true;
                    raiseJudged(token, JudgeCode.Ready);
                    if (extras.Count >= restriction.MinPlayer 
                        && extras.Count(ex => ex.Value.IsReady) == extras.Count)
                        Start();
                }
                return true;
            }
            return false;
        }

        public bool Start()
        {
            if(IsAttached && !IsStarted && players.Count >= restriction.MinPlayer)
            {
                if (extras.Count(ex => ex.Value.IsReady) == extras.Count)
                {
                    IsStarted = true;
                    raiseJudged(string.Empty, JudgeCode.Started);
                    tryActive(First);
                    return true;
                }
            }
            return false;
        }

        public void End()
        {
            if (IsStarted)
            {
                IsStarted = false;
                foreach (var ex in extras.Values)
                    ex.Dispose();
                raiseJudged(string.Empty, JudgeCode.Ended);
            }
        }

        public bool HandInput(string token, InputAction action)
        {
            bool isOk = false;
            lock (lockHelper)
            {
                if (!string.IsNullOrEmpty(token) && players.ContainsKey(token))
                {
                    switch (action.Type)
                    {
                        case ActionType.First:
                            isOk = SignAsFirst(token);
                            break;
                        case ActionType.Leave:
                            isOk = Leave(token);
                            break;
                        case ActionType.GiveUp:
                            isOk = GiveUp(token);
                            break;
                        case ActionType.Ready:
                            isOk = Ready(token);
                            break;
                        case ActionType.Input:
                            if (IsStarted && Actived.Token == token)
                            {
                                isOk = inputToBox(action.Data, extras[token].BoxId);
                                if (isOk)
                                {
                                    raiseJudged(token, JudgeCode.Inputed, action);
                                    if (isEnded()) End();
                                    else tryActive(players[token].Next);
                                }
                            }
                            break;
                        case ActionType.Undo:
                            DataPoint p;
                            isOk = dataBox.Undo(out p);
                            break;
                        case ActionType.Redo:
                            //Not support now
                            //DataPoint rp;
                            //accepted = dataBox.Redo(out rp);
                            break;
                    }
                }
            }
            return isOk;
        }

        internal int GetBoxId(string token)
        {
            if (extras.ContainsKey(token))
            {
                return extras[token].BoxId;
            }
            return -1;
        }

        internal int[,] CurrentData()
        {
            return dataBox.Copy();
        }

        private bool isEnded()
        {
            return dataBox.ReachableCount == 0;
        }

        private void raiseJudged(string token, JudgeCode code, object extra = null)
        {
            OnJudged?.Invoke(token, code, extra);
        }

        private bool inputToBox(object data, int mark)
        {
            if(data is IntPoint)
            {
                var p = (IntPoint)data;
                return dataBox.Record(p.X, p.Y, mark);
            }
            return false;
        }

        private void onWinnerAppend(string token)
        {
            Winner = players[token];
            if (IsStarted) End();
            raiseJudged(token, JudgeCode.NewWinner);
        }

        private void tryActive(Player player)
        {
            if (Actived != null)
            {
                Actived.IsActive = false;
                extras[Actived.Token].PauseStopwatch();
            }
            Actived = player;
            Actived.IsActive = true;
            extras[Actived.Token].EnsureStopwatch();
            raiseJudged(player.Token, JudgeCode.Actived);
        }

        private bool isNewWinnerAppend()
        {
            return false;
        }

        private class PlayerExtra : IDisposable
        {
            private System.Diagnostics.Stopwatch stopwatch;
            internal int BoxId { get; set; }
            internal bool IsReady { get; set; }
            public TimeSpan TimeSpan
            {
                get
                {
                    if (stopwatch == null) return TimeSpan.Zero;
                    return stopwatch.Elapsed;
                }
            }

            public void EnsureStopwatch()
            {
                if (stopwatch == null)
                    stopwatch = System.Diagnostics.Stopwatch.StartNew();
                stopwatch.Start();
            }
            public void PauseStopwatch()
            {
                if (stopwatch != null)
                    stopwatch.Stop();
            }
            public void StopStopwatch()
            {
                if (stopwatch != null)
                    stopwatch.Stop();
            }
            public void ResetStopwatch()
            {
                if (stopwatch != null)
                    stopwatch.Reset();
            }
            public void ClearStopwatch()
            {
                if (stopwatch != null)
                    stopwatch.Stop();
                stopwatch = null;
            }

            public void Dispose()
            {
                IsReady = false;
                ClearStopwatch();
            }
        }

    }
    public enum JudgeCode
    {
        Joined,
        Leave,
        GiveUp,
        Ready,
        MarkFirst,
        Actived,
        Accepted,
        Inputed,
        NewWinner,
        Started,
        Ended
    }
}
