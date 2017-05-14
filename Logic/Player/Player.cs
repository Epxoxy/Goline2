using Logic.Data;
using System.Collections.Generic;

namespace Logic
{
    public class Player
    {
        public string Name { get; set; }
        public string Token { get; internal set; }
        public bool IsActive { get; internal set; }
        public bool IsAttached { get; internal set; }
        public GameMaster Master { get; internal set; }
        public Stack<DataPoint> Inputs { get; internal set; }
        public Player Front { get; internal set; }
        public Player Next { get; internal set; }

        public bool Input(DataPoint data)
        {
            if (!IsAttached) return false;
            return Master.HandInput(Token, new InputAction(ActionType.New, data));
        }

        protected void OnRequest()
        {

        }

        protected void OnMasterChanging(GameMaster old, GameMaster newOne)
        {

        }
    }
}
