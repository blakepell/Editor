namespace Nano;

internal sealed class Renderer
{
    private readonly IConsoleDriver _console;
    private TerminalSize _lastSize;

    public Renderer(IConsoleDriver console)
    {
        _console = console;
    }

    public void Render(EditorSession session)
    {
        _console.BeginFrame();
        _console.ShowCursor(false);
        _console.UseBlockCursor();

        TerminalSize size = _console.Size;
        if (size != _lastSize)
        {
            _console.Clear();
            _lastSize = size;
        }

        session.Layout = EditorLayout.From(size);
        session.EnsureCursorVisible();

        DrawTitle(session);
        DrawEditor(session);
        DrawStatus(session);
        DrawShortcuts(session);
        PositionCursor(session);
        _console.EndFrame();
    }

    private void DrawTitle(EditorSession session)
    {
        var doc = session.Document;
        string dirty = doc.Modified ? " Modified" : string.Empty;
        string mode = session.IsReadOnly ? " [Read Only]" : string.Empty;
        string text = $" Nano.cs 0.1   {doc.DisplayName}{dirty}{mode}";
        WritePadded(0, text, ConsoleStyle.Title);
    }

    private void DrawEditor(EditorSession session)
    {
        int rows = session.Layout.EditorRows;
        int columns = session.Layout.Columns;
        int margin = session.LineNumberMargin;
        Dictionary<int, List<HighlightSpan>> highlightsByLine = session.GetHighlightSpans(session.ViewTop, rows);

        for (int screenRow = 0; screenRow < rows; screenRow++)
        {
            int bufferLine = session.ViewTop + screenRow;
            int row = session.Layout.EditorTop + screenRow;

            if (bufferLine >= session.Document.LineCount)
            {
                _console.WriteAt(row, 0, string.Empty.PadRight(columns), ConsoleStyle.Normal);
                continue;
            }

            if (margin > 0)
            {
                string number = (bufferLine + 1).ToString().PadLeft(margin - 1) + " ";
                _console.WriteAt(row, 0, Fit(number, margin), ConsoleStyle.LineNumber);
            }

            string visible = session.GetVisibleLine(bufferLine);
            visible = Fit(visible, Math.Max(0, columns - margin));
            _console.WriteAt(row, margin, visible.PadRight(Math.Max(0, columns - margin)), ConsoleStyle.Normal);
            DrawHighlights(session, highlightsByLine, bufferLine, row, margin);

            if (session.Selection is Position mark)
            {
                DrawSelectionForLine(session, mark, bufferLine, row, margin);
            }
        }
    }

    private void DrawHighlights(
        EditorSession session,
        Dictionary<int, List<HighlightSpan>> highlightsByLine,
        int bufferLine,
        int row,
        int margin)
    {
        int viewLeft = session.ViewLeft;
        int editColumns = Math.Max(0, session.Layout.Columns - margin);
        if (editColumns == 0)
        {
            return;
        }

        string line = session.Document.LineAt(bufferLine).ToString();
        if (!highlightsByLine.TryGetValue(bufferLine, out List<HighlightSpan>? spans))
        {
            return;
        }

        foreach (HighlightSpan span in spans)
        {
            int start = Math.Max(span.Start, viewLeft);
            int end = Math.Min(span.Start + span.Length, viewLeft + editColumns);
            if (end <= start || start >= line.Length)
            {
                continue;
            }

            int length = Math.Min(end - start, line.Length - start);
            if (length <= 0)
            {
                continue;
            }

            _console.WriteAt(row, margin + start - viewLeft, line.Substring(start, length), span.Style);
        }
    }

