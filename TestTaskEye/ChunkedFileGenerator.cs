using System.Diagnostics;
using System.Text;
using Humanizer;
using Humanizer.Bytes;
using Serilog;
using Serilog.Templates;
using Serilog;
using SerilogTimings;

namespace TestTaskEye;

public class ChunkedFileGenerator
{
    private readonly GeneratorConfig _config;
    private readonly string[] _sourceStrings;
    private readonly long _targetBytes;

    private static readonly string[] DefaultSourceStrings = new[]
    {
        "Apple",
        "Banana is yellow",
        "Cherry is the best",
        "Something something something",
        "Orange fruit",
        "Grape cluster",
        "Watermelon is big",
        "Strawberry sweet",
        "Pineapple tropical",
        "Mango delicious",
        "Kiwi green",
        "Peach fuzzy",
        "Plum purple",
        "Lemon sour",
        "Lime citrus"
    };

    public ChunkedFileGenerator(GeneratorConfig config)
    {
        _config = config;
        _sourceStrings = DefaultSourceStrings;
        _targetBytes = _config.TargetFileSizeGB.HasValue
            ? (long)(_config.TargetFileSizeGB.Value * 1024 * 1024 * 1024)
            : 0;
    }

    public void GenerateFile()
    {
        using (Operation.Time("Total file generation"))
        {
            Console.WriteLine($"Generating {_config.TargetFileSizeGB ?? 0:F2} GB file...");

            var cores = Environment.ProcessorCount;
            var bytesPerCore = _targetBytes / cores;

            Console.WriteLine($"Using {cores} cores ({bytesPerCore.Bytes().Megabytes:F2} MB each)\n");

            var tempFiles = new string[cores];

            using (Operation.Time("Parallel generation"))
            {
                Parallel.For(0, cores, index =>
                {
                    tempFiles[index] = $"temp_{index}.txt";
                    FileTools.GenerateFileChunk(tempFiles[index], bytesPerCore, index, _config.MaxNumber, _sourceStrings);
                });
            }

            Console.WriteLine();
            using (Operation.Time("Merging files"))
                FileTools.MergeFiles(tempFiles, _config.OutputFilePath);

            using (Operation.Time("Cleanup temp files"))
                Array.ForEach(tempFiles, File.Delete);

            Console.WriteLine();
            var fileInfo = new FileInfo(_config.OutputFilePath);
            Console.WriteLine($"✓ Generated {fileInfo.Length.Bytes().Gigabytes:F2} GB / {_targetBytes.Bytes().Gigabytes:F2} GB ({(fileInfo.Length * 100.0 / _targetBytes):F2}% accurate)");
        }

        Log.CloseAndFlush();
    }
}
