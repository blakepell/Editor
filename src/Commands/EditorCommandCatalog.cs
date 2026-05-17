/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

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
    }
}
