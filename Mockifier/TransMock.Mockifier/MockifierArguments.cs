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

using CommandLine;
using CommandLine.Text;

namespace TransMock.Mockifier
{
    /// <summary>
    /// Represents the command line arguments for the Mockifier
    /// </summary>
    public class MockifierArguments
    {
        /// <summary>
        /// Gets or sets the path to the input bindings
        /// </summary>
        [Option('b', "bindings", Required = true,
            HelpText = "Input bindings file to be processed.")]
        public string InputBindings { get; set; }

        /// <summary>
        /// Gets or sets the path to the output bindings
        /// </summary>
        [Option('o', "output", Required = false,
            HelpText = "The path to where the processed bindings file will be stored. Optional.")]
        public string OutputBindings { get; set; }

        /// <summary>
        /// Gets or sets the path to the output helper class
        /// </summary>
        [Option('c', "classOutput", Required = false,
            HelpText = "The path to where the mocked URL helper class file will be stored. Optional.")]
        public string OutputClass { get; set; }

        /// <summary>
        /// Gets or sets the path to the mock mapping file
        /// </summary>
        [Option('m', "mockmap", Required = false,
            HelpText = "The path to the mock mapping file.")]
        public string MockMap { get; set; }

        /// <summary>
        /// Gets or sets the version of BizTalk server
        /// </summary>
        [Option('r', "btsVersion", Required = false,
             DefaultValue = "2013",
            HelpText = "The BizTalk server version. Default is the latest version. Optional.")]
        public string BtsVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the unescape mode is defined
        /// </summary>
        [Option('u', "unescape", Required = false,
             DefaultValue = false,
            HelpText = "Specifies whether the transport configuration should be unescaped. Default is false. Optional.")]
        public bool Unescape { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the verbosity is turned on
        /// </summary>
        [Option('v', "verbose", DefaultValue = false,
          HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets the parser state
        /// </summary>
        [ParserState]
        public IParserState LastParserState { get; set; }

        /// <summary>
        /// Gets the description of how to use the mockifier
        /// </summary>
        /// <returns>A string containing the usage description</returns>
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(
                this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        /// <summary>
        /// Parses the parameters and evaluates which ones have been provided
        /// </summary>
        /// <returns>The enumeration value representing which parameters were supplied at the command prompt</returns>
        public ParameterCombination EvaluateParametersCombination()
        {
            if (string.IsNullOrEmpty(this.OutputBindings) && string.IsNullOrEmpty(this.OutputClass))
            {
                return ParameterCombination.DefaultParams;
            }
            else if (!string.IsNullOrEmpty(this.OutputBindings) && string.IsNullOrEmpty(this.OutputClass))
            {
                if (this.BtsVersion == "2013")
                {
                    if (this.Unescape)
                    {
                        return ParameterCombination.OutputBindingsAndUnescape;
                    }
                    else
                    {
                        return ParameterCombination.OutputBindingsOnly;
                    }
                }
                else
                {
                    if (this.Unescape)
                    {
                        return ParameterCombination.OutputBindingsAndBtsVersionAndUnescape;
                    }
                    else
                    {
                        return ParameterCombination.OutputBindingsAndBtsVersion;
                    }
                }
            }
            else if (string.IsNullOrEmpty(this.OutputBindings) && !string.IsNullOrEmpty(this.OutputClass))
            {
                if (this.BtsVersion == "2013")
                {
                    if (this.Unescape)
                    {
                        return ParameterCombination.OutputClassAndUnescape;
                    }
                    else
                    {
                        return ParameterCombination.OutputClassOnly;
                    }
                }
                else
                {
                    if (this.Unescape)
                    {
                        return ParameterCombination.OutputClassAndBtsVersionAndUnescape;
                    }
                    else
                    {
                        return ParameterCombination.OutputClassAndBtsVersion;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(this.OutputBindings) && !string.IsNullOrEmpty(this.OutputClass))
            {
                if (this.BtsVersion == "2013")
                {
                    if (this.Unescape)
                    {
                        return ParameterCombination.OutputBindingsAndClassOutputAndUnescape;
                    }
                    else
                    {
                        return ParameterCombination.OutputBindingsAndClassOutput;
                    }
                }
                else
                {
                    if (this.Unescape)
                    {
                        return ParameterCombination.AllParams;
                    }
                    else
                    {
                        return ParameterCombination.OutputBindingsAndClassOutputAndBtsVersion;
                    }
                }
            }
            else
            {
                return ParameterCombination.NoParams;
            }
        }
    }
}