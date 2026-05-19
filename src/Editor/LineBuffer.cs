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
        private string _text;
        private StringBuilder? _builder;
        private bool _textCurrent;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineBuffer"/> class.
        /// </summary>
        /// <param name="text">The initial line text.</param>
        public LineBuffer(string text)
        {
            _text = text;
            _textCurrent = true;
        }

        /// <summary>
        /// Gets the number of characters in the line.
        /// </summary>
        /// <value>
        /// The current line length.
        /// </value>
        public int Length => _builder?.Length ?? _text.Length;

        /// <summary>
        /// Gets the character at the specified index.
        /// </summary>
        /// <param name="index">The zero-based character index.</param>
        /// <returns>
        /// The character at <paramref name="index" />.
        /// </returns>
        public char this[int index] => _builder is null ? _text[index] : _builder[index];

        /// <summary>
        /// Inserts text at the specified index.
        /// </summary>
        /// <param name="index">The zero-based insertion index.</param>
        /// <param name="text">The text to insert.</param>
        public void Insert(int index, string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            StringBuilder builder = EnsureBuilder();
            builder.Insert(Clamp(index), text);
            _textCurrent = false;
        }

        /// <summary>
        /// Inserts a character at the specified index.
        /// </summary>
        /// <param name="index">The zero-based insertion index.</param>
        /// <param name="value">The character to insert.</param>
        public void Insert(int index, char value)
        {
            StringBuilder builder = EnsureBuilder();
            builder.Insert(Clamp(index), value);
            _textCurrent = false;
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
            StringBuilder? builder = _builder;
            int textLength = builder?.Length ?? _text.Length;
            int length = Math.Min(count, textLength - start);
            if (length > 0)
            {
                EnsureBuilder().Remove(start, length);
                _textCurrent = false;
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
            string text = ToString();
            return text[start..];
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
            string text = ToString();
            int length = Math.Min(Math.Max(0, count), text.Length - start);
            return text.Substring(start, length);
        }

        /// <summary>
        /// Appends text from the specified index to a builder.
        /// </summary>
        /// <param name="builder">The destination builder.</param>
        /// <param name="index">The zero-based starting index.</param>
        public void AppendTo(StringBuilder builder, int index)
        {
            int start = Clamp(index);
            if (_textCurrent)
            {
                builder.Append(_text.AsSpan(start));
                return;
            }

            StringBuilder currentBuilder = _builder!;
            builder.Append(currentBuilder, start, currentBuilder.Length - start);
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
            int textLength = _builder?.Length ?? _text.Length;
            int length = Math.Min(Math.Max(0, count), textLength - start);
            if (_textCurrent)
            {
                builder.Append(_text.AsSpan(start, length));
                return;
            }

            builder.Append(_builder!, start, length);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_textCurrent)
            {
                return _text;
            }

            _text = _builder!.ToString();
            _textCurrent = true;
            return _text;
        }

        private StringBuilder EnsureBuilder() => _builder ??= new StringBuilder(_text);

        private int Clamp(int index) => Math.Clamp(index, 0, Length);
    }
}
