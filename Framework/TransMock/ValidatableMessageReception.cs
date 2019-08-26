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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// This class represents a validatble instance of a mock message which is represented by its index and contents
    /// </summary>
    public class ValidatableMessageReception
    {
        /// <summary>
        /// The index of the received message
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The instance of the mock message that was received
        /// </summary>
        public MockMessage Message { get; set; }
    }
}
