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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizUnit;
using BizUnit.Xaml;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for receiving a message from a one way enpoint
    /// which is utilizing the mock adapter.
    /// </summary>
    public class MockStepBase : TestStepBase
    {
        /// <summary>
        /// The URL of the corresponding enpoint the step will communicate with
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// The encoding to be used for exchanging the data with the endpoint
        /// </summary>
        public string Encoding { get; set; }

        public int Timeout { get; set; }

        protected System.Text.Encoding _encoding = null;

        protected Uri _endpointUri = null;

        public override void Execute(Context context)
        {
            
        }

        public override void Validate(Context context)
        {
            if (string.IsNullOrEmpty(this.Url))
            {
                throw new ArgumentException("Url propery not defined");
            }

            if (string.IsNullOrEmpty(this.Encoding))
            {
                throw new ArgumentException("Encoding propery not defined");
            }

            _encoding = System.Text.Encoding.GetEncoding(this.Encoding);
            _endpointUri = new Uri(Url);

            SubSteps = new System.Collections.ObjectModel.Collection<SubStepBase>();
        }
    }
}
