﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkService
{
    public delegate void MessageReceivedEventHandler(Message msg);
    public delegate void HeartbeatEventHandler();
    public delegate void ConnectStateChangedEventHandler(bool isConnected);

    public class Connector
    {
        public event MessageReceivedEventHandler MessageReceived;
        public event HeartbeatEventHandler HeartbeatStarted;
        public event HeartbeatEventHandler HeartbeatFail;
        public event ConnectStateChangedEventHandler ConnectStateChanged;
        public event ConnectStateChangedEventHandler ConnectionReady;

        public bool IsConnected
        {
            get { return isConnected; }
            private set
            {
                if(isConnected != value)
                {
                    isConnected = value;
                    ConnectStateChanged?.Invoke(isConnected);
                }
            }
        }
        public bool IsConnectionReady
        {
            get { return isConnectionReady; }
            private set
            {
                if (isConnectionReady != value)
                {
                    isConnectionReady = value;
                    if(isConnectionReady)
                        ConnectionReady?.Invoke(isConnected);
                }
            }
        }
        public bool IsListenning { get; private set; }
        public bool IsHeartbeating
        {
            get { return isHeartbeating; }
            private set
            {
                if(isHeartbeating != value)
                {
                    isHeartbeating = value;
                    if (isHeartbeating)
                        HeartbeatStarted?.Invoke();
                }
            }
        }

        private bool isConnected;
        private bool isConnectionReady;
        private bool isHeartbeating;
        private TcpClient sendClient;
        private TcpListener listener;
        private Stream streamToServer;
        private bool heartbeatNeed;
        private CancellationTokenSource heartbeat;
        private Thread workerThread;
        private List<RemoteClient> clients = new List<RemoteClient>();
        private DateTime lastHeartBeat;
        private string connectToken;
        private System.Timers.Timer beatTimer;
        private int heartbeatMs = 2000;

        public Connector()
        {
            this.connectToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        }

        public Connector(string token)
        {
            this.connectToken = token;
        }

        public bool Connect(string ip, int port)
        {
            IPAddress ipAddress;
            try
            {
                ipAddress = IPAddress.Parse(ip);
                return Connect(ipAddress, port);
            }
            catch
            {
                return false;
            }
        }

        //Connect to remote client
        public bool Connect(IPAddress ip, int port)
        {
            IsConnected = true;
            if (heartbeat != null)
                heartbeat.Cancel();
            try
            {
                sendClient = new TcpClient();
                sendClient.Connect(ip, port);
                streamToServer = sendClient.GetStream();
                IsConnectionReady = true;
                heartbeatNeed = false;
                heartbeat = new CancellationTokenSource();
                Task.Run(async () =>
                {
                    var heartbeat = Message.CreateHeart(connectToken);
                    int times = 0;
                    while (heartbeatNeed)
                    {

                        SendObject(heartbeat);
                        ++ times;
                        if (heartbeatNeed)
                            await Task.Delay(heartbeatMs);
                        if(times % 10 == 0)
                        {
                            await Task.Delay(heartbeatMs * 3 + 10);
                        }
                    }
                    IsHeartbeating = false;
                    if (beatTimer != null)
                        beatTimer.Stop();
                }, heartbeat.Token);

                if (beatTimer != null)
                    beatTimer.Dispose();
                lastHeartBeat = DateTime.Now;
                beatTimer = new System.Timers.Timer(heartbeatMs * 3);
                beatTimer.Elapsed += onBeatTestElapsed;
                beatTimer.Start();

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                IsConnected = false;
                IsConnectionReady = false;
                return false;
            }
        }

        public void ListenTo(string ip, int port)
        {
            IPAddress ipAddress;
            try
            {
                ipAddress = IPAddress.Parse(ip);
                Connect(ipAddress, port);
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void ListenTo(IPAddress ip, int port)
        {
            if (IsListenning) return;
            ThreadStart start = new ThreadStart(() =>
            {
                listenImpl(ip, port);
            });
            workerThread = new Thread(start);
            workerThread.IsBackground = true;
            workerThread.Start();
        }


        private void listenImpl(IPAddress address, int port)
        {
            listener = new TcpListener(address, port);
            listener.Start();

            //Get EndPoint
            IPEndPoint endPoint = listener.LocalEndpoint as IPEndPoint;
            int portNumber = endPoint.Port;
            IsListenning = true;

            while (true)
            {
                TcpClient remoteTcpClient;
                try
                {
                    //// Always use a Sleep call in a while(true) loop 
                    // to avoid locking up your CPU.
                    Thread.Sleep(10);
                    // Create a TCP socket. 
                    // If you ran this server on the desktop, you could use 
                    // Socket socket = tcpListener.AcceptSocket() 
                    // for greater flexibility.
                    remoteTcpClient = listener.AcceptTcpClient();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Connector[listenImpl]" + e.Message);
                    break;
                }
                var remoteWrap = new RemoteClient(remoteTcpClient);
                var bytes = Encoding.UTF8.GetBytes(remoteWrap.IPEndPoint.Address + " joined");
                clients.Add(remoteWrap);
                var removeList = new List<RemoteClient>();
                foreach (var wrapper in clients)
                {
                    if (wrapper.Client.Connected && IsOnline(wrapper.Client))
                        wrapper.StreamToClient.Write(bytes, 0, bytes.Length);
                    else
                        removeList.Add(wrapper);
                }
                foreach (var wrapper in removeList)
                {
                    clients.Remove(wrapper);
                }

                remoteWrap.BeginReadAsync(onMessage);
            }
            IsListenning = false;
        }

        private void onBeatTestElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            double ms = (DateTime.Now - lastHeartBeat).Duration().TotalMilliseconds;
            if (ms > heartbeatMs * 3)
            {
                HeartbeatFail?.Invoke();
                IsHeartbeating = false;
            }
        }

        private void onMessage(MemoryStream ms)
        {
            Message msg;
            if(ms.Deserialize(out msg))
            {
                if (msg.Type == MessageType.Heartbeat
                    && msg.Token == connectToken)
                {
                    lastHeartBeat = DateTime.Now;
                    if (!IsHeartbeating)
                        IsHeartbeating = true;
                }
                MessageReceived?.Invoke(msg);
            }
        }


        //Stop listen
        public void StopListen()
        {
            try
            {
                listener.Stop();
                IsListenning = false;
                listener = null;
                workerThread.Abort();
                beatTimer?.Stop();
                beatTimer = null;
            }
            catch(Exception e)
            {
                Debug.WriteLine("Connector[StopListen]" + e.Message);
            }
        }

        public void Disconnct()
        {
            heartbeatNeed = false;
            if (heartbeat != null)
                heartbeat.Cancel();
            if (sendClient != null)
                sendClient.Close();
            if (streamToServer != null)
                streamToServer.Dispose();
            IsConnected = false;
            IsConnectionReady = false;
        }


        public bool SendMessage<T>(MessageType type, T content)
        {
            return SendObject(Message.CreateMessage(connectToken, content, type));
        }

        //Send message
        public bool SendObject<T>(T obj)
        {
            if (!IsConnected) return false;
            try
            {
                lock (streamToServer)
                {
                    using (var ms = new MemoryStream())
                    {
                        if (obj.Serialize(ms))
                        {
                            ms.Position = 0;
                            byte[] buffer = new byte[1024];
                            streamToServer.Flush();
                            int readCount = 0;
                            while ((readCount = ms.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                streamToServer.Write(buffer, 0, readCount);
                                buffer = new byte[1024];
                            }
                            streamToServer.Flush();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Connector[SendObject]" + e.Message);
                return false;
            }
        }
        
        public bool IsOnline(TcpClient client)
        {
            return !((client.Client.Poll(1000, SelectMode.SelectRead) && (client.Client.Available == 0)) || !client.Client.Connected);
        }
    }
}
