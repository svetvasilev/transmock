using System;
using BizUnit;
using TransMock;
using TransMock.Integration.BizUnit;
using TransMock.Wcf.Adapter.Utils;

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
                Url = BizTalkTestsMockAddresses.BTS_OneWayReceive_FILE.ToString(),
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockReceiveStep()
            {
                Url = BizTalkTestsMockAddresses.BTS_OneWaySendFILE.ToString(),
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

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestOneWay_InboundMessageProperties_HappyPath()
        {
            var testCase = new BizUnit.Core.TestBuilder.TestCase();

            var inMsgStep = new MockSendStep()
            {
                Url = BizTalkTestsMockAddresses.BTS_OneWayReceive2_FILE.ToString(),
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            inMsgStep.MessageProperties
                .Add(
                    TransMock.Wcf.Adapter.Utils.BizTalkProperties.BTS.Operation,
                    "SomeTestOperation");

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockReceiveStep()
            {
                Url = BizTalkTestsMockAddresses.BTS_OneWaySendFILE.ToString(),
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

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestOneWayFlow_NewSyntax()
        {
            var epMock = new EndpointsMock<BizTalkTestsNewMockAddresses>();

            epMock.SetupReceive(
                r => r.BTS_OneWayReceive2_FILE);
            epMock.SetupSend(
                s => s.BTS_OneWaySendFILE);

            var messanger = epMock.CreateMessagingClient();

            messanger
                .Send(
                    r => r.BTS_OneWayReceive2_FILE,
                    "StartMessage.xml",
                    System.Text.Encoding.UTF8,
                    10,
                    beforeSendAction: ctx =>  ctx.DebugInfo("Sending messagein to OneWayReceive2_FILE") 
                )
                .Receive(
                    s => s.BTS_OneWaySendFILE,
                    beforeReceiveAction: ctx => ctx.DebugInfo("Receiving message from BTS_OneWaySendFILE"),
                    validator: v =>
                        {
                            Assert.IsTrue(v.Index == 0, "Message index is wrong!");
                            Assert.IsTrue(v.Message.Body.Length > 0, "Received message is empty");

                            return true;
                        }
                );
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
