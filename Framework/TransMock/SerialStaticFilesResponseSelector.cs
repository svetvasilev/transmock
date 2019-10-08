using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// This class implements behavior of selecting multiple responses from static files in a serial manner
    /// applicable for situations where the same mocked endpoint instance is called multiple times f.ex in a loop,
    /// and for each call a different response is required
    /// </summary>
    public class SerialStaticFilesResponseSelector : ResponseSelectionStrategy
    {
        /// <summary>
        /// Gets or sets a list of file paths that shall be used to populate response messages
        /// </summary>
        public IList<string> FilePaths { get; set; }

        /// <summary>
        /// Creates an instance of <see cref="SerialStaticFilesResponseSelector"/> class
        /// </summary>
        public SerialStaticFilesResponseSelector()
        {
            FilePaths = new List<string>(3);
        }

        /// <summary>
        /// Selects the response message from the provided request message and its zero based reception index
        /// </summary>
        /// <param name="requestIndex">Zero base index indicating the order of reception of the message</param>
        /// <param name="requestMessage">The actual request message</param>
        /// <returns></returns>
        public override MockMessage SelectResponseMessage(int requestIndex, MockMessage requestMessage)
        {
            if (requestIndex < FilePaths.Count())
            {
                var mockResponse = new MockMessage(
                    FilePaths.ElementAt(requestIndex),
                    requestMessage.Encoding);

                return mockResponse;
            }
            else
                throw new IndexOutOfRangeException("Provided message index exceeds the number of FilePaths configured");
        }
    }
}
