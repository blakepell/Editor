/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit.Editor
{
    /// <summary>
    /// Stores mutable editor state and applies editing operations to a document.
    /// </summary>
    internal sealed class EditorSession
    {
        private static readonly Lazy<SyntaxLibrary> Syntaxes = new(SyntaxLibrary.LoadEmbedded);
        private readonly UndoStack _undoStack = new();
        private string[]? _typingBefore;
        private Position _typingCursorBefore;
        private SyntaxHighlighter _highlighter;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorSession"/> class.
        /// </summary>
        /// <param name="document">The document buffer to edit.</param>
        /// <param name="options">The editor options for the session.</param>
        /// <param name="console">The console driver used for terminal state.</param>
        public EditorSession(DocumentBuffer document, EditorOptions options, IConsoleDriver console)
        {
            Document = document;
            Options = options;
            Console = console;
            Layout = EditorLayout.From(console.Size);
            _highlighter = new SyntaxHighlighter(Syntaxes.Value.FindForFile(document.FilePath));
        }

        /// <summary>
        /// Gets the active document buffer.
        /// </summary>
        /// <value>
        /// The document currently loaded in the editor.
        /// </value>
        public DocumentBuffer Document { get; private set; }

        /// <summary>
        /// Gets the editor options for the session.
        /// </summary>
        /// <value>
        /// The mutable editor options.
        /// </value>
        public EditorOptions Options { get; }

        /// <summary>
        /// Gets the console driver used by the session.
        /// </summary>
        /// <value>
        /// The terminal I/O abstraction.
        /// </value>
        public IConsoleDriver Console { get; }

        /// <summary>
        /// Gets the current cursor position.
        /// </summary>
        /// <value>
        /// The zero-based document position.
        /// </value>
        public Position Cursor { get; private set; }

        /// <summary>
        /// Gets the selection mark position.
        /// </summary>
        /// <value>
        /// The mark position, or <see langword="null" /> when selection is inactive.
        /// </value>
        public Position? Selection { get; private set; }

        /// <summary>
        /// Gets the first visible document line.
        /// </summary>
        /// <value>
        /// The zero-based top line index.
        /// </value>
        public int ViewTop { get; private set; }

        /// <summary>
        /// Gets the first visible document column.
        /// </summary>
        /// <value>
        /// The zero-based left column index.
        /// </value>
        public int ViewLeft { get; private set; }

        /// <summary>
        /// Gets the preferred column used for vertical cursor movement.
        /// </summary>
        /// <value>
        /// The zero-based desired column.
        /// </value>
        public int DesiredColumn { get; private set; }

        /// <summary>
        /// Gets the editor clipboard text.
        /// </summary>
        /// <value>
        /// The text copied or cut by the editor.
        /// </value>
        public string Clipboard { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current terminal layout.
        /// </summary>
        /// <value>
        /// The renderer layout for the current terminal size.
        /// </value>
        public EditorLayout Layout { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the input loop should continue.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the editor should keep running; otherwise, <see langword="false" />.
        /// </value>
        public bool Running { get; set; } = true;

        /// <summary>
        /// Gets the current status bar message.
        /// </summary>
        /// <value>
        /// The message displayed in the status bar.
        /// </value>
        public string StatusMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the style used for the current status bar message.
        /// </summary>
        /// <value>
        /// The status style override, or <see langword="null" /> to use the default status style.
        /// </value>
        public ConsoleStyle? StatusStyle { get; private set; }

        /// <summary>
        /// Gets or sets the most recent search term.
        /// </summary>
        /// <value>
        /// The last search text, or <see langword="null" /> when no search has run.
        /// </value>
        public string? LastSearch { get; set; }

        /// <summary>
        /// Gets the suggested save path for an untitled buffer.
        /// </summary>
        /// <value>
        /// The suggested path, or <see langword="null" /> when no suggestion is available.
        /// </value>
        public string? SuggestedSavePath { get; private set; }

        /// <summary>
        /// Gets a value that indicates whether the current document is read-only.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if editing commands should be blocked; otherwise, <see langword="false" />.
        /// </value>
        public bool IsReadOnly => Options.ReadOnly || Document.ReadOnlyFromFile;

        /// <summary>
        /// Gets a value that indicates whether a non-empty selection exists.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if text is selected; otherwise, <see langword="false" />.
        /// </value>
        public bool HasSelection => SelectionRange is not null;

        /// <summary>
        /// Gets the current ordered selection range.
        /// </summary>
        /// <value>
        /// The selected range, or <see langword="null" /> when there is no non-empty selection.
        /// </value>
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

        /// <summary>
        /// Gets the width of the line number margin.
        /// </summary>
        /// <value>
        /// The margin width in columns, or <c>0</c> when line numbers are disabled.
        /// </value>
        public int LineNumberMargin => Options.LineNumbers ? Math.Max(3, Document.LineCount.ToString().Length + 1) : 0;

        /// <summary>
        /// Gets syntax highlight spans for a single line.
        /// </summary>
        /// <param name="lineIndex">The zero-based line index.</param>
        /// <returns>
        /// The highlight spans for the requested line.
        /// </returns>
        public IReadOnlyList<HighlightSpan> GetHighlightSpans(int lineIndex) => _highlighter.Highlight(Document, lineIndex);

        /// <summary>
        /// Gets the comment prefix and suffix tokens for the active syntax.
        /// </summary>
        /// <value>
        /// The prefix (e.g., <c>//</c>) and optional suffix (e.g., <c>--&gt;</c>), or <see langword="null" /> when
        /// no comment style is defined for the current file type.
        /// </value>
        public (string? Prefix, string? Suffix) CommentTokens => (_highlighter.CommentPrefix, _highlighter.CommentSuffix);

        /// <summary>
        /// Gets syntax highlight spans for a range of lines.
        /// </summary>
        /// <param name="firstLine">The first zero-based line index.</param>
        /// <param name="lineCount">The number of lines to highlight.</param>
        /// <returns>
        /// A map of line indexes to highlight spans.
        /// </returns>
        public Dictionary<int, List<HighlightSpan>> GetHighlightSpans(int firstLine, int lineCount) =>
            _highlighter.HighlightRange(Document, firstLine, lineCount);

        /// <summary>
        /// Sets the current status bar message.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="alert"><see langword="true" /> to display the message as an alert; otherwise, <see langword="false" />.</param>
        /// <param name="foreground">The optional foreground color override.</param>
        /// <param name="background">The optional background color override.</param>
        public void SetStatus(string message, bool alert = false, ConsoleColor? foreground = null, ConsoleColor? background = null)
        {
            StatusMessage = message;
            if (alert)
            {
                StatusStyle = ConsoleStyle.Status with { Background = ConsoleColor.Red };
            }
            else if (foreground is not null || background is not null)
            {
                StatusStyle = new ConsoleStyle(foreground ?? ConsoleStyle.Status.Foreground, background ?? ConsoleStyle.Status.Background);
            }
            else
            {
                StatusStyle = null;
            }
        }

        /// <summary>
        /// Sets a success status bar message.
        /// </summary>
        /// <param name="message">The status message.</param>
        public void SetStatusSuccess(string message)
        {
            SetStatus(message, false, ConsoleColor.White, ConsoleColor.DarkGreen);
        }

        /// <summary>
        /// Sets a warning status bar message.
        /// </summary>
        /// <param name="message">The status message.</param>
        public void SetStatusWarning(string message)
        {
            SetStatus(message, false, ConsoleColor.White, ConsoleColor.DarkYellow);
        }

        /// <summary>
        /// Clears the status bar message and style override.
        /// </summary>
        public void ClearStatus()
        {
            StatusMessage = string.Empty;
            StatusStyle = null;
        }

        /// <summary>
        /// Moves the cursor to a document position.
        /// </summary>
        /// <param name="line">The zero-based line index.</param>
        /// <param name="column">The zero-based column index.</param>
        /// <param name="preserveDesiredColumn"><see langword="true" /> to keep the current desired column; otherwise, <see langword="false" />.</param>
        public void MoveTo(int line, int column, bool preserveDesiredColumn = false)
        {
            Cursor = Document.Clamp(new Position(line, column));
            if (!preserveDesiredColumn)
            {
                DesiredColumn = Cursor.Column;
            }
        }

        /// <summary>
        /// Moves the cursor one character to the left.
        /// </summary>
        /// <param name="extendSelection"><see langword="true" /> to extend the active selection; otherwise, <see langword="false" />.</param>
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

        /// <summary>
        /// Moves the cursor one character to the right.
        /// </summary>
        /// <param name="extendSelection"><see langword="true" /> to extend the active selection; otherwise, <see langword="false" />.</param>
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

        /// <summary>
        /// Moves the cursor one line up.
        /// </summary>
        /// <param name="extendSelection"><see langword="true" /> to extend the active selection; otherwise, <see langword="false" />.</param>
        public void MoveUp(bool extendSelection = false)
        {
            BeginSelectionExtension(extendSelection);
            if (Cursor.Line > 0)
            {
                MoveTo(Cursor.Line - 1, Math.Min(DesiredColumn, Document.LineAt(Cursor.Line - 1).Length), preserveDesiredColumn: true);
            }

            EndSelectionExtension(extendSelection);
        }

        /// <summary>
        /// Moves the cursor one line down.
        /// </summary>
        /// <param name="extendSelection"><see langword="true" /> to extend the active selection; otherwise, <see langword="false" />.</param>
        public void MoveDown(bool extendSelection = false)
        {
            BeginSelectionExtension(extendSelection);
            if (Cursor.Line + 1 < Document.LineCount)
            {
                MoveTo(Cursor.Line + 1, Math.Min(DesiredColumn, Document.LineAt(Cursor.Line + 1).Length), preserveDesiredColumn: true);
            }

            EndSelectionExtension(extendSelection);
        }

        /// <summary>
        /// Moves the cursor up by one editor viewport.
        /// </summary>
        public void PageUp()
        {
            ClearSelection();
            MoveTo(Math.Max(0, Cursor.Line - Layout.EditorRows), Cursor.Column);
        }

        /// <summary>
        /// Moves the cursor down by one editor viewport.
        /// </summary>
        public void PageDown()
        {
            ClearSelection();
            MoveTo(Math.Min(Document.LineCount - 1, Cursor.Line + Layout.EditorRows), Cursor.Column);
        }

        /// <summary>
        /// Moves the cursor to the start of the current line.
        /// </summary>
        public void Home()
        {
            ClearSelection();
            MoveTo(Cursor.Line, 0);
        }

        /// <summary>
        /// Moves the cursor to the end of the current line.
        /// </summary>
        public void End()
        {
            ClearSelection();
            MoveTo(Cursor.Line, Document.LineAt(Cursor.Line).Length);
        }

        /// <summary>
        /// Moves the cursor to the start of the document.
        /// </summary>
        public void FileStart()
        {
            ClearSelection();
            MoveTo(0, 0);
        }

        /// <summary>
        /// Moves the cursor to the end of the document.
        /// </summary>
        public void FileEnd()
        {
            ClearSelection();
            MoveTo(Document.LineCount - 1, Document.LineAt(Document.LineCount - 1).Length);
        }

        /// <summary>
        /// Selects the entire document.
        /// </summary>
        public void SelectAll()
        {
            EndTypingGroup();
            Selection = new Position(0, 0);
            MoveTo(Document.LineCount - 1, Document.LineAt(Document.LineCount - 1).Length);
            SetStatus("Selected all");
        }

        /// <summary>
        /// Clears the active selection.
        /// </summary>
        public void ClearSelection()
        {
            EndTypingGroup();
            Selection = null;
        }

        /// <summary>
        /// Toggles the selection mark at the current cursor position.
        /// </summary>
        public void ToggleSelection()
        {
            EndTypingGroup();
            Selection = Selection is null ? Cursor : null;
            SetStatus(Selection is null ? "Mark removed" : "Mark set");
        }

        /// <summary>
        /// Toggles line number display for the session.
        /// </summary>
        public void ToggleLineNumbers()
        {
            EndTypingGroup();
            Options.LineNumbers = !Options.LineNumbers;
            ViewLeft = Math.Min(ViewLeft, Cursor.Column);
            SetStatus(Options.LineNumbers ? "Line numbers enabled" : "Line numbers disabled");
        }

        /// <summary>
        /// Moves the current line, or all lines spanned by the active selection, up by one line.
        /// </summary>
        public void MoveLineUp()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            (int first, int last) = GetLineMoveRange();
            if (first == 0)
            {
                return;
            }

            Position savedCursor = Cursor;
            Position? savedSelection = Selection;

            WithUndo(() =>
            {
                LineBuffer above = Document.Lines[first - 1];
                Document.Lines.RemoveAt(first - 1);
                Document.Lines.Insert(last, above);

                Cursor = new Position(savedCursor.Line - 1, savedCursor.Column);
                Selection = savedSelection is { } mark ? new Position(mark.Line - 1, mark.Column) : null;
            });
        }

        /// <summary>
        /// Moves the current line, or all lines spanned by the active selection, down by one line.
        /// </summary>
        public void MoveLineDown()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            (int first, int last) = GetLineMoveRange();
            if (last >= Document.LineCount - 1)
            {
                return;
            }

            Position savedCursor = Cursor;
            Position? savedSelection = Selection;

            WithUndo(() =>
            {
                LineBuffer below = Document.Lines[last + 1];
                Document.Lines.RemoveAt(last + 1);
                Document.Lines.Insert(first, below);

                Cursor = new Position(savedCursor.Line + 1, savedCursor.Column);
                Selection = savedSelection is { } mark ? new Position(mark.Line + 1, mark.Column) : null;
            });
        }

        /// <summary>
        /// Comments out the current line or all lines spanned by the active selection using the
        /// syntax-defined comment token. For block-comment languages (e.g., HTML) the selection
        /// is wrapped in open/close tokens; for line-comment languages each line is prefixed.
        /// </summary>
        public void CommentLines()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            var (prefix, suffix) = CommentTokens;
            if (prefix is null)
            {
                SetStatus("No comment style defined for this file type", alert: true);
                return;
            }

            (int first, int last) = GetLineMoveRange();

            WithUndo(() =>
            {
                if (suffix is not null)
                {
                    // Block comment: append close token to last line, prepend open token to first line.
                    Document.LineAt(last).Insert(Document.LineAt(last).Length, suffix);
                    Document.LineAt(first).Insert(0, prefix);
                }
                else
                {
                    for (int i = first; i <= last; i++)
                    {
                        Document.LineAt(i).Insert(0, prefix);
                    }
                }
            });

            int count = last - first + 1;
            SetStatusSuccess($"Commented {count} line{(count == 1 ? string.Empty : "s")}");
        }

        /// <summary>
        /// Removes comment tokens from the current line or all lines spanned by the active selection.
        /// </summary>
        public void UncommentLines()
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            var (prefix, suffix) = CommentTokens;
            if (prefix is null)
            {
                SetStatus("No comment style defined for this file type", alert: true);
                return;
            }

            (int first, int last) = GetLineMoveRange();

            if (suffix is not null)
            {
                string firstLine = Document.LineAt(first).ToString();
                string lastLine = Document.LineAt(last).ToString();

                if (!firstLine.StartsWith(prefix, StringComparison.Ordinal) ||
                    !lastLine.EndsWith(suffix, StringComparison.Ordinal))
                {
                    SetStatus("Selection is not block-commented", alert: true);
                    return;
                }

                WithUndo(() =>
                {
                    Document.LineAt(last).Remove(Document.LineAt(last).Length - suffix.Length, suffix.Length);
                    Document.LineAt(first).Remove(0, prefix.Length);
                });

                int count = last - first + 1;
                SetStatusSuccess($"Uncommented {count} line{(count == 1 ? string.Empty : "s")}");
            }
            else
            {
                int removed = 0;

                WithUndo(() =>
                {
                    for (int i = first; i <= last; i++)
                    {
                        string lineText = Document.LineAt(i).ToString();
                        int tokenStart = 0;
                        while (tokenStart < lineText.Length && char.IsWhiteSpace(lineText[tokenStart]))
                        {
                            tokenStart++;
                        }

                        if (tokenStart < lineText.Length &&
                            lineText[tokenStart..].StartsWith(prefix, StringComparison.Ordinal))
                        {
                            Document.LineAt(i).Remove(tokenStart, prefix.Length);
                            removed++;
                        }
                    }
                });

                if (removed == 0)
                {
                    SetStatus("No commented lines in selection", alert: true);
                }
                else
                {
                    SetStatusSuccess($"Uncommented {removed} line{(removed == 1 ? string.Empty : "s")}");
                }
            }
        }


        private (int First, int Last) GetLineMoveRange()
        {
            if (SelectionRange is { } range)
            {
                int first = range.Start.Line;
                int last = range.End.Column == 0 && range.End.Line > first
                    ? range.End.Line - 1
                    : range.End.Line;
                return (first, last);
            }

            return (Cursor.Line, Cursor.Line);
        }

        /// <summary>
        /// Inserts a printable character at the cursor.
        /// </summary>
        /// <param name="value">The character to insert.</param>
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
            Cursor = Document.InsertCharacter(Cursor, value);
            Document.Modified = true;
        }

        /// <summary>
        /// Inserts text at the cursor.
        /// </summary>
        /// <param name="text">The text to insert.</param>
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

        /// <summary>
        /// Inserts a newline at the cursor.
        /// </summary>
        public void Enter() => InsertText("\n");

        /// <summary>
        /// Inserts spaces for one configured tab stop at the cursor.
        /// </summary>
        public void Tab() => InsertText(new string(' ', Options.TabSize));

        /// <summary>
        /// Deletes the character before the cursor or the active selection.
        /// </summary>
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

        /// <summary>
        /// Deletes the character after the cursor or the active selection.
        /// </summary>
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

        /// <summary>
        /// Cuts the active selection or current line to the editor clipboard.
        /// </summary>
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

        /// <summary>
        /// Copies the active selection or current line to the editor clipboard.
        /// </summary>
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

        /// <summary>
        /// Gets the currently selected text.
        /// </summary>
        /// <returns>
        /// The selected text, or <see langword="null" /> when no text is selected.
        /// </returns>
        public string? GetSelectedText()
        {
            EndTypingGroup();
            return SelectionRange is { } range ? Document.GetText(range.Start, range.End) : null;
        }

        /// <summary>
        /// Replaces the active selection with the supplied text.
        /// </summary>
        /// <param name="replacement">The replacement text.</param>
        /// <param name="statusMessage">The status message shown after replacement.</param>
        /// <returns>
        /// <see langword="true" /> if the selection was replaced; otherwise, <see langword="false" />.
        /// </returns>
        public void ReplaceSelection(string replacement, string statusMessage)
        {
            if (IsReadOnly)
            {
                ReadOnlyWarning();
                return;
            }

            if (SelectionRange is not { } range)
            {
                EndTypingGroup();
                SetStatus("Select text first", alert: true);
                return;
            }

            WithUndo(() =>
            {
                Document.DeleteRange(range.Start, range.End);
                Cursor = Document.InsertText(range.Start, replacement);
                Selection = null;
            });

            SetStatus(statusMessage);
        }

        /// <summary>
        /// Pastes the editor clipboard at the cursor.
        /// </summary>
        public void Paste()
        {
            if (string.IsNullOrEmpty(Clipboard))
            {
                SetStatus("Cutbuffer is empty", alert: true);
                return;
            }

            InsertText(Clipboard);
        }

        /// <summary>
        /// Undoes the most recent undoable edit.
        /// </summary>
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

        /// <summary>
        /// Redoes the most recently undone edit.
        /// </summary>
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

        /// <summary>
        /// Searches for text from the current cursor position.
        /// </summary>
        /// <param name="needle">The text to find.</param>
        /// <param name="backwards"><see langword="true" /> to search toward the start of the document; otherwise, <see langword="false" />.</param>
        /// <returns>
        /// <see langword="true" /> if a match was found; otherwise, <see langword="false" />.
        /// </returns>
        public void Search(string needle, bool backwards = false)
        {
            EndTypingGroup();
            if (string.IsNullOrEmpty(needle))
            {
                return;
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
                        return;
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
        }

        /// <summary>
        /// Replaces every matching occurrence in the document.
        /// </summary>
        /// <param name="needle">The text to find.</param>
        /// <param name="replacement">The replacement text.</param>
        /// <returns>
        /// The number of replacements made.
        /// </returns>
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

        /// <summary>
        /// Trims leading and trailing whitespace from the current line.
        /// </summary>
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

        /// <summary>
        /// Trims leading and trailing whitespace from every line.
        /// </summary>
        public void TrimAllLines()
        {
            TrimAllLines(TrimLineKind.Both, "All lines are already trimmed", count => $"Trimmed {count} line{(count == 1 ? string.Empty : "s")}");
        }

        /// <summary>
        /// Trims leading whitespace from every line.
        /// </summary>
        public void TrimAllLinesLeadingSpace()
        {
            TrimAllLines(TrimLineKind.Leading, "No leading whitespace to trim", count => $"Trimmed leading whitespace on {count} line{(count == 1 ? string.Empty : "s")}");
        }

        /// <summary>
        /// Trims trailing whitespace from every line.
        /// </summary>
        public void TrimAllLinesTrailingSpace()
        {
            TrimAllLines(TrimLineKind.Trailing, "No trailing whitespace to trim", count => $"Trimmed trailing whitespace on {count} line{(count == 1 ? string.Empty : "s")}");
        }

        /// <summary>
        /// Removes blank and whitespace-only lines from the document.
        /// </summary>
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

        /// <summary>
        /// Converts all tab characters in the document to spaces.
        /// </summary>
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

        /// <summary>
        /// Inserts a new GUID at the cursor.
        /// </summary>
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

        /// <summary>
        /// Loads a new file into the editor, replacing the current document and resetting all state.
        /// </summary>
        /// <param name="path">The file path to open.</param>
        public void OpenFile(string path)
        {
            EndTypingGroup();
            Document = DocumentBuffer.Load(path, Options);
            Cursor = new Position(0, 0);
            DesiredColumn = 0;
            ViewTop = 0;
            ViewLeft = 0;
            Selection = null;
            SuggestedSavePath = null;
            _undoStack.Clear();
            _typingBefore = null;
            RefreshSyntax();
            SetStatusSuccess($"Opened: {Document.DisplayName}");
        }

        /// <summary>
        /// Starts a new untitled document and resets document-specific editor state.
        /// </summary>
        public void NewDocument()
        {
            EndTypingGroup();
            Document = DocumentBuffer.Load(null, Options);
            Cursor = new Position(0, 0);
            DesiredColumn = 0;
            ViewTop = 0;
            ViewLeft = 0;
            Selection = null;
            SuggestedSavePath = null;
            _undoStack.Clear();
            _typingBefore = null;
            RefreshSyntax();
            SetStatusSuccess("New document");
        }

        /// <summary>
        /// Inserts the current local date at the cursor.
        /// </summary>
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

        /// <summary>
        /// Inserts the current local date and time at the cursor.
        /// </summary>
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

        /// <summary>
        /// Gets a line prepared for display by expanding tab characters.
        /// </summary>
        /// <param name="line">The zero-based line index.</param>
        /// <returns>
        /// The display text for the requested line.
        /// </returns>
        public string GetDisplayLine(int line)
        {
            return ExpandTabs(Document.LineAt(line).ToString());
        }

        /// <summary>
        /// Adjusts the viewport so the cursor is visible.
        /// </summary>
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

        /// <summary>
        /// Saves the current document.
        /// </summary>
        /// <param name="path">The target file path, or <see langword="null" /> to use the document path.</param>
        /// <returns>
        /// <see langword="true" /> if the document was saved; otherwise, <see langword="false" />.
        /// </returns>
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

        /// <summary>
        /// Inserts the contents of a file at the cursor.
        /// </summary>
        /// <param name="path">The file path to insert.</param>
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
                                           Document is { Modified: false, LineCount: 1 } &&
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

        /// <summary>
        /// Finalizes the current grouped typing undo record.
        /// </summary>
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
            /// <summary>
            /// Trims both leading and trailing whitespace.
            /// </summary>
            Both,

            /// <summary>
            /// Trims leading whitespace.
            /// </summary>
            Leading,

            /// <summary>
            /// Trims trailing whitespace.
            /// </summary>
            Trailing
        }
    }
}
