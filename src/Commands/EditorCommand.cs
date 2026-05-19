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
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorCommand"/> class.
        /// </summary>
        /// <param name="name">The display name shown in the command palette.</param>
        /// <param name="description">The description shown for the command.</param>
        /// <param name="hotKey">The optional keyboard shortcut label.</param>
        /// <param name="command">The executable command implementation.</param>
        /// <param name="alias">The optional text alias accepted by the command palette.</param>
        /// <param name="argumentPrompt">The optional prompt label used when an argument is required.</param>
        /// <param name="useFilePicker"><see langword="true" /> to open the file picker before command execution; otherwise, <see langword="false" />.</param>
        /// <param name="useNewDocument"><see langword="true" /> to start a new document before command execution; otherwise, <see langword="false" />.</param>
        public EditorCommand(string name, string description, string? hotKey, ICommand command, string? alias = null, string? argumentPrompt = null, bool useFilePicker = false, bool useNewDocument = false)
        {
            Name = name;
            Description = description;
            HotKey = hotKey;
            Command = command;
            Alias = alias;
            ArgumentPrompt = argumentPrompt;
            UseFilePicker = useFilePicker;
            UseNewDocument = useNewDocument;
        }

        /// <summary>
        /// Gets the display name shown in the command palette.
        /// </summary>
        /// <value>
        /// The command name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the description shown in the command palette.
        /// </summary>
        /// <value>
        /// The command description.
        /// </value>
        public string Description { get; }

        /// <summary>
        /// Gets the optional keyboard shortcut label.
        /// </summary>
        /// <value>
        /// The shortcut text, or <see langword="null" /> when no shortcut is displayed.
        /// </value>
        public string? HotKey { get; }

        /// <summary>
        /// Gets the executable command implementation.
        /// </summary>
        /// <value>
        /// The command invoked when this entry is selected.
        /// </value>
        public ICommand Command { get; }

        /// <summary>
        /// Gets the optional short alias that can be typed in the command palette.
        /// </summary>
        /// <value>
        /// The alias text, or <see langword="null" /> when the command has no alias.
        /// </value>
        public string? Alias { get; }

        /// <summary>
        /// Gets the prompt label displayed when the command requires an argument.
        /// </summary>
        /// <value>
        /// The prompt label, or <see langword="null" /> when no argument prompt is required.
        /// </value>
        public string? ArgumentPrompt { get; }

        /// <summary>
        /// Gets a value that indicates whether the command opens the file picker.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the file picker should be shown; otherwise, <see langword="false" />.
        /// </value>
        public bool UseFilePicker { get; }

        /// <summary>
        /// Gets a value that indicates whether the command starts a new document.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if a new document should be started; otherwise, <see langword="false" />.
        /// </value>
        public bool UseNewDocument { get; }

        /// <summary>
        /// Determines whether the command can execute in the supplied context.
        /// </summary>
        /// <param name="context">The command invocation context.</param>
        /// <returns>
        /// <see langword="true" /> if the command can execute; otherwise, <see langword="false" />.
        /// </returns>
        public bool CanExecute(EditorCommandContext context) => Command.CanExecute(context);

        /// <summary>
        /// Executes the command in the supplied context.
        /// </summary>
        /// <param name="context">The command invocation context.</param>
        public void Execute(EditorCommandContext context) => Command.Execute(context);
    }
}
