using System.Text;
using FileSorter.Dto;
using Humanizer;
using Serilog;
using SerilogTimings;

namespace FileSorter.Core;

public class ParallelExternalMergeSorter
{
    // Buffer size constants
    private const int StandardBufferSize = 4 * 1024 * 1024;      // 4 MB
    private const int ChunkReaderBufferSize = 16 * 1024 * 1024;  // 16 MB
    private const int OutputWriterBufferSize = 128 * 1024 * 1024; // 128 MB

    // Conversion constants
    private const long BytesPerMegabyte = 1024L * 1024L;

    // Encoding constant - reused across all file operations to avoid repeated object creation
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly SorterConfig _config;
    private readonly long _chunkSizeBytes;

    public ParallelExternalMergeSorter(SorterConfig config)
    {
        _config = config;
        _chunkSizeBytes = _config.ChunkSizeMB * BytesPerMegabyte;
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
            var newLineByteCount = Utf8NoBom.GetByteCount(Environment.NewLine);

            using var reader = new StreamReader(File.OpenRead(_config.InputFilePath), Utf8NoBom, true, StandardBufferSize);

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
                    writer = new StreamWriter(path, false, Utf8NoBom, StandardBufferSize);
                    size = 0;
                }

                writer.WriteLine(line);
                size += Utf8NoBom.GetByteCount(line) + newLineByteCount;
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

                    var sortedLines = File.ReadAllLines(rawChunkFile, Utf8NoBom)
                        .Select(LineData.Parse)
                        .ToArray();

                    Array.Sort(sortedLines);

                    using (var writer = new StreamWriter(sortedChunkFile, false, Utf8NoBom, StandardBufferSize))
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
            using var outputFileStream = new FileStream(_config.OutputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, OutputWriterBufferSize);
            using var writer = new StreamWriter(outputFileStream, Utf8NoBom);

            var readers = chunkFiles
                .Select(f =>
                {
                    var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkReaderBufferSize);
                    return new StreamReader(fs, Utf8NoBom);
                })
                .ToList();

            try
            {
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
            }
            finally
            {
                readers.ForEach(x => x.Dispose());
            }
        }
    }
}
