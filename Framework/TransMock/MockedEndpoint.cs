
/***************************************
//   Copyright 2019 - Svetoslav Vasilev

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
/// Module      :  MockedEndpoint.cs
/// Description :  This class represents a mocked endpoint.
/// -----------------------------------------------------------------------------------------------------------

namespace TransMock
{
    /// <summary>
    /// Describes a mocked endpoint by defining its basic properties
    /// </summary>
    public abstract class MockedEndpoint
    {
        /// <summary>
        /// The URL of the endpoint
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// The timeout in seconds to wait for connection to/from the endpoint
        /// </summary>
        public int TimeoutInSeconds { get; set; }

        /// <summary>
        /// The expected encoding of the messages communicated to/from the endpoint
        /// </summary>
        public System.Text.Encoding MessageEncoding { get; set; }

    }
}
