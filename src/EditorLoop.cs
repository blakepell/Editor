/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit
{
    internal sealed class EditorLoop
    {
        private readonly EditorSession _session;
        private readonly Renderer _renderer;

        public EditorLoop(EditorSession session, Renderer renderer)
        {
            _session = session;
            _renderer = renderer;
        }

        public void Run()
        {
            _renderer.Render(_session);

            while (_session.Running)
            {
                ConsoleKeyInfo key = _session.Console.ReadKey();
                HandleKey(key);
                _session.EnsureCursorVisible();
                _renderer.Render(_session);
            }

            _session.EndTypingGroup();
        }

        private void HandleKey(ConsoleKeyInfo key)
        {
            _session.ClearStatus();

            if (IsCtrlKey(key, ConsoleKey.Home))
            {
                _session.FileStart();
            }
            else if (IsCtrlKey(key, ConsoleKey.End))
            {
                _session.FileEnd();
            }
            else if (key.Key is ConsoleKey.LeftArrow)
            {
                _session.MoveLeft();
            }
            else if (key.Key is ConsoleKey.RightArrow)
            {
                _session.MoveRight();
            }
            else if (key.Key is ConsoleKey.UpArrow)
            {
                _session.MoveUp();
            }
            else if (key.Key is ConsoleKey.DownArrow)
            {
                _session.MoveDown();
            }
            else if (key.Key is ConsoleKey.PageUp)
            {
                _session.PageUp();
            }
            else if (key.Key is ConsoleKey.PageDown)
            {
                _session.PageDown();
            }
            else if (key.Key is ConsoleKey.Home)
            {
                _session.Home();
            }
            else if (key.Key is ConsoleKey.End)
            {
                _session.End();
            }
            else if (key.Key is ConsoleKey.Backspace)
            {
                _session.Backspace();
            }
            else if (key.Key is ConsoleKey.Delete)
            {
                _session.Delete();
            }
            else if (key.Key is ConsoleKey.Enter)
            {
                _session.Enter();
            }
            else if (key.Key is ConsoleKey.Tab)
            {
                _session.Tab();
            }
            else if (key.Key is ConsoleKey.F12)
            {
                ShowHelp();
            }
            else if (IsCtrl(key, 'G'))
            {
                InsertGuid();
            }
            else if (IsCtrl(key, 'X'))
            {
                Exit();
            }
            else if (IsCtrl(key, 'O'))
            {
                WriteOut();
            }
            else if (IsCtrl(key, 'R'))
            {
                InsertFile();
            }
            else if (IsCtrl(key, 'L'))
            {
                _session.ToggleLineNumbers();
            }
            else if (IsCtrl(key, 'W') || IsCtrl(key, 'F'))
            {
                Search();
            }
            else if (key.KeyChar == 28 || IsCtrl(key, 'H'))
            {
                Replace();
            }
            else if (IsCtrl(key, 'K'))
            {
                _session.Cut();
            }
            else if (IsCtrl(key, 'U'))
            {
                _session.Paste();
            }
            else if (key.KeyChar == 30)
            {
                _session.ToggleSelection();
            }
            else if (IsCtrl(key, 'C'))
            {
                var cursor = _session.Cursor;
                _session.SetStatus($"Line {cursor.Line + 1}/{_session.Document.LineCount}, Col {cursor.Column + 1}");
            }
            else if (key.KeyChar == 31)
            {
                GoToLine();
            }
            else if (IsAlt(key, '6'))
            {
                _session.Copy();
            }
            else if (IsAlt(key, 'U'))
            {
                _session.Undo();
            }
            else if (IsAlt(key, 'E'))
            {
                _session.Redo();
            }
            else if (IsAlt(key, '\\'))
            {
                _session.FileStart();
            }
            else if (IsAlt(key, '/'))
            {
                _session.FileEnd();
            }
            else if (!char.IsControl(key.KeyChar))
            {
                _session.InsertPrintable(key.KeyChar);
            }
        }

        private void Exit()
        {
            _session.EndTypingGroup();
            if (_session.Document.Modified)
            {
                YesNoCancel answer = PromptYesNoCancel("Save modified buffer?");
                if (answer is YesNoCancel.Cancel)
                {
                    _session.SetStatus("Cancelled");
                    return;
                }

                if (answer is YesNoCancel.Yes && !WriteOut())
                {
                    return;
                }
            }

            _session.Running = false;
        }

        private bool WriteOut()
        {
            _session.EndTypingGroup();
            string currentName = _session.Document.FilePath ?? _session.SuggestedSavePath ?? string.Empty;
            string? fileName = Prompt("File Name to Write", currentName, allowEmpty: false);
            if (fileName is null)
            {
                _session.SetStatus("Cancelled");
                return false;
            }

            return _session.Save(fileName);
        }

        private void InsertFile()
        {
            string? fileName = Prompt("File to Insert", string.Empty, allowEmpty: false);
            if (fileName is null)
            {
                _session.SetStatus("Cancelled");
                return;
            }

            _session.InsertFile(fileName);
        }

        private void Search()
        {
            string? needle = Prompt("Search", _session.LastSearch ?? string.Empty, allowEmpty: false);
            if (needle is null)
            {
                _session.SetStatus("Cancelled");
                return;
            }

            _session.Search(needle);
        }

        private void Replace()
        {
            string? needle = Prompt("Search to replace", _session.LastSearch ?? string.Empty, allowEmpty: false);
            if (needle is null)
            {
                _session.SetStatus("Cancelled");
                return;
            }

            string? replacement = Prompt("Replace with", string.Empty, allowEmpty: true);
            if (replacement is null)
            {
                _session.SetStatus("Cancelled");
                return;
            }

            _session.ReplaceAll(needle, replacement);
        }

        private void GoToLine()
        {
            string? answer = Prompt("Enter line,column", $"{_session.Cursor.Line + 1},{_session.Cursor.Column + 1}", allowEmpty: false);
            if (answer is null)
            {
                _session.SetStatus("Cancelled");
                return;
            }

            string[] pieces = answer.Split(',', ':');
            if (pieces.Length > 0 && int.TryParse(pieces[0], out int line))
            {
                int column = 1;
                if (pieces.Length > 1)
                {
                    _ = int.TryParse(pieces[1], out column);
                }

                _session.MoveTo(Math.Max(0, line - 1), Math.Max(0, column - 1));
            }
        }

        private void InsertGuid()
        {
            _session.EndTypingGroup();
            _session.InsertText(Guid.NewGuid().ToString());
        }

        private void ShowHelp()
        {
            _session.EndTypingGroup();
            string[] lines =
            [
                $"{Program.AppName} v{Program.BuildVersion} Help",
                "",
                "^X Exit       ^O Write Out   ^R Read File   ^W Where Is",
                "^\\ Replace    ^K Cut         ^U Paste       ^6 Mark",
                "M-6 Copy      M-U Undo       M-E Redo       ^_ Go To Line",
                "^G Insert GUID at cursor.    ^L toggles line numbers.",
                "Ctrl+Home goto first line.   Ctrl+End goto last line.",
                "",
                "Press any key to return."
            ];

            _session.Console.Clear();
            for (int i = 0; i < lines.Length && i < _session.Console.Size.Rows; i++)
            {
                _session.Console.WriteAt(i, 0, lines[i].PadRight(_session.Console.Size.Columns), ConsoleStyle.Normal);
            }

            _ = _session.Console.ReadKey();
            _session.Console.Clear();
        }

        private string? Prompt(string label, string initial, bool allowEmpty)
        {
            _session.EndTypingGroup();
            string input = initial;
            int cursor = input.Length;

            while (true)
            {
                DrawPrompt(label, input, cursor);
                ConsoleKeyInfo key = _session.Console.ReadKey();

                if (key.Key is ConsoleKey.Escape || IsCtrl(key, 'C'))
                {
                    return null;
                }

                if (key.Key is ConsoleKey.Enter)
                {
                    return allowEmpty || input.Length > 0 ? input : null;
                }

                if (key.Key is ConsoleKey.LeftArrow)
                {
                    cursor = Math.Max(0, cursor - 1);
                }
                else if (key.Key is ConsoleKey.RightArrow)
                {
                    cursor = Math.Min(input.Length, cursor + 1);
                }
                else if (key.Key is ConsoleKey.Home)
                {
                    cursor = 0;
                }
                else if (key.Key is ConsoleKey.End)
                {
                    cursor = input.Length;
                }
                else if (key.Key is ConsoleKey.Backspace && cursor > 0)
                {
                    input = input.Remove(cursor - 1, 1);
                    cursor--;
                }
                else if (key.Key is ConsoleKey.Delete && cursor < input.Length)
                {
                    input = input.Remove(cursor, 1);
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input = input.Insert(cursor, key.KeyChar.ToString());
                    cursor++;
                }
            }
        }

        private YesNoCancel PromptYesNoCancel(string message)
        {
            while (true)
            {
                DrawPrompt($"{message} [Y]es/[N]o/[C]ancel", string.Empty, 0);
                ConsoleKeyInfo key = _session.Console.ReadKey();
                char ch = char.ToUpperInvariant(key.KeyChar);
                if (ch == 'Y')
                {
                    return YesNoCancel.Yes;
                }

                if (ch == 'N')
                {
                    return YesNoCancel.No;
                }

                if (ch == 'C' || key.Key is ConsoleKey.Escape)
                {
                    return YesNoCancel.Cancel;
                }
            }
        }

        private void DrawPrompt(string label, string input, int cursor)
        {
            int row = _session.Layout.StatusRow;
            int columns = _session.Console.Size.Columns;
            string prefix = $"{label}: ";
            string text = prefix + input;
            if (text.Length > columns)
            {
                int keep = Math.Max(0, columns - prefix.Length);
                input = input.Length <= keep ? input : input[^keep..];
                text = prefix + input;
                cursor = Math.Min(input.Length, cursor);
            }

            _session.Console.WriteAt(row, 0, text.PadRight(columns), ConsoleStyle.Status);
            _session.Console.MoveCursor(row, Math.Min(columns - 1, prefix.Length + cursor));
        }

        private static bool IsCtrl(ConsoleKeyInfo key, char letter)
        {
            char upper = char.ToUpperInvariant(letter);
            return key.KeyChar == upper - '@' || ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key.ToString().Equals(upper.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCtrlKey(ConsoleKeyInfo key, ConsoleKey consoleKey)
        {
            return key.Key == consoleKey && (key.Modifiers & ConsoleModifiers.Control) != 0;
        }

        private static bool IsAlt(ConsoleKeyInfo key, char value)
        {
            return (key.Modifiers & ConsoleModifiers.Alt) != 0 && char.ToUpperInvariant(key.KeyChar) == char.ToUpperInvariant(value);
        }

        private enum YesNoCancel
        {
            Yes,
            No,
            Cancel
        }
    }
}
