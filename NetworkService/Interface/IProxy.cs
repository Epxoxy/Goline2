using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService
{
    public interface IProxy : IDisposable
    {
        string Token { get; set; }
        bool Relay(Message msg);
        void Enable(IProxyCenter center);
    }

}
