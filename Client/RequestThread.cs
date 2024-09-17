using EMotion.Cemuhook;
using System.Net.Sockets;
using System.Threading;

namespace EMotion.Client
{
    internal class RequestThread
    {
        private uint clientId;
        private UdpClient udpClient;
        private byte slot;
        private bool status;

        internal RequestThread(uint clientId, UdpClient udpClient, byte slot)
        {
            this.clientId = clientId;
            this.udpClient = udpClient;
            this.slot = slot;
        }

        internal void start()
        {
            status = true;
            Thread thread = new Thread(requestThread);
            thread.IsBackground = true;
            thread.Start();
        }

        internal void stop()
        {
            status = false;
        }

        internal UdpClient getUdpClient()
        {
            return udpClient;
        }

        private void requestThread()
        {
            byte[] infReqMsg = Protocol.doCrc32(Protocol.generateInformationReqMsg(clientId, slot));
            byte[] dataReqMsg = Protocol.doCrc32(Protocol.generateActualDataReqMsg(clientId));
            while (status)
            {
                udpClient.Send(infReqMsg, infReqMsg.Length);
                udpClient.Send(dataReqMsg, dataReqMsg.Length);
                Thread.Sleep(3000);
            }
        }

    }
}
