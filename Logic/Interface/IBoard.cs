using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit.Interface
{
    public delegate void LatticClickEventHandler(int x, int y);
    public interface IBoard
    {
        event LatticClickEventHandler LatticClick;
        void DrawChess(int x, int y, int color);
        void RemoveChess(int x, int y);
    }
}
