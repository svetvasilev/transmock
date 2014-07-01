using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TransMock.Integration.BizUnit
{
    public static class FileStreamExtension
    {
        public static int ReadWithoutBOM(this FileStream fs, byte[] buffer, int offset, int length)
        {
            int byteCount = fs.Read(buffer, offset, length);

            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            {
                System.Diagnostics.Debug.WriteLine("BOM detected and will be skipped!");
                buffer = buffer.Skip(3).ToArray();
                byteCount = buffer.Length;
            }

            return byteCount;
        }
    }
}
