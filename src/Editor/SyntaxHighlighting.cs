/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Reflection;
using System.Text.RegularExpressions;

namespace NEdit.Editor
{
    internal readonly record struct HighlightSpan(int Start, int Length, ConsoleStyle Style);

    internal sealed class SyntaxRule
    {
        public SyntaxRule(ConsoleStyle style, bool caseInsensitive)
        {
            Style = style;
            CaseInsensitive = caseInsensitive;
        }

        public ConsoleStyle Style { get; }
        public bool CaseInsensitive { get; }
        public List<Regex> Patterns { get; } = [];
        public Regex? Start { get; set; }
        public Regex? End { get; set; }
        public bool IsMultiline => Start is not null && End is not null;
    }

    internal sealed class SyntaxDefinition
    {
        public SyntaxDefinition(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public List<Regex> FileMatches { get; } = [];
        public List<SyntaxRule> Rules { get; } = [];
    }

    internal sealed class SyntaxLibrary
    {
        private readonly List<SyntaxDefinition> _syntaxes;
        private readonly SyntaxDefinition? _defaultSyntax;

        private SyntaxLibrary(List<SyntaxDefinition> syntaxes)
        {
            _syntaxes = syntaxes;
            _defaultSyntax = syntaxes.FirstOrDefault(s => s.Name.Equals("default", StringComparison.OrdinalIgnoreCase));
        }

        public static SyntaxLibrary LoadEmbedded()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var syntaxes = new List<SyntaxDefinition>();

            foreach (string resourceName in assembly.GetManifestResourceNames()
                         .Where(name => name.EndsWith(".nanorc", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(name => name.Contains(".LocalSyntax.", StringComparison.Ordinal) ? 0 : 1)
                         .ThenBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    continue;
                }

                using var reader = new StreamReader(stream);
                SyntaxDefinition? syntax = NanorcParser.Parse(reader.ReadToEnd());
                if (syntax is not null)
                {
                    syntaxes.Add(syntax);
                }
            }

            return new SyntaxLibrary(syntaxes);
        }

        public SyntaxDefinition? FindForFile(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                string fileName = Path.GetFileName(path);
                foreach (SyntaxDefinition syntax in _syntaxes)
                {
                    if (syntax.FileMatches.Count == 0)
                    {
                        continue;
                    }

                    if (syntax.FileMatches.Any(regex => regex.IsMatch(fileName)))
                    {
                        return syntax;
                    }
                }
            }

            return _defaultSyntax;
        }
    }

    internal sealed class SyntaxHighlighter
    {
        private readonly SyntaxDefinition? _syntax;

        public SyntaxHighlighter(SyntaxDefinition? syntax)
        {
            _syntax = syntax;
        }

        public IReadOnlyList<HighlightSpan> Highlight(DocumentBuffer document, int lineIndex)
        {
            Dictionary<int, List<HighlightSpan>> range = HighlightRange(document, lineIndex, 1);
            return range.TryGetValue(lineIndex, out List<HighlightSpan>? spans) ? spans : [];
        }

        public Dictionary<int, List<HighlightSpan>> HighlightRange(DocumentBuffer document, int firstLine, int lineCount)
        {
            var spansByLine = new Dictionary<int, List<HighlightSpan>>();
            if (_syntax is null || lineCount <= 0 || firstLine >= document.LineCount)
            {
                return spansByLine;
            }

            int startLine = Math.Max(0, firstLine);
            int endLine = Math.Min(document.LineCount - 1, firstLine + lineCount - 1);

            foreach (SyntaxRule rule in _syntax.Rules)
            {
                if (rule.IsMultiline)
                {
                    AddMultilineSpans(document, startLine, endLine, rule, spansByLine);
                    continue;
                }

                for (int lineIndex = startLine; lineIndex <= endLine; lineIndex++)
                {
                    string text = document.LineAt(lineIndex).ToString();
                    if (text.Length == 0)
                    {
                        continue;
                    }

                    List<HighlightSpan> spans = GetList(spansByLine, lineIndex);

                    foreach (Regex pattern in rule.Patterns)
                    {
                        AddMatches(text, pattern, rule.Style, spans);
                    }
                }
            }

            return spansByLine;
        }

