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
        /// <param name="useSearch"><see langword="true" /> to invoke the interactive find/search prompt; otherwise, <see langword="false" />.</param>
        /// <param name="useReplace"><see langword="true" /> to invoke the interactive find-and-replace prompt; otherwise, <see langword="false" />.</param>
        /// <param name="useSave"><see langword="true" /> to invoke the save workflow (prompting for a filename when needed); otherwise, <see langword="false" />.</param>
        /// <param name="useExit"><see langword="true" /> to invoke the editor exit workflow; otherwise, <see langword="false" />.</param>
        /// <param name="useGrep"><see langword="true" /> to invoke the interactive grep search; otherwise, <see langword="false" />.</param>
        /// <param name="useRunFile"><see langword="true" /> to run the current file; otherwise, <see langword="false" />.</param>
        /// <param name="useMake"><see langword="true" /> to invoke the make workflow; otherwise, <see langword="false" />.</param>
        /// <param name="useOpenRecentFiles"><see langword="true" /> to show the recent files panel; otherwise, <see langword="false" />.</param>
        /// <param name="useBookmarks"><see langword="true" /> to show the bookmarks panel; otherwise, <see langword="false" />.</param>
        /// <param name="showInStatusBar"><see langword="true" /> to display this command in the bottom shortcut bar; otherwise, <see langword="false" />.</param>
        /// <param name="sortOrder">The position order in the bottom shortcut bar. Lower values appear first.</param>
        /// <param name="shortLabel">The short label displayed in the bottom shortcut bar. When <see langword="null" />, the <paramref name="name" /> is used.</param>
        public EditorCommand(string name, string description, string? hotKey, ICommand command, string? alias = null, string? argumentPrompt = null, bool useFilePicker = false, bool useNewDocument = false, bool useSearch = false, bool useReplace = false, bool useSave = false, bool useExit = false, bool useGrep = false, bool useRunFile = false, bool useMake = false, bool useOpenRecentFiles = false, bool useBookmarks = false, bool showInStatusBar = false, int sortOrder = int.MaxValue, string? shortLabel = null)
        {
            Name = name;
            Description = description;
            HotKey = hotKey;
            Command = command;
            Alias = alias;
            ArgumentPrompt = argumentPrompt;
            UseFilePicker = useFilePicker;
            UseNewDocument = useNewDocument;
            UseSearch = useSearch;
            UseReplace = useReplace;
            UseSave = useSave;
            UseExit = useExit;
            UseGrep = useGrep;
            UseRunFile = useRunFile;
            UseMake = useMake;
            UseOpenRecentFiles = useOpenRecentFiles;
            UseBookmarks = useBookmarks;
            ShowInStatusBar = showInStatusBar;
            SortOrder = sortOrder;
            ShortLabel = shortLabel;
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
        /// Gets a value that indicates whether the command invokes the interactive find/search prompt.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the search prompt should be shown; otherwise, <see langword="false" />.
        /// </value>
        public bool UseSearch { get; }

        /// <summary>
        /// Gets a value that indicates whether the command invokes the interactive find-and-replace prompt.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the replace prompt should be shown; otherwise, <see langword="false" />.
        /// </value>
        public bool UseReplace { get; }

        /// <summary>
        /// Gets a value that indicates whether the command invokes the save workflow.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the save workflow should be invoked; otherwise, <see langword="false" />.
        /// </value>
        public bool UseSave { get; }

        /// <summary>
        /// Gets a value that indicates whether the command invokes the editor exit workflow.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the exit workflow should be invoked; otherwise, <see langword="false" />.
        /// </value>
        public bool UseExit { get; }

        /// <summary>
        /// Gets a value that indicates whether the command invokes the interactive grep search.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the grep search should be shown; otherwise, <see langword="false" />.
        /// </value>
        public bool UseGrep { get; }

        /// <summary>
        /// Gets a value that indicates whether the command runs the current file.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the current file should be run; otherwise, <see langword="false" />.
        /// </value>
        public bool UseRunFile { get; }

        /// <summary>
        /// Gets a value that indicates whether the command invokes the make workflow.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the make workflow should be invoked; otherwise, <see langword="false" />.
        /// </value>
        public bool UseMake { get; }

        /// <summary>
        /// Gets a value that indicates whether the command shows the recent files panel.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the recent files panel should be shown; otherwise, <see langword="false" />.
        /// </value>
        public bool UseOpenRecentFiles { get; }

        /// <summary>
        /// Gets a value that indicates whether the command shows the bookmarks panel.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the bookmarks panel should be shown; otherwise, <see langword="false" />.
        /// </value>
        public bool UseBookmarks { get; }

        /// <summary>
        /// Gets a value that indicates whether this command is shown in the bottom shortcut bar.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the command appears in the shortcut bar; otherwise, <see langword="false" />.
        /// </value>
        public bool ShowInStatusBar { get; }

        /// <summary>
        /// Gets the position order used when laying out the bottom shortcut bar.
        /// </summary>
        /// <value>
        /// A non-negative integer; lower values appear further left. Defaults to <see cref="int.MaxValue"/>.
        /// </value>
        public int SortOrder { get; }

        /// <summary>
        /// Gets the short label displayed in the bottom shortcut bar.
        /// </summary>
        /// <value>
        /// The short label, or <see langword="null" /> when <see cref="Name" /> should be used instead.
        /// </value>
        public string? ShortLabel { get; }

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
