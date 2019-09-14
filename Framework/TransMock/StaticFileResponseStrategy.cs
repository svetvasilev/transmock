
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
        /// <summary>
        /// The path to the file which will be used to populate the responce mock message contents
        /// </summary>
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

            var mockResponse = new MockMessage(
                this.FilePath,
                requestMessage.Encoding);

            return mockResponse;
        }
    }
}
