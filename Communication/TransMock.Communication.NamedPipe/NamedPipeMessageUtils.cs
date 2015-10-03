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

            if (byteCount > 2)
            {
                // For longer strings only the last one is EOF byte                        
                eofReached = data[byteCount - 1] == 0x0 &&
                                data[byteCount - 2] != 0x0 &&
                                data[byteCount - 3] != 0x0;
            }
            
            if (byteCount > 1 && !eofReached)
            {
                // For Unicode case last 2 bytes are EOF
                eofReached = data[byteCount - 1] == 0x0 &&
                    data[byteCount - 2] == 0x0;
            }
            else if (byteCount == 1)
            {
                // In case we read the last byte alone
                eofReached = data[byteCount - 1] == 0x0;
            }

            return eofReached;
        }
    }
}
