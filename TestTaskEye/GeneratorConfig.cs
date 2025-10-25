using CommandLine;

namespace TestTaskEye;

public class GeneratorConfig
{
    [Option('o', "output", Required = false, Default = "output.txt", HelpText = "Output file path")]
    public string OutputFilePath { get; set; } = "output.txt";

    [Option('s', "size", Required = false, Default = 50.0, HelpText = "Target file size in GB")]
    public double? TargetFileSizeGB { get; set; } = 1.0;

    [Option('m', "max-number", Required = false, Default = 100, HelpText = "Maximum number for random generation")]
    public int MaxNumber { get; set; } = 100;
}
