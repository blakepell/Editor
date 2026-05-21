/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using CommunityToolkit.Mvvm.Input;
using NEdit.Memory;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace NEdit.Commands
{
    /// <summary>
    /// Provides the commands available to the editor command palette.
    /// </summary>
    internal sealed class EditorCommandCatalog
    {
        /// <summary>
        /// Shared <see cref="HttpClient"/> used by the Insert from URL command.
        /// </summary>
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Common makefile names recognized by this editor, checked in order.
        /// </summary>
        private static readonly string[] MakefileNames =
        [
            "Makefile", "makefile", "GNUmakefile", "GNUMakefile",
            "nmakefile", "NMakefile", "MAKEFILE"
        ];

        private readonly List<EditorCommand> _commands;

        private EditorCommandCatalog(List<EditorCommand> commands)
        {
            _commands = [.. commands.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
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
            var appSettings = AppServices.GetRequiredService<AppSettings>();
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
                    "URL Encode Selection",
                    "Percent-encode the selected text for use in a URL.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        UrlEncodeSelection,
                        context => context is not null)),
                new EditorCommand(
                    "URL Decode Selection",
                    "Decode percent-encoded characters in the selected text.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        UrlDecodeSelection,
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
                    useNewDocument: true,
                    showInStatusBar: true,
                    sortOrder: 20,
                    shortLabel: "New"),
                new EditorCommand(
                    "Open File",
                    "Browse and open a file from the current directory.",
                    "Ctrl+O",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useFilePicker: true,
                    showInStatusBar: true,
                    sortOrder: 50,
                    shortLabel: "Open"),
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
                    "Insert Text from URL",
                    "Perform an HTTP GET on a URL and insert the response body at the cursor.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        InsertFromUrl,
                        context => context?.Session.IsReadOnly == false),
                    alias: "url",
                    argumentPrompt: "URL"),
                new EditorCommand(
                    "Find Text",
                    "Search for text in the document.",
                    "Ctrl+F",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useSearch: true,
                    showInStatusBar: true,
                    sortOrder: 70,
                    shortLabel: "Find"),
                new EditorCommand(
                    "Replace Text",
                    "Find and replace text throughout the document.",
                    "Ctrl+H",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context?.Session.IsReadOnly == false),
                    useReplace: true,
                    showInStatusBar: true,
                    sortOrder: 80,
                    shortLabel: "Replace"),
                new EditorCommand(
                    "Save File",
                    "Save the current document to disk.",
                    "Ctrl+Alt+S",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useSave: true,
                    showInStatusBar: true,
                    sortOrder: 60,
                    shortLabel: "Save"),
                new EditorCommand(
                    "Toggle Line Numbers",
                    "Show or hide line numbers in the editor.",
                    "Ctrl+L",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.ToggleLineNumbers(),
                        context => context is not null),
                    showInStatusBar: true,
                    sortOrder: 150,
                    shortLabel: "Line #s"),
                new EditorCommand(
                    "Goto Line",
                    "Move the cursor to the specified line number.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        GotoLine,
                        context => context is not null),
                    alias: "goto",
                    argumentPrompt: "Line"),
                new EditorCommand(
                    "Commands",
                    "Open the command palette.",
                    "Ctrl+T",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    showInStatusBar: true,
                    sortOrder: 10,
                    shortLabel: "Commands"),
                new EditorCommand(
                    "Run File",
                    "Run the current file using the registered runner for its extension.",
                    "F5",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useRunFile: true,
                    showInStatusBar: true,
                    sortOrder: 30,
                    shortLabel: "Run"),
                new EditorCommand(
                    "Make",
                    "Run make in the current working directory.",
                    "Ctrl+M",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null && MakefileExists()),
                    alias: "make",
                    useMake: true),
                new EditorCommand(
                    "Exit",
                    "Exit the editor, prompting to save any unsaved changes.",
                    "Ctrl+X",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useExit: true,
                    showInStatusBar: true,
                    sortOrder: 40,
                    shortLabel: "Exit"),
                new EditorCommand(
                    "Cut",
                    "Cut the selected text or the current line to the clipboard.",
                    "Ctrl+K",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.Cut(),
                        context => context?.Session.IsReadOnly == false),
                    showInStatusBar: true,
                    sortOrder: 90,
                    shortLabel: "Cut"),
                new EditorCommand(
                    "Copy",
                    "Copy the selected text or the current line to the clipboard.",
                    "Ctrl+C",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.Copy(),
                        context => context is not null),
                    showInStatusBar: true,
                    sortOrder: 110,
                    shortLabel: "Copy"),
                new EditorCommand(
                    "Paste",
                    "Paste the clipboard contents at the cursor.",
                    "Ctrl+P",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.Paste(),
                        context => context?.Session.IsReadOnly == false),
                    showInStatusBar: true,
                    sortOrder: 100,
                    shortLabel: "Paste"),
                new EditorCommand(
                    "Undo",
                    "Undo the last edit.",
                    "Ctrl+Z",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.Undo(),
                        context => context?.Session.IsReadOnly == false),
                    showInStatusBar: true,
                    sortOrder: 120,
                    shortLabel: "Undo"),
                new EditorCommand(
                    "Redo",
                    "Redo the last undone edit.",
                    "Ctrl+Alt+Z",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.Redo(),
                        context => context?.Session.IsReadOnly == false),
                    showInStatusBar: true,
                    sortOrder: 130,
                    shortLabel: "Redo"),
                new EditorCommand(
                    "Grep",
                    "Search across files in the current directory using grep.",
                    "Ctrl+G",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    useGrep: true,
                    showInStatusBar: true,
                    sortOrder: 140,
                    shortLabel: "Grep"),
                new EditorCommand(
                    "Comment Code",
                    "Comment the current line or selected lines using the syntax-defined comment token.",
                    "Ctrl+K Ctrl+C",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.CommentLines(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Uncomment Code",
                    "Uncomment the current line or selected lines by removing the syntax-defined comment token.",
                    "Ctrl+K Ctrl+U",
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.UncommentLines(),
                        context => context?.Session.IsReadOnly == false)),
                new EditorCommand(
                    "Compress JSON",
                    "Minify the document or selected JSON by removing all unnecessary whitespace.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        CompressJson,
                        context => context?.Session.IsReadOnly == false && IsJsonContext(context))),
                new EditorCommand(
                    "Format JSON",
                    "Prettify the document or selected JSON with indentation for readability.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        FormatJson,
                        context => context?.Session.IsReadOnly == false && IsJsonContext(context))),
                new EditorCommand(
                    "Validate JSON",
                    "Check whether the document or selected text is valid JSON.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        ValidateJson,
                        context => IsJsonContext(context))),
                new EditorCommand(
                    "Open Recent Files",
                    "Browse and open a recently accessed file.",
                    "Ctrl+Alt+O",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null && appSettings.RecentFiles.Count > 0),
                    useOpenRecentFiles: true),
                new EditorCommand(
                    "Bookmarks",
                    "Browse and navigate to a stored bookmark.",
                    "Ctrl+B",
                    new RelayCommand<EditorCommandContext>(
                        _ => { },
                        context => context is not null),
                    alias: "bookmarks",
                    useBookmarks: true),
                new EditorCommand(
                    "Clear Bookmarks",
                    "Remove all stored bookmarks.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        _ => appSettings.Bookmarks.Clear(),
                        context => context is not null && appSettings.Bookmarks.Count > 0))
            ]);
        }

        /// <summary>
        /// Determines whether a makefile exists in the current working directory.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if a recognized makefile name is found; otherwise, <see langword="false" />.
        /// </returns>
        internal static bool MakefileExists()
        {
            string cwd = Directory.GetCurrentDirectory();
            return MakefileNames.Any(name => File.Exists(Path.Combine(cwd, name)));
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

        private static void UrlEncodeSelection(EditorCommandContext? context)
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

            string encoded = Uri.EscapeDataString(selectedText);
            context.ReplaceSelection(encoded, "URL encoded selection");
        }

        private static void UrlDecodeSelection(EditorCommandContext? context)
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
                string decoded = Uri.UnescapeDataString(selectedText);
                context.ReplaceSelection(decoded, "URL decoded selection");
            }
            catch (UriFormatException)
            {
                context.Session.SetStatus("Selected text is not valid URL-encoded text", alert: true);
            }
        }

        private static readonly JsonWriterOptions _jsonIndentedWriterOptions = new() { Indented = true };

        private static bool IsJsonContext(EditorCommandContext? context)
        {
            if (context is null)
            {
                return false;
            }

            string? path = context.Session.Document.FilePath;
            if (path is not null && Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return context.HasSelectedText;
        }

        private static void CompressJson(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            bool hadSelection = context.HasSelectedText;
            if (!hadSelection)
            {
                context.Session.SelectAll();
            }

            string? input = context.GetSelectedText();
            if (string.IsNullOrEmpty(input))
            {
                if (!hadSelection)
                {
                    context.Session.ClearSelection();
                }

                context.Session.SetStatus("No content to compress.", alert: true);
                return;
            }

            try
            {
                using JsonDocument jsonDoc = JsonDocument.Parse(input);
                using var ms = new MemoryStream();
                using var writer = new Utf8JsonWriter(ms);
                jsonDoc.RootElement.WriteTo(writer);
                writer.Flush();
                string result = Encoding.UTF8.GetString(ms.ToArray());
                context.ReplaceSelection(result, hadSelection ? "Compressed JSON selection" : "Compressed JSON");
            }
            catch (JsonException ex)
            {
                if (!hadSelection)
                {
                    context.Session.ClearSelection();
                }

                context.Session.SetStatus($"Invalid JSON: {ex.Message}", alert: true);
            }
        }

        private static void FormatJson(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            bool hadSelection = context.HasSelectedText;
            if (!hadSelection)
            {
                context.Session.SelectAll();
            }

            string? input = context.GetSelectedText();
            if (string.IsNullOrEmpty(input))
            {
                if (!hadSelection)
                {
                    context.Session.ClearSelection();
                }

                context.Session.SetStatus("No content to format.", alert: true);
                return;
            }

            try
            {
                using JsonDocument jsonDoc = JsonDocument.Parse(input);
                using var ms = new MemoryStream();
                using var writer = new Utf8JsonWriter(ms, _jsonIndentedWriterOptions);
                jsonDoc.RootElement.WriteTo(writer);
                writer.Flush();
                string result = Encoding.UTF8.GetString(ms.ToArray());
                context.ReplaceSelection(result, hadSelection ? "Formatted JSON selection" : "Formatted JSON");
            }
            catch (JsonException ex)
            {
                if (!hadSelection)
                {
                    context.Session.ClearSelection();
                }

                context.Session.SetStatus($"Invalid JSON: {ex.Message}", alert: true);
            }
        }

        private static void ValidateJson(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            string input;
            bool hasSelection = context.HasSelectedText;

            if (hasSelection)
            {
                input = context.GetSelectedText() ?? string.Empty;
            }
            else
            {
                input = string.Join("\n", context.Session.Document.Lines.Select(l => l.ToString()));
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                context.Session.SetStatus("No content to validate.", alert: true);
                return;
            }

            try
            {
                using JsonDocument _ = JsonDocument.Parse(input);
                context.Session.SetStatusSuccess(hasSelection ? "Selection is valid JSON" : "Document is valid JSON");
            }
            catch (JsonException ex)
            {
                context.Session.SetStatus($"Invalid JSON: {ex.Message}", alert: true);
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

        private static void InsertFromUrl(EditorCommandContext? context)
        {
            if (context is null)
            {
                return;
            }

            string? url = context.Argument?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                context.Session.SetStatus("Enter a URL.", alert: true);
                return;
            }

            try
            {
                string text = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                context.Session.InsertText(text);
                context.Session.SetStatus($"Inserted {text.Length:N0} characters from {url}");
            }
            catch (Exception ex)
            {
                string message = ex is TaskCanceledException or TimeoutException
                    ? $"Request timed out: {url}"
                    : $"Error fetching URL: {ex.Message}";
                context.Session.SetStatus(message, alert: true);
            }
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
