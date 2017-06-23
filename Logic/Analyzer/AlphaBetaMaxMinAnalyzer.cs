using System;
using System.Collections.Generic;
using LogicUnit.Data;

namespace LogicUnit.Analyzer
{
    /// <summary>
    /// Alpha-beta cut max-min search algorinthm analyzer
    /// </summary>
    public class AlphaBetaMaxMinAnalyzer : Interface.IAnalyzer<int[,], IntPoint>
    {
        public IntPoint Analysis(int[,] data, int deep)
        {
            return FindMaxMin(data, deep);
        }

        public AlphaBetaMaxMinAnalyzer(Interface.IMap map, int env, int human)
        {
            this.map = map;
            this.env = env;
            this.human = human;
            notAllow = map.NotAllow;
            allow = map.Allow;
        }

        private Interface.IMap map;
        int minValue = -GlobalLevel.SSS * 4;
        int maxValue = GlobalLevel.SSS * 4;
        int notAllow = 0;
        int allow = 1;
        int human;
        int env;

        public IntPoint FindMaxMin(int[,] board, int deep)
        {
            var best = minValue;
            var bestPoints = new List<IntPoint>();
            var points = generateAvaliable(ref board, deep);
            //Init scores
            var scores = new int[board.GetLength(0), board.GetLength(1)];
            fullEvaluate(ref board, ref scores);
            //Start maxValueminValue
            for (int i = 0; i < points.Count; ++i)
            {
                var point = points[i];
                board[point.X, point.Y] = env;
                int[,] scoresCopy = ArrayHelper.CopyMatrix(ref scores);
                evaluatePoint(ref board, ref scoresCopy, point.X, point.Y);
                var v = min(ref board, ref scoresCopy, deep - 1, (best > minValue ? best : minValue), maxValue);
                if (v == best) bestPoints.Add(point);
                if (v > best)
                {
                    best = v;
                    bestPoints.Clear();
                    bestPoints.Add(point);
                }
                board[point.X, point.Y] = allow;
            }
            //If best is not found, get best from scores
            if (bestPoints.Count < 1)
            {
                for (int i = 0; i < scores.GetLength(0); ++i)
                {
                    for (int j = 0; j < scores.GetLength(0); ++j)
                    {
                        if (board[i, j] == allow)
                        {
                            if (scores[i, j] == best) bestPoints.Add(new IntPoint(i, j));
                            if (scores[i, j] > best)
                            {
                                best = scores[i, j];
                                bestPoints.Clear();
                                bestPoints.Add(new IntPoint(i, j));
                            }
                        }
                    }
                }
            }
            if (bestPoints.Count == 0)
                return default(IntPoint);
            int index = (new Random()).Next(0, bestPoints.Count);
            return bestPoints[index];
        }

        private int max(ref int[,] board, ref int[,] scores, int deep, int alpha, int beta)
        {
            var v0 = calculateTotalScores(ref scores);
            if (deep <= 0 || ended(board)) return v0;
            var best = minValue;
            var locations = generateAvaliable(ref board, deep);
            for (int i = 0; i < locations.Count; ++i)
            {
                var loc = locations[i];
                board[loc.X, loc.Y] = env;
                int[,] scoresCopy = ArrayHelper.CopyMatrix(ref scores);
                evaluatePoint(ref board, ref scoresCopy, loc.X, loc.Y);
                var v = min(ref board, ref scoresCopy, deep - 1, alpha, (best > beta ? best : beta));
                board[loc.X, loc.Y] = allow;
                if (v > best) best = v;
                if (v > alpha) break;//ABcut++;
            }
            return best;
        }

        private int min(ref int[,] board, ref int[,] scores, int deep, int alpha, int beta)
        {
            var v0 = calculateTotalScores(ref scores);
            if (deep <= 0 || ended(board)) return v0;
            var best = maxValue;
            var locations = generateAvaliable(ref board, deep);
            for (int i = 0; i < locations.Count; ++i)
            {
                var loc = locations[i];
                board[loc.X, loc.Y] = human;

                //Every time to copy scores,
                //so that evaluate next location can evaluate with origin score.
                //Copy scores and Evaluate location
                int[,] scoresCopy = ArrayHelper.CopyMatrix(ref scores);
                evaluatePoint(ref board, ref scoresCopy, loc.X, loc.Y);
                var v = max(ref board, ref scoresCopy, deep - 1, (best < alpha ? best : alpha), beta);
                board[loc.X, loc.Y] = allow;
                if (v < best) best = v;
                if (v < beta) break;//Abcut++;
            }
            return best;
        }

