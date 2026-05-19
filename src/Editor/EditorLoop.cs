/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using NEdit.Commands;
using NEdit.Memory;
using System.Diagnostics;

namespace NEdit.Editor
{
    /// <summary>
    /// Processes terminal input and dispatches commands to an <see cref="EditorSession"/>.
    /// </summary>
    internal sealed class EditorLoop
    {
        /// <summary>
        /// Maps file extensions to the shell command template and working directory behavior.
        /// </summary>
        private static readonly Dictionary<string, (string Command, bool UseFileDirectory)> FileRunners = new(StringComparer.OrdinalIgnoreCase)
        {
            [".ps1"] = ("pwsh.exe \"{file}\"", false),
            [".c"] = ("make", true),
        };
        private readonly EditorSession _session;
        private readonly Renderer _renderer;
        private readonly AppSettings _appSettings;
        private readonly EditorCommandCatalog _commandCatalog;
        private readonly EditorCommandContext _commandContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorLoop"/> class.
        /// </summary>
        /// <param name="session">The editor session to mutate.</param>
        /// <param name="renderer">The renderer used to update the terminal.</param>
        public EditorLoop(EditorSession session, Renderer renderer)
        {
            _session = session;
            _renderer = renderer;
            _appSettings = AppServices.GetRequiredService<AppSettings>();
            _commandCatalog = EditorCommandCatalog.CreateDefault();
            _commandContext = new EditorCommandContext(_session);
        }

