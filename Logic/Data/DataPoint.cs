using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Data
{
    public struct DataPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Data { get; set; }
        public DataPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
            this.Data = default(int);
        }
        public DataPoint(int x, int y, int data)
        {
            this.X = x;
            this.Y = y;
            this.Data = data;
        }
    }
}
