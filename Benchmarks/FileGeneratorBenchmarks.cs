using BenchmarkDotNet.Attributes;
using Generator.Core;
using Generator.Dto;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class FileGeneratorBenchmarks
{
    private const string OutputPath = "benchmark_output.txt";

    [Params(100, 500, 1000)] // MB
    public int FileSizeMB { get; set; }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(OutputPath))
            File.Delete(OutputPath);
    }

    [Benchmark]
    public void GenerateFile()
    {
        var config = new GeneratorConfig
        {
            OutputFilePath = OutputPath,
            TargetFileSizeGB = FileSizeMB / 1024.0, // Convert MB to GB
            MaxNumber = 1000
        };

        var generator = new ChunkedFileGenerator(config);
        generator.GenerateFile();
    }
}
