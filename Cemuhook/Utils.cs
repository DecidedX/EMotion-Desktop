using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMotion.Cemuhook
{
    internal class Utils
    {
        internal static byte[] uint16ToBytes(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return bytes;
        }

        internal static byte[] uint32ToBytes(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return bytes;
        }

        internal static byte[] int32ToBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return bytes;
        }

        internal static uint bytesToUint32(byte[] bytes)
        {
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes,0);
        }
    }
}
