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
        void Relay(Message msg);
        void Enable(IBridge center);
    }

}
