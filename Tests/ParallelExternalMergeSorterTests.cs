using FileSorter.Core;
using FileSorter.Dto;

namespace Tests;

[TestClass]
public class ParallelExternalMergeSorterTests
{
    private const string TestInputPath = "test_input.txt";
    private const string TestOutputPath = "test_sorted.txt";
    private const string TestTempDir = "./test_temp";

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(TestInputPath))
            File.Delete(TestInputPath);
        if (File.Exists(TestOutputPath))
            File.Delete(TestOutputPath);
        if (Directory.Exists(TestTempDir))
            Directory.Delete(TestTempDir, true);
    }

    private void CreateTestFile(string path, string[] lines)
    {
        File.WriteAllLines(path, lines);
    }

    [TestMethod]
    public void Sort_CreatesOutputFile()
    {
        // Arrange
        var testData = new[]
        {
            "3. Apple",
            "1. Banana",
            "2. Cherry"
        };
        CreateTestFile(TestInputPath, testData);

        var config = new SorterConfig
        {
            InputFilePath = TestInputPath,
            OutputFilePath = TestOutputPath,
            ChunkSizeMB = 1,
            TempDirectory = TestTempDir
        };
        var sorter = new ParallelExternalMergeSorter(config);

        // Act
        sorter.Sort();

        // Assert
        Assert.IsTrue(File.Exists(TestOutputPath), "Output file should be created");
    }

    [TestMethod]
    public void Sort_SortsByTextThenNumber()
    {
        // Arrange
        var testData = new[]
        {
            "3. Apple",
            "1. Banana",
            "2. Apple",
            "1. Apple"
        };
        CreateTestFile(TestInputPath, testData);

        var config = new SorterConfig
        {
            InputFilePath = TestInputPath,
            OutputFilePath = TestOutputPath,
            ChunkSizeMB = 1,
            TempDirectory = TestTempDir
        };
        var sorter = new ParallelExternalMergeSorter(config);

        // Act
        sorter.Sort();

        // Assert
        var sortedLines = File.ReadAllLines(TestOutputPath);
        Assert.AreEqual(4, sortedLines.Length);
        Assert.AreEqual("1. Apple", sortedLines[0]);
        Assert.AreEqual("2. Apple", sortedLines[1]);
        Assert.AreEqual("3. Apple", sortedLines[2]);
        Assert.AreEqual("1. Banana", sortedLines[3]);
    }

    [TestMethod]
    public void Sort_HandlesLargeDataset()
    {
        // Arrange - Create 10,000 random lines
        var random = new Random(42);
        var texts = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" };
        var testData = Enumerable.Range(0, 10000)
            .Select(_ => $"{random.Next(1, 1001)}. {texts[random.Next(texts.Length)]}")
            .ToArray();

        CreateTestFile(TestInputPath, testData);

        var config = new SorterConfig
        {
            InputFilePath = TestInputPath,
            OutputFilePath = TestOutputPath,
            ChunkSizeMB = 1,
            TempDirectory = TestTempDir
        };
        var sorter = new ParallelExternalMergeSorter(config);

        // Act
        sorter.Sort();

        // Assert
        var sortedLines = File.ReadAllLines(TestOutputPath);
        Assert.AreEqual(testData.Length, sortedLines.Length, "All lines should be preserved");

        // Verify sorted order
        for (int i = 0; i < sortedLines.Length - 1; i++)
        {
            var current = ParseLine(sortedLines[i]);
            var next = ParseLine(sortedLines[i + 1]);

            var textComparison = string.Compare(current.Text, next.Text, StringComparison.Ordinal);
            if (textComparison == 0)
            {
                Assert.IsTrue(current.Number <= next.Number,
                    $"Numbers should be sorted within same text: {sortedLines[i]} vs {sortedLines[i + 1]}");
            }
            else
            {
                Assert.IsTrue(textComparison < 0,
                    $"Text should be sorted: {sortedLines[i]} vs {sortedLines[i + 1]}");
            }
        }
    }

    [TestMethod]
    public void Sort_PreservesAllLines()
    {
        // Arrange
        var testData = Enumerable.Range(1, 1000)
            .Select(i => $"{i}. Item{i % 10}")
            .ToArray();
        CreateTestFile(TestInputPath, testData);

        var config = new SorterConfig
        {
            InputFilePath = TestInputPath,
            OutputFilePath = TestOutputPath,
            ChunkSizeMB = 1,
            TempDirectory = TestTempDir
        };
        var sorter = new ParallelExternalMergeSorter(config);

        // Act
        sorter.Sort();

        // Assert
        var sortedLines = File.ReadAllLines(TestOutputPath);
        Assert.AreEqual(testData.Length, sortedLines.Length, "No lines should be lost");
    }

    [TestMethod]
    public void Sort_CleanupsTempFiles()
    {
        // Arrange
        var testData = new[] { "1. Test", "2. Test" };
        CreateTestFile(TestInputPath, testData);

        var config = new SorterConfig
        {
            InputFilePath = TestInputPath,
            OutputFilePath = TestOutputPath,
            ChunkSizeMB = 1,
            TempDirectory = TestTempDir
        };
        var sorter = new ParallelExternalMergeSorter(config);

        // Act
        sorter.Sort();

        // Assert
        Assert.IsFalse(Directory.Exists(TestTempDir), "Temp directory should be cleaned up");
    }

    private (string Text, int Number) ParseLine(string line)
    {
        var parts = line.Split(". ", 2);
        return (parts[1], int.Parse(parts[0]));
    }
}
