/***************************************
//   Copyright 2014 - Svetoslav Vasilev

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
*****************************************/

/// -----------------------------------------------------------------------------------------------------------
/// Module      :  MockAdapterConnection.cs
/// Description :  Defines the connection to the target system.
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// The mock adapter connection class
    /// </summary>
    public class MockAdapterConnection : IConnection
    {
        #region Private Fields
        /// <summary>
        /// The connection factory for the mock adapter
        /// </summary>
        private MockAdapterConnectionFactory connectionFactory;

        /// <summary>
        /// The connection Id
        /// </summary>
        private string connectionId;

        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterConnection"/> class with the WCFMockAdapterConnectionFactory
        /// </summary>
        /// <param name="connectionFactory">The connection factory instance</param>
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
        /// Gets the Id of the Connection
        /// </summary>
        public string ConnectionId
        {
            get
            {
                return this.connectionId;
            }
        }

        /// <summary>
        /// Opens the connection to the target system.
        /// </summary>
        /// <param name="timeout">THe timeout for opening the connection</param>
        public void Open(TimeSpan timeout)
        {
            // Not opening the connection here, only creating trace output
            MockAdapterUtilities.Trace.Trace(
                System.Diagnostics.TraceEventType.Information,
                "1001", 
                "Mock Connection opened");
        }

        /// <summary>
        /// Closes the connection to the target system
        /// </summary>
        /// <param name="timeout">The timeout for closing the connection</param>
        public void Close(TimeSpan timeout)
        {
            // Not closing the connection here, only creating tracing output
            MockAdapterUtilities.Trace.Trace(
                System.Diagnostics.TraceEventType.Information,
                "1001", 
                "Mock Connection closed");
        }

        /// <summary>
        /// Returns a value indicating whether the connection is still valid
        /// </summary>
        /// <param name="timeout">The time period for which the connection should be valid</param>
        /// <returns>A boolean indicating whether the connection is still valid</returns>
        public bool IsValid(TimeSpan timeout)
        {
            // Always return true
            return true;
        }

        /// <summary>
        /// Clears the context of the Connection. This method is called when the connection is set back to the connection pool
        /// </summary>
        public void ClearContext()
        {
            // Only creating trace output
            MockAdapterUtilities.Trace.Trace(
                System.Diagnostics.TraceEventType.Information,
                "1001", 
                "Mock connection ClearContex invoked");
        }

        /// <summary>
        /// Builds a new instance of the specified IConnectionHandler type
        /// </summary>
        /// <param name="metadataLookup">The meta data lookup object</param>
        /// <typeparam name="TConnectionHandler">The type of the connection handler</typeparam>
        /// <returns>An instance of an adapter handler</returns>        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Need to return the object instances the method creates to be used elsewhere in the code")]
        public TConnectionHandler BuildHandler<TConnectionHandler>(MetadataLookup metadataLookup)
             where TConnectionHandler : class, IConnectionHandler
        {           
            if (typeof(IOutboundHandler).IsAssignableFrom(typeof(TConnectionHandler)))
            {
                return new MockAdapterOutboundHandler(this, metadataLookup) as TConnectionHandler;
            }
            
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
            // Not aborting the connection here, only creating trace output
            MockAdapterUtilities.Trace.Trace(
                System.Diagnostics.TraceEventType.Information,
                "1001", 
                "Mock Abort invoked");            
        }

        #endregion IConnection Members
    }
}
