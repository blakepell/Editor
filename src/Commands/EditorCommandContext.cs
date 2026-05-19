/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using NEdit.Editor;

namespace NEdit.Commands
{
    /// <summary>
    /// Carries editor state into a command invocation.
    /// </summary>
    /// <param name="Session">The active editor session.</param>
    internal sealed record EditorCommandContext(EditorSession Session)
    {
        /// <summary>
        /// Gets the optional argument supplied by the user.
        /// </summary>
        /// <value>
        /// The alias argument or prompted value, or <see langword="null" /> when no argument was supplied.
        /// </value>
        public string? Argument { get; init; }

        /// <summary>
        /// Gets a value that indicates whether the current session has selected text.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if a non-empty text range is selected; otherwise, <see langword="false" />.
        /// </value>
        public bool HasSelectedText => Session.HasSelection;

        /// <summary>
        /// Gets the currently selected text.
        /// </summary>
        /// <returns>
        /// The selected text, or <see langword="null" /> when no text is selected.
        /// </returns>
        public string? GetSelectedText() => Session.GetSelectedText();

        /// <summary>
        /// Replaces the current selection with the supplied text.
        /// </summary>
        /// <param name="replacement">The replacement text.</param>
        /// <param name="statusMessage">The status message shown after replacement.</param>
        /// <returns>
        /// <see langword="true" /> if the selection was replaced; otherwise, <see langword="false" />.
        /// </returns>
        public bool ReplaceSelection(string replacement, string statusMessage) =>
            Session.ReplaceSelection(replacement, statusMessage);
    }
}
