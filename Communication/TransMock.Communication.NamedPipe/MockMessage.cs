
/***************************************
//   Copyright 2018 - Svetoslav Vasilev

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
/// Module      :  MockMessage.cs
/// Description :  This class implements the behavior of a message exchanged between the test and the Mock adapter.
/// -----------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Defines a message that has contents and accompanying meta properties
    /// </summary>
    [Serializable]
    public class MockMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockMessage"/> class
        /// </summary>
        public MockMessage()
        {
            if (this.Properties == null)
            {
                Properties = new Dictionary<string, string>();
            }

            if(this.Encoding == null)
            {
                this.Encoding = Encoding.UTF8;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockMessage"/> class with a specific encoding
        /// </summary>
        public MockMessage(Encoding encoding) : this()
        {
            this.Encoding = encoding;            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockMessage"/> class with a given body contents and specific encoding
        /// </summary>
        public MockMessage(byte[] messageBody, Encoding encoding) : this(encoding)
        {
            this.MessageBody = messageBody;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockMessage"/> class with contents from a file at the specified path and specific encoding
        /// </summary>
        public MockMessage(string filePath, Encoding encoding) : this(encoding)
        {
            this.MessageBody = System.IO.File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// Contains the promoted properties as key-value pairs
        /// </summary>
        public Dictionary<string, string> Properties
        {
            set; get;
        }

        /// <summary>
        /// The encoding of the message body
        /// </summary>
        public Encoding Encoding
        {
            set; get;
        }

        /// <summary>
        /// The byte contents of the message body
        /// </summary>
        protected byte[] MessageBody
        {
            set; get;
        }

        /// <summary>
        /// String representation of the message body
        /// </summary>
        public string Body
        {
            set
            {
                this.MessageBody = this.Encoding.GetBytes(value);
            }

            get
            {
                return this.Encoding.GetString(
                    this.MessageBody);
            }
        }

        /// <summary>
        /// Based64 encoded string representation of the message body
        /// </summary>
        public string BodyBase64
        {
            get
            {
                return Convert.ToBase64String(this.MessageBody);
            }
        }

        /// <summary>
        /// The body stream
        /// </summary>
        public Stream BodyStream
        {
            get
            {
                if (this.MessageBody != null)
                {
                    var stream = new MemoryStream(this.MessageBody);

                    return stream;
                }
                else
                    return null;
            }
        }
    }
}
