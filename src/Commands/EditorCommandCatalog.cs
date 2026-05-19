/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Text;

namespace NEdit.Commands
{
    /// <summary>
    /// Provides the commands available to the editor command palette.
    /// </summary>
    internal sealed class EditorCommandCatalog
    {
        private readonly List<EditorCommand> _commands;

        private EditorCommandCatalog(List<EditorCommand> commands)
        {
            _commands = commands;
        }

        /// <summary>
        /// Gets the commands available to the command palette.
        /// </summary>
        /// <value>
        /// The registered editor commands.
        /// </value>
        public IReadOnlyList<EditorCommand> Commands => _commands;

        /// <summary>
        /// Creates the default command catalog for the editor.
        /// </summary>
        /// <returns>
        /// A catalog populated with built-in editor commands.
        /// </returns>
        public static EditorCommandCatalog CreateDefault()
        {
            return new EditorCommandCatalog(
            [
                new EditorCommand(
                    "Trim Current Line",
                    "Trim leading and trailing whitespace from the current line.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.TrimCurrentLine(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Trim All Lines",
                    "Trim leading and trailing whitespace from every line.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.TrimAllLines(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Trim All Lines Leading Space",
                    "Trim leading whitespace from every line.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.TrimAllLinesLeadingSpace(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Trim All Lines Trailing Space",
                    "Trim trailing whitespace from every line.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.TrimAllLinesTrailingSpace(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Remove Empty Lines",
                    "Remove blank and whitespace-only lines from the document.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.RemoveEmptyLines(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Convert Tabs To Spaces",
                    "Expand every tab in the document using the configured tab size.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.ConvertTabsToSpaces(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Convert to Base64",
                    "Convert the selected text to Base64.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        ConvertSelectionToBase64,
                        context => context is not null)),
                new EditorCommand(
                    "Convert from Base64",
                    "Decode the selected Base64 text.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        ConvertSelectionFromBase64,
                        context => context is not null)),
                new EditorCommand(
                    "Insert GUID",
                    "Insert a new GUID at the cursor.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.InsertGuid(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Run Garbage Collector",
                    "Run the garbage collector to free up memory.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => GC.Collect(),
                        context => context is not null)),
                new EditorCommand(
                    "Insert Date",
                    "Insert the current local date at the cursor.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.InsertDate(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Insert Date/Time",
                    "Insert the current local date and time at the cursor.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.InsertDateTime(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "New Document",
                    "Start a new untitled document.",
                    "Ctrl+N",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useNewDocument: true),
                new EditorCommand(
                    "Open File",
                    "Browse and open a file from the current directory.",
                    "Ctrl+O",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useFilePicker: true),
                new EditorCommand(
                    "Show Working Directory",
                    "Display the current working directory in the status bar.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.ShowCurrentDirectory(),
                        context => context is not null)),
                new EditorCommand(
                    "Change Directory",
                    "Change the current working directory to the specified path.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        ChangeDirectory,
                        context => context is not null),
                    alias: "cd",
                    argumentPrompt: "Directory"),
                new EditorCommand(
                    "Shell",
                    "Run a shell command and display its output, then return to the editor.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        RunShell,
                        context => context is not null),
                    alias: "shell",
                    argumentPrompt: "Command"),
                new EditorCommand(
                    "Goto Line",
                    "Move the cursor to the specified line number.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        GotoLine,
                        context => context is not null),
                    alias: "goto",
                    argumentPrompt: "Line")
            ]);
        }

        /// <summary>
        /// Filters commands by name, description, or alias.
        /// </summary>
        /// <param name="query">The user-entered search query.</param>
        /// <returns>
        /// The commands that match <paramref name="query" />.
        /// </returns>
        public IReadOnlyList<EditorCommand> Filter(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return _commands;
            }

            string aliasWord = FirstWord(query);

            return _commands
                .Where(command =>
                    Contains(command.Name, query) ||
                    Contains(command.Description, query) ||
                    MatchesAlias(command, aliasWord))
                .ToArray();
        }

        /// <summary>
        /// Parses an inline alias argument from a command palette query.
        /// </summary>
        /// <param name="command">The command whose alias should be matched.</param>
        /// <param name="query">The command palette query.</param>
        /// <returns>
        /// The argument after the command alias, or <see langword="null" /> when the query does not start with the alias.
        /// </returns>
        public string? ParseAliasArgument(EditorCommand command, string query)
        {
            if (command.Alias is null)
            {
                return null;
            }

            string aliasWord = FirstWord(query);
            if (!MatchesAlias(command, aliasWord))
            {
                return null;
            }

            string argument = query.Length > aliasWord.Length
                ? query[(aliasWord.Length + 1)..].Trim()
                : string.Empty;

            return argument;
        }

        private static string FirstWord(string query)
        {
            int space = query.IndexOf(' ');
            return space < 0 ? query : query[..space];
        }

        private static bool MatchesAlias(EditorCommand command, string word) =>
            command.Alias is not null &&
            string.Equals(command.Alias, word, StringComparison.OrdinalIgnoreCase);

        private static bool Contains(string value, string query) =>
            value.Contains(query, StringComparison.OrdinalIgnoreCase);

        private static void ConvertSelectionToBase64(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            string? selectedText = context.GetSelectedText();
            if (selectedText is null)
            {
                context.Session.SetStatus("Select text first", alert: true);
                return;
            }

            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(selectedText));
            context.ReplaceSelection(encoded, "Converted selection to Base64");
        }

        private static void ConvertSelectionFromBase64(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            string? selectedText = context.GetSelectedText();
            if (selectedText is null)
            {
                context.Session.SetStatus("Select text first", alert: true);
                return;
            }

            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(selectedText));
                context.ReplaceSelection(decoded, "Converted selection from Base64");
            }
            catch (FormatException)
            {
                context.Session.SetStatus("Selected text is not valid Base64", alert: true);
            }
        }

        private static void ChangeDirectory(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            string? path = context.Argument;
            if (string.IsNullOrWhiteSpace(path))
            {
                context.Session.SetStatus("Usage: cd <path>", alert: true);
                return;
            }

            try
            {
                string resolved = Path.GetFullPath(path);
                if (!Directory.Exists(resolved))
                {
                    context.Session.SetStatus($"Directory not found: {path}", alert: true);
                    return;
                }

                Directory.SetCurrentDirectory(resolved);
                context.Session.SetStatusSuccess($"Directory: {resolved}");
            }
            catch (Exception ex)
            {
                context.Session.SetStatus($"Error: {ex.Message}", alert: true);
            }
        }

        private static void RunShell(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            string? command = context.Argument?.Trim();
            if (string.IsNullOrWhiteSpace(command))
            {
                context.Session.SetStatus("Usage: shell <command>", alert: true);
                return;
            }

            context.Session.Console.LeaveEditorMode();

            int exitCode = -1;
            try
            {
                var psi = new ProcessStartInfo { UseShellExecute = false };

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

            context.Session.Console.EnterEditorMode();
            context.Session.SetStatus(
                exitCode == 0 ? $"Shell: {command}" : $"Shell: {command} (exit {exitCode})",
                alert: exitCode != 0);
        }

        private static void GotoLine(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            string? input = context.Argument?.Trim();
            if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out int lineNumber))
            {
                context.Session.SetStatus("Enter a line number.", alert: true);
                return;
            }

            int lineCount = context.Session.Document.LineCount;
            if (lineNumber < 1 || lineNumber > lineCount)
            {
                context.Session.SetStatus($"Line {lineNumber} does not exist (document has {lineCount} line{(lineCount == 1 ? string.Empty : "s")}).", alert: true);
                return;
            }

            context.Session.MoveTo(lineNumber - 1, 0);
            context.Session.SetStatus($"Line {lineNumber}");
        }
    }
}
