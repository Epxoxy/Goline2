using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public class LogicControls
    {
        public int MinPlayer { get; set; }
        public int MaxPlayer { get; set; }
        public int MaxStep { get; set; }

        public LogicControls(int minPlayer, int maxPlayer, int maxStep)
        {
            this.MinPlayer = minPlayer;
            this.MaxPlayer = maxPlayer;
            this.MaxStep = maxStep;
        }
    }
}
