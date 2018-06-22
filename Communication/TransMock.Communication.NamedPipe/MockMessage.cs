using System;
using System.Collections.Generic;
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

        public MockMessage(Encoding encoding) : this()
        {
            this.Encoding = encoding;            
        }

        public MockMessage(byte[] messageBody, Encoding encoding) : this(encoding)
        {
            this.MessageBody = messageBody;
        }

        public MockMessage(string filePath, Encoding encoding) : this(encoding)
        {
            this.MessageBody = System.IO.File.ReadAllBytes(filePath);
        }

        public Dictionary<string, string> Properties
        {
            set; get;
        }

        public Encoding Encoding
        {
            set; get;
        }

        protected byte[] MessageBody
        {
            set; get;
        }

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

        public string BodyBase64
        {
            get
            {
                return Convert.ToBase64String(this.MessageBody);
            }
        }
    }
}
