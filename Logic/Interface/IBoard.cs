using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LogicUnit.Interface
{
    public delegate void LatticClickEventHandler(int x, int y);
    public interface IBoard
    {
        event LatticClickEventHandler LatticClick;
        System.Windows.Threading.Dispatcher Dispatcher { get; }
        void DrawChess(int x, int y, bool host);
        void RemoveChess(int x, int y);
        void ClearChess();
    }
}
