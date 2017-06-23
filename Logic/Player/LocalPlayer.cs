using LogicUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit
{
    public class LocalPlayer : Player
    {
        public override bool IsVirtual()
        {
            return false;
        }
    }
}
