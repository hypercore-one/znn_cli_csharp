using CommandLine;
using CommandLine.Text;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ZenonCli.Commands;

namespace ZenonCli
{
    public class Program
    {
        [RequiresUnreferencedCode("Calls System.Reflection.Assembly.GetTypes()")]
        private static Type[] LoadVerbs()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }

        [RequiresUnreferencedCode("Calls LoadVerbs()")]
        public static async Task Main(string[] args)
        {
            var types = LoadVerbs();

            var parser = new Parser(config =>
            {
                config.AutoVersion = false;
                config.HelpWriter = null;
            });

            var parserResult = await parser.ParseArguments(args, types)
                .WithParsedAsync(RunAsync);

            parserResult.WithNotParsed(errs => DisplayHelp(parserResult));
        }

        static void DisplayHelp<T>(ParserResult<T> result)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = $"{ThisAssembly.AssemblyName} v{ThisAssembly.AssemblyVersion}";
                h.Copyright = "Copyright (c) 2023 Zenon Community";

                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e, verbsIndex: false, maxDisplayWidth: 255);
            Console.WriteLine(helpText);
        }

        static async Task RunAsync(object obj)
        {
            if (obj is ICommand)
            {
                await ((ICommand)obj).ExecuteAsync();
            }
        }
    }
}