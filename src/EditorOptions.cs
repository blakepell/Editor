namespace Nano;

internal sealed class EditorOptions
{
    public bool ReadOnly { get; set; }
    public bool LineNumbers { get; set; }
    public bool SoftWrap { get; set; }
    public bool CaseSensitiveSearch { get; set; }
    public int TabSize { get; set; } = 4;
}

internal readonly record struct Position(int Line, int Column) : IComparable<Position>
{
    public int CompareTo(Position other)
    {
        int lineCompare = Line.CompareTo(other.Line);
        return lineCompare != 0 ? lineCompare : Column.CompareTo(other.Column);
    }
}

internal readonly record struct TerminalSize(int Rows, int Columns);

internal enum NewLineKind
{
    Unix,
    Windows,
    Mac
}
