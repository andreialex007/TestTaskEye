using Humanizer;
using Serilog;
using SerilogTimings;
using TestTaskEye.Dto;

namespace TestTaskEye.Core;

public class ChunkedFileGenerator
{
    private readonly GeneratorConfig _config;
    private readonly string[] _sourceStrings;
    private readonly long _targetBytes;

    public ChunkedFileGenerator(GeneratorConfig config)
    {
        _config = config;
        _sourceStrings = DefaultSourceStrings.Values;
        _targetBytes = _config.TargetFileSizeGB.HasValue
            ? (long)(_config.TargetFileSizeGB.Value * 1024 * 1024 * 1024)
            : 0;
    }

    public void GenerateFile()
    {
        using (Operation.Time("🎯 Total file generation"))
        {
            Log.Information($"📝 Generating {_config.TargetFileSizeGB ?? 0:F2} GB file...");

            var cores = Environment.ProcessorCount;
            var bytesPerCore = _targetBytes / cores;

            Log.Information("💪 Using {Cores} cores ({MegabytesPerCore:F2} MB each)", cores, bytesPerCore.Bytes().Megabytes);

            var tempFiles = new string[cores];

            using (Operation.Time("⚙️ Parallel generation"))
            {
                Parallel.For(0, cores, index =>
                {
                    tempFiles[index] = $"temp_{index}.txt";
                    FileTools.GenerateFileChunk(tempFiles[index], bytesPerCore, index, _config.MaxNumber, _sourceStrings);
                });
            }

            Log.Information("🔗 Please wait, merging files...");
            using (Operation.Time("🔗 Merging files"))
                FileTools.MergeFiles(tempFiles, _config.OutputFilePath);

            using (Operation.Time("🧹 Cleanup temp files"))
                Array.ForEach(tempFiles, File.Delete);

            var fileInfo = new FileInfo(_config.OutputFilePath);
            Log.Information($"✅  Generated {fileInfo.Length.Bytes().Gigabytes:F2} GB / {_targetBytes.Bytes().Gigabytes:F2} GB ({(fileInfo.Length * 100.0 / _targetBytes):F2}% accurate)");
        }

        Log.CloseAndFlush();
    }
}
