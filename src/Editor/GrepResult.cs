/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit.Editor
{
    /// <summary>
    /// Represents a single grep match result within a file.
    /// </summary>
    /// <param name="FilePath">The full path to the file containing the match.</param>
    /// <param name="FileName">The file name portion of the path, used for display.</param>
    /// <param name="LineNumber">The one-based line number of the match.</param>
    /// <param name="LineText">The text of the matched line.</param>
    internal readonly record struct GrepResult(string FilePath, string FileName, int LineNumber, string LineText);
}
