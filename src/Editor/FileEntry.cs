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
    /// <param name="Name">The display name of the file system entry.</param>
    /// <param name="IsDirectory"><see langword="true" /> if the entry is a directory; otherwise, <see langword="false" />.</param>
    internal readonly record struct FileEntry(string Name, bool IsDirectory);
}
