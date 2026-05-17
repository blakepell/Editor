/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Text;

namespace NEdit
{
    internal sealed class LineBuffer
    {
        private readonly StringBuilder _text;

        public LineBuffer(string text)
        {
            _text = new StringBuilder(text);
        }

        public int Length => _text.Length;

        public char this[int index] => _text[index];

        public void Insert(int index, string text)
        {
            _text.Insert(Clamp(index), text);
        }

        public void Insert(int index, char value)
        {
            _text.Insert(Clamp(index), value);
        }

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

        public string Substring(int index)
        {
            int start = Clamp(index);
            return _text.ToString(start, _text.Length - start);
        }

        public string Substring(int index, int count)
        {
            int start = Clamp(index);
            int length = Math.Min(Math.Max(0, count), _text.Length - start);
            return _text.ToString(start, length);
        }

        public void AppendTo(StringBuilder builder, int index)
        {
            int start = Clamp(index);
            builder.Append(_text, start, _text.Length - start);
        }

        public void AppendTo(StringBuilder builder, int index, int count)
        {
            int start = Clamp(index);
            int length = Math.Min(Math.Max(0, count), _text.Length - start);
            builder.Append(_text, start, length);
        }

        public override string ToString() => _text.ToString();

        private int Clamp(int index) => Math.Clamp(index, 0, _text.Length);
    }
}
