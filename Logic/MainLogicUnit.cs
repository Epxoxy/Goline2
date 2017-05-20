using GameLogic.Data;
using GameLogic.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameLogic
{
    public delegate void AcceptedEventHandler(string token, InputAction action);
    public delegate void PlayerEventHandler(string token);
    public delegate void StateChangedEventHandler();

    public class MainLogicUnit
    {
        public event AcceptedEventHandler Accepted;
        public event PlayerEventHandler ActivedChanged;
        public event PlayerEventHandler FirstChanged;
        public event PlayerEventHandler PlayerJoined;
        public event PlayerEventHandler PlayerLeave;
        public event PlayerEventHandler WinnerAppend;
        public event StateChangedEventHandler Started;
        public event StateChangedEventHandler Ended;

        public TimeSpan Elapsed { get; private set; }

        public Player First { get; private set; }
        public Player Actived { get; private set; }
        public Player Winner { get; private set; }
        public Player Next => Actived == null ? null : Actived.Next;
        public Player Front => Actived == null ? null : Actived.Front;

        public bool IsStarted { get; private set; }
        public bool IsAttached { get; private set; }

        private Dictionary<string, Player> players { get; set; }
        private Dictionary<string, PlayerData> watches { get; set; }
        private LogicControls logics;
        private DataBox dataBox;
        private object lockPlayers = new object();

        public MainLogicUnit(LogicControls lc)
        {
            this.logics = lc;
            dataBox = new DataBox(lc.Entry, lc.ReachableMark, lc.DeniedMark);
        }

        //When gameMaster is attach,you can join a valid player
        //before game start
        public bool Join(Player player)
        {
            if (player == null || !IsAttached || IsStarted)
                return false;
            lock (lockPlayers)
            {
                if (players.ContainsValue(player)) return true;
                if (players.Count >= logics.MaxPlayer) return false;
                string token = generateToken();
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
                watches.Add(token, new PlayerData()
                {
                    BoxId = players.Count + 1
                });
                player.Master = this;
                player.IsAttached = true;
                System.Diagnostics.Debug.WriteLine($"Player[{player.Name}] Joined. Token[{token}]");
                PlayerJoined?.Invoke(token);

                return true;
            }
        }

        public bool Leave(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsAttached || IsStarted || players.Count < 1)
                return false;
            lock (lockPlayers)
            {
                if (players.ContainsKey(token))
                {
                    var player = players[token];
                    players.Remove(token);
                    watches.Remove(token);
                    player.Master = null;

                    var front = player.Front;
                    var next = player.Next;
                    front.Next = next;

                    System.Diagnostics.Debug.WriteLine($"Player[{player.Name}] Leave.");
                    PlayerLeave?.Invoke(token);
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
            lock (lockPlayers)
            {
                if (players.ContainsKey(token))
                {
                    var player = players[token];
                    if (player.IsActive)
                    {
                        var watch = watches[token];
                        watches.Remove(token);
                        watch.Dispose();
                    }
                    players.Remove(token);
                    player.Master = null;

                    var front = player.Front;
                    var next = player.Next;
                    front.Next = next;

                    if (IsStarted && players.Count == 1)
                        onWinnerAppend(front.Token);
                    else
                        tryActive(next);

                    System.Diagnostics.Debug.WriteLine($"Player[{player.Name}] Leave.");
                    PlayerLeave?.Invoke(token);
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
            FirstChanged?.Invoke(token);
            return true;
        }

        public void Attach()
        {
            IsAttached = true;
            players = new Dictionary<string, Player>();
            watches = new Dictionary<string, PlayerData>();
        }

        public void Detach()
        {
            players.Clear();
            foreach(var item in watches.Values)
                item.Dispose();
            watches.Clear();
            players = null;
            watches = null;
        }

        public bool Start()
        {
            if(IsAttached && !IsStarted)
            {
                IsStarted = true;
                Started?.Invoke();
                tryActive(First);
                return true;
            }
            return false;
        }

        public void End()
        {
            if (IsStarted)
            {
                IsStarted = false;
                Ended?.Invoke();
            }
        }

        public bool HandInput(string token, InputAction action)
        {
            bool accepted = false;
            if (!string.IsNullOrEmpty(token) && players.ContainsKey(token))
            {
                switch (action.Type)
                {
                    case ActionType.Leave:
                        accepted = Leave(token);
                        break;
                    case ActionType.GiveUp:
                        accepted = GiveUp(token);
                        break;
                    case ActionType.Input:
                        accepted = handDataInput(action.Data, watches[token].BoxId);
                        break;
                    case ActionType.Undo:
                        DataPoint p;
                        accepted = dataBox.Undo(out p);
                        break;
                    case ActionType.Redo:
                        DataPoint rp;
                        accepted = dataBox.Redo(out rp);
                        break;
                }
            }
            if (accepted)
                Accepted?.Invoke(token, action);
            return accepted;
        }


        private string generateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        }

        private bool handDataInput(object data, int mark)
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
            WinnerAppend?.Invoke(token);
        }

        private void tryActive(Player player)
        {
            if (Actived != null)
            {
                Actived.IsActive = false;
                watches[Actived.Token].PauseStopwatch();
            }
            Actived = player;
            Actived.IsActive = true;
            watches[Actived.Token].EnsureStopwatch();
            ActivedChanged?.Invoke(player.Token);
        }

        private bool isNewWinnerAppend()
        {
            return false;
        }

        private class PlayerData : IDisposable
        {
            private System.Diagnostics.Stopwatch stopwatch;
            public TimeSpan TimeSpan
            {
                get
                {
                    if (stopwatch == null) return TimeSpan.Zero;
                    return stopwatch.Elapsed;
                }
            }
            public int BoxId { get; set; }

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
                ClearStopwatch();
            }
        }
    }
}
