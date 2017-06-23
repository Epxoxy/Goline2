using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService
{
    public interface IProxyCenter
    {
        void HandMessage(Message msg);
        bool AttachProxy(IProxy proxy);
        void DetachProxy(string token);
        void Enable();
    }
}