        /// <summary>
        /// Calculate total score of exist scores matrix
        /// </summary>
        /// <param name="scores"></param>
        /// <returns></returns>
        private int calculateTotalScores(ref int[,] scores)
        {
            int value = 0;
            for (int i = 0; i < scores.GetLength(0); ++i)
            {
                for (int j = 0; j < scores.GetLength(1); ++j)
                {
                    value += scores[i, j];
                }
            }
            return value;
        }

        /// <summary>
        /// Full evaluate board
        /// </summary>
        /// <param name="board"></param>
        /// <param name="scores"></param>
        private void fullEvaluate(ref int[,] board, ref int[,] scores)
        {
            for (int i = 0; i < board.GetLength(0); ++i)
            {
                for (int j = 0; j < board.GetLength(0); ++j)
                {
                    if (board[i, j] != allow && board[i, j] != notAllow)
                    {
                        int lineCount;
                        evaluatePoint(ref board, ref scores, out lineCount, i, j);
                        scores[i, j] += lineCount;
                    }
                }
            }
        }

        /// <summary>
        /// Evaluate special location
        /// </summary>
        /// <param name="board">Origin board</param>
        /// <param name="scores">Current scores</param>
        /// <param name="lineCount">Line's count</param>
        /// <param name="x">Location's x</param>
        /// <param name="y">Location's y</param>
        private void evaluatePoint(ref int[,] board, ref int[,] scores, out int lineCount, int x, int y)
        {
            scores[x, y] = evaluatePointScore(ref board, out lineCount, x, y);
        }

        /// <summary>
        /// Evaluate special location
        /// </summary>
        /// <param name="board">Origin board</param>
        /// <param name="scores">Current scores</param>
        /// <param name="x">Location's x</param>
        /// <param name="y">Location's y</param>
        private void evaluatePoint(ref int[,] board, ref int[,] scores, int x, int y)
        {
            int lineCount;
            scores[x, y] = evaluatePointScore(ref board, out lineCount, x, y);
        }

        /// <summary>
        /// Evaluate location's score
        /// </summary>
        /// <param name="board">Origin board</param>
        /// <param name="lineCount"></param>
        /// <param name="x">Location's x</param>
        /// <param name="y">Location's y</param>
        /// <returns></returns>
        private int evaluatePointScore(ref int[,] board, out int lineCount, int x, int y)
        {
            lineCount = 0;
            var lines = map.LinesOf(x, y);
            int totalScore = 0, sseCount = 0, ooeCount = 0;
            foreach (var line in lines)
            {
                ++lineCount;

                int reachable = 0, selfOccupy = 0, otherOccupy = 0;
                int score = 0;

                var points = line.ToPoints();
                foreach (var point in points)
                {
                    if (board[point.X, point.Y] == allow) ++reachable;
                    else
                    {
                        if (board[point.X, point.Y] == env) ++selfOccupy;
                        else ++otherOccupy;
                    }
                }

                if (reachable == 3) score = GlobalLevel.EEE;
                if (reachable == 0)
                {
                    if (selfOccupy == 0) score = GlobalLevel.OOO;
                    //May be it has error, method shouldn't go here
                    return GlobalLevel.SSS;
                }

                switch (selfOccupy)
                {
                    case 0:
                        if (otherOccupy == 2) ++ooeCount;
                        if (otherOccupy == 1) score = GlobalLevel.OEE;
                        break;
                    case 1:
                        if (otherOccupy == 1) score = GlobalLevel.SOE;
                        if (otherOccupy == 0) score = GlobalLevel.SEE;
                        break;
                    case 2:
                        ++sseCount;
                        break;
                    default: break;
                }
                totalScore += score;
            }

            if (sseCount > 1) totalScore += GlobalLevel.DoubleSSE * (sseCount * (sseCount + 1) / 2);
            else totalScore += GlobalLevel.SSE;

            if (ooeCount > 1) totalScore += GlobalLevel.DoubleOOE * (ooeCount * (ooeCount + 1) / 2);
            else totalScore += GlobalLevel.OOE;

            return totalScore;
        }

