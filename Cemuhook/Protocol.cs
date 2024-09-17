using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Hashing;

namespace EMotion.Cemuhook
{

    struct Header
    {
        internal string magicString;
        internal ushort version;
        internal ushort length;
        internal uint id;
        internal byte[] bytes
        {
            get
            {
                byte[] bytes = new byte[16];
                Encoding.Default.GetBytes(magicString).CopyTo(bytes, 0);
                BitConverter.GetBytes(version).CopyTo(bytes, 4);
                BitConverter.GetBytes(length).CopyTo(bytes, 6);
                BitConverter.GetBytes(id).CopyTo(bytes, 12);
                return bytes;
            }
        }


        internal Header(string magicString, ushort version, ushort length, uint id)
        {
            this.magicString = magicString;
            this.version = version;
            this.length = length;
            this.id = id;
        }
        internal Header(byte[] header)
        {
            magicString = BitConverter.ToString(header.Take(4).ToArray());
            version = (ushort) BitConverter.ToInt16(header, 4);
            length = (ushort) BitConverter.ToInt16(header, 6);
            id = (uint) BitConverter.ToInt32(header, 12);
        }
        
    }

    internal class Protocol
    {

        internal static byte[] generateActualDataReqMsg(uint id)
        {
            byte[] bytes = new byte[28];
            new Header("DSUC", 1001, 12, id).bytes.CopyTo(bytes, 0);
            uint messageType = 0x100002;
            BitConverter.GetBytes(messageType).CopyTo(bytes, 16);
            return bytes;
        }

        internal static byte[] generateInformationReqMsg(uint id, byte slot)
        {
            byte[] bytes = new byte[28];
            new Header("DSUC", 1001, 12, id).bytes.CopyTo(bytes, 0);
            uint messageType = 0x100001;
            BitConverter.GetBytes(messageType).CopyTo(bytes, 16);
            BitConverter.GetBytes(1).CopyTo(bytes, 20);
            bytes[24] = slot;
            return bytes;
        }

        internal static byte[] generateMotorMsg(uint id, byte largeMotor) 
        {
            byte[] bytes = new byte[30];
            new Header("DSUC", 1001, 12, id).bytes.CopyTo(bytes, 0);
            uint messageType = 0x110002;
            BitConverter.GetBytes(messageType).CopyTo(bytes, 16);
            bytes[29] = largeMotor;
            return bytes;
        }

        internal static byte[] doCrc32(byte[] bytes)
        {
            byte[] crc32 = Crc32.Hash(bytes);
            crc32.CopyTo(bytes, 8);
            return bytes;
        }

        internal static bool verifyCrc32(byte[] bytes)
        {
            byte[] crc = bytes.Skip(8).Take(4).ToArray();
            Array.Clear(bytes, 8, 4);
            return crc.SequenceEqual(Crc32.Hash(bytes));
        }
    }
}
