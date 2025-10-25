using CommandLine;

namespace TestTaskEye.Dto;

public class GeneratorConfig
{
    [Option('o', "output", Required = false, HelpText = "Output file path (default: output.txt)")]
    public string OutputFilePath { get; set; } = "output.txt";

    [Option('s', "size", Required = false, HelpText = "Target file size in GB (default: 50.0)")]
    public double? TargetFileSizeGB { get; set; } = 2.0;

    [Option('m', "max-number", Required = false, HelpText = "Maximum number for random generation (default: 100)")]
    public int MaxNumber { get; set; } = 100;
}
