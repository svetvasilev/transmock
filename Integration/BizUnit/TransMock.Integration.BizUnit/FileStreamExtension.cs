/***************************************
//   Copyright 2014 - Svetoslav Vasilev

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
*****************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Extension helper class
    /// </summary>
    public static class FileStreamExtension
    {
        /// <summary>
        /// Extension method for the FileStream class implementing logic for reading a file without the BOM bytes for Unicode encoded files.
        /// </summary>
        /// <param name="fs">The file stream object</param>
        /// <param name="buffer">The buffer of bytes to be read from the file stream</param>
        /// <param name="offset">The offset from where the read will start</param>
        /// <param name="length">The number of bytes to read</param>
        /// <returns>An integer indicating the number of bytes read</returns>
        public static int ReadWithoutBOM(this FileStream fs, byte[] buffer, int offset, int length)
        {
            int byteCount = fs.Read(buffer, offset, length);

            if (byteCount > 0 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            {
                System.Diagnostics.Debug.WriteLine("BOM detected and will be skipped!");

                buffer = buffer.Skip(3).ToArray();                
            }

            return byteCount;
        }
    }
}
