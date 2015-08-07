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
using System.Globalization;
using System.Linq;
using System.Text;

using CommandLine;
using CommandLine.Text;

using TransMock.Mockifier.Parser;

namespace TransMock.Mockifier
{
    /// <summary>
    /// The program class of the Mockifier
    /// </summary>
    public class Mockifier
    {
        /// <summary>
        /// The main entry point for the program
        /// </summary>
        /// <param name="args">A string array containing the arguments as passed on the command prompt</param>
        static void Main(string[] args)
        {
            Console.Out.WriteLine("TransMock Mockifier tool starting. Copyright 2014, Svetoslav Vasilev");
            try
            {
                var parsedArguments = new MockifierArguments();

                if (CommandLine.Parser.Default.ParseArguments(args, parsedArguments))
                {
                    Console.Out.WriteLine("About to execute with the following parameters:");  
               
                    // Parsing the arguments was successfull, parsing the bindings file
                    ParameterCombination paramCombination = parsedArguments.EvaluateParametersCombination();

                    BizTalkBindingsParser bindingsParser = new BizTalkBindingsParser();

                    switch (paramCombination)
                    {
                        case ParameterCombination.NoParams:
                            break;
                        case ParameterCombination.DefaultParams:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);

                            // Saving to the same file as the input
                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings, 
                                parsedArguments.InputBindings);
                            break;
                        case ParameterCombination.OutputBindingsOnly:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("output: " + parsedArguments.OutputBindings);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.OutputBindings);
                            break;
                        case ParameterCombination.OutputClassOnly:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("classOutput: " + parsedArguments.OutputClass);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.InputBindings, 
                                parsedArguments.OutputClass);
                            break;
                        case ParameterCombination.OutputBindingsAndUnescape:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("output: " + parsedArguments.OutputBindings);
                            Console.Out.WriteLine("classOutput: " + parsedArguments.OutputClass);
                            Console.Out.WriteLine("unescape: " + parsedArguments.Unescape);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.OutputBindings, 
                                null,
                                "2013", 
                                parsedArguments.Unescape);
                            break;
                        case ParameterCombination.OutputClassAndUnescape:                            
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("classOutput: " + parsedArguments.OutputClass);
                            Console.Out.WriteLine("unescape: " + parsedArguments.Unescape);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.InputBindings, 
                                parsedArguments.OutputClass,
                                "2013", 
                                parsedArguments.Unescape);
                            break;
                        case ParameterCombination.OutputBindingsAndClassOutput:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("output: " + parsedArguments.OutputBindings);
                            Console.Out.WriteLine("classOutput: " + parsedArguments.OutputClass);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.OutputBindings,
                                parsedArguments.OutputClass);
                            break;
                        case ParameterCombination.OutputBindingsAndClassOutputAndUnescape:
                            // TODO:
                            break;
                        case ParameterCombination.OutputBindingsAndBtsVersion:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("output: " + parsedArguments.OutputBindings);
                            Console.Out.WriteLine("btsVersion: " + parsedArguments.BtsVersion);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings, 
                                parsedArguments.OutputBindings,
                                parsedArguments.BtsVersion);
                            break;                        
                        case ParameterCombination.OutputClassAndBtsVersion:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("output: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("classOutput: " + parsedArguments.OutputClass);
                            Console.Out.WriteLine("btsVersion: " + parsedArguments.BtsVersion);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.InputBindings,
                                parsedArguments.OutputClass,
                                parsedArguments.BtsVersion);
                            break;
                        case ParameterCombination.OutputBindingsAndBtsVersionAndUnescape:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("output: " + parsedArguments.InputBindings);                            
                            Console.Out.WriteLine("btsVersion: " + parsedArguments.BtsVersion);
                            Console.Out.WriteLine("unescape: " + parsedArguments.Unescape);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.OutputBindings,
                                null,
                                parsedArguments.BtsVersion,
                                parsedArguments.Unescape);                            
                            break;
                        case ParameterCombination.AllParams:
                            Console.Out.WriteLine("bindings: " + parsedArguments.InputBindings);
                            Console.Out.WriteLine("output: " + parsedArguments.OutputBindings);
                            Console.Out.WriteLine("classOutput: " + parsedArguments.OutputClass);
                            Console.Out.WriteLine("btsVersion: " + parsedArguments.BtsVersion);
                            Console.Out.WriteLine("unescape: " + parsedArguments.Unescape);

                            bindingsParser.ParseBindings(
                                parsedArguments.InputBindings,
                                parsedArguments.OutputBindings,
                                parsedArguments.OutputClass,
                                parsedArguments.BtsVersion,
                                parsedArguments.Unescape);
                            break;
                        default:
                            Console.Out.WriteLine("Mockifying with mock map is still not supported!");
                            break;
                    }

                    Console.Out.WriteLine("Bindings mockified successfully!Exiting...");
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                            "Mockifier threw an exception: {0} and exiting.",
                            ex.Message));
            }
        }
    }    
}
