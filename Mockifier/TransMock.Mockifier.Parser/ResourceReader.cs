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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// The public interface of the resource reader
    /// </summary>
    public interface IResourceReader
    {
        /// <summary>
        /// Gets the stream to the Mock schema
        /// </summary>
       Stream MockSchema { get; }

        /// <summary>
        /// Gets the mock transport configuration
        /// </summary>
        /// <param name="btsVersion">The version of the BizTalk server for the configuration</param>
        /// <param name="configKey">The key of the configuration setting to be returned</param>
        /// <returns>A string containing the mock adapter transport configuration for the specified key</returns>
       string GetMockTransportConfig(string btsVersion, string configKey);
    }

    /// <summary>
    /// Implements a resource reader for resources built in assembly
    /// </summary>
    public class ResourceReader : IResourceReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceReader"/> class
        /// </summary>
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
                return Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("TransMock.Mockifier.Parser.Mock.xsd");               
            }
        }

        /// <summary>
        /// Gets the config data of the mock adapter transport
        /// </summary>
        /// <param name="btsVersion">The version of the BizTalk server</param>
        /// <param name="configKey">The key for the specific configuration</param>
        /// <returns>A string containing the mock adapter transport configuration</returns>
        public string GetMockTransportConfig(string btsVersion, string configKey)
        {
            try
            {
                int btsVersionNumber;
                if (!int.TryParse(btsVersion, out btsVersionNumber))
                {
                    btsVersionNumber = 2013;
                }
                
                return Resources.ResourceManager
                    .GetString(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "BTS{0}_{1}",
                            btsVersionNumber >= 2013 ? "2013" : btsVersion, 
                            configKey));           
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetMocktransportConfig faile with an exception: " + ex.Message);

                return "Not able to fetch the resource";
            }
        }
    }
}
