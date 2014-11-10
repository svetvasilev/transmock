using System;
using System.IO.Pipes;
using System.Collections;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.XLANGs.Core;
using Microsoft.XLANGs.BaseTypes;

using Moq;

namespace TransMock.TestUtils.BizTalk.Tests
{   
    [TestClass]
    public class TestTestableDynamicSendPortHelper
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
            //Mock the port
            var portMock = new Mock<TestablePortBase>();
            portMock.Setup(p => p.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));
            //PortInfo portInfo = new PortInfo(null, null, Polarity.implements, true, Guid.NewGuid(), null);

            var portInfo = CreateValidPortInfo();
            portMock.SetupGet<PortInfo>(p => p.PortInfo).Returns(portInfo);
            
            //Mock the XLANGsMessage
            var messageMock = new Mock<XLANGMessage>();
            messageMock.Setup(m => m.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));
            
            //Mock the ports returned by the Orchestration
            var portInfoMock = new Mock<ILogicalPortInfo>();
            portInfoMock.SetupGet<string>(pi => pi.Name).Returns("TestPort");
            portInfoMock.SetupGet<string>(pi => pi.TypeName).Returns(typeof(TestablePortType).AssemblyQualifiedName);
            portInfoMock.SetupGet<bool>(pi => pi.IsDynamic).Returns(true);
            
            ILogicalPortInfo[] logicalPorts = new ILogicalPortInfo[1];
            logicalPorts[0] = portInfoMock.Object;

            var orchestrationMock = new Mock<IOrchestrationInfo>();
            orchestrationMock.SetupGet<IEnumerable<ILogicalPortInfo>>( o => o.LogicalPorts).Returns(logicalPorts);
            //Injecting the Orchestration info instance
            DynamicSendPortMockHelper.OrchestrationInfo = orchestrationMock.Object;

            var portMockInstance = portMock.Object as PortBase;
            var messageMockInstance = messageMock.Object;

            bool mocked = DynamicSendPortMockHelper
                .MockDynamicSendPort(portMockInstance, messageMockInstance);

            Assert.IsTrue(mocked, "The port was not mocked");

            //Verify the orchestration info mock
            orchestrationMock.VerifyGet<IEnumerable<ILogicalPortInfo>>(p => p.LogicalPorts, 
                Times.Once(), "The LogicalPorts property was not colled");
            
            //Verify the port mock
            portMock.Verify(p => p.SetPropertyValue(typeof(Address), 
                It.Is<object>(v => v.ToString() == "mock://localhost/TestPort")), Times.Once(), "The address property is wrong");
            portMock.Verify(p => p.SetPropertyValue(typeof(TransportType), 
                It.Is<object>(v => v.ToString() == "WCF-Custom")), Times.Once(), "The transport type property is wrong");

            //Verify the message mock
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingType),
                It.Is<object>(s => s.ToString() == "mockBinding")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingConfiguration),
                It.Is<object>(s => s.ToString() == @"<binding name=""mockBinding"" Encoding=""UTF-8"" />")), 
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                It.Is<object>(s => s.ToString() == @"<behavior name=""EndPointBehavior"" />")), 
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundBodyLocation),
                It.Is<object>(s => s.ToString() == "Template")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                It.Is<object>(s => s.ToString() == @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>")), 
                Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseBodyPath")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyPathExpression),
                It.Is<object>(s => s.ToString() == @"/MessageContent")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundNodeEncoding),
                It.Is<object>(s => s.ToString() == @"base64")), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.PropagateFaultMessage),
                It.Is<object>(s => Convert.ToInt32(s) == -1)), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.UseSSO),
                It.Is<object>(s => Convert.ToInt32(s) == 0)), Times.Once(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EnableTransaction),
                It.Is<object>(s => Convert.ToInt32(s) == 0)), Times.Once(), "The binding type in the message context is wrong");
        }

        [TestMethod]
        public void TestMockDynamicSendPort_InvalidPort()
        {
            //Mock the port
            var portMock = new Mock<TestablePortBase>();
            portMock.Setup(p => p.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));
            //PortInfo portInfo = new PortInfo(null, null, Polarity.implements, true, Guid.NewGuid(), null);

            var portInfo = CreateValidPortInfo();
            portMock.SetupGet<PortInfo>(p => p.PortInfo).Returns(portInfo);

            //Mock the XLANGsMessage
            var messageMock = new Mock<XLANGMessage>();
            messageMock.Setup(m => m.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));

            //Mock the ports returned by the Orchestration
            var portInfoMock = new Mock<ILogicalPortInfo>();
            portInfoMock.SetupGet<string>(pi => pi.Name).Returns("TestPort");
            portInfoMock.SetupGet<string>(pi => pi.TypeName).Returns("InvalidPortType");
            portInfoMock.SetupGet<bool>(pi => pi.IsDynamic).Returns(true);

            ILogicalPortInfo[] logicalPorts = new ILogicalPortInfo[1];
            logicalPorts[0] = portInfoMock.Object;

            var orchestrationMock = new Mock<IOrchestrationInfo>();
            orchestrationMock.SetupGet<IEnumerable<ILogicalPortInfo>>(o => o.LogicalPorts).Returns(logicalPorts);
            //Injecting the Orchestration info instance
            DynamicSendPortMockHelper.OrchestrationInfo = orchestrationMock.Object;

            var portMockInstance = portMock.Object as PortBase;
            var messageMockInstance = messageMock.Object;

            bool mocked = DynamicSendPortMockHelper
                .MockDynamicSendPort(portMockInstance, messageMockInstance);

            Assert.IsFalse(mocked, "The port was not mocked");

            //Verify the orchestration info mock
            orchestrationMock.VerifyGet<IEnumerable<ILogicalPortInfo>>(p => p.LogicalPorts,
                Times.Once(), "The LogicalPorts property was not colled");

            //Verify the port mock
            portMock.Verify(p => p.SetPropertyValue(typeof(Address),
                It.Is<object>(v => v.ToString() == "mock://localhost/TestPort")), Times.Never(), "The address property is wrong");
            portMock.Verify(p => p.SetPropertyValue(typeof(TransportType),
                It.Is<object>(v => v.ToString() == "WCF-Custom")), Times.Never(), "The transport type property is wrong");

            //Verify the message mock
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingType),
                It.Is<object>(s => s.ToString() == "mockBinding")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingConfiguration),
                It.Is<object>(s => s.ToString() == @"<binding name=""mockBinding"" Encoding=""UTF-8"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                It.Is<object>(s => s.ToString() == @"<behavior name=""EndPointBehavior"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundBodyLocation),
                It.Is<object>(s => s.ToString() == "Template")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                It.Is<object>(s => s.ToString() == @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseBodyPath")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyPathExpression),
                It.Is<object>(s => s.ToString() == @"/MessageContent")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundNodeEncoding),
                It.Is<object>(s => s.ToString() == @"base64")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.PropagateFaultMessage),
                It.Is<object>(s => Convert.ToInt32(s) == -1)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.UseSSO),
                It.Is<object>(s => Convert.ToInt32(s) == 0)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EnableTransaction),
                It.Is<object>(s => Convert.ToInt32(s) == 0)), Times.Never(), "The binding type in the message context is wrong");
        }

        [TestMethod]
        public void TestMockDynamicSendPort_NoBeacon()
        {
            //Stop the beacon server
            ClosePipeServer();

            //Mock the port
            var portMock = new Mock<TestablePortBase>();
            portMock.Setup(p => p.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));
            //PortInfo portInfo = new PortInfo(null, null, Polarity.implements, true, Guid.NewGuid(), null);

            var portInfo = CreateValidPortInfo();
            portMock.SetupGet<PortInfo>(p => p.PortInfo).Returns(portInfo);

            //Mock the XLANGsMessage
            var messageMock = new Mock<XLANGMessage>();
            messageMock.Setup(m => m.SetPropertyValue(It.IsAny<Type>(), It.IsAny<object>()));

            //Mock the ports returned by the Orchestration
            var portInfoMock = new Mock<ILogicalPortInfo>();
            portInfoMock.SetupGet<string>(pi => pi.Name).Returns("TestPort");
            portInfoMock.SetupGet<string>(pi => pi.TypeName).Returns(typeof(TestablePortType).AssemblyQualifiedName);
            portInfoMock.SetupGet<bool>(pi => pi.IsDynamic).Returns(true);

            ILogicalPortInfo[] logicalPorts = new ILogicalPortInfo[1];
            logicalPorts[0] = portInfoMock.Object;

            var orchestrationMock = new Mock<IOrchestrationInfo>();
            orchestrationMock.SetupGet<IEnumerable<ILogicalPortInfo>>(o => o.LogicalPorts).Returns(logicalPorts);
            //Injecting the Orchestration info instance
            DynamicSendPortMockHelper.OrchestrationInfo = orchestrationMock.Object;

            var portMockInstance = portMock.Object as PortBase;
            var messageMockInstance = messageMock.Object;

            bool mocked = DynamicSendPortMockHelper
                .MockDynamicSendPort(portMockInstance, messageMockInstance);

            Assert.IsFalse(mocked, "The port was mocked");

            //Verify the orchestration info mock
            orchestrationMock.VerifyGet<IEnumerable<ILogicalPortInfo>>(p => p.LogicalPorts,
                Times.Never(), "The LogicalPorts property was not colled");

            //Verify the port mock
            portMock.Verify(p => p.SetPropertyValue(typeof(Address),
                It.Is<object>(v => v.ToString() == "mock://localhost/TestPort")), Times.Never(), "The address property is wrong");
            portMock.Verify(p => p.SetPropertyValue(typeof(TransportType),
                It.Is<object>(v => v.ToString() == "WCF-Custom")), Times.Never(), "The transport type property is wrong");

            //Verify the message mock
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingType),
                It.Is<object>(s => s.ToString() == "mockBinding")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.BindingConfiguration),
                It.Is<object>(s => s.ToString() == @"<binding name=""mockBinding"" Encoding=""UTF-8"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EndpointBehaviorConfiguration),
                It.Is<object>(s => s.ToString() == @"<behavior name=""EndPointBehavior"" />")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundBodyLocation),
                It.Is<object>(s => s.ToString() == "Template")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.OutboundXmlTemplate),
                It.Is<object>(s => s.ToString() == @"<bts-msg-body xmlns=""http://www.microsoft.com/schemas/bts2007"" encoding=""base64""/>")),
                Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyLocation),
                It.Is<object>(s => s.ToString() == "UseBodyPath")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundBodyPathExpression),
                It.Is<object>(s => s.ToString() == @"/MessageContent")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.InboundNodeEncoding),
                It.Is<object>(s => s.ToString() == @"base64")), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.PropagateFaultMessage),
                It.Is<object>(s => Convert.ToInt32(s) == -1)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.UseSSO),
                It.Is<object>(s => Convert.ToInt32(s) == 0)), Times.Never(), "The binding type in the message context is wrong");
            messageMock.Verify(m => m.SetPropertyValue(typeof(WCF.EnableTransaction),
                It.Is<object>(s => Convert.ToInt32(s) == 0)), Times.Never(), "The binding type in the message context is wrong");
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

        private PortInfo CreateValidPortInfo()
        {
            var portInfo = new PortInfo(
                new OperationInfo[] {
                    new OperationInfo("SendTestRequest",
                    System.Web.Services.Description.OperationFlow.OneWay,
                    typeof(TestablePortType), 
                    typeof(TestableRequest),
                    null, 
                    null, 
                    null)},
                    typeof(TestableOrchestration).GetField("SendPort"), Polarity.implements, true, Guid.NewGuid(), null);

            return portInfo;
        }
    }

    public class TestablePortBase : PortBase
    {
        public TestablePortBase() : base(1)
        {
                
        }

        public override PortBinding Binding
        {
            get { throw new NotImplementedException(); }
        }

        public override object BindingInfo
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override PortBase Clone()
        {
            throw new NotImplementedException();
        }

        public override void Close(Context cxt, Segment seg)
        {
            throw new NotImplementedException();
        }

        public override void CreateSubscription(XlangStore store, int operationId, PredicateGroup[] preds, Guid convoyId, Subscription sub)
        {
            throw new NotImplementedException();
        }

        public override void DestroySubscription(Guid subscriptionID)
        {
            throw new NotImplementedException();
        }

        public override void DisableSubscription(Guid subscriptionID)
        {
            throw new NotImplementedException();
        }

        public override object GetPropertyValue(Type prop)
        {
            throw new NotImplementedException();
        }

        public override PortInfo PortInfo
        {
            get { throw new NotImplementedException(); }
        }

        public override void ReceiveMessage(int iOperation, Envelope env, XLANGMessage msg, Correlation[] initCorrelation, Context cxt, Segment s)
        {
            throw new NotImplementedException();
        }

        public override void SendFault(int iOperation, int iFaultType, XLANGMessage msg, Correlation[] initCorrelations, Correlation[] followCorrelations, Context cxt, Segment seg, ActivityFlags flags)
        {
            throw new NotImplementedException();
        }

        public override void SendMessage(int iOperation, XLANGMessage msg, Correlation[] initCorrelations, Correlation[] followCorrelations, out SubscriptionWrapper subscriptionWrapper, Context cxt, Segment seg, ActivityFlags flags)
        {
            throw new NotImplementedException();
        }

        public override void SendMessage(int iOperation, XLANGMessage msg, Correlation[] initCorrelations, Correlation[] followCorrelations, Context cxt, Segment seg, ActivityFlags flags)
        {
            throw new NotImplementedException();
        }

        public override void SetPropertyValue(Type prop, object val, Context ctx)
        {
            throw new NotImplementedException();
        }

        public override void SetPropertyValue(Type prop, object val)
        {
            throw new NotImplementedException();
        }

        public override void VerifyTransport(Envelope env, int operationId, Context ctx)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TestableOrchestration
    {
        public TestablePortType SendPort;
    }

    /// <summary>
    /// 
    /// </summary>
    public class TestablePortType
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class TestableRequest
    {
    }
}
