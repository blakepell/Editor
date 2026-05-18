/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Windows.Input;

namespace NEdit.Commands
{
    /// <summary>
    /// Describes an editor command that can be listed, filtered, and invoked.
    /// </summary>
    internal sealed class EditorCommand
    {
        public EditorCommand(string name, string description, string? hotKey, ICommand command, string? alias = null, string? argumentPrompt = null)
        {
            Name = name;
            Description = description;
            HotKey = hotKey;
            Command = command;
            Alias = alias;
            ArgumentPrompt = argumentPrompt;
        }

        public string Name { get; }
        public string Description { get; }
        public string? HotKey { get; }
        public ICommand Command { get; }

        /// <summary>
        /// Optional short alias (e.g. <c>cd</c>) that can be typed in the command palette
        /// followed by an argument (e.g. <c>cd c:\temp</c>).
        /// </summary>
        public string? Alias { get; }

        /// <summary>
        /// When set, the command palette will display a prompt with this label when the
        /// command is invoked without an argument. The user's input is passed as
        /// <see cref="EditorCommandContext.Argument"/>.
        /// </summary>
        public string? ArgumentPrompt { get; }

        public bool CanExecute(EditorCommandContext context) => Command.CanExecute(context);

        public void Execute(EditorCommandContext context) => Command.Execute(context);
    }
}