        private static void AddMatches(string text, Regex pattern, ConsoleStyle style, List<HighlightSpan> spans)
        {
            try
            {
                foreach (Match match in pattern.Matches(text))
                {
                    if (match.Success && match.Length > 0)
                    {
                        spans.Add(new HighlightSpan(match.Index, match.Length, style));
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Skip expensive syntax rules for this frame.
            }
        }

        private static void AddMultilineSpans(
            DocumentBuffer document,
            int startLine,
            int endLine,
            SyntaxRule rule,
            Dictionary<int, List<HighlightSpan>> spansByLine)
        {
            if (rule.Start is null || rule.End is null)
            {
                return;
            }

            bool inside = false;

            for (int lineIndex = 0; lineIndex <= endLine; lineIndex++)
            {
                string text = document.LineAt(lineIndex).ToString();
                int index = 0;

                while (index <= text.Length)
                {
                    if (!inside)
                    {
                        Match start = MatchFrom(rule.Start, text, index);
                        if (!start.Success)
                        {
                            break;
                        }

                        inside = true;
                        index = Math.Max(start.Index + Math.Max(1, start.Length), start.Index + 1);

                        if (lineIndex >= startLine)
                        {
                            Match endSameLine = MatchFrom(rule.End, text, index);
                            int end = endSameLine.Success ? endSameLine.Index + endSameLine.Length : text.Length;
                            List<HighlightSpan> spans = GetList(spansByLine, lineIndex);
                            spans.Add(new HighlightSpan(start.Index, Math.Max(0, end - start.Index), rule.Style));
                            if (endSameLine.Success)
                            {
                                inside = false;
                                index = Math.Max(end, index);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Match end = MatchFrom(rule.End, text, index);
                        if (lineIndex >= startLine)
                        {
                            int spanEnd = end.Success ? end.Index + end.Length : text.Length;
                            List<HighlightSpan> spans = GetList(spansByLine, lineIndex);
                            spans.Add(new HighlightSpan(0, spanEnd, rule.Style));
                        }

                        if (!end.Success)
                        {
                            break;
                        }

                        inside = false;
                        index = Math.Max(end.Index + Math.Max(1, end.Length), index + 1);
                    }
                }
            }
        }

        private static List<HighlightSpan> GetList(Dictionary<int, List<HighlightSpan>> spansByLine, int lineIndex)
        {
            if (!spansByLine.TryGetValue(lineIndex, out List<HighlightSpan>? spans))
            {
                spans = [];
                spansByLine[lineIndex] = spans;
            }

            return spans;
        }

        private static Match MatchFrom(Regex regex, string text, int startAt)
        {
            if (startAt < 0 || startAt > text.Length)
            {
                return Match.Empty;
            }

            try
            {
                return regex.Match(text, startAt);
            }
            catch (RegexMatchTimeoutException)
            {
                return Match.Empty;
            }
        }
    }

    internal static class NanorcParser
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(25);

        public static SyntaxDefinition? Parse(string text)
        {
            SyntaxDefinition? definition = null;

            foreach (string rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith("syntax ", StringComparison.Ordinal))
                {
                    List<string> tokens = ParseValues(line["syntax ".Length..]);
                    if (tokens.Count == 0)
                    {
                        continue;
                    }

                    definition = new SyntaxDefinition(tokens[0]);
                    foreach (string pattern in tokens.Skip(1))
                    {
                        Regex? regex = Compile(pattern, ignoreCase: true);
                        if (regex is not null)
                        {
                            definition.FileMatches.Add(regex);
                        }
                    }
                }
                else if (definition is not null &&
                         (line.StartsWith("color ", StringComparison.Ordinal) ||
                          line.StartsWith("icolor ", StringComparison.Ordinal)))
                {
                    bool ignoreCase = line.StartsWith("icolor ", StringComparison.Ordinal);
                    string rest = line[(ignoreCase ? "icolor " : "color ").Length..].TrimStart();
                    int styleEnd = FindWhitespace(rest);
                    if (styleEnd <= 0)
                    {
                        continue;
                    }

                    var rule = new SyntaxRule(ParseStyle(rest[..styleEnd]), ignoreCase);
                    foreach (NanorcValue value in ParseRuleValues(rest[styleEnd..]))
                    {
                        Regex? regex = Compile(value.Value, ignoreCase);
                        if (regex is null)
                        {
                            continue;
                        }

                        if (value.Key.Equals("start", StringComparison.OrdinalIgnoreCase))
                        {
                            rule.Start = regex;
                        }
                        else if (value.Key.Equals("end", StringComparison.OrdinalIgnoreCase))
                        {
                            rule.End = regex;
                        }
                        else
                        {
                            rule.Patterns.Add(regex);
                        }
                    }

                    if (rule.Patterns.Count > 0 || rule.IsMultiline)
                    {
                        definition.Rules.Add(rule);
                    }
                }
            }

            return definition;
        }

        private static Regex? Compile(string pattern, bool ignoreCase)
        {
            string translated = TranslateRegex(pattern);
            RegexOptions options = RegexOptions.CultureInvariant;
            if (ignoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            try
            {
                return new Regex(translated, options, RegexTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static string TranslateRegex(string pattern)
        {
            return pattern
                .Replace("\\<", "\\b", StringComparison.Ordinal)
                .Replace("\\>", "\\b", StringComparison.Ordinal)
                .Replace("[:blank:]", " \\t", StringComparison.Ordinal)
                .Replace("[:space:]", "\\s", StringComparison.Ordinal)
                .Replace("[:digit:]", "0-9", StringComparison.Ordinal)
                .Replace("[:upper:]", "A-Z", StringComparison.Ordinal)
                .Replace("[:lower:]", "a-z", StringComparison.Ordinal)
                .Replace("[:alpha:]", "A-Za-z", StringComparison.Ordinal)
                .Replace("[:alnum:]", "A-Za-z0-9", StringComparison.Ordinal)
                .Replace("[:punct:]", @"!""#$%&'()*+,\-./:;<=>?@\[\]^_`{|}~", StringComparison.Ordinal);
        }

        private static ConsoleStyle ParseStyle(string value)
        {
            string[] parts = value.Split(',', 2);
            ConsoleColor foreground = string.IsNullOrWhiteSpace(parts[0]) ? ConsoleStyle.Normal.Foreground : ParseColor(parts[0], ConsoleColor.Gray);
            ConsoleColor background = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
                ? ParseColor(parts[1], ConsoleColor.Black)
                : ConsoleColor.Black;

            return new ConsoleStyle(foreground, background);
        }

        private static ConsoleColor ParseColor(string value, ConsoleColor fallback)
        {
            string normalized = value.Trim().ToLowerInvariant();
            bool bright = normalized.StartsWith("bright", StringComparison.Ordinal);
            if (bright)
            {
                normalized = normalized["bright".Length..];
            }

            return normalized switch
            {
                "black" => bright ? ConsoleColor.DarkGray : ConsoleColor.Black,
                "red" => bright ? ConsoleColor.Red : ConsoleColor.DarkRed,
                "green" => bright ? ConsoleColor.Green : ConsoleColor.DarkGreen,
                "yellow" => bright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow,
                "blue" => bright ? ConsoleColor.Blue : ConsoleColor.DarkBlue,
                "magenta" => bright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta,
                "cyan" => bright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan,
                "white" => bright ? ConsoleColor.White : ConsoleColor.Gray,
                _ => fallback
            };
        }

        private static int FindWhitespace(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<string> ParseValues(string value)
        {
            return ParseRuleValues(value).Select(v => v.Value).ToList();
        }

        private static List<NanorcValue> ParseRuleValues(string value)
        {
            var values = new List<NanorcValue>();
            int index = 0;

            while (index < value.Length)
            {
                SkipWhitespace(value, ref index);
                if (index >= value.Length)
                {
                    break;
                }

                string key = string.Empty;
                int keyStart = index;
                while (index < value.Length && !char.IsWhiteSpace(value[index]) && value[index] != '=')
                {
                    index++;
                }

                if (index < value.Length && value[index] == '=')
                {
                    key = value[keyStart..index];
                    index++;
                }
                else
                {
                    index = keyStart;
                }

                string parsed = ReadValue(value, ref index);
                if (parsed.Length > 0)
                {
                    values.Add(new NanorcValue(key, parsed));
                }
            }

            return values;
        }

        private static string ReadValue(string value, ref int index)
        {
            SkipWhitespace(value, ref index);
            if (index >= value.Length)
            {
                return string.Empty;
            }

            if (value[index] != '"')
            {
                int start = index;
                while (index < value.Length && !char.IsWhiteSpace(value[index]))
                {
                    index++;
                }

                return value[start..index];
            }

            index++;
            var builder = new System.Text.StringBuilder();
            while (index < value.Length)
            {
                char ch = value[index];
                if (ch == '"' && (index + 1 >= value.Length || char.IsWhiteSpace(value[index + 1])))
                {
                    index++;
                    break;
                }

                builder.Append(ch);
                index++;
            }

            return builder.ToString();
        }

        private static void SkipWhitespace(string value, ref int index)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index++;
            }
        }

        private readonly record struct NanorcValue(string Key, string Value);
    }
}
