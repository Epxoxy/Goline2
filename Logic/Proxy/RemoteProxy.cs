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
        public bool Connected => connector.IsConnected;
        private Connector connector;
        private string remoteToken;

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
            if (msg.Type == MessageType.Proxy)
                remoteToken = msg.Token;
            if (!string.IsNullOrEmpty(remoteToken) && msg.Token == remoteToken)
                msg.Token = Token;
            bridgeOf(msg);
        }

        public override void Relay(Message msg)
        {
            if(!string.IsNullOrEmpty(remoteToken) && msg.Token == Token)
                msg.Token = remoteToken;
            connector.SendObject(msg);
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