    private void DrawSelectionForLine(EditorSession session, Position mark, int bufferLine, int row, int margin)
    {
        var (start, end) = DocumentBuffer.Order(mark, session.Cursor);
        if (bufferLine < start.Line || bufferLine > end.Line)
        {
            return;
        }

        int lineLength = session.Document.LineAt(bufferLine).Length;
        int startColumn = bufferLine == start.Line ? start.Column : 0;
        int endColumn = bufferLine == end.Line ? end.Column : lineLength;
        if (endColumn <= startColumn)
        {
            return;
        }

        int viewLeft = session.ViewLeft;
        int first = Math.Max(startColumn, viewLeft);
        int last = Math.Min(endColumn, viewLeft + session.Layout.EditColumns);
        if (last <= first)
        {
            return;
        }

        string selected = session.Document.LineAt(bufferLine).Substring(first, last - first);
        _console.WriteAt(row, margin + first - viewLeft, selected, ConsoleStyle.Selection);
    }

    private void DrawStatus(EditorSession session)
    {
        string text = session.StatusMessage;
        if (string.IsNullOrEmpty(text))
        {
            var cursor = session.Cursor;
            text = $"Line {cursor.Line + 1}, Col {cursor.Column + 1}";
        }

        WritePadded(session.Layout.StatusRow, text, session.StatusIsAlert ? ConsoleStyle.Status with { Background = ConsoleColor.Red } : ConsoleStyle.Status);
    }

    private void DrawShortcuts(EditorSession session)
    {
        (string Key, string Text)[][] rows =
        [
            [("^G", "Help"), ("^X", "Exit"), ("^O", "Write Out"), ("^R", "Read File"), ("^W", "Where Is"), ("^\\", "Replace")],
            [("^K", "Cut"), ("^U", "Paste"), ("^6", "Mark"), ("M-6", "Copy"), ("M-U", "Undo"), ("M-E", "Redo")]
        ];

        for (int i = 0; i < rows.Length; i++)
        {
            int row = session.Layout.ShortcutTop + i;
            if (row < session.Layout.Rows)
            {
                DrawShortcutRow(row, rows[i]);
            }
        }
    }

    private void DrawShortcutRow(int row, (string Key, string Text)[] items)
    {
        int columns = _console.Size.Columns;
        _console.WriteAt(row, 0, string.Empty.PadRight(columns), ConsoleStyle.ShortcutText);

        int itemWidth = Math.Max(10, columns / Math.Max(1, items.Length));
        int column = 0;

        foreach ((string key, string text) in items)
        {
            if (column >= columns)
            {
                break;
            }

            string keyText = Fit(key, Math.Min(4, columns - column)).PadRight(Math.Min(4, columns - column));
            _console.WriteAt(row, column, keyText, ConsoleStyle.ShortcutKey);
            column += keyText.Length;

            int textWidth = Math.Min(Math.Max(0, itemWidth - keyText.Length), columns - column);
            if (textWidth > 0)
            {
                _console.WriteAt(row, column, Fit(" " + text, textWidth).PadRight(textWidth), ConsoleStyle.ShortcutText);
                column += textWidth;
            }
        }
    }

    private void PositionCursor(EditorSession session)
    {
        int row = session.Layout.EditorTop + session.Cursor.Line - session.ViewTop;
        int column = session.LineNumberMargin + session.Cursor.Column - session.ViewLeft;
        _console.MoveCursor(row, column);
        _console.UseBlockCursor();
        _console.ShowCursor(true);
    }

    private void WritePadded(int row, string text, ConsoleStyle style)
    {
        int columns = _console.Size.Columns;
        _console.WriteAt(row, 0, Fit(text, columns).PadRight(columns), style);
    }

    private static string Fit(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        return value.Length <= width ? value : value[..width];
    }
}

internal readonly record struct EditorLayout(int Rows, int Columns, int EditorTop, int EditorRows, int StatusRow, int ShortcutTop)
{
    public int EditColumns => Columns;

    public static EditorLayout From(TerminalSize size)
    {
        int rows = Math.Max(5, size.Rows);
        int editorRows = Math.Max(1, rows - 4);
        return new EditorLayout(rows, size.Columns, 1, editorRows, rows - 3, rows - 2);
    }
}
