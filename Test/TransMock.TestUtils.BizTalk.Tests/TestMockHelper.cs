using System;
using System.IO.Pipes;
using System.Collections;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.XLANGs.BaseTypes;

using Moq;

namespace TransMock.TestUtils.BizTalk.Tests
{   
    [TestClass]
    public class TestMockHelper
    {
        private NamedPipeServerStream beaconServer;
        private IAsyncResult connectResult;

        [TestInitialize]
        public void TestInit()
        {
            CreatePipeServer();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ClosePipeServer();
        }

        [TestMethod]
        public void TestMockDynamicSendPort()
        {   
            //Mock the XLANGsMessage
            var messageMock = new Mock<XLANGMessage>();
            messageMock.Setup(m => m.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));
            
            //var portMockInstance = portMock.Object as PortBase;
            var messageMockInstance = messageMock.Object;

            MockTransportConfig mockConfig = MockHelper
                .MockDynamicSendPort("TestPort", messageMockInstance);

            Assert.IsNotNull(mockConfig, "The port was not mocked");
            
            //Verify the port config
            Assert.AreEqual("mock://localhost/DynamicTestPort", mockConfig.Address, "The address property is wrong");
            Assert.AreEqual("WCF-Custom", mockConfig.TransportType, "The address property is wrong");            

            //Verify the message mock
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingType),
                It.Is<object>(s => s.ToString() == "mockBinding")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingConfiguration),
                It.Is<object>(s => s.ToString() == @"<binding name=""mockBinding"" Encoding=""UTF-8"" />")), 
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                It.Is<object>(s => s.ToString() == @"<behavior name=""EndpointBehavior"" />")), 
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseTemplate")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                It.Is<object>(s => s.ToString() == @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>")), 
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseBodyPath")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyPathExpression),
                It.Is<object>(s => s.ToString() == @"/MessageContent")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundNodeEncoding),
                It.Is<object>(s => s.ToString() == @"Base64")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.PropagateFaultMessage),
                It.Is<object>(s => Convert.ToBoolean(s) == true)), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.UseSSO),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EnableTransaction),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Once(), "The binding type in the message context is wrong");
        }

        [TestMethod]
        public void TestMockDynamicSendPort_CustomBehavior()
        {
            //Mock the XLANGsMessage
            var messageMock = new Mock<XLANGMessage>();
            messageMock.Setup(m => m.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));

            //var portMockInstance = portMock.Object as PortBase;
            var messageMockInstance = messageMock.Object;

            MockTransportConfig mockConfig = MockHelper
                .MockDynamicSendPort("TestPort",
                @"<behavior Name=""TestBehavior""><Property>Some property value</Property></behavior>", 
                messageMockInstance);

            Assert.IsNotNull(mockConfig, "The port was not mocked");

            //Verify the port config
            Assert.AreEqual("mock://localhost/DynamicTestPort", mockConfig.Address, "The address property is wrong");
            Assert.AreEqual("WCF-Custom", mockConfig.TransportType, "The address property is wrong");

            //Verify the message mock
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingType),
                It.Is<object>(s => s.ToString() == "mockBinding")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingConfiguration),
                It.Is<object>(s => s.ToString() == @"<binding name=""mockBinding"" Encoding=""UTF-8"" />")),
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                It.Is<object>(s => s.ToString() == @"<behavior Name=""TestBehavior""><Property>Some property value</Property></behavior>")),
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseTemplate")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                It.Is<object>(s => s.ToString() == @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>")),
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseBodyPath")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyPathExpression),
                It.Is<object>(s => s.ToString() == @"/MessageContent")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundNodeEncoding),
                It.Is<object>(s => s.ToString() == @"Base64")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.PropagateFaultMessage),
                It.Is<object>(s => Convert.ToBoolean(s) == true)), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.UseSSO),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EnableTransaction),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Once(), "The binding type in the message context is wrong");
        }

        [TestMethod]
        public void TestMockDynamicSendPort_NoBeacon()
        {
            //Stop the beacon server
            ClosePipeServer();

            //Mock the XLANGsMessage
            var messageMock = new Mock<XLANGMessage>();
            messageMock.Setup(m => m.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));

            //var portMockInstance = portMock.Object as PortBase;
            var messageMockInstance = messageMock.Object;

            MockTransportConfig mockConfig = MockHelper
                .MockDynamicSendPort("TestPort",
                @"<behavior Name=""TestBehavior""><Property>Some property value</Property></behavior>",
                messageMockInstance);

            Assert.IsNull(mockConfig, "The port was not mocked");           

            //Verify the message mock
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingType),
                It.Is<object>(s => s.ToString() == "mockBinding")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingConfiguration),
                It.Is<object>(s => s.ToString() == @"<binding name=""mockBinding"" Encoding=""UTF-8"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                It.Is<object>(s => s.ToString() == @"<behavior name=""EndpointBehavior"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseTemplate")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                It.Is<object>(s => s.ToString() == @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseBodyPath")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyPathExpression),
                It.Is<object>(s => s.ToString() == @"/MessageContent")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundNodeEncoding),
                It.Is<object>(s => s.ToString() == @"Base64")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.PropagateFaultMessage),
                It.Is<object>(s => Convert.ToBoolean(s) == true)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.UseSSO),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EnableTransaction),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Never(), "The binding type in the message context is wrong");
        }

        [TestMethod]
        public void TestMockDynamicSendPort_CustomBehavior_NoBeacon()
        {
            //Stop the beacon server
            ClosePipeServer();

            //Mock the XLANGsMessage
            var messageMock = new Mock<XLANGMessage>();
            messageMock.Setup(m => m.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));

            //var portMockInstance = portMock.Object as PortBase;
            var messageMockInstance = messageMock.Object;

            MockTransportConfig mockConfig = MockHelper
                .MockDynamicSendPort("TestPort", messageMockInstance);

            Assert.IsNull(mockConfig, "The port was not mocked");

            //Verify the message mock
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingType),
                It.Is<object>(s => s.ToString() == "mockBinding")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingConfiguration),
                It.Is<object>(s => s.ToString() == @"<binding name=""mockBinding"" Encoding=""UTF-8"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                It.Is<object>(s => s.ToString() == @"<behavior name=""EndpointBehavior"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseTemplate")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                It.Is<object>(s => s.ToString() == @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseBodyPath")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyPathExpression),
                It.Is<object>(s => s.ToString() == @"/MessageContent")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundNodeEncoding),
                It.Is<object>(s => s.ToString() == @"Base64")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.PropagateFaultMessage),
                It.Is<object>(s => Convert.ToBoolean(s) == true)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.UseSSO),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EnableTransaction),
                It.Is<object>(s => Convert.ToBoolean(s) == false)), Times.Never(), "The binding type in the message context is wrong");
        }

        private void CreatePipeServer()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("Users", 
                PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite,
                System.Security.AccessControl.AccessControlType.Allow));

            beaconServer = new NamedPipeServerStream("TransMockBeacon",
                PipeDirection.InOut,
                10,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous,
                8,8, ps);

            connectResult = beaconServer.BeginWaitForConnection(cb => ClientConnected(cb), beaconServer);
        }

        private void ClientConnected(IAsyncResult cb)
        {
            try
            {               
                var server = cb.AsyncState as NamedPipeServerStream;

                server.EndWaitForConnection(cb);

                server.Disconnect(); 
            }
            catch (Exception)
            {
             
            }                       
        }

        private void ClosePipeServer()
        {
            try
            {
                if (beaconServer != null)
                {
                    beaconServer.Close();

                    beaconServer = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("testCleanup exception: " + ex.Message);
            }
        }

    }    
}
