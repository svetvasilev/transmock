/***************************************
//   Copyright 2015 - Svetoslav Vasilev

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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.IO;
using System.IO.Pipes;
using System.ServiceModel;
using System.ServiceModel.Channels;

using TransMock.TestUtils;
using TransMock.Wcf.Adapter;
using TransMock.Integration.BizUnit;

using BizUnit;
using Moq;
using BizUnit.Core.Utilites;
using BizUnit.Core.TestBuilder;

namespace TransMock.Integration.BizUnit.Tests
{
    /// <summary>
    /// Tests all the steps with a real BizTalk integration
    /// </summary>
    [TestClass]
    public class TestWithBizTalk
    {
        [TestMethod]
        [DeploymentItem(@"TestData\TestRequest.xml")]
        public void TestOneWay_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockSendStep sendDtep = new MockSendStep()
            {
                Url = "mock://localhost/OneWayReceive",
                RequestPath = "TestRequest.xml",
                Encoding = "UTF-8",
                Timeout = 30
            };
            
            //Validated the step
            sendDtep.Validate(context);
            //Executing the step
            sendDtep.Execute(context);

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s) &&
                    s == "Reading request content from path TestRequest.xml"),
                It.Is<string>(s => !string.IsNullOrEmpty(s) && 
                s == File.ReadAllText("TestRequest.xml"))), 
                    Times.AtLeastOnce(), 
                    "The LogData message was not called");
            
            //Now we receive the message
            MockReceiveStep receiveStep = new MockReceiveStep()
            {
                Url = "mock://localhost/OneWaySend",
                Encoding = "UTF-8"
            };

            // Calling Validate to start the receive server
            receiveStep.Validate(context);

            // Executing the step
            receiveStep.Execute(context);

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s) &&
                    s == "MockReceiveStep received a message with content"),
                It.Is<string>(s => !string.IsNullOrEmpty(s) &&
                    s == File.ReadAllText("TestRequest.xml"))),
                    Times.AtLeastOnce(), 
                    "The LogData message was not called");
        }

        internal static Mock<ILogger> CreateLoggerMock()
        {
            Mock<ILogger> loggerMock = new Mock<ILogger>();

            loggerMock.Setup(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))))
                .Verifiable();

            return loggerMock;
        }

    }
}
