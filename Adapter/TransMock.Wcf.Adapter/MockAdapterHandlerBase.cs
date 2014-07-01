/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterHandlerBase.cs
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
    public abstract class MockAdapterHandlerBase
    {
        #region Private Fields

        private MockAdapterConnection connection;
        private MetadataLookup metadataLookup;

        #endregion Private Fields

        protected MockAdapterHandlerBase(MockAdapterConnection connection
            , MetadataLookup metadataLookup)
        {
            this.connection = connection;
            this.metadataLookup = metadataLookup;
        }

        #region Public Properties

        public MockAdapterConnection Connection
        {
            get
            {
                return this.connection;
            }
        }

        public MetadataLookup MetadataLookup
        {
            get
            {
                return this.metadataLookup;
            }
        }

        #endregion Public Properties

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        protected virtual void Dispose(bool disposing)
        {
            //
            //TODO: Implement Dispose. Override this method in respective Handler classes
            //
            
        }
    }
}

