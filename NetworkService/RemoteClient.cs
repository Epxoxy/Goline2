using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetworkService
{
    public class RemoteClient
    {
        public TcpClient Client { get; private set; }
        public NetworkStream StreamToClient { get; private set; }
        public IPEndPoint IPEndPoint { get; private set; }
        public bool ReadingStarted { get; private set; }
        private const int bufferSize = 1024;

        public RemoteClient(TcpClient client)
        {
            this.Client = client;
            IPEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            StreamToClient = client.GetStream();
        }

        public void BeginRead(Action<MemoryStream> onCompleted)
        {
            if (ReadingStarted) return;
            ReadingStarted = true;
            onReading(onCompleted);
        }

        public void BeginReadAsync(Action<MemoryStream> onCompleted)
        {
            if (ReadingStarted) return;
            ReadingStarted = true;
            Task.Run(() => onReading(onCompleted));
        }

        private void onReading(Action<MemoryStream> onCompleted)
        {
            int readCount;
            byte[] buffer = new byte[bufferSize];
            MemoryStream bufferStream = null;
            try
            {
                bufferStream = new MemoryStream();
                while ((readCount = StreamToClient.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Debug.WriteLine("Reading data, " + readCount + " bytes ...");
                    bufferStream.Write(buffer, 0, readCount);
                    //client.Available > 0
                    //streamToClient.DataAvailable
                    if (!StreamToClient.DataAvailable)
                    {
                        //When stream read completed
                        var completedStream = bufferStream;
                        Message msg;
                        completedStream.Deserialize(out msg);
                        completedStream.Seek(0, SeekOrigin.Begin);
                        switch (msg.Type)
                        {
                            case MessageType.Heartbeat:
                                Debug.WriteLine("Heartbeat");
                                break;
                        }
                        bufferStream = new MemoryStream();
                        Task.Run(() => onCompleted?.Invoke(completedStream));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            finally
            {
                if (StreamToClient != null)
                    StreamToClient.Dispose();
                if (bufferStream != null)
                    bufferStream.Dispose();
                Client.Close();
                ReadingStarted = false;
            }
        }
    }
}
