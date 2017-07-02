using LogicUnit.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit.Interface
{
    public interface IMap
    {
        int Allow { get; }
        int NotAllow { get; }
        IEnumerable<Point3Line> LinesOf(IntPoint p);
        IEnumerable<Point3Line> LinesOf(int x, int y);
        IEnumerable<Point3Line> Lines();
        int[,] CurrentData();
    }
}
