using CommandLine;
using FileSorter.Core;
using FileSorter.Dto;
using Serilog;
using Serilog.Templates;

namespace FileSorter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new ExpressionTemplate(
                    "[{@t:HH:mm:ss}] {@m}{#if IsDefined(Elapsed)} - {Elapsed/1000:0.0}s{#end}\n"))
                .CreateLogger();

            Parser.Default.ParseArguments<SorterConfig>(args)
                .WithParsed(config =>
                {
                    Log.Information("📝 Sorting file: {InputPath}", config.InputFilePath);
                    Log.Information("💪 Chunk size: {ChunkSize} MB", config.ChunkSizeMB);

                    var sorter = new ParallelExternalMergeSorter(config);
                    sorter.Sort();

                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                })
                .WithNotParsed(errors =>
                {
                    Log.Error("Failed to parse arguments. Use --help for usage information.");
                });
        }
    }
}
