using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TransMock.Tests.BTS2016
{
    /// <summary>
    /// Tests more complex scenarios
    /// </summary>
    [TestClass]    
    public class TestComplexMock
    {
        [TestMethod]
        [DeploymentItem(@"TestData\TestRequest.xml")]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestComplexFlow_1ParallelOperation_HappyPath()
        {
            var flowMock = new ComplexFlowMock();
            flowMock.RunComplexFlow1(
                "mock://localhost/Receive_Test_2Way", "mock://localhost/Send_Test_2Way");

            var integrationMock = new EndpointsMock<ComplexFlowMockAddresses>();

            integrationMock.
                SetupSendRequestAndReceiveResponse(
                    r => r.Send_Test_2Way)
                .SetupReceiveRequestAndSendResponse(
                    s => s.Receive_Test_2Way);

            var messageClient = integrationMock.CreateMessagingClient();

            messageClient.InParallel(
                 (m) => m.ReceiveRequestAndSendResponse(
                     s => s.Send_Test_2Way,
                     rs => new StaticFileResponseSelector()
                     {
                         FilePath = "TestResponse.xml"
                     },
                     expectedMessageCount: 2,
                     requestValidator: v =>
                     {
                         Assert.IsTrue(v.Message.Body.Length > 0, "The received request is empty!");

                         var xDoc = XDocument.Load(v.Message.BodyStream);

                         Assert.IsTrue(
                             xDoc.Root.Name.LocalName == "TestRequest",
                             "The contents of the request message is not the same");
                         return true;
                     }
                )                
            )
            .SendRequestAndReceiveResponse(
                r => r.Receive_Test_2Way,
                "TestRequest.xml",
                // timeoutInSeconds: 15,
                responseValidator: v =>
                {
                    Assert.IsTrue(v.Body.Length > 0, "The response message is empty");

                    var xDoc = XDocument.Load(v.BodyStream);

                    Assert.IsTrue(
                        xDoc.Root.Name.LocalName == "TestResponse",
                        "The contents of the response message is not the same");
                    return true;
                }
            )
            .VerifyParallel();

            flowMock.Clenup();

        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestRequest.xml")]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestComplexFlow_2ParallelOperations_HappyPath()
        {
            var flowMock = new ComplexFlowMock();
            flowMock.RunComplexFlow2(
                "mock://localhost/Receive_Test_2Way", 
                "mock://localhost/Send_Test_2Way",
                "mock://localhost/Send_Test_2Way2");

            var integrationMock = new EndpointsMock<ComplexFlowMockAddresses>();

            integrationMock
                .SetupSendRequestAndReceiveResponse(
                    r => r.Send_Test_2Way)
                .SetupSendRequestAndReceiveResponse(
                    r => r.Send_Test_2Way2)
                .SetupReceiveRequestAndSendResponse(
                    s => s.Receive_Test_2Way);

            var messageClient = integrationMock.CreateMessagingClient();

            messageClient.InParallel(
                 (m) => m.ReceiveRequestAndSendResponse(
                     s => s.Send_Test_2Way,
                     rs => new StaticFileResponseSelector()
                     {
                         FilePath = "TestResponse.xml"
                     },
                     //expectedMessageCount: 2,
                     requestValidator: v =>
                     {
                         Assert.IsTrue(v.Message.Body.Length > 0, "The received request is empty!");

                         var xDoc = XDocument.Load(v.Message.BodyStream);

                         Assert.IsTrue(
                             xDoc.Root.Name.LocalName == "TestRequest",
                             "The contents of the request message is not the same");
                         return true;
                     }
                )
                ,(m) => m.ReceiveRequestAndSendResponse(
                      s => s.Send_Test_2Way2,
                      rs => new StaticFileResponseSelector()
                      {
                          FilePath = "TestResponse.xml"
                      },
                      expectedMessageCount: 2,
                      requestValidator: v =>
                      {
                          Assert.IsTrue(v.Message.Body.Length > 0, "The received request is empty!");

                          var xDoc = XDocument.Load(v.Message.BodyStream);

                          Assert.IsTrue(
                              xDoc.Root.Name.LocalName == "TestRequest",
                              "The contents of the request message is not the same");
                          return true;
                      }
                 )
            )
            .SendRequestAndReceiveResponse(
                r => r.Receive_Test_2Way,
                "TestRequest.xml",
                // timeoutInSeconds: 15,
                responseValidator: v =>
                {
                    Assert.IsTrue(v.Body.Length > 0, "The response message is empty");

                    var xDoc = XDocument.Load(v.BodyStream);

                    Assert.IsTrue(
                        xDoc.Root.Name.LocalName == "TestResponse",
                        "The contents of the response message is not the same");
                    return true;
                }
            )
            .VerifyParallel();

            flowMock.Clenup();

        }
    }
   
}
