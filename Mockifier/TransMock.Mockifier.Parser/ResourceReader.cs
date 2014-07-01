using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml;

namespace TransMock.Mockifier.Parser
{
    public interface IResourceReader
    {
       Stream MockSchema { get; }
    }

    public class ResourceReader : IResourceReader
    {
        public Stream MockSchema
        {
            get 
            {
                return Assembly.GetExecutingAssembly().GetManifestResourceStream("TransMock.Mockifier.Parser.Mock.xsd");               
            }
        }
    }
}
