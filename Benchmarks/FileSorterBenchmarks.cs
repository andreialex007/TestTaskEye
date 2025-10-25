using BenchmarkDotNet.Attributes;
using FileSorter.Core;
using FileSorter.Dto;
using Generator.Core;
using Generator.Dto;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class FileSorterBenchmarks
{
    private const string InputPath = "benchmark_input.txt";
    private const string OutputPath = "benchmark_sorted.txt";
    private const string TempDir = "./benchmark_temp";

    [Params(100, 500, 1000)] // MB
    public int FileSizeMB { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Generate test file
        var genConfig = new GeneratorConfig
        {
            OutputFilePath = InputPath,
            TargetFileSizeGB = FileSizeMB / 1024.0, // Convert MB to GB
            MaxNumber = 1000
        };

        var generator = new ChunkedFileGenerator(genConfig);
        generator.GenerateFile();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(InputPath))
            File.Delete(InputPath);
        if (File.Exists(OutputPath))
            File.Delete(OutputPath);
        if (Directory.Exists(TempDir))
            Directory.Delete(TempDir, true);
    }

    [Benchmark]
    public void SortFile()
    {
        var config = new SorterConfig
        {
            InputFilePath = InputPath,
            OutputFilePath = OutputPath,
            ChunkSizeMB = 100,
            TempDirectory = TempDir
        };

        var sorter = new ParallelExternalMergeSorter(config);
        sorter.Sort();
    }
}
