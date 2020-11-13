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
#if NET462 || NET48
    public class ComplexFlowMock
    {
        IStreamingServerAsync mockServer;

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

                    //syncEvent.Release();

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

                    await CleanupServer()
                        .ConfigureAwait(false);

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

            receiverTask = Task.Run(            
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

                    var parallelTask1 = Task.Run(async () =>
                    {
                        Console.WriteLine("Starting execution of parallel task 1");

                        MockMessage responseMessage = null;
                        // Then we send a request and wait for a response - first time
                        using (var mockClient = await InitClientAsync(sendUrl1))
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

                        return responseMessage;
                    });

                    var parallelTask2 = Task.Run(async () =>
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

                                await mockClient.WriteMessageAsync(secondRequest)
                                    .ConfigureAwait(false);

                                responseMessage = await mockClient.ReadMessageAsync()
                                    .ConfigureAwait(false);

                                if (responseMessage == null)
                                {
                                    throw new ApplicationException("Second response message was null!");
                                }

                                Console.WriteLine($"Response message {i+1} received from server.");
                                // Disconnecting as with the actual implementation
                                await mockClient.DisconnectAsync()
                                    .ConfigureAwait(false);
                            }
                        }
                    });

                    await Task.WhenAll(parallelTask1, parallelTask2);

                    if (parallelTask1.IsFaulted)
                        throw parallelTask1.Exception;

                    if (parallelTask2.IsFaulted)
                        throw parallelTask2.Exception;
                    // Sending the first response back to initial caller
                    await mockServer.WriteMessageAsync(
                        connectionId,
                        parallelTask1.Result)
                        .ConfigureAwait(false);

                    await CleanupServer()
                        .ConfigureAwait(false);
                    
                });
            }


        public void Clenup()
        {
            
        }

        private async Task InitServerAsync(string receiveURL)
        {
            inboundMesageQueue = new ConcurrentDictionary<int, AsyncReadEventArgs>();

            mockServer = new StreamingNamedPipeServerAsync(new Uri(receiveURL).AbsolutePath);
            mockServer.ReadCompleted += MockServer_ReadCompleted;

            await mockServer.StartAsync();

            Console.WriteLine($"ComplexFlow server started listening on {receiveURL}");
        }

        private void MockServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            connectionId = e.ConnectionId;
            inboundMesageQueue.TryAdd(e.ConnectionId, e);

            syncEvent.Release();

        }

        private async Task CleanupServer()
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
#endif
}
