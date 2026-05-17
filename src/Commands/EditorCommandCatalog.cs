/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Text;
using CommunityToolkit.Mvvm.Input;

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

        public IReadOnlyList<EditorCommand> Commands => _commands;

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
                        context => context?.Session.IsReadOnly == false))
            ]);
        }

        public IReadOnlyList<EditorCommand> Filter(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return _commands;
            }

            return _commands
                .Where(command => Contains(command.Name, query) || Contains(command.Description, query))
                .ToArray();
        }

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
    }
}
