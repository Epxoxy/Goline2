using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Data
{
    public class InputAction
    {
        public ActionType Type { get; set; }
        public object Data { get; set; }

        public InputAction(ActionType type, object data)
        {
            this.Type = type;
            this.Data = data;
        }
    }
}
