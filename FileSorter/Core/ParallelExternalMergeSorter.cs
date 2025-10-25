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

            // Step 2: Sort chunks in parallel (multiple chunks at once)
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

            using var reader = new StreamReader(File.OpenRead(_config.InputFilePath), enc, true, 16 * 1024 * 1024);

            StreamWriter? writer = null;
            var size = 0;
            var index = 0;
            System.Diagnostics.Stopwatch? chunkStopwatch = null;
            var chunkLineCount = 0;
            long chunkByteCount = 0;
            var currentChunkIndex = -1;

            void FinalizeChunk()
            {
                if (writer is null || chunkStopwatch is null || currentChunkIndex < 0)
                    return;

                writer.Flush();
                writer.Dispose();

                var elapsedMs = chunkStopwatch.Elapsed.TotalMilliseconds;
                chunkStopwatch.Stop();

                Log.Information("  ⏱️  Raw chunk {Index:D4}: Lines={LineCount:N0}, Bytes={ByteCount:N0}, Time={TimeMs:F0}ms",
                    currentChunkIndex, chunkLineCount, chunkByteCount, elapsedMs);

                writer = null;
                chunkStopwatch = null;
                chunkLineCount = 0;
                chunkByteCount = 0;
                currentChunkIndex = -1;
            }

            while (reader.ReadLine() is { } line)
            {
                if (writer is null || size >= _chunkSizeBytes)
                {
                    FinalizeChunk();
                    var path = Path.Combine(_config.TempDirectory, $"raw_chunk_{index:D4}.txt");
                    currentChunkIndex = index;
                    index++;
                    chunkCount++;
                    writer = new StreamWriter(path, false, enc, 16 * 1024 * 1024);
                    size = 0;
                    chunkStopwatch = System.Diagnostics.Stopwatch.StartNew();
                }

                writer.WriteLine(line);
                var lineBytes = enc.GetByteCount(line) + nlBytes;
                size += lineBytes;
                chunkLineCount++;
                chunkByteCount += lineBytes;
            }

            FinalizeChunk();
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
               // .AsParallel()
                // .WithDegreeOfParallelism(maxParallelism)
                .Select(x =>
                {
                    var (rawChunkFile, i) = x;

                    Log.Information("🔄 Sorting chunk {Index}/{Total}...", i + 1, rawChunkFiles.Count);
                    var sortedChunkFile = Path.Combine(_config.TempDirectory, $"sorted_chunk_{i:D4}.txt");

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    var lines = File.ReadAllLines(rawChunkFile, new UTF8Encoding(false));
                    var readTime = sw.Elapsed.TotalMilliseconds;

                    var sortedLines = new LineData[lines.Length];

                    Parallel.For(0, lines.Length,
                        new ParallelOptions { MaxDegreeOfParallelism = 32 },
                        j =>
                    {
                        sortedLines[j] = LineData.Parse(lines[j]);
                    });
                    var parseTime = sw.Elapsed.TotalMilliseconds - readTime;

                    Array.Sort(sortedLines);
                    var sortTime = sw.Elapsed.TotalMilliseconds - readTime - parseTime;

                    using (var writer = new StreamWriter(sortedChunkFile, false, new UTF8Encoding(false), 16 * 1024 * 1024))
                    {
                        foreach (var line in sortedLines)
                            writer.WriteLine(line.OriginalLine);
                    }
                    var writeTime = sw.Elapsed.TotalMilliseconds - readTime - parseTime - sortTime;

                    File.Delete(rawChunkFile);
                    var deleteTime = sw.Elapsed.TotalMilliseconds - readTime - parseTime - sortTime - writeTime;
                    var totalTime = sw.Elapsed.TotalMilliseconds;

                    Log.Information("  ⏱️  Chunk {Index}: Lines={LineCount:N0}, Read={ReadMs:F0}ms, Parse={ParseMs:F0}ms, Sort={SortMs:F0}ms, Write={WriteMs:F0}ms, Delete={DeleteMs:F0}ms, Total={TotalMs:F0}ms",
                        i + 1, sortedLines.Length, readTime, parseTime, sortTime, writeTime, deleteTime, totalTime);

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
            var sw = System.Diagnostics.Stopwatch.StartNew();

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

            var setupTime = sw.Elapsed.TotalMilliseconds;
            var linesMerged = 0L;

            // K-WAY MERGE LOOP
            while (queue.TryDequeue(out var readerIndex, out var data))
            {
                writer.WriteLine(data.OriginalLine);
                linesMerged++;

                var nextLine = readers[readerIndex].ReadLine();
                if (nextLine == null) continue;

                var nextData = LineData.Parse(nextLine);
                queue.Enqueue(readerIndex, nextData);
            }

            readers.ForEach(x => x.Dispose());

            var loopTime = sw.Elapsed.TotalMilliseconds - setupTime;
            var totalTime = sw.Elapsed.TotalMilliseconds;
            var cleanupTime = totalTime - setupTime - loopTime;

            Log.Information("  ⏱️  Merge stats: Setup={SetupMs:F0}ms, Loop={LoopMs:F0}ms, Cleanup={CleanupMs:F0}ms, Lines={LinesMerged:N0}, Total={TotalMs:F0}ms",
                setupTime, loopTime, cleanupTime, linesMerged, totalTime);
        }
    }
}
