using NetworkService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit
{
    class RemoteProxy : ProxyBase
    {
        private Connector connector;

        public RemoteProxy(Connector connector)
        {
            this.connector = connector;
            this.connector.MessageReceived += onMessageReceived;
        }

        public RemoteProxy EnableOut(string ip, int port)
        {
            connector.Connect(ip, port);
            return this;
        }
        
        public RemoteProxy EnableIn(System.Net.IPAddress ip, int port)
        {
            this.connector.ListenTo(ip, port);
            return this;
        }

        private void onMessageReceived(Message msg)
        {
            this.onMessage(msg);
        }

        public override bool Relay(Message msg)
        {
            return connector.SendObject(msg);
        }

        protected override void onDispose()
        {
            base.onDispose();
            if(connector != null)
            {
                connector.StopListen();
                connector.Disconnct();
            }
            connector = null;
        }
    }
}
