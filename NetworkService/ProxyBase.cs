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
        private IBridge bridge;

        //Enable proxy
        public void Enable(IBridge center)
        {
            this.bridge = center;
            OnEnable();
        }

        //Do something on enable proxy
        protected virtual void OnEnable()
        {

        }

        //Relay message from message center
        public abstract void Relay(Message msg);

        //Relay message to message center
        //Let message hand message
        protected void bridgeOf(Message msg)
        {
            if (this.bridge == null) return;
            System.Diagnostics.Debug.WriteLine("Remote <-- " + msg.Type);
            this.bridge.HandMessage(msg);
        }

        public void Dispose()
        {
            if (this.bridge != null)
                this.bridge.DetachProxy(this.Token);
            this.bridge = null;
            onDispose();
        }

        protected virtual void onDispose()
        {

        }
    }
}
