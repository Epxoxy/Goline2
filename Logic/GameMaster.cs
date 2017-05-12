using Logic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class GameMaster
    {
        public GameMode Mode { get; private set; }
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

        public GameMaster(LogicControls logics)
        {
            this.logics = logics;
        }

        //When gameMaster is attach,you can join a valid player
        //before game start
        public bool Join(Player player)
        {
            if (player == null || !IsAttached || IsStarted )
                return false;

            if (players.ContainsValue(player)) return true;
            if (players.Count >= logics.PlayerLimits) return false;
            string token = GenerateToken();
            player.Token = token;

            if(players.Count > 0)
            {
                var first = players.First().Value;
                var last = players.Last().Value;
                last.Next = player;
                player.Front = last;
                player.Next = first;
            }
            player.Register(this);
            players.Add(token, player);
            System.Diagnostics.Debug.WriteLine($"Player[{player.Name}] Joined. Token[{token}]");

            return true;
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
                return true;
            }else
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
            return false;
        }

        public void End()
        {
            
        }

        public bool HandInput(string token, InputAction action)
        {
            if (string.IsNullOrEmpty(token) || !players.ContainsKey(token))
                return false;
            return false;
        }

        private void FillAI(int num, AILevel level)
        {
            for (int i = 0; i < num; i++)
            {
                var player = MakeVirtualPlayer(level);
                this.Join(player);
            }
        }

        private string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        }

        private Player MakeVirtualPlayer(AILevel level)
        {
            return null;
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
