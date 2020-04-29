using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Contains helper methods related to messages exchange over named pipes
    /// </summary>
    public static class NamedPipeMessageUtils
    {
        /// <summary>
        /// Тhe frame size for a single block of message transmission
        /// </summary>
        public const int FrameSize = 256;

        public static readonly byte[] EndOfMessage = new byte[3] {
            0x54, // T
            0x4d, // M
            0x04 // End of transmission
        };

        /// <summary>
        /// Identifies whether the end of the message represented by the byte array has been reached
        /// </summary>
        /// <param name="data">The part of the message that is currently read</param>
        /// <param name="byteCount">The count of bytes in the array</param>
        /// <returns>True if the end of the message was reached, otherwise false</returns>
        public static bool IsEndOfMessage(byte[] data, int byteCount)
        {
            if (byteCount == 0)
            {
                return true;
            }

            bool eofReached = false;

            // Take the last meaningful 3 bytes
            var eot = data.Skip(byteCount - 3)
                .Take(3)
                .ToArray();
            
            eofReached = eot.SequenceEqual(EndOfMessage);            
            
            return eofReached;
        }
    }
}
