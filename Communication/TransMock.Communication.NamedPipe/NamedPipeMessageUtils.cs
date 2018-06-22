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
        /// Identifies whether the end of the message represented by the byte array has been reached
        /// </summary>
        /// <param name="data">The part of the message that is currently read</param>
        /// <param name="byteCount">The count of bytes in the array</param>
        /// <returns>True if the end of the message was reached, otherwise false</returns>
        public static bool IsEndOfMessage(byte[] data, int byteCount)
        {
            bool eofReached = false;

            eofReached = (byteCount > 0 && data[byteCount - 1] == 0x0) || byteCount == 0;
            
            return eofReached;
        }
    }
}
