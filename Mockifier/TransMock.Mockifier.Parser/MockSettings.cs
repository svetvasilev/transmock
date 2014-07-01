using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransMock.Mockifier.Parser
{   
    internal class MockSettings
    {
        public MockSettings()
        {
            PromotedProperties = new Dictionary<string, string>();
            Encoding = "UTF-8";
        }

        public string Host { get; set; }
        
        public string Encoding { get; set; }        

        public string EndpointName { get; set; }

        public string Operation { get; set; }

        public Dictionary<string, string> PromotedProperties { get; set; }
    }
}
