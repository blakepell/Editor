/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit.Editor
{
    /// <summary>
    /// Represents a file system entry displayed in the file browser.
    /// </summary>
    internal readonly record struct FileEntry(string Name, bool IsDirectory);
}