        /// <summary>
        /// Check if board is end.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        private bool ended(int[,] board)
        {
            int avaliable = 0;
            foreach (var line in map.Lines())
            {
                var points = line.ToPoints();
                int humanOccupy = 0, aiOccupy = 0;
                foreach (var point in points)
                {
                    if (board[point.X, point.Y] == human) ++humanOccupy;
                    else if (board[point.X, point.Y] == env) ++aiOccupy;
                    else if (board[point.X, point.Y] == avaliable) ++avaliable;
                }
                if (humanOccupy == 3 || aiOccupy == 3) return true;
            }
            if (avaliable == 0) return true;
            return false;
        }

        /// <summary>
        /// Check if board will append winner
        /// </summary>
        /// <param name="board"></param>
        /// <param name="location"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool hasNewWinner(ref int[,] board, IntPoint newest, int id)
        {
            var lines = map.LinesOf(newest);
            foreach (var line in lines)
            {
                int count = 0;
                var points = line.ToPoints();
                foreach (var point in points)
                {
                    if (board[point.X, point.Y] == id) ++count;
                }
                if (count == 3) return true;
            }
            return false;
        }

        /// <summary>
        /// Generate avaliable location for current board
        /// </summary>
        /// <param name="board"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        private IList<IntPoint> generateAvaliable(ref int[,] board, int deep)
        {
            var three = new List<IntPoint>();
            var doubleTwo = new List<IntPoint>();
            var twos = new List<IntPoint>();
            var remainAvaliables = new List<IntPoint>();
            for (int i = 0; i < board.GetLength(0); ++i)
            {
                for (int j = 0; j < board.GetLength(1); ++j)
                {
                    if (board[i, j] == allow)
                    {
                        var scoreCom = evaluateIfInput(ref board, i, j, env);
                        if (scoreCom == 0)
                        {
                            //If current score equal to zero
                            //Means current location don't have chess around it.
                            //Skip it.
                            continue;
                        }
                        var scoreHum = evaluateIfInput(ref board, i, j, human);
                        var p = new IntPoint(i, j);
                        if (scoreCom >= OneSidedLevel.SSS)
                            return new IntPoint[] { p };
                        else if (scoreHum >= OneSidedLevel.SSS)
                            three.Add(p);
                        else if (scoreCom >= 2 * OneSidedLevel.SSE)
                            doubleTwo.Insert(0, p);
                        else if (scoreHum >= 2 * OneSidedLevel.SSE)
                            doubleTwo.Add(p);
                        else if (scoreCom >= OneSidedLevel.SSE)
                            twos.Insert(0, p);
                        else if (scoreHum >= OneSidedLevel.SSE)
                            twos.Add(p);
                        else
                            remainAvaliables.Add(p);
                    }
                }
            }
            if (three.Count > 0) return three;
            if (doubleTwo.Count > 0) return doubleTwo;
            if (twos.Count > 0) return twos;
            return remainAvaliables;
        }

        /// <summary>
        /// Evaluate location's score after input there
        /// </summary>
        /// <param name="board"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private int evaluateIfInput(ref int[,] board, int x, int y, int id)
        {
            int score = 0;
            var lines = map.LinesOf(x, y);
            foreach (var line in lines)
            {
                var points = line.ToPoints();
                int reachable = 0, selfOccupy = 0, otherOccupy = 0;
                foreach (var point in points)
                {
                    int value = board[point.X, point.Y];
                    if (value == allow) ++reachable;
                    else
                    {
                        if (value == id) ++selfOccupy;
                        else ++otherOccupy;
                    }
                }
                //self-self-empty to self-self-self
                if (selfOccupy == 2 && reachable == 1) score += OneSidedLevel.SSS;
                //self-empty-empty to self-self-empty
                else if (selfOccupy == 1 && reachable == 2) score += OneSidedLevel.SSE;
                //empty-empty-empty to self-empty-empty
                else if (reachable != 3) score += OneSidedLevel.SEE;
            }
            return score;
        }

        class GlobalLevel
        {
            public const int SSS = 12000;
            public const int DoubleSSE = 2500;
            public const int SSE = 500;
            public const int SEE = 100;
            public const int EEE = 0;
            public const int SOE = 0;
            public const int OEE = -100;
            public const int OOE = -500;
            public const int DoubleOOE = -2500;
            public const int OOO = -12000;
        }

        class OneSidedLevel
        {
            public const int SSS = 10000;
            public const int SSE = 100;
            public const int SEE = 1;
        }
    }
}
