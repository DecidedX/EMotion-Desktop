using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EMotion.Client
{
    internal class QuickConnectThread
    {
        internal static int PORT = 26761;
        private UdpClient udpClient;
        private bool status;
        private MainWindow window;

        internal QuickConnectThread(MainWindow window)
        {
            this.window = window;
            udpClient = new UdpClient(PORT);
        }
        internal void start()
        {
            status = true;
            Thread thread = new Thread(quickConnectThread);
            thread.IsBackground = true;
            thread.Start();
        }

        internal void stop()
        {
            status = false;
        }

        private void quickConnectThread()
        {
            while (status)
            {
                IPEndPoint client = new IPEndPoint(IPAddress.Any, PORT);
                byte[] received = udpClient.Receive(ref client);
                if (client.Address.ToString().Equals(new IPAddress(received).ToString()))
                {
                    window.showQuickConnect(client.Address.ToString());
                }
            }
        }

        internal void ackQC(IPEndPoint client)
        {
            byte[] msg = getSameLANAddress(client.Address).GetAddressBytes();
            udpClient.Send(msg, msg.Length, client);
        }

        private IPAddress getSameLANAddress(IPAddress iPAddress)
        {
            IPAddress laddr = IPAddress.Any;
            byte[] raddrN = iPAddress.GetAddressBytes();
            raddrN[3] = 0;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] laddrN = ip.GetAddressBytes();
                    laddrN[3] = 0;
                    if(laddrN.SequenceEqual(raddrN)) laddr = ip;
                }
            }
            return laddr;
        }

    }
}
