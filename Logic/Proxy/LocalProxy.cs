using NetworkService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit
{
    class LocalProxy : ProxyBase
    {
        public string Name { get; set; }
        private Action<object, Message> onRelay;

        public LocalProxy(Action<object, Message> onRelay)
        {
            this.onRelay = onRelay;
        }

        public override bool Relay(Message msg)
        {
            onRelay?.Invoke(this, msg);
            return true;
        }

        //Pass a message to message center
        public void Pass(Message msg)
        {
            this.onMessage(msg);
        }

        protected override void onDispose()
        {
            base.onDispose();
            onRelay = null;
        }
    }
}
