using System.Diagnostics;
using System.Text;

namespace TestTaskEye
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new GeneratorConfig
            {
                OutputFilePath = "output.txt",
                TargetFileSizeGB = 100,
                ChunkSize = 100_000,
                MaxNumber = 100_000,
                MinRepetitionsPerString = 100,
                MaxRepetitionsPerString = 10_000
            };

            var generator = new ChunkedFileGenerator(config);
            generator.GenerateFile();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    public class GeneratorConfig
    {
        public string OutputFilePath { get; set; } = "output.txt";
        public double? TargetFileSizeGB { get; set; } = null;
        public int ChunkSize { get; set; } = 100_000;
        public int MaxNumber { get; set; } = 100_000;
        public int? MinRepetitionsPerString { get; set; } = null;
        public int? MaxRepetitionsPerString { get; set; } = null;
    }

    public class ChunkedFileGenerator
    {
        private readonly GeneratorConfig _config;
        private readonly string[] _sourceStrings;
        private long _bytesWritten;
        private long _targetBytes;

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

            _bytesWritten = 0;
            _targetBytes = _config.TargetFileSizeGB.HasValue
                ? (long)(_config.TargetFileSizeGB.Value * 1024 * 1024 * 1024)
                : 0;
        }

        public void GenerateFile()
        {
            Console.WriteLine("Starting chunked file generation...");
            Console.WriteLine($"Target file size: {_config.TargetFileSizeGB.Value:F2} GB (exact byte tracking)");
            Console.WriteLine($"Chunk size: {_config.ChunkSize:N0} lines");
            Console.WriteLine($"Output: {_config.OutputFilePath}");
            Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();

            using (var writer = new StreamWriter(_config.OutputFilePath, false, Encoding.UTF8, bufferSize: 1024 * 1024))
            {
                while (true)
                {
                    var shouldContinue = GenerateAndWriteChunk(writer, _config.ChunkSize);
                    if (!shouldContinue)
                        break;
                }

                writer.Flush();
            }

            stopwatch.Stop();

            Console.WriteLine($"\n\nCompleted in {stopwatch.Elapsed.TotalSeconds:F2} seconds");

            var fileInfo = new FileInfo(_config.OutputFilePath);
            Console.WriteLine($"File size: {fileInfo.Length / (1024.0 * 1024.0):F2} MB ({fileInfo.Length / (1024.0 * 1024.0 * 1024.0):F2} GB)");

            if (_targetBytes > 0)
            {
                Console.WriteLine($"Target: {_targetBytes / (1024.0 * 1024.0 * 1024.0):F2} GB");
                Console.WriteLine($"Actual: {fileInfo.Length / (1024.0 * 1024.0 * 1024.0):F2} GB");
                Console.WriteLine($"Accuracy: {(fileInfo.Length * 100.0 / _targetBytes):F2}%");
            }
        }

        private bool GenerateAndWriteChunk(StreamWriter writer, int lineCount)
        {
            int cores = Environment.ProcessorCount;
            var tasks = new Task<List<string>>[cores];
            int linesPerCore = (int)Math.Ceiling((double)lineCount / cores);

            for (int t = 0; t < cores; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    var localLines = new List<string>(linesPerCore);

                    for (int i = 0; i < linesPerCore; i++)
                    {
                        var number = Random.Shared.Next(1, _config.MaxNumber + 1);
                        var textIndex = Random.Shared.Next(_sourceStrings.Length);
                        var text = _sourceStrings[textIndex];

                        localLines.Add($"{number}. {text}");
                    }

                    return localLines;
                });
            }

            Task.WaitAll(tasks);

            foreach (var task in tasks)
            {
                foreach (var line in task.Result)
                {
                    writer.WriteLine(line);

                    if (_targetBytes <= 0) continue;

                    _bytesWritten += line.Length + Environment.NewLine.Length;

                    if (_bytesWritten >= _targetBytes)
                        return false;
                }
            }

            return true;
        }

    }
}