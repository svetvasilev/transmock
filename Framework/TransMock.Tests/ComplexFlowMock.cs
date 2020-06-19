using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TransMock.Tests.BTS2016
{
    public class ComplexFlowMock
    {
        IStreamingServerAsync mockServer;

        // IStreamingClient mockClient;

        ConcurrentQueue<AsyncReadEventArgs> inboundMesageQueue;

        Task receiverTask;

        ManualResetEventSlim syncEvent;      

        int connectionId = 0;

        public void RunComplexFlow1(
            string receiveURL,
            string sendUrl)
        {
            syncEvent = new ManualResetEventSlim(false);

            receiverTask = Task.Run(
            //    .ContinueWith(
            //ThreadPool.QueueUserWorkItem(
                async () =>              
                {
                    Console.WriteLine("Starting execution of flow 1");
                    

                    await InitServerAsync(receiveURL)
                        .ConfigureAwait(false);

                    MockMessage requestMessage;

                    bool success = syncEvent.Wait(20000);

                    if (!success)
                    {
                        Console.Out.Write("The receive operation timed out");

                        throw new TimeoutException("The receive operation timed out");
                    }

                    AsyncReadEventArgs receivedMessageEvent;
                    inboundMesageQueue.TryDequeue(out receivedMessageEvent);

                    connectionId = receivedMessageEvent.ConnectionId;
                    requestMessage = receivedMessageEvent.Message;

                    syncEvent.Reset();

                    MockMessage responseMessage = null;
                    // Then we send a request and wait for a response - first time
                    using (var mockClient = await InitClientAsync(sendUrl))
                    {
                        await mockClient.WriteMessageAsync(requestMessage)
                            .ConfigureAwait(false);
                        // We wait for the response
                        responseMessage = await mockClient.ReadMessageAsync()
                            .ConfigureAwait(false);

                        if (responseMessage == null)
                        {
                            throw new ApplicationException("First response message was null!");

                        }

                        Console.WriteLine("First response message received from server.");
                        // Disconnecting as with the actual implementation
                        await mockClient.DisconnectAsync()
                            .ConfigureAwait(false);
                    }

                    // Second pass with request/response
                    using (var mockClient = await InitClientAsync(sendUrl))
                    {

                        await mockClient.WriteMessageAsync(requestMessage)
                            .ConfigureAwait(false);

                        responseMessage = await mockClient.ReadMessageAsync()
                            .ConfigureAwait(false);

                        if (responseMessage == null)
                        {
                            throw new ApplicationException("Second response message was null!");
                        }

                        Console.WriteLine("Second response message received from server.");
                        // Disconnecting as with the actual implementation
                        await mockClient.DisconnectAsync()
                            .ConfigureAwait(false);
                    }

                    // Sending the second response back to initial caller
                    await mockServer.WriteMessageAsync(
                        connectionId,
                        responseMessage)
                        .ConfigureAwait(false);

                });

            // Initiating the wait just to kick start the task
            //receiverTask.Start();
        }

    
        public void Clenup()
        {
            
        }

        private async Task InitServerAsync(string receiveURL)
        {
            inboundMesageQueue = new ConcurrentQueue<AsyncReadEventArgs>();

            mockServer = new StreamingNamedPipeServerAsync(new Uri(receiveURL).AbsolutePath);
            mockServer.ReadCompleted += MockServer_ReadCompleted;

            await mockServer.StartAsync();

            Console.WriteLine($"ComplexFlow server started listening on {receiveURL}");
        }

        private void MockServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            
            inboundMesageQueue.Enqueue(e);

            syncEvent.Set();

        }

        private async void CleanupServer()
        {
            inboundMesageQueue = null;

            await mockServer.StopAsync();
            mockServer = null;

            Console.WriteLine($"ComplexFlow server stopped successfully");
        }

        public async Task<StreamingNamedPipeClientAsync> InitClientAsync(string URL)
        {
            var mockClient = new StreamingNamedPipeClientAsync(new Uri(URL));

            bool connected = await mockClient.ConnectAsync(10000);

            if (!connected)
            {
                Console.Out.Write("The receive operation timed out");

                throw new ApplicationException($"The client did not manage to connect to the receiving party at {URL}");
            }

            return mockClient;
        }
    }
}
