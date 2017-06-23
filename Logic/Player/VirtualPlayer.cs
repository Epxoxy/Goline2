using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit
{
    public class VirtualPlayer : Player
    {
        public string OriginToken { get; set; }

        public VirtualPlayer(string originToken)
        {
            this.OriginToken = originToken;
        }

        public override bool IsVirtual()
        {
            return true;
        }
    }
}
