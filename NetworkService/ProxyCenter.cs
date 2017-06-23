using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService
{
    public abstract class ProxyCenter : IProxyCenter
    {
        private Dictionary<string,IProxy> proxies;
        private readonly object lockHelper = new object();

        public ProxyCenter()
        {
            this.proxies = new Dictionary<string, IProxy>();
        }

        public bool AttachProxy(IProxy proxy)
        {
            if (proxy == null)
                return false;
            if (proxies.ContainsValue(proxy))
            {
                var old = proxies.FirstOrDefault(p => p.Value == proxy);
                string key = old.Key;
                proxies.Remove(key);
            }
            proxy.Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
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
                proxies[token].Dispose();
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