        /// <summary>
        /// Runs the editor input loop until the session exits.
        /// </summary>
        public void Run()
        {
            _renderer.Render(_session);
            TerminalSize lastSize = _session.Console.Size;

            while (_session.Running)
            {
                // Poll for keypresses so resize events can be detected between keystrokes.
                while (!_session.Console.KeyAvailable)
                {
                    Thread.Sleep(_appSettings.Options.KeyboardPollingInterval);
                    TerminalSize current = _session.Console.Size;
                    if (current != lastSize)
                    {
                        lastSize = current;
                        _renderer.Render(_session);
                    }
                }

                if (!_session.Running)
                {
                    break;
                }

                ConsoleKeyInfo key = _session.Console.ReadKey();
                HandleKey(key);
                _session.EnsureCursorVisible();
                _renderer.Render(_session);
                lastSize = _session.Console.Size;
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
                _session.MoveLeft(IsShift(key));
            }
            else if (key.Key is ConsoleKey.RightArrow)
            {
                _session.MoveRight(IsShift(key));
            }
            else if (key.Key is ConsoleKey.UpArrow)
            {
                _session.MoveUp(IsShift(key));
            }
            else if (key.Key is ConsoleKey.DownArrow)
            {
                _session.MoveDown(IsShift(key));
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
            else if (key.Key is ConsoleKey.F5)
            {
                RunCurrentFile();
            }
            else if (IsCtrl(key, 'T'))
            {
                ShowCommandPalette();
            }
            else if (IsCtrl(key, 'A'))
            {
                _session.SelectAll();
            }
            else if (IsCtrl(key, 'G'))
            {
                InsertGuid();
            }
            else if (IsCtrl(key, 'X'))
            {
                Exit();
            }
            else if (IsCtrl(key, 'N'))
            {
                NewDocument();
            }
            else if (IsCtrl(key, 'O'))
            {
                OpenFile();
            }
            else if (IsCtrlAlt(key, 'S'))
            {
                Save();
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
            if (!ConfirmSaveModifiedBuffer())
            {
                return;
            }

            _session.Running = false;
        }

        private void NewDocument()
        {
            _session.EndTypingGroup();
            if (!ConfirmSaveModifiedBuffer())
            {
                return;
            }

            _session.NewDocument();
        }

        private bool ConfirmSaveModifiedBuffer()
        {
            if (!_session.Document.Modified)
            {
                return true;
            }

            YesNoCancel answer = PromptYesNoCancel("Save modified buffer?");
            if (answer is YesNoCancel.Cancel)
            {
                _session.SetStatus("Cancelled");
                return false;
            }

            return answer is not YesNoCancel.Yes || Save();
        }

        private bool Save()
        {
            _session.EndTypingGroup();

            // If the file already has a known path, save silently without prompting.
            if (_session.Document.FilePath is not null)
            {
                return _session.Save();
            }

            // New or unsaved buffer — prompt for a filename.
            string currentName = _session.SuggestedSavePath ?? string.Empty;
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
            _session.InsertGuid();
        }

        private void ShowCommandPalette()
        {
            _session.EndTypingGroup();
            string query = string.Empty;
            int cursor = 0;
            int selectedIndex = 0;

            while (true)
            {
                IReadOnlyList<EditorCommand> matches = _commandCatalog.Filter(query);
                if (selectedIndex >= matches.Count)
                {
                    selectedIndex = Math.Max(0, matches.Count - 1);
                }

                _renderer.RenderCommandPalette(_session, query, cursor, matches, selectedIndex, _commandContext);
                ConsoleKeyInfo key = _session.Console.ReadKey();

                if (key.Key is ConsoleKey.Escape || IsCtrl(key, 'C'))
                {
                    _session.SetStatus("Cancelled");
                    return;
                }

                if (key.Key is ConsoleKey.Enter)
                {
                    ExecuteSelectedCommand(matches, selectedIndex, query);
                    return;
                }

                if (key.Key is ConsoleKey.UpArrow)
                {
                    selectedIndex = Math.Max(0, selectedIndex - 1);
                }
                else if (key.Key is ConsoleKey.DownArrow)
                {
                    selectedIndex = Math.Min(Math.Max(0, matches.Count - 1), selectedIndex + 1);
                }
                else if (key.Key is ConsoleKey.Home)
                {
                    cursor = 0;
                }
                else if (key.Key is ConsoleKey.End)
                {
                    cursor = query.Length;
                }
                else if (key.Key is ConsoleKey.LeftArrow)
                {
                    cursor = Math.Max(0, cursor - 1);
                }
                else if (key.Key is ConsoleKey.RightArrow)
                {
                    cursor = Math.Min(query.Length, cursor + 1);
                }
                else if (key.Key is ConsoleKey.Backspace && cursor > 0)
                {
                    query = query.Remove(cursor - 1, 1);
                    cursor--;
                    selectedIndex = 0;
                }
                else if (key.Key is ConsoleKey.Delete && cursor < query.Length)
                {
                    query = query.Remove(cursor, 1);
                    selectedIndex = 0;
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    query = query.Insert(cursor, key.KeyChar.ToString());
                    cursor++;
                    selectedIndex = 0;
                }
            }
        }

        private void ExecuteSelectedCommand(IReadOnlyList<EditorCommand> matches, int selectedIndex, string query)
        {
            if (matches.Count == 0)
            {
                _session.SetStatus("No command matches", alert: true);
                return;
            }

            EditorCommand command = matches[Math.Clamp(selectedIndex, 0, matches.Count - 1)];
            if (!command.CanExecute(_commandContext))
            {
                _session.SetStatus($"{command.Name} is not available", alert: true);
                return;
            }

            if (command.UseFilePicker)
            {
                OpenFile();
                return;
            }

            if (command.UseNewDocument)
            {
                NewDocument();
                return;
            }

            // Extract the argument provided inline via alias (e.g. "cd c:\temp" → "c:\temp").
            string? argument = _commandCatalog.ParseAliasArgument(command, query);

            // If the command requires an argument and none was supplied via alias, prompt for it.
            if (command.ArgumentPrompt is not null && string.IsNullOrWhiteSpace(argument))
            {
                argument = Prompt(command.ArgumentPrompt, string.Empty, allowEmpty: false);
                if (argument is null)
                {
                    _session.SetStatus("Cancelled");
                    return;
                }
            }

            EditorCommandContext context = argument is not null
                ? _commandContext with { Argument = argument }
                : _commandContext;

            command.Execute(context);
        }

        private void OpenFile()
        {
            string browserDir = Directory.GetCurrentDirectory();
            string query = string.Empty;
            int cursor = 0;
            int selectedIndex = 0;

            while (true)
            {
                IReadOnlyList<FileEntry> entries = GetFileEntries(browserDir, query);
                if (selectedIndex >= entries.Count)
                {
                    selectedIndex = Math.Max(0, entries.Count - 1);
                }

                _renderer.RenderFileBrowser(_session, query, cursor, entries, selectedIndex, browserDir);
                ConsoleKeyInfo key = _session.Console.ReadKey();

                if (key.Key is ConsoleKey.Escape || IsCtrl(key, 'C'))
                {
                    _session.SetStatus("Cancelled");
                    return;
                }

                if (key.Key is ConsoleKey.Enter)
                {
                    if (entries.Count == 0)
                    {
                        continue;
                    }

                    FileEntry selected = entries[selectedIndex];
                    if (selected.IsDirectory)
                    {
                        browserDir = Path.GetFullPath(Path.Combine(browserDir, selected.Name));
                        query = string.Empty;
                        cursor = 0;
                        selectedIndex = 0;
                    }
                    else
                    {
                        string path = Path.GetFullPath(Path.Combine(browserDir, selected.Name));
                        if (!ConfirmSaveModifiedBuffer())
                        {
                            return;
                        }

                        _session.OpenFile(path);
                        return;
                    }
                }
                else if (key.Key is ConsoleKey.UpArrow)
                {
                    selectedIndex = Math.Max(0, selectedIndex - 1);
                }
                else if (key.Key is ConsoleKey.DownArrow)
                {
                    selectedIndex = Math.Min(Math.Max(0, entries.Count - 1), selectedIndex + 1);
                }
                else if (key.Key is ConsoleKey.Backspace)
                {
                    if (cursor > 0)
                    {
                        query = query.Remove(cursor - 1, 1);
                        cursor--;
                        selectedIndex = 0;
                    }
                    else
                    {
                        string? parent = Path.GetDirectoryName(browserDir);
                        if (parent is not null)
                        {
                            browserDir = parent;
                            selectedIndex = 0;
                        }
                    }
                }
                else if (key.Key is ConsoleKey.Delete && cursor < query.Length)
                {
                    query = query.Remove(cursor, 1);
                    selectedIndex = 0;
                }
                else if (key.Key is ConsoleKey.LeftArrow)
                {
                    cursor = Math.Max(0, cursor - 1);
                }
                else if (key.Key is ConsoleKey.RightArrow)
                {
                    cursor = Math.Min(query.Length, cursor + 1);
                }
                else if (key.Key is ConsoleKey.Home)
                {
                    cursor = 0;
                }
                else if (key.Key is ConsoleKey.End)
                {
                    cursor = query.Length;
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    query = query.Insert(cursor, key.KeyChar.ToString());
                    cursor++;
                    selectedIndex = 0;
                }
            }
        }

        private static IReadOnlyList<FileEntry> GetFileEntries(string dir, string filter)
        {
            try
            {
                IEnumerable<FileEntry> dirs = Directory.GetDirectories(dir)
                    .Select(d => new FileEntry(Path.GetFileName(d) ?? d, true));
                IEnumerable<FileEntry> files = Directory.GetFiles(dir)
                    .Select(f => new FileEntry(Path.GetFileName(f) ?? f, false));

                IEnumerable<FileEntry> all = dirs.Concat(files);
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    all = all.Where(e => e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
                }

                return all
                    .OrderBy(e => !e.IsDirectory)
                    .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                return [];
            }
        }

        private void RunCurrentFile()
        {
            _session.EndTypingGroup();

            string? filePath = _session.Document.FilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _session.SetStatus("Save the file first before running.", alert: true);
                return;
            }

            string ext = Path.GetExtension(filePath);
            if (!FileRunners.TryGetValue(ext, out var runner))
            {
                _session.SetStatus($"No runner registered for {(string.IsNullOrEmpty(ext) ? "this file type" : ext + " files")}.", alert: true);
                return;
            }

            if (_session.Document.Modified)
            {
                YesNoCancel answer = PromptYesNoCancel("Save before running?");
                if (answer is YesNoCancel.Cancel)
                {
                    _session.SetStatus("Cancelled");
                    return;
                }

                if (answer is YesNoCancel.Yes && !Save())
                {
                    return;
                }
            }

            string command = runner.Command.Replace("{file}", filePath);
            string? workingDir = runner.UseFileDirectory ? Path.GetDirectoryName(filePath) : null;

            _session.Console.LeaveEditorMode();

            int exitCode = -1;
            try
            {
                var psi = new ProcessStartInfo { UseShellExecute = false };

                if (workingDir is not null)
                {
                    psi.WorkingDirectory = workingDir;
                }

                if (OperatingSystem.IsWindows())
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c " + command;
                }
                else
                {
                    psi.FileName = "/bin/sh";
                    psi.Arguments = "-c " + command;
                }

                using Process? process = Process.Start(psi);
                if (process is not null)
                {
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine();
            Console.Write(exitCode == 0
                ? "Completed (exit 0). Press any key to return to editor..."
                : $"Exit code {exitCode}. Press any key to return to editor...");
            Console.ReadKey(intercept: true);

            _session.Console.EnterEditorMode();
            _session.SetStatus(
                exitCode == 0 ? $"Run: {Path.GetFileName(filePath)}" : $"Run: {Path.GetFileName(filePath)} (exit {exitCode})",
                alert: exitCode != 0);
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

        private static bool IsCtrlAlt(ConsoleKeyInfo key, char letter)
        {
            return (key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) == (ConsoleModifiers.Control | ConsoleModifiers.Alt)
                && key.Key.ToString().Equals(char.ToUpperInvariant(letter).ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAlt(ConsoleKeyInfo key, char value)
        {
            return (key.Modifiers & ConsoleModifiers.Alt) != 0 && char.ToUpperInvariant(key.KeyChar) == char.ToUpperInvariant(value);
        }

        private static bool IsShift(ConsoleKeyInfo key)
        {
            return (key.Modifiers & ConsoleModifiers.Shift) != 0;
        }

        private enum YesNoCancel
        {
            /// <summary>
            /// Indicates an affirmative answer.
            /// </summary>
            Yes,

            /// <summary>
            /// Indicates a negative answer.
            /// </summary>
            No,

            /// <summary>
            /// Indicates that the prompt was cancelled.
            /// </summary>
            Cancel
        }
    }
}
