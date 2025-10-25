namespace FileSorter.Dto;

public readonly struct LineData : IComparable<LineData>
{
    public int Number { get; }
    public string Text { get; }
    public string OriginalLine { get; }

    public LineData(int number, string text, string originalLine)
    {
        Number = number;
        Text = text;
        OriginalLine = originalLine;
    }

    public int CompareTo(LineData other)
    {
        // First sort by text (alphabetically)
        var textComparison = string.Compare(Text, other.Text, StringComparison.Ordinal);
        if (textComparison != 0)
            return textComparison;

        // If text is the same, sort by number (ascending)
        return Number.CompareTo(other.Number);
    }

    public static LineData Parse(string line)
    {
        var dotIndex = line.IndexOf('.');
        if (dotIndex == -1)
            throw new FormatException($"Invalid line format: {line}");

        var numberPart = line[..dotIndex].Trim();
        var textPart = line[(dotIndex + 1)..].Trim();

        if (!int.TryParse(numberPart, out var number))
            throw new FormatException($"Invalid number in line: {line}");

        return new LineData(number, textPart, line);
    }
}
