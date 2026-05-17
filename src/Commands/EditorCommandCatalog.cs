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
                    "Convert Tabs To Spaces",
                    "Expand every tab in the document using the configured tab size.",
                    null,
                    new RelayCommand<EditorCommandContext>(
                        context => context?.Session.ConvertTabsToSpaces(),
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
