/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit.Editor
{
    internal sealed class EditorSession
    {
        private static readonly Lazy<SyntaxLibrary> Syntaxes = new(SyntaxLibrary.LoadEmbedded);
        private readonly UndoStack _undoStack = new();
        private string[]? _typingBefore;
        private Position _typingCursorBefore;
        private SyntaxHighlighter _highlighter;

        public EditorSession(DocumentBuffer document, EditorOptions options, IConsoleDriver console)
        {
            Document = document;
            Options = options;
            Console = console;
            Layout = EditorLayout.From(console.Size);
            _highlighter = new SyntaxHighlighter(Syntaxes.Value.FindForFile(document.FilePath));
        }

        public DocumentBuffer Document { get; private set; }
        public EditorOptions Options { get; }
        public IConsoleDriver Console { get; }
        public Position Cursor { get; private set; }
        public Position? Selection { get; private set; }
        public int ViewTop { get; private set; }
        public int ViewLeft { get; private set; }
        public int DesiredColumn { get; private set; }
        public string Clipboard { get; private set; } = string.Empty;
        public EditorLayout Layout { get; set; }
        public bool Running { get; set; } = true;
        public string StatusMessage { get; private set; } = string.Empty;
        public bool StatusIsAlert { get; private set; }
        public string? LastSearch { get; set; }
        public string? SuggestedSavePath { get; private set; }

        public bool IsReadOnly => Options.ReadOnly || Document.ReadOnlyFromFile;

        public bool HasSelection => SelectionRange is not null;

        public (Position Start, Position End)? SelectionRange
        {
            get
            {
                if (Selection is not Position mark)
                {
                    return null;
                }

                var range = DocumentBuffer.Order(mark, Cursor);
                return range.Start.CompareTo(range.End) == 0 ? null : range;
            }
        }

        public int LineNumberMargin => Options.LineNumbers ? Math.Max(3, Document.LineCount.ToString().Length + 1) : 0;

        public IReadOnlyList<HighlightSpan> GetHighlightSpans(int lineIndex) => _highlighter.Highlight(Document, lineIndex);

        public Dictionary<int, List<HighlightSpan>> GetHighlightSpans(int firstLine, int lineCount) =>
            _highlighter.HighlightRange(Document, firstLine, lineCount);

        public void SetStatus(string message, bool alert = false)
        {
            StatusMessage = message;
            StatusIsAlert = alert;
        }

        public void ClearStatus()
        {
            StatusMessage = string.Empty;
            StatusIsAlert = false;
        }

        public void MoveTo(int line, int column, bool preserveDesiredColumn = false)
        {
            Cursor = Document.Clamp(new Position(line, column));
            if (!preserveDesiredColumn)
            {
                DesiredColumn = Cursor.Column;
            }
        }

        public void MoveLeft(bool extendSelection = false)
        {
            BeginSelectionExtension(extendSelection);
            if (Cursor.Column > 0)
            {
                MoveTo(Cursor.Line, Cursor.Column - 1);
            }
            else if (Cursor.Line > 0)
            {
                MoveTo(Cursor.Line - 1, Document.LineAt(Cursor.Line - 1).Length);
            }

            EndSelectionExtension(extendSelection);
        }

        public void MoveRight(bool extendSelection = false)
        {
            BeginSelectionExtension(extendSelection);
            if (Cursor.Column < Document.LineAt(Cursor.Line).Length)
            {
                MoveTo(Cursor.Line, Cursor.Column + 1);
            }
            else if (Cursor.Line + 1 < Document.LineCount)
            {
                MoveTo(Cursor.Line + 1, 0);
            }

            EndSelectionExtension(extendSelection);
        }

        public void MoveUp(bool extendSelection = false)
        {
            BeginSelectionExtension(extendSelection);
            if (Cursor.Line > 0)
            {
                MoveTo(Cursor.Line - 1, Math.Min(DesiredColumn, Document.LineAt(Cursor.Line - 1).Length), preserveDesiredColumn: true);
            }

            EndSelectionExtension(extendSelection);
        }

        public void MoveDown(bool extendSelection = false)
        {
            BeginSelectionExtension(extendSelection);
            if (Cursor.Line + 1 < Document.LineCount)
            {
                MoveTo(Cursor.Line + 1, Math.Min(DesiredColumn, Document.LineAt(Cursor.Line + 1).Length), preserveDesiredColumn: true);
            }

            EndSelectionExtension(extendSelection);
        }

        public void PageUp()
        {
            ClearSelection();
            MoveTo(Math.Max(0, Cursor.Line - Layout.EditorRows), Cursor.Column);
        }

        public void PageDown()
        {
            ClearSelection();
            MoveTo(Math.Min(Document.LineCount - 1, Cursor.Line + Layout.EditorRows), Cursor.Column);
        }

        public void Home()
        {
            ClearSelection();
            MoveTo(Cursor.Line, 0);
        }

        public void End()
        {
            ClearSelection();
            MoveTo(Cursor.Line, Document.LineAt(Cursor.Line).Length);
        }

        public void FileStart()
        {
            ClearSelection();
            MoveTo(0, 0);
        }

        public void FileEnd()
        {
            ClearSelection();
            MoveTo(Document.LineCount - 1, Document.LineAt(Document.LineCount - 1).Length);
        }

        public void SelectAll()
        {
            EndTypingGroup();
            Selection = new Position(0, 0);
            MoveTo(Document.LineCount - 1, Document.LineAt(Document.LineCount - 1).Length);
            SetStatus("Selected all");
        }

        public void ClearSelection()
        {
            EndTypingGroup();
            Selection = null;
        }

        public void ToggleSelection()
        {
            EndTypingGroup();
            Selection = Selection is null ? Cursor : null;
            SetStatus(Selection is null ? "Mark removed" : "Mark set");
        }

        public void ToggleLineNumbers()
        {
            EndTypingGroup();
            Options.LineNumbers = !Options.LineNumbers;
            ViewLeft = Math.Min(ViewLeft, Cursor.Column);
            SetStatus(Options.LineNumbers ? "Line numbers enabled" : "Line numbers disabled");
        }

        public void InsertPrintable(char value)
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            if (_typingBefore is null)
            {
                _typingBefore = Snapshot();
                _typingCursorBefore = Cursor;
            }

            DeleteSelectionWithoutUndo();
            Cursor = Document.InsertText(Cursor, value.ToString());
            Document.Modified = true;
        }

        public void InsertText(string text)
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            WithUndo(() =>
            {
                DeleteSelectionWithoutUndo();
                Cursor = Document.InsertText(Cursor, text);
            });
        }

        public void Enter() => InsertText("\n");

        public void Tab() => InsertText(new string(' ', Options.TabSize));

        public void Backspace()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            WithUndo(() =>
            {
                if (DeleteSelectionWithoutUndo())
                {
                    return;
                }

                if (Cursor.Column > 0)
                {
                    var start = new Position(Cursor.Line, Cursor.Column - 1);
                    Document.DeleteRange(start, Cursor);
                    Cursor = start;
                }
                else if (Cursor.Line > 0)
                {
                    int previousLength = Document.LineAt(Cursor.Line - 1).Length;
                    var start = new Position(Cursor.Line - 1, previousLength);
                    Document.DeleteRange(start, Cursor);
                    Cursor = start;
                }
            });
        }

        public void Delete()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            WithUndo(() =>
            {
                if (DeleteSelectionWithoutUndo())
                {
                    return;
                }

                Position end = Cursor.Column < Document.LineAt(Cursor.Line).Length
                    ? new Position(Cursor.Line, Cursor.Column + 1)
                    : new Position(Math.Min(Document.LineCount - 1, Cursor.Line + 1), 0);
                Document.DeleteRange(Cursor, end);
            });
        }

        public void Cut()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            WithUndo(() =>
            {
                if (SelectionRange is { } range)
                {
                    Clipboard = Document.GetText(range.Start, range.End);
                    Document.DeleteRange(range.Start, range.End);
                    Cursor = range.Start;
                    Selection = null;
                }
                else
                {
                    Clipboard = Document.LineAt(Cursor.Line).ToString();
                    if (Document.LineCount == 1)
                    {
                        Document.LineAt(0).Remove(0, Document.LineAt(0).Length);
                        Cursor = new Position(0, 0);
                    }
                    else
                    {
                        Document.Lines.RemoveAt(Cursor.Line);
                        MoveTo(Math.Min(Cursor.Line, Document.LineCount - 1), 0);
                    }
                }
            });

            SetStatus("Cut");
        }

        public void Copy()
        {
            EndTypingGroup();
            if (SelectionRange is { } range)
            {
                Clipboard = Document.GetText(range.Start, range.End);
            }
            else
            {
                Clipboard = Document.LineAt(Cursor.Line).ToString();
            }

            SetStatus("Copied");
        }

        public string? GetSelectedText()
        {
            EndTypingGroup();
            return SelectionRange is { } range ? Document.GetText(range.Start, range.End) : null;
        }

        public bool ReplaceSelection(string replacement, string statusMessage)
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return false;
            }

            if (SelectionRange is not { } range)
            {
                EndTypingGroup();
                SetStatus("Select text first", alert: true);
                return false;
            }

            WithUndo(() =>
            {
                Document.DeleteRange(range.Start, range.End);
                Cursor = Document.InsertText(range.Start, replacement);
                Selection = null;
            });

            SetStatus(statusMessage);
            return true;
        }

        public void Paste()
        {
            if (string.IsNullOrEmpty(Clipboard))
            {
                SetStatus("Cutbuffer is empty", alert: true);
                return;
            }

            InsertText(Clipboard);
        }

        public void Undo()
        {
            EndTypingGroup();
            UndoRecord? record = _undoStack.PopUndo();
            if (record is null)
            {
                SetStatus("Nothing to undo", alert: true);
                return;
            }

            Restore(record.BeforeLines);
            Cursor = Document.Clamp(record.BeforeCursor);
            Document.Modified = true;
            SetStatus("Undid action");
        }

        public void Redo()
        {
            EndTypingGroup();
            UndoRecord? record = _undoStack.PopRedo();
            if (record is null)
            {
                SetStatus("Nothing to redo", alert: true);
                return;
            }

            Restore(record.AfterLines);
            Cursor = Document.Clamp(record.AfterCursor);
            Document.Modified = true;
            SetStatus("Redid action");
        }

        public bool Search(string needle, bool backwards = false)
        {
            EndTypingGroup();
            if (string.IsNullOrEmpty(needle))
            {
                return false;
            }

            LastSearch = needle;
            StringComparison comparison = Options.CaseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int line = Cursor.Line;
            int column = backwards ? Math.Max(0, Cursor.Column - 1) : Math.Min(Document.LineAt(line).Length, Cursor.Column + 1);

            for (int pass = 0; pass < 2; pass++)
            {
                while (line >= 0 && line < Document.LineCount)
                {
                    string text = Document.LineAt(line).ToString();
                    int found;
                    if (backwards)
                    {
                        int startIndex = text.Length == 0 ? -1 : Math.Min(column, text.Length - 1);
                        found = startIndex < 0 ? -1 : text.LastIndexOf(needle, startIndex, comparison);
                    }
                    else
                    {
                        int startIndex = Math.Min(column, text.Length);
                        found = startIndex >= text.Length ? -1 : text.IndexOf(needle, startIndex, comparison);
                    }

                    if (found >= 0)
                    {
                        MoveTo(line, found);
                        SetStatus($"Found: {needle}");
                        return true;
                    }

                    line += backwards ? -1 : 1;
                    if (line >= 0 && line < Document.LineCount)
                    {
                        column = backwards ? Document.LineAt(line).Length : 0;
                    }
                }

                line = backwards ? Document.LineCount - 1 : 0;
                column = backwards ? Document.LineAt(line).Length : 0;
            }

            SetStatus($"Not found: {needle}", alert: true);
            return false;
        }

        public int ReplaceAll(string needle, string replacement)
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return 0;
            }

            int count = 0;
            WithUndo(() =>
            {
                StringComparison comparison = Options.CaseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                for (int lineIndex = 0; lineIndex < Document.LineCount; lineIndex++)
                {
                    string line = Document.LineAt(lineIndex).ToString();
                    int index = line.IndexOf(needle, comparison);
                    while (index >= 0)
                    {
                        Document.ReplaceRange(new Position(lineIndex, index), new Position(lineIndex, index + needle.Length), replacement);
                        count++;
                        line = Document.LineAt(lineIndex).ToString();
                        index = line.IndexOf(needle, index + replacement.Length, comparison);
                    }
                }
            });

            SetStatus($"Replaced {count} occurrence{(count == 1 ? string.Empty : "s")}");
            return count;
        }

        public void TrimCurrentLine()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            string line = Document.LineAt(Cursor.Line).ToString();
            string trimmed = line.Trim();
            if (string.Equals(line, trimmed, StringComparison.Ordinal))
            {
                EndTypingGroup();
                SetStatus("Current line is already trimmed");
                return;
            }

            int oldLine = Cursor.Line;
            int oldColumn = Cursor.Column;
            int leadingRemoved = line.Length - line.TrimStart().Length;
            int newColumn = Math.Clamp(oldColumn - Math.Min(oldColumn, leadingRemoved), 0, trimmed.Length);

            WithUndo(() =>
            {
                ReplaceLine(oldLine, trimmed);
                Selection = null;
                MoveTo(oldLine, newColumn);
            });

            SetStatus("Trimmed current line");
        }

        public void TrimAllLines()
        {
            TrimAllLines(TrimLineKind.Both, "All lines are already trimmed", count => $"Trimmed {count} line{(count == 1 ? string.Empty : "s")}");
        }

        public void TrimAllLinesLeadingSpace()
        {
            TrimAllLines(TrimLineKind.Leading, "No leading whitespace to trim", count => $"Trimmed leading whitespace on {count} line{(count == 1 ? string.Empty : "s")}");
        }

        public void TrimAllLinesTrailingSpace()
        {
            TrimAllLines(TrimLineKind.Trailing, "No trailing whitespace to trim", count => $"Trimmed trailing whitespace on {count} line{(count == 1 ? string.Empty : "s")}");
        }

        public void RemoveEmptyLines()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            int removed = Document.Lines.Count(line => string.IsNullOrWhiteSpace(line.ToString()));
            if (removed == 0)
            {
                EndTypingGroup();
                SetStatus("No empty lines to remove");
                return;
            }

            int oldLine = Cursor.Line;
            int oldColumn = Cursor.Column;

            WithUndo(() =>
            {
                int removedBeforeCursor = 0;
                bool removedCursorLine = false;

                for (int lineIndex = Document.LineCount - 1; lineIndex >= 0; lineIndex--)
                {
                    if (!string.IsNullOrWhiteSpace(Document.LineAt(lineIndex).ToString()))
                    {
                        continue;
                    }

                    if (lineIndex < oldLine)
                    {
                        removedBeforeCursor++;
                    }
                    else if (lineIndex == oldLine)
                    {
                        removedCursorLine = true;
                    }

                    Document.Lines.RemoveAt(lineIndex);
                }

                if (Document.Lines.Count == 0)
                {
                    Document.Lines.Add(new LineBuffer(string.Empty));
                }

                Selection = null;
                int newLine = removedCursorLine ? oldLine - removedBeforeCursor : oldLine - removedBeforeCursor;
                MoveTo(Math.Clamp(newLine, 0, Document.LineCount - 1), oldColumn);
            });

            SetStatus($"Removed {removed} empty line{(removed == 1 ? string.Empty : "s")}");
        }

        public void ConvertTabsToSpaces()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            int changedLines = 0;
            int tabCount = 0;
            for (int lineIndex = 0; lineIndex < Document.LineCount; lineIndex++)
            {
                string line = Document.LineAt(lineIndex).ToString();
                int lineTabs = line.Count(ch => ch == '\t');
                if (lineTabs > 0)
                {
                    changedLines++;
                    tabCount += lineTabs;
                }
            }

            if (tabCount == 0)
            {
                EndTypingGroup();
                SetStatus("No tabs to convert");
                return;
            }

            int oldLine = Cursor.Line;
            int oldColumn = Cursor.Column;
            int newColumn = oldColumn;

            WithUndo(() =>
            {
                for (int lineIndex = 0; lineIndex < Document.LineCount; lineIndex++)
                {
                    string line = Document.LineAt(lineIndex).ToString();
                    if (!line.Contains('\t'))
                    {
                        continue;
                    }

                    if (lineIndex == oldLine)
                    {
                        string beforeCursor = line[..Math.Min(oldColumn, line.Length)];
                        newColumn = ExpandTabs(beforeCursor).Length;
                    }

                    ReplaceLine(lineIndex, ExpandTabs(line));
                }

                Selection = null;
                MoveTo(oldLine, newColumn);
            });

            SetStatus($"Converted {tabCount} tab{(tabCount == 1 ? string.Empty : "s")} on {changedLines} line{(changedLines == 1 ? string.Empty : "s")}");
        }

        public void InsertGuid()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            InsertText(Guid.NewGuid().ToString());
            SetStatus("Inserted GUID");
        }

        /// <summary>
        /// Shows the current working directory in the status bar.
        /// </summary>
        public void ShowCurrentDirectory() =>
            SetStatus($"Directory: {Directory.GetCurrentDirectory()}");

        public void InsertDate()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            InsertText(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            SetStatus("Inserted date");
        }

        public void InsertDateTime()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            InsertText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            SetStatus("Inserted date/time");
        }

        public string GetDisplayLine(int line)
        {
            return ExpandTabs(Document.LineAt(line).ToString());
        }

        public void EnsureCursorVisible()
        {
            int editRows = Math.Max(1, Layout.EditorRows);
            int editColumns = Math.Max(1, Layout.Columns - LineNumberMargin);

            if (Cursor.Line < ViewTop)
            {
                ViewTop = Cursor.Line;
            }
            else if (Cursor.Line >= ViewTop + editRows)
            {
                ViewTop = Cursor.Line - editRows + 1;
            }

            if (Cursor.Column < ViewLeft)
            {
                ViewLeft = Cursor.Column;
            }
            else if (Cursor.Column >= ViewLeft + editColumns)
            {
                ViewLeft = Cursor.Column - editColumns + 1;
            }
        }

        public bool Save(string? path = null)
        {
            EndTypingGroup();
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return false;
            }

            try
            {
                Document.Save(path);
                RefreshSyntax();
                SuggestedSavePath = null;
                SetStatus($"Wrote {Document.LineCount} lines");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus($"Error writing file: {ex.Message}", alert: true);
                return false;
            }
        }

        public void InsertFile(string path)
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            try
            {
                bool blankUntitledBuffer = Document.FilePath is null &&
                    !Document.Modified &&
                    Document.LineCount == 1 &&
                    Document.LineAt(0).Length == 0;
                string text = File.ReadAllText(path);
                InsertText(text);
                if (blankUntitledBuffer)
                {
                    SuggestedSavePath = path;
                }

                SetStatus($"Inserted {path}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error reading file: {ex.Message}", alert: true);
            }
        }

        public void EndTypingGroup()
        {
            if (_typingBefore is null)
            {
                return;
            }

            if (_undoStack.Push(new UndoRecord(_typingBefore, _typingCursorBefore, Snapshot(), Cursor)))
            {
                Document.Modified = true;
            }

            _typingBefore = null;
        }

        private void WithUndo(Action edit)
        {
            EndTypingGroup();
            string[] before = Snapshot();
            Position beforeCursor = Cursor;
            edit();
            if (_undoStack.Push(new UndoRecord(before, beforeCursor, Snapshot(), Cursor)))
            {
                Document.Modified = true;
            }
        }

        private void ReplaceLine(int lineIndex, string text)
        {
            LineBuffer line = Document.LineAt(lineIndex);
            Document.ReplaceRange(new Position(lineIndex, 0), new Position(lineIndex, line.Length), text);
        }

        private void TrimAllLines(TrimLineKind kind, string noChangeStatus, Func<int, string> changedStatus)
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            int changed = 0;
            for (int lineIndex = 0; lineIndex < Document.LineCount; lineIndex++)
            {
                string line = Document.LineAt(lineIndex).ToString();
                if (!string.Equals(line, TrimLine(line, kind), StringComparison.Ordinal))
                {
                    changed++;
                }
            }

            if (changed == 0)
            {
                EndTypingGroup();
                SetStatus(noChangeStatus);
                return;
            }

            int oldLine = Cursor.Line;
            int oldColumn = Cursor.Column;
            int newColumn = oldColumn;

            WithUndo(() =>
            {
                for (int lineIndex = 0; lineIndex < Document.LineCount; lineIndex++)
                {
                    string line = Document.LineAt(lineIndex).ToString();
                    string trimmed = TrimLine(line, kind);
                    if (string.Equals(line, trimmed, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (lineIndex == oldLine)
                    {
                        newColumn = AdjustTrimmedColumn(line, trimmed, kind, oldColumn);
                    }

                    ReplaceLine(lineIndex, trimmed);
                }

                Selection = null;
                MoveTo(oldLine, newColumn);
            });

            SetStatus(changedStatus(changed));
        }

        private static string TrimLine(string line, TrimLineKind kind) =>
            kind switch
            {
                TrimLineKind.Leading => line.TrimStart(),
                TrimLineKind.Trailing => line.TrimEnd(),
                _ => line.Trim()
            };

        private static int AdjustTrimmedColumn(string original, string trimmed, TrimLineKind kind, int column)
        {
            int leadingRemoved = kind is TrimLineKind.Leading or TrimLineKind.Both
                ? original.Length - original.TrimStart().Length
                : 0;
            return Math.Clamp(column - Math.Min(column, leadingRemoved), 0, trimmed.Length);
        }

        private void BeginSelectionExtension(bool extendSelection)
        {
            EndTypingGroup();
            if (extendSelection)
            {
                Selection ??= Cursor;
                return;
            }

            Selection = null;
        }

        private void EndSelectionExtension(bool extendSelection)
        {
            if (!extendSelection)
            {
                Selection = null;
            }
        }

        private bool DeleteSelectionWithoutUndo()
        {
            if (SelectionRange is not { } range)
            {
                Selection = null;
                return false;
            }

            Document.DeleteRange(range.Start, range.End);
            Cursor = range.Start;
            Selection = null;
            return true;
        }

        private string[] Snapshot() => Document.Lines.Select(line => line.ToString()).ToArray();

        private void Restore(string[] lines)
        {
            Document.Lines.Clear();
            foreach (string line in lines)
            {
                Document.Lines.Add(new LineBuffer(line));
            }

            if (Document.Lines.Count == 0)
            {
                Document.Lines.Add(new LineBuffer(string.Empty));
            }

            Selection = null;
        }

        private string ExpandTabs(string text)
        {
            if (!text.Contains('\t'))
            {
                return text;
            }

            var output = new System.Text.StringBuilder();
            int column = 0;
            foreach (char ch in text)
            {
                if (ch == '\t')
                {
                    int spaces = Options.TabSize - column % Options.TabSize;
                    output.Append(' ', spaces);
                    column += spaces;
                }
                else
                {
                    output.Append(ch);
                    column++;
                }
            }

            return output.ToString();
        }

        private void ReadOnlyWarning() => SetStatus("File is read-only", alert: true);

        private void RefreshSyntax()
        {
            _highlighter = new SyntaxHighlighter(Syntaxes.Value.FindForFile(Document.FilePath));
        }

        private enum TrimLineKind
        {
            Both,
            Leading,
            Trailing
        }
    }
}
