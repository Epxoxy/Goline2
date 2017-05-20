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
        public int[,] Entry => entry;
        public int ReachableMark => 1;
        public int DeniedMark => 0;
        private int[,] entry = new int[,]
        {
            { 1,0,0,1,0,0,1 },
            { 0,0,1,1,1,0,0 },
            { 0,1,0,1,0,1,0 },
            { 1,1,1,0,1,1,1 },
            { 0,1,0,1,0,1,0 },
            { 0,0,1,1,1,0,0 },
            { 1,0,0,1,0,0,1 }
        };

        public LogicControls(int minPlayer, int maxPlayer, int maxStep)
        {
            this.MinPlayer = minPlayer;
            this.MaxPlayer = maxPlayer;
            this.MaxStep = maxStep;

        }
    }
}
