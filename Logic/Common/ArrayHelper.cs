using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit
{
    internal class ArrayHelper
    {
        public static int[,] CopyMatrix(int[,] sourceMatrix)
        {
            int[,] target = new int[sourceMatrix.GetLength(0), sourceMatrix.GetLength(1)];
            for (int i = 0; i < sourceMatrix.GetLength(0); ++i)
                for (int j = 0; j < sourceMatrix.GetLength(1); ++j)
                    target[i, j] = sourceMatrix[i, j];
            return target;
        }
        public static int[,] CopyMatrix(ref int[,] sourceMatrix)
        {
            int[,] target = new int[sourceMatrix.GetLength(0), sourceMatrix.GetLength(1)];
            for (int i = 0; i < sourceMatrix.GetLength(0); ++i)
                for (int j = 0; j < sourceMatrix.GetLength(1); ++j)
                    target[i, j] = sourceMatrix[i, j];
            return target;
        }
        public static void CopyMatrix(ref int[,] sourceMatrix, out int[,] target)
        {
            target = new int[sourceMatrix.GetLength(0), sourceMatrix.GetLength(1)];
            for (int i = 0; i < sourceMatrix.GetLength(0); ++i)
                for (int j = 0; j < sourceMatrix.GetLength(1); ++j)
                    target[i, j] = sourceMatrix[i, j];
        }
    }
}
