using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using EMotion.Cemuhook;
using Nefarius.ViGEm.Client.Targets.DualShock4;

namespace EMotion.Client
{

    class RController
    {
        internal Thread thread;
        internal UdpClient? udpClient;
        internal bool? status;
        internal byte slot;
        internal IVirtualGamepad controller;
        internal bool isDS4;

        internal RController(Thread thread, UdpClient? udpClient, bool? status, byte slot, IVirtualGamepad controller, bool isDS4)
        {
            this.thread = thread;
            this.udpClient = udpClient;
            this.status = status;
            this.slot = slot;
            this.controller = controller;
            this.isDS4 = isDS4;
        }

    }

    internal class ControllerManager
    {
        private Dictionary<string, RController> controllers;
        private byte slots;
        private readonly uint clientId;
        private readonly ViGEmClient ViGEmClient;
        private MotionTransmit transmit;
        private ushort timestamp;
        private byte tpad, touch1, touch2;

        internal ControllerManager()
        {
            controllers = new Dictionary<string,RController>();
            slots = 0;
            clientId = (uint) DateTime.Now.ToFileTime();
            ViGEmClient = new ViGEmClient();
            transmit = new MotionTransmit();
            tpad = 0;
            touch1 = 0;
            touch2 = 0;
        }
        internal void createController(string ip)
        {
            Thread thread = new Thread(controllerThread!);
            thread.IsBackground = true;
            RController controller = new RController(thread, null, null, distributeSlot(), ViGEmClient.CreateDualShock4Controller(), true);
            controller.controller.AutoSubmitReport = false;
            controllers[ip] = controller;
            thread.Start(ip);
        }

        internal void destoryController(string ip)
        {
            controllers[ip].status = false;
            controllers[ip].udpClient!.Close();
        }

        internal byte distributeSlot()
        {
            byte i = 0;
            for (; i < 8; i++)
            {
                if((1 & (slots >> i)) == 0)
                {
                    slots |= (byte)(1 << i);
                    break;
                }
            }
            return i;
        }

        internal void releaseSlot(byte slot)
        {
            slots ^= (byte)(1 << slot); 
        }

        internal List<RController> getControllers()
        {
            return controllers.Values.ToList();
        }

        internal int getCount()
        {
            return controllers.Count;
        }

        internal bool? getStatus(string ip)
        {
            return controllers[ip].status;
        }

        internal bool isDS4(string ip)
        {
            return controllers[ip].isDS4;
        }

        internal void switchGamepad(string ip)
        {
            unregisterViberation(ip);
            controllers[ip].controller.Disconnect();
            controllers[ip].controller = (controllers[ip].isDS4) ? ViGEmClient.CreateXbox360Controller() : ViGEmClient.CreateDualShock4Controller();
            controllers[ip].isDS4 = !controllers[ip].isDS4;
            registerViberation(ip);
            controllers[ip].controller.Connect();
        }

