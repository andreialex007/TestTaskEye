using System.Text;

namespace Generator.Core;

public static class FileTools
{
    public static void GenerateFileChunk(string filePath, long targetBytes, int coreIndex, int maxNumber, string[] sourceStrings)
    {
        long bytesWritten = 0;

        using var writer = new StreamWriter(filePath, false, new UTF8Encoding(false), bufferSize: 1024 * 1024);

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
        const int bufferSize = 10 * 1024 * 1024; // 10MB buffer

        using var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);

        foreach (var tempFile in tempFiles)
        {
            using var input = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
            input.CopyTo(output, bufferSize);
        }
    }
}
