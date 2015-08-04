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
/// Module      :  MockAdapterHandlerBase.cs
/// Description :  This is the base class for handlers used to store common properties/helper functions
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// The base class for the mock adapter handlers
    /// </summary>
    public abstract class MockAdapterHandlerBase : IDisposable
    {
        #region Private Fields
        /// <summary>
        /// The connection for the mock adapter
        /// </summary>
        private MockAdapterConnection connection;

        /// <summary>
        /// The metadata lookup object
        /// </summary>
        private MetadataLookup metadataLookup;

        #endregion Private Fields
        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterHandlerBase"/> class
        /// </summary>
        /// <param name="connection">The mock adapter connection instance</param>
        /// <param name="metadataLookup">The instance of the metadata lookup object</param>
        protected MockAdapterHandlerBase(
            MockAdapterConnection connection,
            MetadataLookup metadataLookup)
        {
            this.connection = connection;
            this.metadataLookup = metadataLookup;
        }

        #region Public Properties
        /// <summary>
        /// Gets the adapter connection
        /// </summary>
        public MockAdapterConnection Connection
        {
            get
            {
                return this.connection;
            }
        }

        /// <summary>
        /// Gets the metadata lookup object
        /// </summary>
        public MetadataLookup MetadataLookup
        {
            get
            {
                return this.metadataLookup;
            }
        }

        #endregion Public Properties

        #region IDisposable
        /// <summary>
        /// Disposes the object instance
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
        /// <summary>
        /// Disposes the object
        /// </summary>
        /// <param name="disposing">Governs how the object shall be disposed</param>
        protected virtual void Dispose(bool disposing)
        {   
        }
    }
}