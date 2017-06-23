using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService
{
    public abstract class ProxyBase : IProxy
    {
        public string Token { get; set; }
        private IProxyCenter center;

        //Enable proxy
        public void Enable(IProxyCenter center)
        {
            this.center = center;
            OnEnable();
        }

        //Do something on enable proxy
        protected virtual void OnEnable()
        {

        }

        //Relay message from message center
        public abstract bool Relay(Message msg);

        //Relay message to message center
        //Let message hand message
        protected void onMessage(Message msg)
        {
            if (this.center == null) return;
            System.Diagnostics.Debug.WriteLine("Remote <-- " + msg.Type);
            this.center.HandMessage(msg);
        }

        public void Dispose()
        {
            if (this.center != null)
                this.center.DetachProxy(this.Token);
            this.center = null;
            onDispose();
        }

        protected virtual void onDispose()
        {

        }
    }
}
