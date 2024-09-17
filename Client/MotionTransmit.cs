using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EMotion.Client
{
    internal class MotionTransmit
    {
        private UdpClient motionServer;
        private UdpClient motionClient;
        private IPEndPoint targetAddress;
        private bool connected;

        internal MotionTransmit()
        {
            motionServer = new UdpClient(26760);
            motionClient = new UdpClient();
            targetAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            Task.Run(() =>
            {
                motionServer.Receive(ref targetAddress);
                connected = true;
                while (true)
                {
                    IPEndPoint address = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
                    motionServer.Receive(ref address);
                    if (address.Port != targetAddress.Port)
                    {
                        targetAddress.Port = address.Port;
                    }
                }
            });
        }

        internal void transmit(byte[] bytes)
        {
            if (connected)
            {
                Task.Run(() => {
                    motionClient.Send(bytes, bytes.Length, targetAddress);
                });
            }
        }

    }
}
