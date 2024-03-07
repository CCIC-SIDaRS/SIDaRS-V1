using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Backend.MonitorManager
{
    class MonitorSystem
    {
        private IPAddress _monitorAddress { get; set; }
        private Socket _captureSocket { get; set; }
        private Thread _captureThread { get; set; }
        private bool _runCapture { get; set; }

        public MonitorSystem(string monitorAddress)
        {
            _monitorAddress = IPAddress.Parse(monitorAddress);
            _captureSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

            SetupCapture();
        }

        public void SetupCapture()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            _captureSocket.Bind(endPoint);
            // _captureSocket.IOControl(IOControlCode.ReceiveAll, null, null);

            _captureSocket.BeginReceive(new byte[] {}, 0, 10000, SocketFlags.None, new AsyncCallback(Capture), null);

            //_captureThread = new Thread(Capture);
            //_captureThread.IsBackground = true;
            //_captureThread.Start();
        }

        public void Capture(IAsyncResult res)
        {
            while (_runCapture)
            {
                string data = "";
                byte[] bytes = new byte[2048];

                Socket client = _captureSocket.Accept();

                while (true)
                {
                    int numBytes = client.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, numBytes);

                    if (data.IndexOf("\r\n") > -1)
                    {
                        break;
                    }
                }

                Debug.WriteLine(data);
            }
        }
    }
}