using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit.Data
{
    [Serializable]
    public class InputAction
    {
        public ActionType Type { get; set; }
        public object Data { get; set; }

        public InputAction(ActionType type, object data)
        {
            this.Type = type;
            this.Data = data;
        }

        public override string ToString()
        {
            return string.Format("[{0}]:{1}", Type, Data);
        }
    }
}
