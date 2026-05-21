/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using NEdit.Commands;

namespace NEdit.Editor
{
    /// <summary>
    /// Renders the editor interface to an <see cref="IConsoleDriver"/>.
    /// </summary>
    internal sealed class Renderer
    {
        private static readonly string Spaces = new(' ', 256);
        private readonly IConsoleDriver _console;
        private readonly EditorCommandCatalog _commandCatalog;
        private TerminalSize _lastSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="console">The console driver used for terminal output.</param>
        /// <param name="commandCatalog">The command catalog used to populate the shortcut bar.</param>
        public Renderer(IConsoleDriver console, EditorCommandCatalog commandCatalog)
        {
            _console = console;
            _commandCatalog = commandCatalog;
        }

        /// <summary>
        /// Renders the normal editor view.
        /// </summary>
        /// <param name="session">The editor session to render.</param>
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

        /// <summary>
        /// Renders the editor with the command palette overlay.
        /// </summary>
        /// <param name="session">The editor session to render.</param>
        /// <param name="query">The command search query.</param>
        /// <param name="queryCursor">The cursor position within <paramref name="query" />.</param>
        /// <param name="commands">The commands currently visible in the palette.</param>
        /// <param name="selectedIndex">The selected command index.</param>
        /// <param name="context">The command context used to determine availability.</param>
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

        /// <summary>
        /// Renders the editor with the file browser overlay.
        /// </summary>
        /// <param name="session">The editor session to render.</param>
        /// <param name="query">The file filter query.</param>
        /// <param name="queryCursor">The cursor position within <paramref name="query" />.</param>
        /// <param name="entries">The file entries currently visible in the browser.</param>
        /// <param name="selectedIndex">The selected file entry index.</param>
        /// <param name="currentDir">The directory currently being browsed.</param>
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
            var barCommands = _commandCatalog.Commands
                .Where(c => c.ShowInStatusBar)
                .OrderBy(c => c.SortOrder)
                .ToList();

            const int itemsPerRow = 8;
            for (int rowIndex = 0; rowIndex * itemsPerRow < barCommands.Count; rowIndex++)
            {
                int row = session.Layout.ShortcutTop + rowIndex;
                if (row >= session.Layout.Rows)
                {
                    break;
                }

                var rowItems = barCommands
                    .Skip(rowIndex * itemsPerRow)
                    .Take(itemsPerRow)
                    .Select(c => (Key: ToShortKey(c.HotKey), Text: c.ShortLabel ?? c.Name))
                    .ToArray();

                DrawShortcutRow(row, rowItems);
            }
        }

        /// <summary>
        /// Converts a full hotkey label such as "Ctrl+F" or "Ctrl+Alt+S" to the short nano-style
        /// form used in the shortcut bar (e.g. "^F" or "^!S").
        /// </summary>
        private static string ToShortKey(string? hotKey)
        {
            if (hotKey is null)
            {
                return string.Empty;
            }

            return hotKey
                .Replace("Ctrl+Alt+", "^!", StringComparison.Ordinal)
                .Replace("Ctrl+", "^", StringComparison.Ordinal)
                .Replace("Alt+", "!", StringComparison.Ordinal);
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
                string hotKeyLabel = command.HotKey is not null ? $"[{command.HotKey}]" : string.Empty;
                int hotKeyLen = hotKeyLabel.Length;
                int nameWidth = Math.Max(0, innerWidth - hotKeyLen);
                string nameLabel = Fit(prefix + command.Name + disabled, nameWidth).PadRight(nameWidth);
                ConsoleStyle style = selected ? ConsoleStyle.Selection : enabled ? ConsoleStyle.Normal : ConsoleStyle.LineNumber;
                _console.WriteAt(listTop + i, left + 1, nameLabel, style);
                if (hotKeyLen > 0 && nameWidth + hotKeyLen <= innerWidth)
                {
                    ConsoleStyle keyStyle = selected ? ConsoleStyle.Selection : ConsoleStyle.LineNumber;
                    _console.WriteAt(listTop + i, left + 1 + nameWidth, hotKeyLabel, keyStyle);
                }
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

        /// <summary>
        /// Renders the editor with the grep search overlay.
        /// </summary>
        /// <param name="session">The editor session to render.</param>
        /// <param name="query">The raw grep input as typed by the user.</param>
        /// <param name="queryCursor">The cursor position within <paramref name="query" />.</param>
        /// <param name="results">The grep results currently visible in the panel.</param>
        /// <param name="selectedIndex">The selected result index.</param>
        /// <param name="filePattern">The active file pattern filter derived from the input.</param>
        public void RenderGrepSearch(
            EditorSession session,
            string query,
            int queryCursor,
            IReadOnlyList<GrepResult> results,
            int selectedIndex,
            string filePattern = "*")
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
            int inputColumn = DrawGrepInput(session, query, queryCursor);
            DrawShortcuts(session);
            DrawGrepPanel(session, results, selectedIndex, filePattern);
            _console.MoveCursor(session.Layout.StatusRow, inputColumn);
            _console.UseBlockCursor();
            _console.ShowCursor(true);
            _console.EndFrame();
        }

