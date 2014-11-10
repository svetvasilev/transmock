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
using System.IO.Pipes;
using System.Reflection;

using Microsoft.XLANGs.RuntimeTypes;
using Microsoft.XLANGs.Core;
using Microsoft.XLANGs.BaseTypes;
using Microsoft.BizTalk.XLANGs.BTXEngine;

namespace TransMock.TestUtils.BizTalk
{
    /// <summary>
    /// This class allows for making dynamic send ports in BizTalk server testable with TransMock
    /// </summary>
    public static class DynamicSendPortMockHelper
    {
        /// <summary>
        /// Mocks a dynamic send port with the given name by setting the necessary transport properties on the outbound message
        /// </summary>
        /// <param name="portName">The name of the send port as defined in the orchestration</param>
        /// <param name="outboundMessage">The outbound message instance that is to be sent over the dynamic send port</param>
        /// <returns>An instance of the MockTransportConfig class if the orchestration is executed within the context of a TransMock test case. Otherwise a null is returned.</returns>
        public static MockTransportConfig MockDynamicSendPort(string portName, XLANGMessage outboundMessage)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MockDynamicSendPort(portName, XLANGMessage) called.");

                bool applyMock = IsTransMockTestCaseExecuting();

                if (applyMock)
                {
                    System.Diagnostics.Debug.WriteLine("Applying mock transport settings");

                    ApplyMockTransportConfig(outboundMessage);

                    var mockConfig = new MockTransportConfig(portName);

                    return mockConfig;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Not executing in TransMock test case. Returning null.");

                    return null;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "MockDynamicSendPort(portName, XLANGMessage) threw an exception: " + ex.Message);

                return null;
            }
        }
         
        /// <summary>
        /// Checks whether a TransMock test case is currently under execution
        /// </summary>
        /// <returns>True if the TransMock beacon is on, otherwise false</returns>
        private static bool IsTransMockTestCaseExecuting()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("IsTransMockTestCaseExecuting() called");

                using (NamedPipeClientStream beaconClient = 
                    new NamedPipeClientStream("localhost", "TransMockBeacon",
                        PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    System.Diagnostics.Debug.WriteLine("Connecting to the beacon");

                    beaconClient.Connect(10);
                    //Closing the stream immediately after connecting

                    System.Diagnostics.Debug.WriteLine("Disonnecting to the beacon");
                    
                    beaconClient.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("IstransMockTestCaseExecuting() threw exception: " + ex.Message);

                return false;
            }
        }       

        /// <summary>
        /// Applies the TransMock transport configuration to the context of the provided message 
        /// </summary>
        /// <param name="outboundMessage">The outbound message instance that is to be sent over the dynamic send port</param>
        private static void ApplyMockTransportConfig(XLANGMessage outboundMessage)
        {
            //Adding the mock binding properties to tme message context                    
            outboundMessage.SetPropertyValue(typeof(WCF.BindingType), "mockBinding");
            outboundMessage.SetPropertyValue(typeof(WCF.BindingConfiguration),
                @"<binding name=""mockBinding"" Encoding=""UTF-8"" />");
            outboundMessage.SetPropertyValue(typeof(WCF.Action), "*");
            outboundMessage.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                @"<behavior name=""EndpointBehavior"" />");
            outboundMessage.SetPropertyValue(typeof(WCF.OutboundBodyLocation), "UseTemplate");
            outboundMessage.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>");

            outboundMessage.SetPropertyValue(typeof(WCF.InboundBodyLocation), @"UseBodyPath");
            outboundMessage.SetPropertyValue(typeof(WCF.InboundBodyPathExpression), @"/MessageContent");
            outboundMessage.SetPropertyValue(typeof(WCF.InboundNodeEncoding), @"Base64");
            outboundMessage.SetPropertyValue(typeof(WCF.PropagateFaultMessage), true);

            outboundMessage.SetPropertyValue(typeof(WCF.UseSSO), false);
            outboundMessage.SetPropertyValue(typeof(WCF.EnableTransaction), false);
        }
    }
}
