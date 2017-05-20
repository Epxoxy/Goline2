using GameLogic.Data;
using System.Collections.Generic;

namespace GameLogic
{
    public class DataBox
    {
        private const int UNREACHABLE = 99;
        private const int REACHABLE = 1;

        public int ReachableCount { get; private set; }
        public bool Recordable => ReachableCount > 0;
        public bool CanUndo => undoList.Count > 0;
        public bool CanRedo => redoList.Count > 0;

        private int[,] entry;
        private int[,] fixEntry;
        private Dictionary<int, int> stepCountor;
        private Stack<DataPoint> undoList;
        private Stack<DataPoint> redoList;

        public DataBox(int[,] entry, int reachable, int denied)
        {
            fixEntry = new int[entry.GetLength(0), entry.GetLength(1)];

            for (int i = 0; i < entry.GetLength(0); i++)
                for (int j = 0; j < entry.GetLength(1); j++)
                    fixEntry[i, j] = entry[i, j] == reachable ? REACHABLE : UNREACHABLE;

            initilize();
        }


        public bool IsValid(int x, int y, int data)
        {
           return Recordable 
                && x < entry.GetLength(0) 
                && y < entry.GetLength(1)
                && entry[x, y] == REACHABLE
                && data < UNREACHABLE
                && data > REACHABLE;
        }

        //Record data if it's valid
        //Record step at the same time
        public bool Record(int x, int y, int data)
        {
            if (recordInternal(x, y, data))
            {
                undoList.Push(new DataPoint(x, y, data));
                redoList.Clear();
                return true;
            }
            return false;
        }

        //Reset a recorded point to default value
        public bool Reset(int x, int y)
        {
            if(x< entry.GetLength(0) && y < entry.GetLength(1))
            {
                int data = entry[x, y];
                if(data > REACHABLE && data < UNREACHABLE)
                {
                    --ReachableCount;
                    --stepCountor[data];
                    entry[x, y] = REACHABLE;
                    return true;
                }
            }
            return false;
        }
        

        internal bool Undo(out DataPoint dp)
        {
            dp = default(DataPoint);
            if(CanUndo)
            {
                dp = undoList.Pop();
                redoList.Push(dp);
                Reset(dp.X, dp.Y);
                return true;
            }
            return false;
        }

        internal bool Redo(out DataPoint dp)
        {
            dp = default(DataPoint);
            if (CanRedo)
            {
                dp = redoList.Pop();
                undoList.Push(dp);
                Reset(dp.X, dp.Y);
                return true;
            }
            return false;
        }

        internal bool PeekRedo(out DataPoint dp)
        {
            if (CanRedo)
            {
                dp = redoList.Peek();
                return true;
            }
            dp = default(DataPoint);
            return false;
        }

        internal bool PeekUndo(out DataPoint dp)
        {
            if (CanUndo)
            {
                dp = undoList.Peek();
                return true;
            }
            dp = default(DataPoint);
            return false;
        }

        internal void ResetData()
        {
            initilize();
        }


        private int[,] copy(int[,] src)
        {
            int[,] copy = new int[src.GetLength(0), src.GetLength(1)];
            for (int i = 0; i < src.GetLength(0); i++)
                for (int j = 0; j < src.GetLength(1); j++)
                    copy[i, j] = src[i, j];
            return copy;
        }

        private void initilize()
        {
            ReachableCount = 0;
            entry = copy(fixEntry);
            stepCountor = new Dictionary<int, int>();
            undoList = new Stack<DataPoint>();
            redoList = new Stack<DataPoint>();

            for (int i = 0; i < entry.GetLength(0); i++)
                for (int j = 0; j < entry.GetLength(0); j++)
                    if (entry[i, j] == REACHABLE)
                        ++ReachableCount;
        }

        private bool recordInternal(int x, int y, int data)
        {
            //Check if data valid
            if (IsValid(x, y, data))
            {
                entry[x, y] = data;
                --ReachableCount;

                if (!stepCountor.ContainsKey(data))
                    stepCountor.Add(data, 1);
                else
                    ++stepCountor[data];

                return true;
            }
            return false;
        }
    }
}
