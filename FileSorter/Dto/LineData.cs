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

        return new LineData(number, textPart, line);
    }
}
