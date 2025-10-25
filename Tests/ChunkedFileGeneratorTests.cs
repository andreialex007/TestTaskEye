using Generator.Core;
using Generator.Dto;

namespace Tests;

[TestClass]
public class ChunkedFileGeneratorTests
{
    private const string TestOutputPath = "test_output.txt";

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(TestOutputPath))
            File.Delete(TestOutputPath);

        // Clean up temp files created by ChunkedFileGenerator
        var tempFiles = Directory.GetFiles(".", "temp_*.txt");
        foreach (var file in tempFiles)
        {
            try { File.Delete(file); } catch { }
        }
    }

    [TestMethod]
    public void GenerateFile_CreatesFile()
    {
        // Arrange
        var config = new GeneratorConfig
        {
            OutputFilePath = TestOutputPath,
            TargetFileSizeGB = 0.001, // 1 MB
            MaxNumber = 100
        };
        var generator = new ChunkedFileGenerator(config);

        // Act
        generator.GenerateFile();

        // Assert
        Assert.IsTrue(File.Exists(TestOutputPath), "Output file should be created");
    }

    [TestMethod]
    public void GenerateFile_CreatesNonEmptyFile()
    {
        // Arrange
        var config = new GeneratorConfig
        {
            OutputFilePath = TestOutputPath,
            TargetFileSizeGB = 0.001, // 1 MB
            MaxNumber = 100
        };
        var generator = new ChunkedFileGenerator(config);

        // Act
        generator.GenerateFile();

        // Assert
        var fileInfo = new FileInfo(TestOutputPath);
        Assert.IsTrue(fileInfo.Length > 0, "File should not be empty");
    }

    [TestMethod]
    public void GenerateFile_ProducesValidFormat()
    {
        // Arrange
        var config = new GeneratorConfig
        {
            OutputFilePath = TestOutputPath,
            TargetFileSizeGB = 0.001, // 1 MB
            MaxNumber = 100
        };
        var generator = new ChunkedFileGenerator(config);

        // Act
        generator.GenerateFile();

        // Assert
        var lines = File.ReadAllLines(TestOutputPath);
        Assert.IsTrue(lines.Length > 0, "File should contain lines");

        // Check first 10 lines have correct format: "Number. Text"
        foreach (var line in lines.Take(10))
        {
            var parts = line.Split(". ", 2);
            Assert.AreEqual(2, parts.Length, $"Line should have format 'Number. Text': {line}");
            Assert.IsTrue(int.TryParse(parts[0], out _), $"First part should be a number: {line}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(parts[1]), $"Second part should not be empty: {line}");
        }
    }

    [TestMethod]
    public void GenerateFile_RespectsSizeTarget()
    {
        // Arrange
        var targetSizeGB = 0.005; // 5 MB
        var config = new GeneratorConfig
        {
            OutputFilePath = TestOutputPath,
            TargetFileSizeGB = targetSizeGB,
            MaxNumber = 100
        };
        var generator = new ChunkedFileGenerator(config);

        // Act
        generator.GenerateFile();

        // Assert
        var fileInfo = new FileInfo(TestOutputPath);
        var actualSizeGB = fileInfo.Length / (1024.0 * 1024.0 * 1024.0);

        // Allow 20% tolerance
        var tolerance = targetSizeGB * 0.2;
        Assert.IsTrue(Math.Abs(actualSizeGB - targetSizeGB) <= tolerance,
            $"File size {actualSizeGB:F3} GB should be close to target {targetSizeGB:F3} GB");
    }
}
