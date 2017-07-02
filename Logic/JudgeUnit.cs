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

        public string FirstToken => First == null ? null : First.Token;
        public string ActiveToken => Active == null ? null : Active.Token;
        public string WinnerToken => Winner == null ? null : Winner.Token;

        private DataExtra First { get; set; }
        private DataExtra Active { get; set; }
        private DataExtra Winner { get; set; }

        public bool IsStarted { get; private set; }
        public bool IsAttached { get; private set; }

        private Dictionary<string, DataExtra> extras { get; set; }
        private MapFormation map;
        private DataBox dataBox => map.DataBox;
        private object lockHelper = new object();
        private object judgeLockHelper = new object();

        public JudgeUnit(MapFormation map)
        {
            this.map = map;
        }

        public IMap GetMap()
        {
            return map;
        }

        //When gameMaster is attach,you can join a valid player
        //before game start
        public bool Join(ref string rsToken)
        {
            if (!IsAttached || IsStarted)
                return false;
            lock (lockHelper)
            {
                if (extras.Count >= map.MaxPlayer) return false;
                string token = TokenHelper.NewToken();

                var current = new DataExtra
                {
                    Token = token,
                    BoxId = extras.Count + 2,
                    Watcher = new Watcher()
                };
                rsToken = token;
                if (extras.Count > 0)
                {
                    var first = extras.First().Value;
                    var last = extras.Last().Value;
                    last.Next = current;
                    current.Front = last;
                    current.Next = first;
                }
                extras.Add(token, current);
                System.Diagnostics.Debug.WriteLine($"New Joined. Token[{token}]");
                raiseJudged(token, JudgeCode.Joined);
                if (extras.Count == 1)
                    SignAsFirst(current.Token);
                return true;
            }
        }

        public bool Leave(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsAttached || IsStarted || extras.Count < 1)
                return false;
            lock (lockHelper)
            {
                if (extras.ContainsKey(token))
                {
                    var extra = extras[token];
                    extra.Watcher.Dispose();
                    extras.Remove(token);

                    var front = extra.Front;
                    var next = extra.Next;
                    front.Next = next;

                    System.Diagnostics.Debug.WriteLine($"token[{extra.Token}] Leave.");
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
            if (string.IsNullOrEmpty(token) || !IsStarted || extras.Count < 1) return false;
            lock (lockHelper)
            {
                if (extras.ContainsKey(token))
                {
                    var extra = extras[token];
                    extra.Watcher.Dispose();
                    extras.Remove(token);

                    var front = extra.Front;
                    var next = extra.Next;
                    front.Next = next;

                    if (IsStarted && extras.Count == 1)
                        onWinnerAppend(front.Token);
                    else
                        tryActive(next);

                    System.Diagnostics.Debug.WriteLine($"token[{token}] Leave.");
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
                || !extras.ContainsKey(token))
                return false;
            First = extras[token];
            raiseJudged(token, JudgeCode.MarkFirst);
            return true;
        }

        public void Attach()
        {
            IsAttached = true;
            extras = new Dictionary<string, DataExtra>();
        }

        public void Detach()
        {
            foreach (var pair in extras.Values)
                pair.Watcher.Dispose();
            extras.Clear();
            extras = null;
        }
        
        public bool Ready(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsAttached || IsStarted || extras.Count < 1)
                return false;
            if (extras.ContainsKey(token))
            {
                if (!extras[token].IsReady)
                {
                    extras[token].IsReady = true;
                    raiseJudged(token, JudgeCode.Ready);
                    if (extras.Count >= map.MinPlayer 
                        && extras.Count(ex => ex.Value.IsReady) == extras.Count)
                        Start();
                }
                return true;
            }
            return false;
        }

        public bool Start()
        {
            this.Winner = null;
            if(IsAttached && !IsStarted && extras.Count >= map.MinPlayer)
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
                foreach (var pair in extras.Values)
                    pair.Watcher.Dispose();
                raiseJudged(string.Empty, JudgeCode.Ended);
            }
        }

        public void Reset()
        {
            dataBox.ResetData();
            foreach (var pair in extras.Values)
            {
                pair.Watcher.StopStopwatch();
                pair.Watcher.ResetStopwatch();
            }
            raiseJudged(string.Empty, JudgeCode.Reset);
            Start();
        }

        public bool HandInput(string token, InputAction action)
        {
            bool isOk = false;
            lock (lockHelper)
            {
                if (!string.IsNullOrEmpty(token) && extras.ContainsKey(token))
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
                            isOk = recordInput(token, action);
                            break;
                        case ActionType.Undo:
                            if (token != Active.Token) break;
                            isOk = undoRecord(token, action);
                            break;
                        case ActionType.AllowUndo:
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
        
        private bool isEnded()
        {
            return dataBox.ReachableCount == 0;
        }

        private void raiseJudged(string token, JudgeCode code, object extra = null)
        {
            lock (judgeLockHelper)
            {
                OnJudged?.Invoke(token, code, extra);
            }
        }

        private bool undoRecord(string token, InputAction action)
        {
            int boxId = GetBoxId(token);
            if (boxId > 0)
            {
                DataPoint p;
                bool undo = false;
                do
                {
                    undo = dataBox.Undo(out p);
                    if (undo)
                    {
                        action.Data = new IntPoint(p.X, p.Y);
                        raiseJudged(token, JudgeCode.Undo, action);
                    }
                } while (undo && p.Data != boxId);
                if(undo && p.Data == boxId)
                    return true;
            }
            return false;
        }

        private bool undoRecord(string token, string undoToken, InputAction action)
        {
            bool isOk = false;
            int boxId = GetBoxId(token);
            if (boxId > 0)
            {
                DataPoint p;
                bool undo = false;
                do
                {
                    undo = dataBox.Undo(out p);
                    if (undo)
                    {
                        action.Data = new IntPoint(p.X, p.Y);
                        raiseJudged(token, JudgeCode.Undo, action);
                    }
                } while (undo && p.Data != boxId);
                tryActive(extras[token]);
                isOk = undo && p.Data == boxId;
            }
            return isOk;
        }

        private bool recordInput(string token, InputAction action)
        {
            bool isOk = false;
            if (IsStarted && Active.Token == token)
            {
                isOk = recordToBox(token, action.Data, extras[token].BoxId);
                if (isOk)
                {
                    raiseJudged(token, JudgeCode.Input, action);
                    if (isNewWinnerAppend())
                    {
                        raiseJudged(token, JudgeCode.NewWinner);
                        End();
                    }
                    else
                    {
                        if (isEnded()) End();
                        else tryActive(extras[token].Next);
                    }
                }
            }
            return isOk;
        }

        private bool recordToBox(string token, object data, int mark)
        {
            if(data is IntPoint)
            {
                var p = (IntPoint)data;
                bool isOk = dataBox.Record(p.X, p.Y, mark);
                if (isOk)
                {
                    var lines = GetMap().LinesOf(p);
                    foreach(var line in lines)
                    {
                        var points = line.ToPoints();
                        int count = 0;
                        foreach (var point in points)
                        {
                            if (dataBox.IsDataIn(point.X, point.Y, mark))
                                ++count;
                        }
                        if(count == 3)
                        {
                            this.Winner = extras[token];
                            break;
                        }
                    }
                }
                return isOk;
            }
            return false;
        }

        private void onWinnerAppend(string token)
        {
            Winner = extras[token];
            if (IsStarted) End();
            raiseJudged(token, JudgeCode.NewWinner);
        }

        private void tryActive(DataExtra extra)
        {
            if (Active != null)
            {
                Active.IsActive = false;
                extras[Active.Token].Watcher.PauseStopwatch();
            }
            Active = extra;
            Active.IsActive = true;
            extras[Active.Token].Watcher.EnsureStopwatch();
            raiseJudged(extra.Token, JudgeCode.Active);
        }

        private bool isNewWinnerAppend()
        {
            return Winner != null;
        }
        
        private class DataExtra
        {
            public int BoxId { get; set; }
            public string Token { get; set; }
            public bool IsActive { get; set; }
            public bool IsReady { get; set; }
            public DataExtra Next { get; set; }
            public DataExtra Front { get; set; }
            public Watcher Watcher { get; set; }
        }

        private class Watcher : IDisposable
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

    public class JudgeResult
    {
        public JudgeCode Code { get; set; }
        public object Extra { get; set; }
    }

    public enum JudgeCode
    {
        Joined,
        Leave,
        GiveUp,
        Ready,
        MarkFirst,
        Active,
        Undo,
        Input,
        NewWinner,
        Started,
        Ended,
        Reset
    }
}
