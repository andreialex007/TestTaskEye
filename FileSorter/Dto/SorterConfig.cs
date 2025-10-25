using CommandLine;

namespace FileSorter.Dto;

public class SorterConfig
{
    [Option('i', "input", Required = false, HelpText = "Input file path (default: input.txt)")]
    public string InputFilePath { get; set; } = @"input.txt";

    [Option('o', "output", Required = false, HelpText = "Output file path (default: output.txt)")]
    public string OutputFilePath { get; set; } = "output.txt";

    [Option('c', "chunk-size", Required = false, HelpText = "Chunk size in MB (default: 100 MB)")]
    public int ChunkSizeMB { get; set; } = 100;

    [Option('t', "temp-dir", Required = false, HelpText = "Temporary directory for chunk files (default: ./temp)")]
    public string TempDirectory { get; set; } = "./temp";
}
