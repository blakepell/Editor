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
    internal sealed record EditorCommandContext(EditorSession Session)
    {
        public bool HasSelectedText => Session.HasSelection;

        public string? GetSelectedText() => Session.GetSelectedText();

        public bool ReplaceSelection(string replacement, string statusMessage) =>
            Session.ReplaceSelection(replacement, statusMessage);
    }
}
