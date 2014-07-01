using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommandLine;
using CommandLine.Text;

using TransMock.Mockifier.Parser;

namespace TransMock.Mockifier
{
    public class Mockifier
    {
        static void Main(string[] args)
        {
            try
            {
                var parsedArguments = new MockifierArguments();

                if (CommandLine.Parser.Default.ParseArguments(args, parsedArguments))
                {
                    //Parsing the arguments was successfull, parsing the bindings file
                    BizTalkBindingsParser bindingsParser = new BizTalkBindingsParser();

                    if (string.IsNullOrEmpty(parsedArguments.OutputBindings))
                        bindingsParser.ParseBindings(parsedArguments.InputBindings, parsedArguments.InputBindings);//Saving to the same file as the input
                    else
                        bindingsParser.ParseBindings(parsedArguments.InputBindings, parsedArguments.OutputBindings);

                    Console.Out.WriteLine("Bindings mockified successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(string.Format("Mockifier threw an exception: {0} and exiting.",
                    ex.Message));
            }

        }
    }

    public class MockifierArguments
    {
        [Option('b', "bindings", Required = true,
            HelpText = "Input bindings file to be processed.")]
        public string InputBindings { get; set; }

        [Option('o', "output", Required = false,
            HelpText = "Path to the processed bindings file.")]
        public string OutputBindings { get; set; }

        [Option('m', "mockmap", Required = false,
            HelpText = "Path to the mock mapping file.")]
        public string MockMap { get; set; }

        [Option('v', "verbose", DefaultValue = false,
          HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {            
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

    }
}
