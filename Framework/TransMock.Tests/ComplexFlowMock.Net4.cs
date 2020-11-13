using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

//using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TransMock.Tests.BTS2016
{
#if NET40 || NET45 || NET451
    public class ComplexFlowMock
    {
        IAsyncStreamingServer mockServer;

        // IStreamingClient mockClient;

        ConcurrentDictionary<int, AsyncReadEventArgs> inboundMesageQueue;

        Task receiverTask;

        SemaphoreSlim syncEvent;      

        int connectionId = 0;

        /// <summary>
        /// Complex flow 1:
        ///   - 1 2-way receive location
        ///   - 1 2-send port that performs 2 consecutive calls
        /// </summary>
        /// <param name="receiveURL">The URL of the receive location</param>
        /// <param name="sendUrl">The URL of the send port</param>
        public void RunComplexFlow1(
            string receiveURL,
            string sendUrl)
        {
            syncEvent = new SemaphoreSlim(0,10);

            receiverTask = Task.Factory.StartNew(
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
                        Console.Out.WriteLine("The receive operation timed out");

                        throw new TimeoutException("The receive operation timed out");
                    }

                    AsyncReadEventArgs receivedMessageEvent;
                    while (!inboundMesageQueue.TryRemove(connectionId, out receivedMessageEvent))
                    {
                        Console.Out.WriteLine($"Did not managed to fetch message for connection {connectionId}. Trying again");
                    }

                    //connectionId = receivedMessageEvent.ConnectionId;
                    requestMessage = receivedMessageEvent.Message;

                    // syncEvent.Release();

                    MockMessage responseMessage = null;
                    // Then we send a request and wait for a response - first time
                    using (var mockClient = await InitClientAsync(sendUrl))
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
                    using (var mockClient = await InitClientAsync(sendUrl))
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

                    // Finally we stop the server
                    mockServer.Stop();

                });            
        }

        /// <summary>
        /// Complexflow 2:
        /// - 1 2-way receive location
        /// - 2 2-way send ports, pointing to different URLs, and running in parallel, where
        ///     - 1 port receives 1 requests
        ///     - 1 port receives 2 consecutive requests
        /// </summary>
        /// <param name="receiveURL"></param>
        /// <param name="sendUrl"></param>
        public void RunComplexFlow2(
            string receiveURL,
            string sendUrl1,
            string sendUrl2,
            string request2Path)
        {
            syncEvent = new SemaphoreSlim(0,10);

            receiverTask = Task.Factory.StartNew(
            //    .ContinueWith(
            //ThreadPool.QueueUserWorkItem(
                async () =>
                {
                    Console.WriteLine("Starting execution of flow 2");


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
                    while (!inboundMesageQueue.TryRemove(connectionId, out receivedMessageEvent))
                    {
                        Console.Out.WriteLine($"Did not managed to fetch message for connection {connectionId}. Trying again");
                    }

                    connectionId = receivedMessageEvent.ConnectionId;
                    requestMessage = receivedMessageEvent.Message;

                    // syncEvent.Release();

                    var parallelTask1 = Task.Factory.StartNew(
                        async () =>
                    {
                        Console.WriteLine("Starting execution of parallel task 1");

                        MockMessage responseMessage = null;
                        // Then we send a request and wait for a response - first time
                        using (var mockClient = await InitClientAsync(sendUrl1))
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

                        return responseMessage;
                    });

                    var parallelTask2 = Task.Factory.StartNew(
                        async () =>
                    {
                        Console.WriteLine("Starting execution of parallel task 2");

                        MockMessage secondRequest = new MockMessage(request2Path, Encoding.UTF8);

                        MockMessage responseMessage = null;
                        // Doing 2 rounds of execution to the second endpont
                        for (int i = 0; i < 2; i++)
                        {
                            Console.WriteLine($"Receiving request {i}");
                            // Second pass with request/response
                            using (var mockClient = await InitClientAsync(sendUrl2))
                            {

                                mockClient.WriteMessage(secondRequest);

                                responseMessage = mockClient.ReadMessage();

                                if (responseMessage == null)
                                {
                                    throw new ApplicationException("Second response message was null!");
                                }

                                Console.WriteLine($"Response message {i+1} received from server.");
                                // Disconnecting as with the actual implementation
                                 mockClient.Disconnect();
                            }
                        }
                    });

                    Task.WaitAll(parallelTask1, parallelTask2);

                    if (parallelTask1.IsFaulted)
                        throw parallelTask1.Exception;

                    if (parallelTask2.IsFaulted)
                        throw parallelTask2.Exception;
                    // Sending the first response back to initial caller
                    mockServer.WriteMessage(
                        connectionId,
                        parallelTask1.Result.Result);

                    // Finally we stop the server
                    mockServer.Stop();
                    
                });
            }


        public void Clenup()
        {
            
        }

        private async Task InitServerAsync(string receiveURL)
        {
            inboundMesageQueue = new ConcurrentDictionary<int, AsyncReadEventArgs>();

            mockServer = new StreamingNamedPipeServer(new Uri(receiveURL).AbsolutePath);
            mockServer.ReadCompleted += MockServer_ReadCompleted;

            await Task.Factory.StartNew(
                () => mockServer.Start()
            );

            Console.WriteLine($"ComplexFlow server started listening on {receiveURL}");
        }

        private void MockServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            connectionId = e.ConnectionId;
            inboundMesageQueue.TryAdd(e.ConnectionId, e);

            syncEvent.Release();

        }

        private async void CleanupServer()
        {
            inboundMesageQueue = null;

            await Task.Factory.StartNew(
                () => mockServer.Stop()
            );

            mockServer = null;

            Console.WriteLine($"ComplexFlow server stopped successfully");
        }

        public async Task<IStreamingClient> InitClientAsync(string URL)
        {
            var mockClient = new StreamingNamedPipeClient(new Uri(URL));

            bool connected = await Task.Factory.StartNew(
                () => mockClient.Connect(10000)
             );
            
            if (!connected)
            {
                Console.Out.Write("The receive operation timed out");

                throw new ApplicationException($"The client did not manage to connect to the receiving party at {URL}");
            }

            return mockClient;
        }
    }
#endif
}
