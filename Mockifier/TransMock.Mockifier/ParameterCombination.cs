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

namespace TransMock.Mockifier
{
    /// <summary>
    /// Enumeration that depicts the combination of passed parameters
    /// </summary>
    public enum ParameterCombination
    {
        /// <summary>
        /// Indicates no parameters were supplied
        /// </summary>
        NoParams,

        /// <summary>
        /// Indicates that the default parameters were supplied
        /// </summary>
        DefaultParams,

        /// <summary>
        /// Indicates that only the output bindings parameter was supplied
        /// </summary>
        OutputBindingsOnly,

        /// <summary>
        /// Indicates that only the output class parameter was supplied
        /// </summary>
        OutputClassOnly,

        /// <summary>
        /// Indicates that the output bindings and unescape parameters were supplied
        /// </summary>
        OutputBindingsAndUnescape,

        /// <summary>
        /// Indicates that the output class and unescape parameters were supplied
        /// </summary>
        OutputClassAndUnescape,

        /// <summary>
        /// Indicates that the output bindings and class output parameters were supplied
        /// </summary>
        OutputBindingsAndClassOutput,

        /// <summary>
        /// Indicates that the output bindings, class output and unescape parameters were supplied
        /// </summary>
        OutputBindingsAndClassOutputAndUnescape,

        /// <summary>
        /// Indicates that the output bindings and BizTalk version parameters were supplied
        /// </summary>
        OutputBindingsAndBtsVersion,

        /// <summary>
        /// Indicates that the output bindings, BizTalk version and unescape parameters were supplied
        /// </summary>
        OutputBindingsAndBtsVersionAndUnescape,

        /// <summary>
        /// Indicates that the output class and BizTalk version parameters were supplied
        /// </summary>
        OutputClassAndBtsVersion,

        /// <summary>
        /// Indicates that the output class, BizTalk version and unescape parameters were supplied
        /// </summary>
        OutputClassAndBtsVersionAndUnescape,

        /// <summary>
        /// Indicates that the output bindings, output class and BizTalk version parameters were supplied
        /// </summary>
        OutputBindingsAndClassOutputAndBtsVersion,

        /// <summary>
        /// Indicates that all parameters were supplied
        /// </summary>
        AllParams
    }
}
