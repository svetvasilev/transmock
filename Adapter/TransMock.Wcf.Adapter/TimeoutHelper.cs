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
using System.Threading.Tasks;

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// Utility class containing helper functions for measuring timeout 
    /// </summary>
    internal class TimeoutHelper
    {
        /// <summary>
        /// The timeout instance
        /// </summary>
        private TimeSpan timeout;

        /// <summary>
        /// The creation time of the instance
        /// </summary>
        private DateTime creationTime;

        /// <summary>
        /// Indicates whether the timeout is set to infinity
        /// </summary>
        private bool isInfinite;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutHelper"/> class
        /// </summary>
        /// <param name="timeout">The initial timeout set to the instance</param>
        public TimeoutHelper(TimeSpan timeout)
        {
            this.creationTime = DateTime.Now;
            this.timeout = timeout;

            if (timeout.Equals(Infinite))
            {
                this.isInfinite = true;
            }
        }

        /// <summary>
        /// Gets the value of infinite timespan
        /// </summary>
        public static TimeSpan Infinite
        {
            get { return TimeSpan.MaxValue; }
        }

        /// <summary>
        /// Gets the value indicating the remaining timeout
        /// </summary>
        public TimeSpan RemainingTimeout
        {
            get
            {
                if (this.isInfinite)
                {
                    return Infinite;
                }

                return this.timeout.Subtract(DateTime.Now.Subtract(this.creationTime));
            }
        }

        /// <summary>
        /// Gets a value indicating whether timeout has expired.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (this.isInfinite)
                {
                    return false;
                }

                return this.RemainingTimeout < TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Gets the remaining timeout value and throw an exception if the timeout
        /// has expired.
        /// </summary>
        /// <param name="exceptionMessage">The message that should be included in the exception</param>
        /// <returns>The remaining timeout</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
            Justification = "Needed as per design")]
        public TimeSpan GetRemainingTimeoutAndThrowIfExpired(string exceptionMessage)
        {
            if (this.isInfinite)
            {
                return Infinite;
            }

            if (this.RemainingTimeout < TimeSpan.Zero)
            {
                throw new TimeoutException(exceptionMessage);
            }

            return this.RemainingTimeout;
        }

        /// <summary>
        /// Throw an exception if the timeout has expired.
        /// </summary>
        /// <param name="exceptionMessage">The message that should be included in the exception</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
            Justification = "Default implementation")]
        public void ThrowIfTimeoutExpired(string exceptionMessage)
        {
            if (this.RemainingTimeout < TimeSpan.Zero)
            {
                throw new TimeoutException(exceptionMessage);
            }
        }
    }
}
