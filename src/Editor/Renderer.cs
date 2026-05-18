/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using NEdit.Commands;

namespace NEdit.Editor
{
    internal sealed class Renderer
    {
        private static readonly string Spaces = new(' ', 256);
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

        public void RenderCommandPalette(
            EditorSession session,
            string query,
            int queryCursor,
            IReadOnlyList<EditorCommand> commands,
            int selectedIndex,
            EditorCommandContext context)
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
            int inputColumn = DrawCommandInput(session, query, queryCursor);
            DrawShortcuts(session);
            DrawCommandPalette(session, commands, selectedIndex, context);
            _console.MoveCursor(session.Layout.StatusRow, inputColumn);
            _console.UseBlockCursor();
            _console.ShowCursor(true);
            _console.EndFrame();
        }

        public void RenderFileBrowser(
            EditorSession session,
            string query,
            int queryCursor,
            IReadOnlyList<FileEntry> entries,
            int selectedIndex,
            string currentDir)
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
            int inputColumn = DrawFileInput(session, query, queryCursor, currentDir);
            DrawShortcuts(session);
            DrawFileBrowser(session, entries, selectedIndex);
            _console.MoveCursor(session.Layout.StatusRow, inputColumn);
            _console.UseBlockCursor();
            _console.ShowCursor(true);
            _console.EndFrame();
        }

        private void DrawTitle(EditorSession session)
        {
            var doc = session.Document;
            string dirty = doc.Modified ? " [Modified]" : string.Empty;
            string mode = session.IsReadOnly ? " [Read Only]" : string.Empty;
            string left = $" {AppSettings.AppName} {AppSettings.Version} : {doc.DisplayName}{dirty}{mode}";
            WritePadded(0, left, ConsoleStyle.Title);

            string build = AppSettings.BuildVersion;
            if (build.Length > 0)
            {
                string right = $"Build: {build} ";
                int col = _console.Size.Columns - right.Length;
                if (col > left.Length)
                {
                    _console.WriteAt(0, col, right, ConsoleStyle.Title);
                }
            }
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
                    WritePadded(row, 0, ReadOnlySpan<char>.Empty, columns, ConsoleStyle.Normal);
                    continue;
                }

                if (margin > 0)
                {
                    string number = (bufferLine + 1).ToString().PadLeft(margin - 1) + " ";
                    _console.WriteAt(row, 0, Fit(number, margin), ConsoleStyle.LineNumber);
                }

                string visible = session.GetDisplayLine(bufferLine);
                int visibleStart = Math.Min(session.ViewLeft, visible.Length);
                WritePadded(row, margin, visible.AsSpan(visibleStart), Math.Max(0, columns - margin), ConsoleStyle.Normal);
                DrawHighlights(session, highlightsByLine, bufferLine, row, margin);

                if (session.Selection is Position mark)
                {
                    DrawSelectionForLine(session, mark, bufferLine, row, margin);
                }
            }
        }

        private void DrawHighlights(EditorSession session, Dictionary<int, List<HighlightSpan>> highlightsByLine, int bufferLine, int row, int margin)
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

                _console.WriteAt(row, margin + start - viewLeft, line.AsSpan(start, length), span.Style);
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

            WritePadded(session.Layout.StatusRow, text, session.StatusStyle ?? ConsoleStyle.Status);
        }

        private void DrawShortcuts(EditorSession session)
        {
            (string Key, string Text)[][] rows =
            [
                [("^T", "Commands"), ("F5", "Run"), ("^X", "Exit"), ("^O", "Open"), ("^!S", "Save"), ("^F", "Find"), ("^H", "Replace")],
                [("^K", "Cut"), ("^U", "Paste"), ("^6", "Mark"), ("M-6", "Copy"), ("M-U", "Undo"), ("M-E", "Redo"), ("^G", "GUID")]
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
            WritePadded(row, 0, ReadOnlySpan<char>.Empty, columns, ConsoleStyle.ShortcutText);

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

        private int DrawCommandInput(EditorSession session, string query, int cursor)
        {
            int row = session.Layout.StatusRow;
            int columns = session.Layout.Columns;
            string prefix = "Command: ";
            int inputWidth = Math.Max(0, columns - prefix.Length);
            int start = 0;
            if (query.Length > inputWidth)
            {
                start = Math.Clamp(cursor - inputWidth + 1, 0, query.Length - inputWidth);
            }

            string visibleInput = inputWidth == 0
                ? string.Empty
                : query.Substring(start, Math.Min(inputWidth, query.Length - start));
            string text = Fit(prefix + visibleInput, columns);
            WritePadded(row, text, ConsoleStyle.Status);
            return Math.Min(columns - 1, prefix.Length + Math.Clamp(cursor - start, 0, visibleInput.Length));
        }

        private void DrawCommandPalette(
            EditorSession session,
            IReadOnlyList<EditorCommand> commands,
            int selectedIndex,
            EditorCommandContext context)
        {
            int height = session.Layout.StatusRow - session.Layout.EditorTop;
            int columns = session.Layout.Columns;
            if (height < 3 || columns < 10)
            {
                return;
            }

            int width = Math.Min(columns, Math.Clamp(columns / 2, 28, 48));
            int left = Math.Max(0, columns - width);
            int top = session.Layout.EditorTop;
            int bottom = top + height - 1;
            int innerWidth = Math.Max(0, width - 2);

            DrawPanelBorder(top, left, width, " Commands ");
            for (int row = top + 1; row < bottom; row++)
            {
                _console.WriteAt(row, left, "|", ConsoleStyle.ShortcutKey);
                WritePadded(row, left + 1, ReadOnlySpan<char>.Empty, innerWidth, ConsoleStyle.Normal);
                _console.WriteAt(row, left + width - 1, "|", ConsoleStyle.ShortcutKey);
            }

            _console.WriteAt(bottom, left, "+" + new string('-', Math.Max(0, width - 2)) + "+", ConsoleStyle.ShortcutKey);

            int listTop = top + 1;
            int listRows = Math.Max(0, bottom - listTop);
            if (commands.Count == 0)
            {
                if (listRows > 0)
                {
                    _console.WriteAt(listTop, left + 1, Fit(" No matches", innerWidth).PadRight(innerWidth), ConsoleStyle.Normal);
                }

                return;
            }

            int first = 0;
            if (selectedIndex >= listRows)
            {
                first = selectedIndex - listRows + 1;
            }

            for (int i = 0; i < listRows && first + i < commands.Count; i++)
            {
                int commandIndex = first + i;
                EditorCommand command = commands[commandIndex];
                bool selected = commandIndex == selectedIndex;
                bool enabled = command.CanExecute(context);
                string prefix = selected ? "> " : "  ";
                string disabled = enabled ? string.Empty : " [disabled]";
                string label = prefix + command.Name + disabled;
                ConsoleStyle style = selected ? ConsoleStyle.Selection : enabled ? ConsoleStyle.Normal : ConsoleStyle.LineNumber;
                _console.WriteAt(listTop + i, left + 1, Fit(label, innerWidth).PadRight(innerWidth), style);
            }
        }

        private void DrawPanelBorder(int row, int left, int width, string title)
        {
            if (width < 2)
            {
                return;
            }

            string line = "+" + new string('-', Math.Max(0, width - 2)) + "+";
            if (width > 4 && title.Length < width - 2)
            {
                line = "+" + Fit(title, width - 2).PadRight(width - 2, '-') + "+";
            }

            _console.WriteAt(row, left, line, ConsoleStyle.ShortcutKey);
        }

        private int DrawFileInput(EditorSession session, string query, int cursor, string currentDir)
        {
            int row = session.Layout.StatusRow;
            int columns = session.Layout.Columns;
            string dirLabel = currentDir.Length > columns / 3 ? ".." + currentDir[^(columns / 3)..] : currentDir;
            string prefix = $"Open [{dirLabel}]: ";
            int inputWidth = Math.Max(0, columns - prefix.Length);
            int start = 0;
            if (query.Length > inputWidth)
            {
                start = Math.Clamp(cursor - inputWidth + 1, 0, query.Length - inputWidth);
            }

            string visibleInput = inputWidth == 0
                ? string.Empty
                : query.Substring(start, Math.Min(inputWidth, query.Length - start));
            string text = Fit(prefix + visibleInput, columns);
            WritePadded(row, text, ConsoleStyle.Status);
            return Math.Min(columns - 1, prefix.Length + Math.Clamp(cursor - start, 0, visibleInput.Length));
        }

        private void DrawFileBrowser(EditorSession session, IReadOnlyList<FileEntry> entries, int selectedIndex)
        {
            int height = session.Layout.StatusRow - session.Layout.EditorTop;
            int columns = session.Layout.Columns;
            if (height < 3 || columns < 10)
            {
                return;
            }

            int width = Math.Min(columns, Math.Clamp(columns / 2, 30, 58));
            int left = Math.Max(0, columns - width);
            int top = session.Layout.EditorTop;
            int bottom = top + height - 1;
            int innerWidth = Math.Max(0, width - 2);

            DrawPanelBorder(top, left, width, " Open File ");
            for (int row = top + 1; row < bottom; row++)
            {
                _console.WriteAt(row, left, "|", ConsoleStyle.ShortcutKey);
                WritePadded(row, left + 1, ReadOnlySpan<char>.Empty, innerWidth, ConsoleStyle.Normal);
                _console.WriteAt(row, left + width - 1, "|", ConsoleStyle.ShortcutKey);
            }

            _console.WriteAt(bottom, left, "+" + new string('-', Math.Max(0, width - 2)) + "+", ConsoleStyle.ShortcutKey);

            int listTop = top + 1;
            int listRows = Math.Max(0, bottom - listTop);
            if (entries.Count == 0)
            {
                if (listRows > 0)
                {
                    _console.WriteAt(listTop, left + 1, Fit(" No files found", innerWidth).PadRight(innerWidth), ConsoleStyle.Normal);
                }

                return;
            }

            int first = 0;
            if (selectedIndex >= listRows)
            {
                first = selectedIndex - listRows + 1;
            }

            for (int i = 0; i < listRows && first + i < entries.Count; i++)
            {
                int entryIndex = first + i;
                FileEntry entry = entries[entryIndex];
                bool selected = entryIndex == selectedIndex;
                string icon = entry.IsDirectory ? "/" : " ";
                string marker = selected ? ">" : " ";
                string label = $"{marker}{icon} {entry.Name}";
                ConsoleStyle style = selected ? ConsoleStyle.Selection : entry.IsDirectory ? ConsoleStyle.LineNumber : ConsoleStyle.Normal;
                _console.WriteAt(listTop + i, left + 1, Fit(label, innerWidth).PadRight(innerWidth), style);
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
            WritePadded(row, 0, text.AsSpan(), columns, style);
        }

        private void WritePadded(int row, int column, ReadOnlySpan<char> text, int width, ConsoleStyle style)
        {
            if (width <= 0)
            {
                return;
            }

            int textLength = Math.Min(text.Length, width);
            if (textLength > 0)
            {
                _console.WriteAt(row, column, text[..textLength], style);
            }

            WriteSpaces(row, column + textLength, width - textLength, style);
        }

        private void WriteSpaces(int row, int column, int count, ConsoleStyle style)
        {
            while (count > 0)
            {
                int chunk = Math.Min(count, Spaces.Length);
                _console.WriteAt(row, column, Spaces.AsSpan(0, chunk), style);
                column += chunk;
                count -= chunk;
            }
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
}
