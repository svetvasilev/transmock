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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Resources;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// The public interface of the resource reader
    /// </summary>
    public interface IResourceReader
    {
       Stream MockSchema { get; }

       string GetMockTransportConfig(string btsVersion, string configKey);
    }

    /// <summary>
    /// Implements a resource reader for resources built in assembly
    /// </summary>
    public class ResourceReader : IResourceReader
    {
        private ResourceManager rm;

        public ResourceReader()
        {
            
        }

        /// <summary>
        /// Gets the stream to the embedded Mock.xsd schema file
        /// </summary>
        public Stream MockSchema
        {
            get 
            {
                return Assembly.GetExecutingAssembly().GetManifestResourceStream("TransMock.Mockifier.Parser.Mock.xsd");               
            }
        }

        /// <summary>
        /// Gets the config data of the mock adapter transport
        /// </summary>
        /// <param name="btsVersion">The version of the BizTalk server</param>
        /// <param name="configKey">The key for the specific configuration</param>
        /// <returns></returns>
        public string GetMockTransportConfig(string btsVersion, string configKey)
        {
            try
            {
                return Resources.ResourceManager
                    .GetString(string.Format("BTS{0}_{1}",
                        btsVersion, configKey));           
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetMocktransportConfig faile with an exception: " + ex.Message);

                return "Not able to fetch the resource";
            }
        }
    }
}