        private void controllerThread(object obj)
        {
            string ip = (string) obj;
            UdpClient udpClient = new UdpClient();
            controllers[ip].udpClient = udpClient;
            udpClient.Connect(ip, 26760);
            RequestThread request = new RequestThread(clientId, udpClient, controllers[ip].slot); 
            request.start();
            Task.Run(() =>
            {
                Thread.Sleep(10000);
                if (controllers[ip].status == null)
                {
                    controllers[ip].status = false;
                }
            });
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), 26760);
            while (controllers[ip].status ?? true)
            {
                byte[] received = new byte[0];
                try { 
                    received = udpClient.Receive(ref iPEndPoint);
                }
                catch (Exception ex) { }
                if (received.Length > 0)
                {
                    if (Protocol.verifyCrc32((byte[])received.Clone()))
                    {
                        if (!controllers[ip].status.HasValue)
                        {
                            controllers[ip].status = true;
                            controllers[ip].controller.Connect();
                            registerViberation(ip);
                        }
                        if (BitConverter.ToUInt32(received, 16) == 0x100002)
                        {
                            if (controllers[ip].isDS4)
                            {
                                setDS4ControllerStatus((IDualShock4Controller)controllers[ip].controller, received.Skip(36).ToArray());
                            }
                            else
                            {
                                transmit.transmit(received);
                                setXboxControllerStatus((IXbox360Controller)controllers[ip].controller, received.Skip(36).ToArray());
                            }
                        }
                    }
                }
            }
            request.stop();
            unregisterViberation(ip);
            releaseSlot(controllers[ip].slot);
            controllers[ip].controller.Disconnect();
            controllers.Remove(ip);
        }

        private void registerViberation(string ip)
        {
            if (controllers[ip].isDS4)
                ((IDualShock4Controller)controllers[ip].controller).FeedbackReceived += ds4VibrationHandle;
            else
                ((IXbox360Controller)controllers[ip].controller).FeedbackReceived += xboxVibrationHandle;
        }

        private void unregisterViberation(string ip)
        {
            if (controllers[ip].isDS4)
                ((IDualShock4Controller)controllers[ip].controller).FeedbackReceived -= ds4VibrationHandle;
            else
                ((IXbox360Controller)controllers[ip].controller).FeedbackReceived -= xboxVibrationHandle;
        }

        private void setXboxControllerStatus(IXbox360Controller controller, byte[] bytes)
        {
            controller.SetButtonState(Xbox360Button.Start, bytes[2] == 255);
            controller.SetButtonState(Xbox360Button.Back, bytes[3] == 255);
            controller.SetAxisValue(Xbox360Axis.LeftThumbX, axisByteToShort(bytes[4]));
            controller.SetAxisValue(Xbox360Axis.LeftThumbY, axisByteToShort(bytes[5]));
            controller.SetAxisValue(Xbox360Axis.RightThumbX, axisByteToShort(bytes[6]));
            controller.SetAxisValue(Xbox360Axis.RightThumbY, axisByteToShort(bytes[7]));
            controller.SetButtonState(Xbox360Button.Left, bytes[8] == 255);
            controller.SetButtonState(Xbox360Button.Down, bytes[9] == 255);
            controller.SetButtonState(Xbox360Button.Right, bytes[10] == 255);
            controller.SetButtonState(Xbox360Button.Up, bytes[11] == 255);
            controller.SetButtonState(Xbox360Button.Y, bytes[12] == 255);
            controller.SetButtonState(Xbox360Button.B, bytes[13] == 255);
            controller.SetButtonState(Xbox360Button.A, bytes[14] == 255);
            controller.SetButtonState(Xbox360Button.X, bytes[15] == 255);
            controller.SetButtonState(Xbox360Button.RightShoulder, bytes[16] == 255);
            controller.SetButtonState(Xbox360Button.LeftShoulder, bytes[17] == 255);
            controller.SetSliderValue(Xbox360Slider.RightTrigger, bytes[18]);
            controller.SetSliderValue(Xbox360Slider.LeftTrigger, bytes[19]);
            controller.SetButtonState(Xbox360Button.LeftThumb, (bytes[0] & 2) == 2);
            controller.SetButtonState(Xbox360Button.RightThumb, (bytes[0] & 4) == 4);
            controller.SubmitReport();
        }

        private void setDS4ControllerStatus(IDualShock4Controller controller, byte[] bytes)
        {
            byte[] report = new byte[63];
            report[0] = (byte)(bytes[4] + 128);
            report[1] = (byte)(127 - bytes[5]);
            report[2] = (byte)(bytes[6] + 128);
            report[3] = (byte)(127 - bytes[7]);
            report[4] = toButtonsBitmask(bytes.Skip(8).Take(8).ToArray());
            byte b5 = 0;
            b5 |= (byte)((((bytes[0] & 2) == 2) ? 1 : 0) << 7);
            b5 |= (byte)((((bytes[0] & 4) == 4) ? 1 : 0) << 6);
            b5 |= (byte)((bytes[2] == 255 ? 1 : 0) << 5);
            b5 |= (byte)((bytes[3] == 255 ? 1 : 0) << 4);
            b5 |= (byte)((bytes[18] == 255 ? 1 : 0) << 3);
            b5 |= (byte)((bytes[19] == 255 ? 1 : 0) << 2);
            b5 |= (byte)((bytes[16] == 255 ? 1 : 0) << 1);
            b5 |= (byte)(bytes[17] == 255 ? 1 : 0);
            report[5] = b5;
            report[7] = bytes[19];
            report[8] = bytes[18];
            BitConverter.GetBytes((ushort)int.Parse(DateTime.Now.ToString("ffffff"))).CopyTo(report, 10);
            BitConverter.GetBytes(getShortAbs(BitConverter.ToSingle(bytes, 52) / 180 * Math.PI * 1000)).CopyTo(report, 12);
            BitConverter.GetBytes(getShortAbs(-BitConverter.ToSingle(bytes, 56) / 180 * Math.PI * 1000)).CopyTo(report, 14);
            BitConverter.GetBytes(getShortAbs(-BitConverter.ToSingle(bytes, 60) / 180 * Math.PI * 1000)).CopyTo(report, 16);
            BitConverter.GetBytes(getShortAbs(-BitConverter.ToSingle(bytes, 40) * 8172)).CopyTo(report, 18);
            BitConverter.GetBytes(getShortAbs(-BitConverter.ToSingle(bytes, 44) * 8172)).CopyTo(report, 20);
            BitConverter.GetBytes(getShortAbs(-BitConverter.ToSingle(bytes, 48) * 8172)).CopyTo(report, 22);
            report[32] = 0x01;
            report[33] = tpad++;
            bool down = bytes[20] == 1;
            if(touch1 >> 7  == bytes[20] && !down)
                touch1++;
            if (down)
                touch1 &= 0x7F;
            else
                touch1 |= 0x80;
            report[34] = touch1;
            B4ToB3(bytes.Skip(22).Take(4).ToArray()).CopyTo(report, 35);
            down = bytes[26] == 1;
            if (touch2 >> 7 == bytes[20] && !down)
                touch2++;
            if (down)
                touch2 &= 0x7F;
            else
                touch2 |= 0x80;
            report[38] = touch2;
            B4ToB3(bytes.Skip(28).Take(4).ToArray()).CopyTo(report, 39);
            //timestamp += 188;
            controller.SubmitRawReport(report);
        }

        private byte toButtonsBitmask(byte[] buttons)
        {
            byte bitmask = 0x08;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < 4 && buttons[i] == 255)
                {
                    switch (i)
                    {
                        case 0: bitmask = 0x06; break;
                        case 1: bitmask = 0x04; break;
                        case 2: bitmask = 0x02; break;
                        case 3: bitmask = 0x00; break;
                    }
                    i = 3;
                }
                else
                {
                    bitmask |= (byte)((buttons[i] == 255 ? 1:0) << 11 - i);
                }
            }
            return bitmask;
        }

        private short getShortAbs(double v)
        {
            double abs = Math.Abs(v);
            return (short)(v /abs * Math.Floor(abs));
        }

        private void xboxVibrationHandle(object sender, Xbox360FeedbackReceivedEventArgs e)
        {
            vibrationHandleMethod(sender, e.LargeMotor + e.SmallMotor);
        }

        private void ds4VibrationHandle(object sender, DualShock4FeedbackReceivedEventArgs e)
        {
            vibrationHandleMethod(sender, e.LargeMotor + e.SmallMotor);
        }

        private void vibrationHandleMethod(object sender, int motor)
        {
            foreach ((string s, RController controller) in controllers)
            {
                if (controller.controller == sender)
                {
                    byte[] bytes = Protocol.doCrc32(Protocol.generateMotorMsg(clientId, (byte)(motor > 255 ? 255 : motor)));
                    controller.udpClient!.Send(bytes, bytes.Length);
                }
            }
        }

        private short axisByteToShort(byte axis)
        {
            double percent = axis / 127D;
            return (short)(percent * 32767);
        }

        private byte[] B4ToB3(byte[] b4)
        {
            short x = BitConverter.ToInt16(b4, 0);
            short y = BitConverter.ToInt16(b4, 2);
            byte[] bytes = new byte[3];
            bytes[0] = (byte)(x & 0xFF);
            bytes[1] = (byte)(((x & 0xF00) >> 8) | ((y & 0xF) << 4));
            bytes[2] = (byte)((y & 0xFF0) >> 4);
            return bytes;
        }

    }
}
