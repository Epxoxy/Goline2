using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic.Data
{
    [Serializable]
    public struct IntPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public IntPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return X + "," + Y;
        }
    }
}
