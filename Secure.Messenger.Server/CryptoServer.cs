using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Secure.Messenger.Shared;

namespace Secure.Messenger.Server
{
    public class CryptoServer
    {
        public event EventHandler<EventArgs<String>> ReceivedData;
        AutoResetEvent _serverStarted;

        public CryptoServer(AutoResetEvent serverStarted)
        {
            _serverStarted = serverStarted;
        }

        public void StartServer(IPAddress localIPAddress, int localPort)
        {
            TcpListener listener = null;

            try
            {
                listener = new TcpListener(localIPAddress, localPort);
                listener.Start();
                _serverStarted.Set(); // Signals the main thread to say local server thread has been started
            }
            catch (Exception ex)
            {
                ReceivedData.SafeInvoke(this, new EventArgs<string>("From Server : Network Error : " + ex.Message));
                return;
            }

            NetworkStream strm = null;

            while (!listener.Pending())
            {
                TcpClient client = listener.AcceptTcpClient();
                strm = client.GetStream();

                MessageData mes = CryptoHelper.ReceiveData(strm);

                if (mes != null)
                {
                    if (mes.MessageBody != "Connection Message 123")
                    {
                        ReceivedData.SafeInvoke(this, new EventArgs<string>(mes.MessageBody));
                    }
                }
                else
                    ReceivedData.SafeInvoke(this, new EventArgs<string>("From Server : Deserialisation Error"));

            }
        }
    }

}
