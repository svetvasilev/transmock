
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
/// Module      :  MockOperationConfig.cs
/// Description :  This class represents an operation config agains a mocked endpoint.
/// -----------------------------------------------------------------------------------------------------------
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// Defines the requres properties for an operation against a mocked endpoint
    /// </summary>
    internal class MessageOperationConfig
    {
        public SendEndpoint SendEndpoint { get; set; }

        public ReceiveEndpoint ReceiveEndpoint { get; set; }

        public TwoWayReceiveEndpoint TwoWayReceiveEndpoint { get; set; }

        public TwoWaySendEndpoint TwoWaySendEndpoint { get; set; }

        public Communication.NamedPipes.IStreamingServerAsync MockMessageServer { get; set; }

        public Communication.NamedPipes.IStreamingClientAsync MockMessageClient { get; set; }

    }
}