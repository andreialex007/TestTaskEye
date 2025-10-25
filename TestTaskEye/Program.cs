using CommandLine;
using Serilog;
using Serilog.Templates;

namespace TestTaskEye
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new ExpressionTemplate(
                    "[{@t:HH:mm:ss}] {@m}{#if IsDefined(Elapsed)} - {Elapsed/1000:0.0}s{#end}\n"))
                .CreateLogger();
            
            Parser.Default.ParseArguments<GeneratorConfig>(args)
                .WithParsed(config =>
                {
                    var generator = new ChunkedFileGenerator(config);
                    generator.GenerateFile();

                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                })
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Failed to parse arguments. Use --help for usage information.");
                });
        }
    }
}