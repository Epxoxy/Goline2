using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class OnlinePlayer : Player
    {
        public string RemoteToken { get; set; }

        public OnlinePlayer(string remoteToken)
        {
            this.RemoteToken = remoteToken;
        }
    }
}
