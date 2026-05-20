/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Text.Json.Serialization;

namespace NEdit.Editor
{
    /// <summary>
    /// Stores configurable editor behavior for the current session and persisted settings.
    /// </summary>
    public sealed class EditorOptions
    {
        /// <summary>
        /// Gets or sets a value that indicates whether the file opens in read-only mode.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if editing commands should be disabled; otherwise, <see langword="false" />.
        /// This is a per-session command-line override and is not persisted to settings.
        /// </value>
        [JsonIgnore]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether line numbers are displayed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the editor displays line numbers; otherwise, <see langword="false" />.
        /// </value>
        public bool LineNumbers { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether soft wrapping is enabled.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if long lines should wrap visually; otherwise, <see langword="false" />.
        /// </value>
        public bool SoftWrap { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether searches are case-sensitive.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if searches match character casing; otherwise, <see langword="false" />.
        /// </value>
        public bool CaseSensitiveSearch { get; set; }

        /// <summary>
        /// Gets or sets the visual width of a tab stop.
        /// </summary>
        /// <value>
        /// The number of columns used when expanding tab characters. The default is <c>4</c>.
        /// </value>
        public int TabSize { get; set; } = 4;

        /// <summary>
        /// Gets or sets a value that indicates whether opening a file changes the working directory
        /// to the directory that contains the opened file.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the working directory should follow the opened file;
        /// otherwise, <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        public bool OpenFileChangesWorkingDirectory { get; set; } = true;

        /// <summary>
        /// Gets or sets the delay between keyboard polling checks.
        /// </summary>
        /// <value>
        /// The polling interval in milliseconds. The default is <c>25</c>.
        /// </value>
        public int KeyboardPollingInterval { get; set; } = 25;
    }

    /// <summary>
    /// Represents a zero-based text position in the document.
    /// </summary>
    /// <param name="Line">The zero-based line index.</param>
    /// <param name="Column">The zero-based column index.</param>
    internal readonly record struct Position(int Line, int Column) : IComparable<Position>
    {
        /// <inheritdoc/>
        public int CompareTo(Position other)
        {
            int lineCompare = Line.CompareTo(other.Line);
            return lineCompare != 0 ? lineCompare : Column.CompareTo(other.Column);
        }
    }

    /// <summary>
    /// Represents the terminal dimensions used for rendering.
    /// </summary>
    /// <param name="Rows">The number of terminal rows.</param>
    /// <param name="Columns">The number of terminal columns.</param>
    internal readonly record struct TerminalSize(int Rows, int Columns);

    /// <summary>
    /// Specifies the newline sequence used when saving a document.
    /// </summary>
    internal enum NewLineKind
    {
        /// <summary>
        /// Uses line feed characters.
        /// </summary>
        Unix,

        /// <summary>
        /// Uses carriage return and line feed character pairs.
        /// </summary>
        Windows,

        /// <summary>
        /// Uses carriage return characters.
        /// </summary>
        Mac
    }
}
