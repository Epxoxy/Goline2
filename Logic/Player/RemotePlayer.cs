using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public class RemotePlayer : Player
    {
        public string RemoteToken { get; set; }

        public RemotePlayer(string remoteToken)
        {
            this.RemoteToken = remoteToken;
        }
    }
}
