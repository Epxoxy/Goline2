using LogicUnit.Data;
using System.Collections.Generic;

namespace LogicUnit
{
    public abstract class Player
    {
        public int Id { get; internal set; }
        public string Name { get; set; }
        public string Token { get; internal set; }
        public bool IsActive { get; internal set; }
        public bool IsAttached { get; internal set; }
        public JudgeUnit Judge { get; internal set; }
        public Stack<DataPoint> Inputs { get; internal set; }
        public Player Front { get; internal set; }
        public Player Next { get; internal set; }

        public abstract bool IsVirtual();

        protected void OnRequest()
        {

        }

        protected void OnJudgeChanging(JudgeUnit old, JudgeUnit newOne)
        {

        }
    }
}
