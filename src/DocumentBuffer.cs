/*
 * @project  : ApexGate Editor
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Text;

namespace Editor
{
    internal sealed class DocumentBuffer
    {
        private DocumentBuffer(string? filePath, List<LineBuffer> lines, Encoding encoding, NewLineKind newLineKind)
        {
            FilePath = filePath;
            Lines = lines.Count == 0 ? [new LineBuffer(string.Empty)] : lines;
            Encoding = encoding;
            NewLineKind = newLineKind;
        }

        public string? FilePath { get; set; }
        public List<LineBuffer> Lines { get; }
        public Encoding Encoding { get; private set; }
        public NewLineKind NewLineKind { get; private set; }
        public bool Modified { get; set; }
        public bool ReadOnlyFromFile { get; private set; }

        public static DocumentBuffer Load(string? path, EditorOptions options)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new DocumentBuffer(null, [new LineBuffer(string.Empty)], new UTF8Encoding(false), NewLineKind.Unix);
            }

            if (!File.Exists(path))
            {
                return new DocumentBuffer(path, [new LineBuffer(string.Empty)], new UTF8Encoding(false), NewLineKind.Unix);
            }

            byte[] bytes = File.ReadAllBytes(path);
            Encoding encoding = DetectEncoding(bytes);
            string text = encoding.GetString(RemoveBom(bytes));
            NewLineKind newline = DetectNewLine(text);
            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            List<LineBuffer> lines = normalized.Split('\n').Select(part => new LineBuffer(part)).ToList();

            var buffer = new DocumentBuffer(path, lines, encoding, newline)
            {
                ReadOnlyFromFile = (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0
            };

            if (options.ReadOnly)
            {
                buffer.ReadOnlyFromFile = true;
            }

            return buffer;
        }

        public void Save(string? path = null)
        {
            string target = path ?? FilePath ?? throw new InvalidOperationException("No file name");
            string newline = NewLineKind switch
            {
                NewLineKind.Windows => "\r\n",
                NewLineKind.Mac => "\r",
                _ => "\n"
            };

            string text = string.Join(newline, Lines.Select(line => line.ToString()));
            string directory = Path.GetDirectoryName(Path.GetFullPath(target)) ?? ".";
            string temp = Path.Combine(directory, $".{Path.GetFileName(target)}.{Environment.ProcessId}.tmp");

            File.WriteAllText(temp, text, Encoding);

            if (File.Exists(target))
            {
                File.Copy(temp, target, overwrite: true);
                File.Delete(temp);
            }
            else
            {
                File.Move(temp, target);
            }

            FilePath = target;
            Modified = false;
            ReadOnlyFromFile = false;
        }

        public string DisplayName => string.IsNullOrEmpty(FilePath) ? "New Buffer" : FilePath;

        public int LineCount => Lines.Count;

        public LineBuffer LineAt(int index) => Lines[Math.Clamp(index, 0, Lines.Count - 1)];

        public Position Clamp(Position position)
        {
            int line = Math.Clamp(position.Line, 0, Lines.Count - 1);
            int column = Math.Clamp(position.Column, 0, Lines[line].Length);
            return new Position(line, column);
        }

        public string GetText(Position start, Position end)
        {
            (start, end) = Order(start, end);
            if (start.Line == end.Line)
            {
                return Lines[start.Line].Substring(start.Column, end.Column - start.Column);
            }

            var builder = new StringBuilder();
            Lines[start.Line].AppendTo(builder, start.Column);
            builder.Append('\n');

            for (int line = start.Line + 1; line < end.Line; line++)
            {
                Lines[line].AppendTo(builder, 0);
                builder.Append('\n');
            }

            Lines[end.Line].AppendTo(builder, 0, end.Column);
            return builder.ToString();
        }

        public void ReplaceRange(Position start, Position end, string replacement)
        {
            (start, end) = Order(Clamp(start), Clamp(end));
            DeleteRange(start, end);
            InsertText(start, replacement);
        }

        public Position InsertText(Position position, string text)
        {
            position = Clamp(position);
            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] pieces = normalized.Split('\n');

            if (pieces.Length == 1)
            {
                Lines[position.Line].Insert(position.Column, pieces[0]);
                return new Position(position.Line, position.Column + pieces[0].Length);
            }

            LineBuffer line = Lines[position.Line];
            string tail = line.Substring(position.Column);
            line.Remove(position.Column, line.Length - position.Column);
            line.Insert(position.Column, pieces[0]);

            int insertAt = position.Line + 1;
            for (int i = 1; i < pieces.Length; i++)
            {
                string content = i == pieces.Length - 1 ? pieces[i] + tail : pieces[i];
                Lines.Insert(insertAt++, new LineBuffer(content));
            }

            return new Position(position.Line + pieces.Length - 1, pieces[^1].Length);
        }

        public void DeleteRange(Position start, Position end)
        {
            (start, end) = Order(Clamp(start), Clamp(end));
            if (start.CompareTo(end) == 0)
            {
                return;
            }

            if (start.Line == end.Line)
            {
                Lines[start.Line].Remove(start.Column, end.Column - start.Column);
                return;
            }

            string tail = Lines[end.Line].Substring(end.Column);
            Lines[start.Line].Remove(start.Column, Lines[start.Line].Length - start.Column);
            Lines[start.Line].Insert(Lines[start.Line].Length, tail);
            Lines.RemoveRange(start.Line + 1, end.Line - start.Line);
        }

        public static (Position Start, Position End) Order(Position a, Position b)
        {
            return a.CompareTo(b) <= 0 ? (a, b) : (b, a);
        }

        private static Encoding DetectEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            }

            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        }

        private static byte[] RemoveBom(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return bytes[3..];
            }

            return bytes;
        }

        private static NewLineKind DetectNewLine(string text)
        {
            int lf = text.IndexOf('\n');
            int cr = text.IndexOf('\r');

            if (cr >= 0 && cr + 1 < text.Length && text[cr + 1] == '\n')
            {
                return NewLineKind.Windows;
            }

            if (cr >= 0 && (lf < 0 || cr < lf))
            {
                return NewLineKind.Mac;
            }

            return NewLineKind.Unix;
        }
    }
}
