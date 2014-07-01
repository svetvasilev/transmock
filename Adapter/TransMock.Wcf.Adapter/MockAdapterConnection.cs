/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterConnection.cs
/// Description :  Defines the connection to the target system.
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    public class MockAdapterConnection : IConnection
    {
        #region Private Fields

        private MockAdapterConnectionFactory connectionFactory;
        private string connectionId;

        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the WCFMockAdapterConnection class with the WCFMockAdapterConnectionFactory
        /// </summary>
        public MockAdapterConnection(MockAdapterConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
            this.connectionId = Guid.NewGuid().ToString();
        }

        #region Public Properties

        /// <summary>
        /// Gets the ConnectionFactory
        /// </summary>
        public MockAdapterConnectionFactory ConnectionFactory
        {
            get
            {
                return this.connectionFactory;
            }
        }

        #endregion Public Properties

        #region IConnection Members

        /// <summary>
        /// Closes the connection to the target system
        /// </summary>
        public void Close(TimeSpan timeout)
        {
            //
            //TODO: Implement physical closing of the connection
            //
            MockAdapterUtilities.Trace.Trace(System.Diagnostics.TraceEventType.Information,
                "1001", "Mock Connection closed");
        }

        /// <summary>
        /// Returns a value indicating whether the connection is still valid
        /// </summary>
        public bool IsValid(TimeSpan timeout)
        {
            //
            //TODO: Implement physical checking for the validity of the opened connection
            //
            return true;

        }

        /// <summary>
        /// Opens the connection to the target system.
        /// </summary>
        public void Open(TimeSpan timeout)
        {
            //
            //TODO: Implement physical opening of the connection
            //
            MockAdapterUtilities.Trace.Trace(System.Diagnostics.TraceEventType.Information,
                "1001", "Mock Connection opened");

        }

        /// <summary>
        /// Clears the context of the Connection. This method is called when the connection is set back to the connection pool
        /// </summary>
        public void ClearContext()
        {
            //
            //TODO: Implement clear context to set the connection back to the pool.
            //
            MockAdapterUtilities.Trace.Trace(System.Diagnostics.TraceEventType.Information,
                "1001", "Mock connection ClearContex invoked");
        }

        /// <summary>
        /// Builds a new instance of the specified IConnectionHandler type
        /// </summary>
        public TConnectionHandler BuildHandler<TConnectionHandler>(MetadataLookup metadataLookup)
             where TConnectionHandler : class, IConnectionHandler
        {

            //if (typeof(IAsyncOutboundHandler).IsAssignableFrom(typeof(TConnectionHandler)))
            //{
            //    return new WCFMockAdapterAsyncOutboundHandler(this, metadataLookup) as TConnectionHandler;
            //}
            if (typeof(IOutboundHandler).IsAssignableFrom(typeof(TConnectionHandler)))
            {
                return new MockAdapterOutboundHandler(this, metadataLookup) as TConnectionHandler;
            }
            //if (typeof(IAsyncInboundHandler).IsAssignableFrom(typeof(TConnectionHandler)))
            //{
            //    return new WCFMockAdapterAsyncInboundHandler(this, metadataLookup) as TConnectionHandler;
            //}
            if (typeof(IInboundHandler).IsAssignableFrom(typeof(TConnectionHandler)))
            {
                return new MockAdapterInboundHandler(this, metadataLookup) as TConnectionHandler;
            }

            return default(TConnectionHandler);
        }

        /// <summary>
        /// Aborts the connection to the target system
        /// </summary>
        public void Abort()
        {
            //
            //TODO: Implement abort logic. DO NOT throw an exception from this method
            //
            MockAdapterUtilities.Trace.Trace(System.Diagnostics.TraceEventType.Information,
                "1001", "Mock Abort invoked");
            
        }


        /// <summary>
        /// Gets the Id of the Connection
        /// </summary>
        public String ConnectionId
        {
            get
            {
                return connectionId;
            }
        }

        #endregion IConnection Members
    }
}
