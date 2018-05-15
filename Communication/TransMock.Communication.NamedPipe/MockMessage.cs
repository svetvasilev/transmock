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
        public Dictionary<string, string> Properties
        {
            set; get;
        }

        public string Body
        {
            set;get;
        }
    }
}
