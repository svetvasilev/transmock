using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using TransMock.Communication.NamedPipes;

namespace TransMock.Tests.BTS2016
{
    public class ComplexFlowMock
    {
        IAsyncStreamingServer mockServer;

        // IStreamingClient mockClient;

        Queue<AsyncReadEventArgs> inboundMesageQueue;

        object receiveSync = new object();        

        int connectionId = 0;

        public void RunComplexFlow1(
            string receiveURL,
            string sendUrl)
        {
            //taskRunner = Task.Run(
            ThreadPool.QueueUserWorkItem(
                cb =>
                {
                    Console.WriteLine("Starting execution of flow 1");

                    InitServer(receiveURL);

                    MockMessage requestMessage;

                    // First we wait for a receive message
                    lock (this.receiveSync)
                    {
                        bool success = Monitor.Wait(receiveSync, 20000);

                        if (!success)
                        {
                            Console.Out.Write("The receive operation timed out");

                            throw new TimeoutException("The receive operation timed out");
                        }

                        var receivedMessageEvent = inboundMesageQueue.Dequeue();
                        connectionId = receivedMessageEvent.ConnectionId;

                        requestMessage = receivedMessageEvent.Message;
                    }

                    MockMessage responseMessage = null;
                    // Then we send a request and wait for a response - first time
                    using (var mockClient = InitClient(sendUrl))
                    {
                        mockClient.WriteMessage(requestMessage);
                        // We wait for the response
                        responseMessage = mockClient.ReadMessage();

                        if (responseMessage == null)
                        {
                            throw new ApplicationException("First response message was null!");

                        }

                        Console.WriteLine("First response message received from server.");
                        // Disconnecting as with the actual implementation
                        mockClient.Disconnect();
                    }

                    // Second pass with request/response
                    using (var mockClient = InitClient(sendUrl))
                    {

                        mockClient.WriteMessage(requestMessage);

                        responseMessage = mockClient.ReadMessage();

                        if (responseMessage == null)
                        {
                            throw new ApplicationException("Second response message was null!");

                        }

                        Console.WriteLine("Second response message received from server.");
                        // Disconnecting as with the actual implementation
                        mockClient.Disconnect();
                    }

                    // Sending the second response back to initial caller
                    mockServer.WriteMessage(
                        connectionId,
                        responseMessage);

                });

            //taskRunner.Wait();
        }

    
        public void Clenup()
        {
            
        }

        private void InitServer(string receiveURL)
        {
            inboundMesageQueue = new Queue<AsyncReadEventArgs>(3);

            mockServer = new StreamingNamedPipeServer(new Uri(receiveURL).AbsolutePath);
            mockServer.ReadCompleted += MockServer_ReadCompleted;

            mockServer.Start();

            Console.WriteLine($"ComplexFlow server started listening on {receiveURL}");
        }

        private void MockServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            lock (this.receiveSync)
            {
                inboundMesageQueue.Enqueue(e);

                Monitor.Pulse(this.receiveSync);
            }
        }

        private void CleanupServer()
        {
            inboundMesageQueue.Clear();

            mockServer.Stop();
            mockServer = null;

            Console.WriteLine($"ComplexFlow server stopped successfully");
        }

        public StreamingNamedPipeClient InitClient(string URL)
        {
            var mockClient = new StreamingNamedPipeClient(new Uri(URL));

            bool connected = mockClient.Connect(10000);

            if (!connected)
            {
                Console.Out.Write("The receive operation timed out");

                throw new ApplicationException($"The client did not manage to connect to the receiving party at {URL}");
            }

            return mockClient;
        }
    }
}
