/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterConnectionUri.cs
/// Description :  This is the class for representing an adapter connection uri
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// This is the class for building the WCFMockAdapterConnectionUri
    /// </summary>
    public class MockAdapterConnectionUri : ConnectionUri
    {

        #region Custom Generated Fields

        private string host = null;

        private string systemEndpoint = null;

        private string operation = null;

        #endregion Custom Generated Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ConnectionUri class
        /// </summary>
        public MockAdapterConnectionUri() { }

        /// <summary>
        /// Initializes a new instance of the ConnectionUri class with a Uri object
        /// </summary>
        public MockAdapterConnectionUri(Uri uri)
            : base()
        {
            this.Uri = uri; 
        }

        #endregion Constructors

        #region Custom Generated Properties

        public string Host
        {
            get
            {
                return this.host;
            }
            set
            {
                this.host = value;
            }
        }

        public string SystemEndpoint
        {
            get
            {
                return this.systemEndpoint;
            }
            set
            {
                this.systemEndpoint = value;
            }
        }

        public string Operation
        {
            get
            {
                return this.operation;
            }
            set
            {
                this.operation = value;
            }
        }
        #endregion Custom Generated Properties

        #region ConnectionUri Members

        /// <summary>
        /// Getter and Setter for the Uri
        /// </summary>
        public override Uri Uri
        {
            get
            {
                //
                //TODO: Return the composed uri in valid format
                //
                if (string.IsNullOrEmpty(host))
                    throw new ArgumentException("The host is not set");
                if (string.IsNullOrEmpty(systemEndpoint))
                    throw new ArgumentException("The system endpoint name is not set");

                if(string.IsNullOrEmpty(operation)){
                    return new Uri(string.Format("{0}://{1}/{2}", MockAdapter.SCHEME, this.host, this.systemEndpoint));
                }
                else{
                    return new Uri(string.Format("{0}://{1}/{2}/{3}", MockAdapter.SCHEME, this.host, this.systemEndpoint, this.operation));
                }
            }
            set
            {
                //
                //TODO: Parse the uri into its relevant parts to produce a valid Uri object. (For example scheme, host, query).
                //
                if(string.IsNullOrEmpty(value.Host))
                    throw new ArgumentException("The host name is not part of the URI");
                if (value.Scheme != MockAdapter.SCHEME)
                    throw new ArgumentException("The host name is not part of the URI");
                if (value.AbsolutePath == "/")
                    throw new ArgumentException("The system endpoint name is not part of the URI");

                this.host = value.Host;
                string[] uriParts = value.AbsolutePath.Split('/');
                
                for (int i = 0; i < uriParts.Length; i++)
                {
                    if(i == 1)
                        this.systemEndpoint = uriParts[i];
                    if (i == 2)
                        this.operation = uriParts[i];
                }
                
             }
        }

        #endregion ConnectionUri Members

    }
}