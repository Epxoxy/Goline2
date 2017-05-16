using GameLogic.Data;
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
        public event StateChangedEventHandler Started;
        public event StateChangedEventHandler Ended;

        public TimeSpan Elapsed { get; private set; }

        public Player First { get; private set; }
        public Player Actived { get; private set; }
        public Player Next { get; private set; }
        public Player Front { get; private set; }
        public Player Winner { get; private set; }

        public bool IsStarted { get; private set; }
        public bool IsAttached { get; private set; }

        private Dictionary<string, Player> players { get; set; }
        private Dictionary<string, PlayerData> data { get; set; }
        private LogicControls logics;
        private object lockPlayers = new object();

        public MainLogicUnit(LogicControls logics)
        {
            this.logics = logics;
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
            if (players.ContainsKey(token))
            {
                var player = players[token];
                players.Remove(token);

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
            data = new Dictionary<string, PlayerData>();
        }

        public void Detach()
        {
            players.Clear();
            foreach(var item in data.Values)
                item.Dispose();
            data.Clear();
            players = null;
            data = null;
        }

        public bool Start()
        {
            if(IsAttached && !IsStarted)
            {
                IsStarted = true;
                Started?.Invoke();
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
            if(action.Type == ActionType.Leave)
            {

            }else if(action.Type == ActionType.GiveUp)
            {

            }else
            {
                if (!string.IsNullOrEmpty(token) && players.ContainsKey(token))
                {
                    switch (action.Type)
                    {
                        case ActionType.Input:
                        case ActionType.Undo:
                        case ActionType.Redo:
                            break;
                    }
                }
            }
            if(accepted)
                Accepted?.Invoke(token, action);
            return accepted;
        }
        
        private string generateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        }

        private void tryActivedNext()
        {
            Actived = Actived.Next;
            ActivedChanged?.Invoke(Actived.Token);
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
