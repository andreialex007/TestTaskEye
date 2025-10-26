using System.Text;

namespace Generator.Core;

public static class FileTools
{
    private const int StreamWriterBufferSize = 1 * 1024 * 1024; // 1 MB
    private const int FileMergeBufferSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// UTF-8 encoding without BOM (Byte Order Mark).
    /// Reused across all file operations to avoid repeated object creation.
    /// </summary>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public static void GenerateFileChunk(string filePath, long targetBytes, int maxNumber, string[] sourceStrings)
    {
        long bytesWritten = 0;

        using var writer = new StreamWriter(filePath, false, Utf8NoBom, bufferSize: StreamWriterBufferSize);

        while (bytesWritten < targetBytes)
        {
            var number = Random.Shared.Next(1, maxNumber + 1);
            var textIndex = Random.Shared.Next(sourceStrings.Length);
            var text = sourceStrings[textIndex];
            var line = $"{number}. {text}";

            writer.WriteLine(line);

            bytesWritten += line.Length + Environment.NewLine.Length;
        }
    }

    public static void MergeFiles(string[] tempFiles, string outputFile)
    {
        using var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, FileMergeBufferSize);

        foreach (var tempFile in tempFiles)
        {
            using var input = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, FileMergeBufferSize);
            input.CopyTo(output, FileMergeBufferSize);
        }
    }
}
