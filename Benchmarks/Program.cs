using BenchmarkDotNet.Running;

namespace Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<FileGeneratorBenchmarks>();
        BenchmarkRunner.Run<FileSorterBenchmarks>();
    }
}
