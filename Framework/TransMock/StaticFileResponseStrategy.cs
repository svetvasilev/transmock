
/***************************************
//   Copyright 2019 - Svetoslav Vasilev

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

/// -----------------------------------------------------------------------------------------------------------
/// Module      :  StaticFileResponseStrategy.cs
/// Description :  This class represents a response selector strategy where contents for response
///                 is taken from a static file.
/// -----------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// This class implements the logic for fetching the response contents from a static file
    /// </summary>
    public class StaticFileResponseStrategy : ResponseSelectionStrategy
    {
        public string FilePath { get; set; }

        /// <summary>
        /// Reads the contents of the file provided in the FilePath property and returns an instance
        /// of the <see cref="MockMessage" /> class initialized with this contents
        /// </summary>
        /// <param name="requestIndex">The index of the request in a multi-request scenario</param>
        /// <param name="requestMessage">The request message</param>
        /// <returns>An instance of <see cref="MockMessage" /> class with contents taken from the file specified in the FilePath property</returns>
        public override MockMessage SelectResponseMessage(int requestIndex, MockMessage requestMessage)
        {
            if (this.FilePath == null)
            {
                throw new InvalidOperationException("No file path specified for fetching the response contents!");
            }

            MemoryStream s = new MemoryStream(128);

            using (var fs = File.OpenRead(this.FilePath))
            {   
                fs.CopyTo(s);                
                // Rewinding the stream
                s.Position = 0;
            }
            // Check whether the first 3 bytes are the BOM
            bool skipBom = false;
            byte[] bomBytes = new byte[3];

            int bytesRead = s.Read(bomBytes, 0, 3);
            if (bytesRead == 3 && bomBytes[0] == 0xEF && bomBytes[1] == 0xBB && bomBytes[2] == 0xBF)
            {
                skipBom = true;
            }

            // Rewind the stream again
            s.Position = 0;
            
            return new MockMessage(
                skipBom ? s.ToArray().Skip(3).ToArray()
                    : s.ToArray(), requestMessage.Encoding);
        }
    }
}
