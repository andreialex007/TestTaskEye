using CommandLine;

namespace Generator.Dto;

public class GeneratorConfig
{
    [Option('o', "output", Required = false, HelpText = "Output file path (default: output.txt)")]
    public string OutputFilePath { get; set; } = "output.txt";

    [Option('s', "size", Required = false, HelpText = "Target file size in GB (default: 2.0)")]
    public double? TargetFileSizeGB { get; set; } = 2.0;

    [Option('m', "max-number", Required = false, Min = 1, HelpText = "Maximum number for random generation (default: 1000, must be >= 1)")]
    public int MaxNumber { get; set; } = 1000;
}
