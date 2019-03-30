using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock
{
    public class StaticFileResponseStrategy : ResponseStrategy
    {
        public string FilePath { get; set; }
        public override Stream FetchResponseMessage()
        {
            if (this.FilePath == null)
            {
                throw new InvalidOperationException("No file path specified for fetching the response");
            }

            MemoryStream s = new MemoryStream(128);

            using (var fs = System.IO.File.OpenRead(this.FilePath))
            {
                fs.CopyTo(s);
                // Rewinding the stream
                s.Position = 0;
            }

            return s;
        }
    }
}
