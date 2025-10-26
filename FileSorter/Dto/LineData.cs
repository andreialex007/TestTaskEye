using System.Collections.Concurrent;

namespace FileSorter.Dto;

public readonly struct LineData(int number, string text, string originalLine) : IComparable<LineData>
{
    /// <summary>
    /// Thread-safe cache for text values to reduce string allocations.
    /// Since text values repeat frequently (limited fruit names), we reuse identical strings.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> _textCache = new();

    public int Number { get; } = number;
    public string Text { get; } = text;
    public string OriginalLine { get; } = originalLine;

    public int CompareTo(LineData other)
    {
        var textComparison = string.Compare(Text, other.Text, StringComparison.Ordinal);
        return textComparison != 0 ? textComparison : Number.CompareTo(other.Number);
    }

    public static LineData Parse(string line)
    {
        var dotIndex = line.IndexOf('.');
        if (dotIndex == -1)
            throw new FormatException($"Invalid line format: {line}");

        var span = line.AsSpan();
        var numberSpan = span[..dotIndex].Trim();
        var textSpan = span[(dotIndex + 1)..].Trim();

        if (!int.TryParse(numberSpan, out var number))
            throw new FormatException($"Invalid number in line: {line}");

        var textPart = new string(textSpan);

        // Reuse cached string if seen before to reduce memory allocations
        // GetOrAdd is thread-safe and atomic
        var cachedText = _textCache.GetOrAdd(textPart, textPart);

        return new LineData(number, cachedText, line);
    }
}
