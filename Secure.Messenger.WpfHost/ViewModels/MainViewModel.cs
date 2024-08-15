using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using Secure.Messenger.Server;
using Secure.Messenger.Shared;

namespace Secure.Messenger.WpfHost
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            SendData = new Command(SendDataExecute, SendDataCanExecute);
            ConnectToServer = new Command(ConnectToServerExecute, ConnectToServerCanExecute);

            RemoteIPAddress = LocalIPAddress().ToString();
            SendMessage = "Hello";

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject())) 
            {
                StartServerThread();
            }
        }

        #region Networking

        CryptoServer _server;
        TcpClient _client;
        NetworkStream _strm;
        public AutoResetEvent _serverStarted = new AutoResetEvent(false);
        Thread ServerThread;
        Int32 _port = 9050;

        private void StartServerThread()
        {
            try
            {
                ServerThread = new Thread(new ThreadStart(StartServer));

                ServerThread.IsBackground = true;
                ServerThread.Priority = ThreadPriority.Normal;
                ServerThread.Start();

                _serverStarted.WaitOne(); //Wait Here Until Server Has Started

                StatusMessages.Add("Local Server Started Successfully at : " + _remoteIPAddress);
            }
            catch (Exception ex)
            {
                StatusMessages.Add("Problem Starting The Local Server : " + ex.Message);
                return;
            }
        }

        private void ConnectingToServer()
        {
            try
            {
                MessageData mes = new MessageData("Connection Message 123");

                _client = new TcpClient(_remoteIPAddress, _port);
                _strm = _client.GetStream();

                if (!CryptoHelper.SendData(_strm, mes))
                    throw new Exception("Send data failed");

                _strm.Close();
                _client.Close();

                StatusMessages.Add("Remote Server Connection Successfull to : " + _remoteIPAddress);

                NotConnectedVisibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                StatusMessages.Add("Problem Connecting to Remote Server at : " + _remoteIPAddress);
                StatusMessages.Add("Message : " + ex.Message);
                return;
            }
        }

        private void SendTextData()
        {
            MessageData mes = new MessageData(SendMessage);

            _client = new TcpClient(_remoteIPAddress.ToString(), _port);
            _strm = _client.GetStream();
            CryptoHelper.SendData(_strm, mes);

            _strm.Close();
            _client.Close();

            SentMessages.Add("Sent to " + _remoteIPAddress.ToString() + " > " + SendMessage);
            //SendMessage = string.Empty;        
        }

        void StartServer()
        {
            _server = new CryptoServer(_serverStarted);
            _server.ReceivedData += server_ReceivedData;
            _server.StartServer(LocalIPAddress(), _port);
        }

        void server_ReceivedData(object sender, EventArgs<string> e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ReceivedMessages.Add("Received from " + _remoteIPAddress + " > " + e.Value);
            }));
        }

        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        #endregion

        #region Visibility

        private Visibility _notConnectedVisibility = Visibility.Visible;
        public Visibility NotConnectedVisibility
        {
            get
            {
                return _notConnectedVisibility;
            }
            set
            {
                _notConnectedVisibility = value;
                RaisePropertyChanged("NotConnectedVisibility");
            }
        }

        #endregion

        #region Commands

        public Command SendData { get; set; }
        public Boolean SendDataCanExecute(Object parameter)
        {
            return true;
        }

        public void SendDataExecute(Object parameter)
        {
            SendTextData();
        }

        public Command ConnectToServer { get; set; }
        public Boolean ConnectToServerCanExecute(Object parameter)
        {
            return true;
        }

        public void ConnectToServerExecute(Object parameter)
        {
            ConnectingToServer();
        }

        #endregion

        #region TextData

        private String _remoteIPAddress = String.Empty;
        public String RemoteIPAddress
        {
            get
            {
                return _remoteIPAddress;
            }
            set
            {
                _remoteIPAddress = value;
                RaisePropertyChanged("RemoteIPAddress");
            }
        }

        private String _sendMessage = String.Empty;
        public String SendMessage
        {
            get
            {
                return _sendMessage;
            }
            set
            {
                _sendMessage = value;
                RaisePropertyChanged("SendMessage");
            }
        }

        #endregion

        #region Observable Collections

        private ObservableCollection<String> _receivedMessages = new ObservableCollection<string>();
        public ObservableCollection<String> ReceivedMessages
        {
            get
            {
                return _receivedMessages;
            }
            set
            {
                _receivedMessages = value;
                RaisePropertyChanged("ReceivedMessages");
            }
        }

        private ObservableCollection<String> _sentMessages = new ObservableCollection<string>();
        public ObservableCollection<String> SentMessages
        {
            get
            {
                return _sentMessages;
            }
            set
            {
                _sentMessages = value;
                RaisePropertyChanged("SentMessages");
            }
        }

        private ObservableCollection<String> _statusMessages = new ObservableCollection<string>();
        public ObservableCollection<String> StatusMessages
        {
            get
            {
                return _statusMessages;
            }
            set
            {
                _statusMessages = value;
                RaisePropertyChanged("StatusMessages");
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName.ToString()));
            }
        }

        #endregion
    }
}
