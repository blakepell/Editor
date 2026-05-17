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
        public EditorCommand(string name, string description, string? hotKey, ICommand command)
        {
            Name = name;
            Description = description;
            HotKey = hotKey;
            Command = command;
        }

        public string Name { get; }
        public string Description { get; }
        public string? HotKey { get; }
        public ICommand Command { get; }

        public bool CanExecute(EditorCommandContext context) => Command.CanExecute(context);

        public void Execute(EditorCommandContext context) => Command.Execute(context);
    }
}
