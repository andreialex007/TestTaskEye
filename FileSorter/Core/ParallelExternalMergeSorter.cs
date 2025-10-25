using System.Text;
using FileSorter.Dto;
using Humanizer;
using Serilog;
using SerilogTimings;

namespace FileSorter.Core;

public class ParallelExternalMergeSorter
{
    private readonly SorterConfig _config;
    private readonly long _chunkSizeBytes;

    public ParallelExternalMergeSorter(SorterConfig config)
    {
        _config = config;
        _chunkSizeBytes = _config.ChunkSizeMB * 1024L * 1024L;
    }

    public void Sort()
    {
        using (Operation.Time("🎯 Total sorting"))
        {
            // Ensure temp directory exists
            if (Directory.Exists(_config.TempDirectory))
                Directory.Delete(_config.TempDirectory, true);
            Directory.CreateDirectory(_config.TempDirectory);

            // Step 1: Split into raw chunk files (sequential disk I/O)
            SplitIntoRawChunks();

            // Step 2: Sort chunks in parallel
            var sortedChunkFiles = SortChunks();

            // Step 3: K-way merge
            MergeChunks(sortedChunkFiles);

            // Step 4: Cleanup
            using (Operation.Time("🧹 Cleanup temp files"))
            {
                Directory.Delete(_config.TempDirectory, true);
            }

            var fileInfo = new FileInfo(_config.OutputFilePath);
            Log.Information("✅ Sorted file: {FilePath} ({Size})", _config.OutputFilePath, fileInfo.Length.Bytes().Humanize());
        }

        Log.CloseAndFlush();
    }

    private void SplitIntoRawChunks()
    {
        var chunkCount = 0;

        using (Operation.Time("📖 Splitting into raw chunks"))
        {
            var enc = new UTF8Encoding(false);
            var nlBytes = enc.GetByteCount(Environment.NewLine);

            using var reader = new StreamReader(File.OpenRead(_config.InputFilePath), enc, true, 4 * 1024 * 1024);

            StreamWriter? writer = null;
            var size = 0;
            var index = 0;

            while (reader.ReadLine() is { } line)
            {
                if (writer is null || size >= _chunkSizeBytes)
                {
                    writer?.Dispose();
                    var path = Path.Combine(_config.TempDirectory, $"raw_chunk_{index++:D4}.txt");
                    chunkCount++;
                    writer = new StreamWriter(path, false, enc, 4 * 1024 * 1024);
                    size = 0;
                }

                writer.WriteLine(line);
                size += enc.GetByteCount(line) + nlBytes;
            }

            writer?.Dispose();
        }

        Log.Information("📦 Split into {ChunkCount} raw chunks", chunkCount);
    }


    private List<string> SortChunks()
    {
        var rawChunkFiles = Directory.GetFiles(_config.TempDirectory, "raw_chunk_*.txt")
            .OrderBy(f => f)
            .ToList();

        var cores = Environment.ProcessorCount;
        var maxParallelism = Math.Min(cores, rawChunkFiles.Count);

        Log.Information("💪 Sorting {ChunkCount} chunks ({MaxParallel} at a time, parallel within each)", rawChunkFiles.Count, maxParallelism);

        using (Operation.Time("⚙️ Parallel chunk sorting"))
        {
            return rawChunkFiles
                .Select((file, index) => (file, index))
                .AsParallel()
                .WithDegreeOfParallelism(maxParallelism)
                .Select(x =>
                {
                    var (rawChunkFile, i) = x;

                    Log.Information("🔄 Sorting chunk {Index}/{Total}...", i + 1, rawChunkFiles.Count);
                    var sortedChunkFile = Path.Combine(_config.TempDirectory, $"sorted_chunk_{i:D4}.txt");

                    var sortedLines = File.ReadAllLines(rawChunkFile, new UTF8Encoding(false))
                        .Select(LineData.Parse)
                        .ToArray();

                    Array.Sort(sortedLines);

                    using (var writer = new StreamWriter(sortedChunkFile, false, new UTF8Encoding(false), 4 * 1024 * 1024))
                    {
                        foreach (var line in sortedLines)
                            writer.WriteLine(line.OriginalLine);
                    }

                    File.Delete(rawChunkFile);

                    return (i, sortedChunkFile);
                })
                .OrderBy(x => x.i)
                .Select(x => x.sortedChunkFile)
                .ToList();
        }
    }

    private void MergeChunks(List<string> chunkFiles)
    {
        using (Operation.Time("🔗 Merging chunks"))
        {
            const int bufferSize = 128 * 1024 * 1024;
            var enc = new UTF8Encoding(false);

            var outputFileStream = new FileStream(_config.OutputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);
            using var writer = new StreamWriter(outputFileStream, enc);

            var readers = chunkFiles
                .Select(f =>
                {
                    var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024 * 1024);
                    return new StreamReader(fs, enc);
                })
                .ToList();

            var queue = readers
                .Select((reader, i) => (index: i, LineData.Parse(reader.ReadLine()!)))
                .ToPriorityQueue();

            // K-WAY MERGE LOOP
            while (queue.TryDequeue(out var readerIndex, out var data))
            {
                writer.WriteLine(data.OriginalLine);

                var nextLine = readers[readerIndex].ReadLine();
                if (nextLine == null) continue;

                var nextData = LineData.Parse(nextLine);
                queue.Enqueue(readerIndex, nextData);
            }

            readers.ForEach(x => x.Dispose());
        }
    }
}
