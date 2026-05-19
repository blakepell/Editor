/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Text;

namespace NEdit.Editor
{
    /// <summary>
    /// Provides mutable storage for a single document line.
    /// </summary>
    internal sealed class LineBuffer
    {
        private readonly StringBuilder _text;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineBuffer"/> class.
        /// </summary>
        /// <param name="text">The initial line text.</param>
        public LineBuffer(string text)
        {
            _text = new StringBuilder(text);
        }

        /// <summary>
        /// Gets the number of characters in the line.
        /// </summary>
        /// <value>
        /// The current line length.
        /// </value>
        public int Length => _text.Length;

        /// <summary>
        /// Gets the character at the specified index.
        /// </summary>
        /// <param name="index">The zero-based character index.</param>
        /// <returns>
        /// The character at <paramref name="index" />.
        /// </returns>
        public char this[int index] => _text[index];

        /// <summary>
        /// Inserts text at the specified index.
        /// </summary>
        /// <param name="index">The zero-based insertion index.</param>
        /// <param name="text">The text to insert.</param>
        public void Insert(int index, string text)
        {
            _text.Insert(Clamp(index), text);
        }

        /// <summary>
        /// Inserts a character at the specified index.
        /// </summary>
        /// <param name="index">The zero-based insertion index.</param>
        /// <param name="value">The character to insert.</param>
        public void Insert(int index, char value)
        {
            _text.Insert(Clamp(index), value);
        }

        /// <summary>
        /// Removes a range of characters from the line.
        /// </summary>
        /// <param name="index">The zero-based starting index.</param>
        /// <param name="count">The number of characters to remove.</param>
        public void Remove(int index, int count)
        {
            if (count <= 0)
            {
                return;
            }

            int start = Clamp(index);
            int length = Math.Min(count, _text.Length - start);
            if (length > 0)
            {
                _text.Remove(start, length);
            }
        }

        /// <summary>
        /// Gets a substring from the specified index to the end of the line.
        /// </summary>
        /// <param name="index">The zero-based starting index.</param>
        /// <returns>
        /// The requested substring.
        /// </returns>
        public string Substring(int index)
        {
            int start = Clamp(index);
            return _text.ToString(start, _text.Length - start);
        }

        /// <summary>
        /// Gets a substring from the specified range.
        /// </summary>
        /// <param name="index">The zero-based starting index.</param>
        /// <param name="count">The number of characters to include.</param>
        /// <returns>
        /// The requested substring.
        /// </returns>
        public string Substring(int index, int count)
        {
            int start = Clamp(index);
            int length = Math.Min(Math.Max(0, count), _text.Length - start);
            return _text.ToString(start, length);
        }

        /// <summary>
        /// Appends text from the specified index to a builder.
        /// </summary>
        /// <param name="builder">The destination builder.</param>
        /// <param name="index">The zero-based starting index.</param>
        public void AppendTo(StringBuilder builder, int index)
        {
            int start = Clamp(index);
            builder.Append(_text, start, _text.Length - start);
        }

        /// <summary>
        /// Appends text from the specified range to a builder.
        /// </summary>
        /// <param name="builder">The destination builder.</param>
        /// <param name="index">The zero-based starting index.</param>
        /// <param name="count">The number of characters to append.</param>
        public void AppendTo(StringBuilder builder, int index, int count)
        {
            int start = Clamp(index);
            int length = Math.Min(Math.Max(0, count), _text.Length - start);
            builder.Append(_text, start, length);
        }

        /// <inheritdoc/>
        public override string ToString() => _text.ToString();

        private int Clamp(int index) => Math.Clamp(index, 0, _text.Length);
    }
}
