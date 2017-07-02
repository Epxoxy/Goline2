using NetworkService;
using System;

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

        public void Send(Message msg)
        {
            msg.Token = Token;
            bridgeOf(msg);
        }

        public void SendByToken(string token, Message msg)
        {
            msg.Token = token;
            bridgeOf(msg);
        }

        public override void Relay(Message msg)
        {
            onRelay?.Invoke(this, msg);
        }

        protected override void onDispose()
        {
            base.onDispose();
            onRelay = null;
        }
    }
}
