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
using System.Linq;
using System.Text;

namespace TransMock.Mockifier.Parser
{   
    /// <summary>
    /// The class which contains the mock settings when deserialized from the markup
    /// </summary>
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
