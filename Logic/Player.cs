using Logic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class Player
    {
        public string Name { get; set; }
        public string Token { get; internal set; }
        public bool IsActive { get; internal set; }
        public GameMaster Master { get; internal set; }
        public Stack<DataPoint> Inputs { get; internal set; }
        public Player Front { get; internal set; }
        public Player Next { get; internal set; }

        public bool Input(DataPoint data)
        {
            return false;
        }

        public bool Register(GameMaster master)
        {
            return false;
        }

        protected void OnRequest()
        {

        }

        protected void OnMasterChanging(GameMaster old, GameMaster newOne)
        {

        }
    }
}
