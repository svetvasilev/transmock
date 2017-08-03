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
using BizUnit.Core;
using BizUnit.Core.TestBuilder;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for receiving a message from a one way endpoint
    /// which is utilizing the mock adapter.
    /// </summary>
    public class MockStepBase : TestStepBase
    {
        /// <summary>
        /// The encoding used for the message content
        /// </summary>
        protected System.Text.Encoding encoding = null;

        /// <summary>
        /// The endpoint URI
        /// </summary>
        protected Uri endpointUri = null;

        /// <summary>
        /// Gets or sets the URL of the corresponding endpoint the step will communicate with
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the encoding to be used for exchanging the data with the endpoint
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for the receiving a message
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockStepBase.cs"/> class
        /// </summary>
        public MockStepBase() 
        {
            if (this.SubSteps == null)
            {
                this.SubSteps = new System.Collections.ObjectModel.Collection<SubStepBase>();
            }
        }

        /// <summary>
        /// Executes the step
        /// </summary>
        /// <param name="context">The execution conte</param>
        public override void Execute(Context context)
        {            
        }

        /// <summary>
        /// Validates the step before execution
        /// </summary>
        /// <param name="context">The execution context</param>
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

            this.encoding = System.Text.Encoding.GetEncoding(this.Encoding);
            this.endpointUri = new Uri(this.Url);
        }

        /// <summary>
        /// Implements cleanup logic.
        /// </summary>
        public virtual void Cleanup()
        {
        }
    }
}
