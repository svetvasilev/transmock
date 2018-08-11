using System;
using BizUnit;
using TransMock.Integration.BizUnit;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransMock.Communication.NamedPipes;

namespace BizTalkTests.IntegrationTests
{
    [TestClass]
    public class TestStaticPorts
    {
        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestOneWay_HappyPath()
        {
            var testCase = new BizUnit.Core.TestBuilder.TestCase();

            var inMsgStep = new MockSendStep()
            {
                Url = BizTalkTestsMockAddresses.BTS_OneWayStaticReceive_FILE,
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockReceiveStep()
            {
                Url = BizTalkTestsMockAddresses.BTS_OneWaySendFILE,
                Encoding = "UTF-8",
                Timeout = 10
            };

            var outMsgValidationStep = new TransMock.Integration.BizUnit.Validation.LambdaValidationStep()
            {
                MessageValidationCallback = (message) => ValidateOutMessage(message)
            };

            outMsgStep.SubSteps.Add(outMsgValidationStep);
            testCase.ExecutionSteps.Add(outMsgStep);

            BizUnit.Core.TestRunner testRunner = new BizUnit.Core.TestRunner(testCase);

            testRunner.Run();
        }

        private bool ValidateOutMessage(MockMessage message)
        {
            Assert.IsTrue(
                message.Properties.Count > 0,
                "The number of properties in the received message is not as expected");

            Assert.AreEqual(
                "mockBinding", 
                message.Properties["WCF.BindingType"], 
                "The WCF.BindingName property is incorrect!");

            return true;
        }
    }
}
