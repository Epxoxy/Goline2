using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServices
{
    public class OnlineAccount
    {
        public int UserID { get; set; }
        public string NickName { get; set; }
        public string UserName { get; set; }
        public AccountState State { get; set; }
        public GameScore Score { get; set; }
    }

    public enum AccountState
    {
        None,
        Offline,
        Online
    }
}
