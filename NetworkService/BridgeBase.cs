using System;
using System.Collections.Generic;

namespace NetworkService
{
    public abstract class BridgeBase : IBridge
    {
        private Dictionary<string,IProxy> proxies;
        private readonly object lockHelper = new object();

        public BridgeBase()
        {
            this.proxies = new Dictionary<string, IProxy>();
        }

        public bool AttachProxy(IProxy proxy)
        {
            if (proxy == null || string.IsNullOrEmpty(proxy.Token))
                return false;
            lock (lockHelper)
            {
                proxies.Add(proxy.Token, proxy);
            }
            return true;
        }

        public void DetachProxy(string token)
        {
            if (proxies.ContainsKey(token))
            {
                lock (lockHelper)
                {
                    proxies.Remove(token);
                }
            }
        }
        
        public void Enable()
        {
            foreach (var proxy in proxies.Values)
                proxy.Enable(this);
        }


        public void HandMessage(Message msg)
        {
            onMessage(msg);
        }

        protected virtual void onMessage(Message msg) { }


        public void Unicast(string token, Message msg)
        {
            if (!proxies.ContainsKey(token)) return;
            try
            {
                proxies[token].Relay(msg);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }
        
        public void Broadcast(Message msg)
        {
            lock (lockHelper)
            {
                foreach (var proxy in proxies.Values)
                {
                    try
                    {
                        proxy.Relay(msg);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}
