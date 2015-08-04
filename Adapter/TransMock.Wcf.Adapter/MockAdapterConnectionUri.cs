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
/// Module      :  MockAdapterConnectionUri.cs
/// Description :  This is the class for representing an adapter connection uri
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections;
using System.Globalization;

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
        /// <summary>
        /// The host name
        /// </summary>
        private string host = null;

        /// <summary>
        /// The system endpoint
        /// </summary>
        private string systemEndpoint = null;

        /// <summary>
        /// The operation name
        /// </summary>
        private string operation = null;

        #endregion Custom Generated Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterConnectionUri"/> class
        /// </summary>
        public MockAdapterConnectionUri() 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterConnectionUri"/> class with a Uri object
        /// </summary>
        /// <param name="uri">The URI to which the new instance will be initialized to</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors",
            Justification = "Needed as per design")]
        public MockAdapterConnectionUri(Uri uri)
            : base()
        {
            this.Uri = uri; 
        }

        #endregion Constructors

        #region Custom Generated Properties
        /// <summary>
        /// Gets or sets the host name
        /// </summary>
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

        /// <summary>
        /// Gets or sets the system endpoint name
        /// </summary>
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

        /// <summary>
        /// Gets or sets the operation name
        /// </summary>
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
        /// Gets or sets the Uri
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly",
            Justification = "Needed as per design"),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations",
            Justification = "Needed as per design")]
        public override Uri Uri
        {
            get
            {
                // Return the composed uri in valid format                
                if (string.IsNullOrEmpty(this.host))
                {
                    throw new ArgumentException("The host is not set");
                }

                if (string.IsNullOrEmpty(this.systemEndpoint))
                {
                    throw new ArgumentException("The system endpoint name is not set");
                }

                if (string.IsNullOrEmpty(this.operation))
                {
                    return new Uri(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}://{1}/{2}", 
                        MockAdapter.SCHEME, 
                        this.host, 
                        this.systemEndpoint));
                }
                else
                {
                    return new Uri(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}://{1}/{2}/{3}", 
                        MockAdapter.SCHEME, 
                        this.host, 
                        this.systemEndpoint, 
                        this.operation));
                }
            }

            set
            {   
                // Parse the uri into its relevant parts to produce a valid Uri object. (For example scheme, host, query).                
                if (value == null)
                {
                    throw new ArgumentNullException("Uri");
                }

                if (string.IsNullOrEmpty(value.Host))
                {
                    throw new ArgumentException("The host name is not part of the URI");
                }

                if (value.Scheme != MockAdapter.SCHEME)
                {
                    throw new ArgumentException("The host name is not part of the URI");
                }

                if (value.AbsolutePath == "/")
                {
                    throw new ArgumentException("The system endpoint name is not part of the URI");
                }

                this.host = value.Host;
                string[] uriParts = value.AbsolutePath.Split('/');
                
                for (int i = 0; i < uriParts.Length; i++)
                {
                    if (i == 1)
                    {
                        this.systemEndpoint = uriParts[i];
                    }

                    if (i == 2)
                    {
                        this.operation = uriParts[i];
                    }
                }                
             }
        }

        #endregion ConnectionUri Members
    }
}