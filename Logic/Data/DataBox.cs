using LogicUnit.Data;
using System;
using System.Collections.Generic;

namespace LogicUnit
{
    public class DataBox
    {
        public int ReachableCount { get; private set; }
        public bool Recordable => ReachableCount > 0;
        public bool CanUndo => undoList.Count > 0;
        public bool CanRedo => redoList.Count > 0;

        private int no = 99;
        private int ok = 1;
        private int[,] datas;
        private int[,] fixEntry;
        private Dictionary<int, int> stepCountor;
        private Stack<DataPoint> undoList;
        private Stack<DataPoint> redoList;

        public DataBox(int[,] entry, int reachable, int denied)
        {
            fixEntry = new int[entry.GetLength(0), entry.GetLength(1)];

            ok = reachable;
            no = denied;
            for (int i = 0; i < entry.GetLength(0); i++)
                for (int j = 0; j < entry.GetLength(1); j++)
                    fixEntry[i, j] = entry[i, j];

            initilize();
        }

        //For check if a point(x, y) can place
        //by the point(x, y) is reachable
        //In 2d array, different to screen coordinate
        //the screen's x coordinate means the 1d of array
        //then screen's y coordinate means then 0d of array
        //so when we check it, notice the different just for the 2d array.
        public bool IsValid(int x, int y, int data)
        {
           return Recordable
                && x < datas.GetLength(0) 
                && y < datas.GetLength(1)
                && datas[x, y] == ok
                && data != no
                && data > ok;
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
            if(x< datas.GetLength(0) && y < datas.GetLength(1))
            {
                int data = datas[x, y];
                if(data > ok && data != no)
                {
                    ++ReachableCount;
                    --stepCountor[data];
                    datas[x, y] = ok;
                    return true;
                }
            }
            return false;
        }
        
        public bool IsDataIn(int x, int y, int value)
        {
            int data = datas[x, y];
            return data == value;
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
            throw new NotImplementedException("Developing function");

            dp = default(DataPoint);
            if (CanRedo)
            {
                dp = redoList.Pop();
                undoList.Push(dp);
                Record(dp.X, dp.Y, dp.Data);
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
        
        internal int[,] Copy()
        {
            return ArrayHelper.CopyMatrix(datas);
        }

        private bool recordInternal(int x, int y, int data)
        {
            //Check if data valid
            if (IsValid(x, y, data))
            {
                datas[x, y] = data;
                --ReachableCount;

                if (!stepCountor.ContainsKey(data))
                    stepCountor.Add(data, 1);
                else
                    ++stepCountor[data];

                return true;
            }
            return false;
        }

        private void initilize()
        {
            ReachableCount = 0;
            datas = ArrayHelper.CopyMatrix(fixEntry);
            stepCountor = new Dictionary<int, int>();
            undoList = new Stack<DataPoint>();
            redoList = new Stack<DataPoint>();

            for (int i = 0; i < datas.GetLength(0); i++)
                for (int j = 0; j < datas.GetLength(1); j++)
                    if (datas[i, j] == ok)
                        ++ReachableCount;
        }

    }
}
