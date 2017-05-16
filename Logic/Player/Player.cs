using GameLogic.Data;
using System.Collections.Generic;

namespace GameLogic
{
    public class Player
    {
        public string Name { get; set; }
        public string Token { get; internal set; }
        public bool IsActive { get; internal set; }
        public bool IsAttached { get; internal set; }
        public MainLogicUnit Master { get; internal set; }
        public Stack<DataPoint> Inputs { get; internal set; }
        public Player Front { get; internal set; }
        public Player Next { get; internal set; }

        public bool Input(Data.Point data)
        {
            if (!IsAttached) return false;
            return Master.HandInput(Token, new InputAction(ActionType.Input, data));
        }

        public virtual bool IsVirtual()
        {
            return true;
        }

        protected void OnRequest()
        {

        }

        protected void OnMasterChanging(MainLogicUnit old, MainLogicUnit newOne)
        {

        }
    }
}