        private int DrawGrepInput(EditorSession session, string query, int cursor)
        {
            int row = session.Layout.StatusRow;
            int columns = session.Layout.Columns;
            string prefix = "Grep: ";
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

        private void DrawGrepPanel(EditorSession session, IReadOnlyList<GrepResult> results, int selectedIndex, string filePattern = "*")
        {
            int height = session.Layout.StatusRow - session.Layout.EditorTop;
            int columns = session.Layout.Columns;
            if (height < 3 || columns < 10)
            {
                return;
            }

            int width = Math.Min(columns, Math.Clamp(columns * 2 / 3, 40, 80));
            int left = Math.Max(0, columns - width);
            int top = session.Layout.EditorTop;
            int bottom = top + height - 1;
            int innerWidth = Math.Max(0, width - 2);

            string panelTitle = string.IsNullOrEmpty(filePattern) || filePattern == "*"
                ? " Grep Results "
                : $" Grep Results [{filePattern}] ";
            DrawPanelBorder(top, left, width, panelTitle);
            for (int row = top + 1; row < bottom; row++)
            {
                _console.WriteAt(row, left, "|", ConsoleStyle.ShortcutKey);
                WritePadded(row, left + 1, ReadOnlySpan<char>.Empty, innerWidth, ConsoleStyle.Normal);
                _console.WriteAt(row, left + width - 1, "|", ConsoleStyle.ShortcutKey);
            }

            _console.WriteAt(bottom, left, "+" + new string('-', Math.Max(0, width - 2)) + "+", ConsoleStyle.ShortcutKey);

            int listTop = top + 1;
            int listRows = Math.Max(0, bottom - listTop);
            if (results.Count == 0)
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

            for (int i = 0; i < listRows && first + i < results.Count; i++)
            {
                int resultIndex = first + i;
                GrepResult result = results[resultIndex];
                bool selected = resultIndex == selectedIndex;
                string marker = selected ? ">" : " ";
                string location = $"{result.FileName}:{result.LineNumber}";
                string matchText = SanitizeForDisplay(result.LineText.TrimStart());
                string label = $"{marker} {location}  {matchText}";
                ConsoleStyle style = selected ? ConsoleStyle.Selection : ConsoleStyle.Normal;
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

        /// <summary>
        /// Replaces control characters (other than tab) with a space so they cannot
        /// corrupt the terminal layout when rendered in a single-line panel entry.
        /// </summary>
        private static string SanitizeForDisplay(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            char[]? buf = null;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsControl(c) && c != '\t')
                {
                    buf ??= value.ToCharArray();
                    buf[i] = ' ';
                }
            }

            return buf is null ? value : new string(buf);
        }
    }

    /// <summary>
    /// Describes the row and column layout used by the editor renderer.
    /// </summary>
    /// <param name="Rows">The total number of terminal rows.</param>
    /// <param name="Columns">The total number of terminal columns.</param>
    /// <param name="EditorTop">The first editor body row.</param>
    /// <param name="EditorRows">The number of editor body rows.</param>
    /// <param name="StatusRow">The status bar row.</param>
    /// <param name="ShortcutTop">The first shortcut row.</param>
    internal readonly record struct EditorLayout(int Rows, int Columns, int EditorTop, int EditorRows, int StatusRow, int ShortcutTop)
    {
        /// <summary>
        /// Gets the number of editable text columns.
        /// </summary>
        /// <value>
        /// The editor body column count.
        /// </value>
        public int EditColumns => Columns;

        /// <summary>
        /// Creates a renderer layout from the terminal size.
        /// </summary>
        /// <param name="size">The current terminal size.</param>
        /// <returns>
        /// The computed editor layout.
        /// </returns>
        public static EditorLayout From(TerminalSize size)
        {
            int rows = Math.Max(5, size.Rows);
            int editorRows = Math.Max(1, rows - 4);
            return new EditorLayout(rows, size.Columns, 1, editorRows, rows - 3, rows - 2);
        }
    }
}
