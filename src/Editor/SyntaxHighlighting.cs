/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Reflection;
using System.Text.RegularExpressions;

namespace NEdit.Editor
{
    /// <summary>
    /// Represents a styled range within a single line.
    /// </summary>
    /// <param name="Start">The zero-based start column.</param>
    /// <param name="Length">The number of highlighted characters.</param>
    /// <param name="Style">The style applied to the range.</param>
    /// <summary>
    /// The zero-based declaration index of the rule that produced this span.
    /// Spans with a higher priority are painted last and win over overlapping lower-priority spans,
    /// matching GNU nano's "last matching rule wins" semantics.
    /// </summary>
    internal readonly record struct HighlightSpan(int Start, int Length, ConsoleStyle Style, int Priority);

    /// <summary>
    /// Describes a syntax highlighting rule parsed from a nanorc file.
    /// </summary>
    internal sealed class SyntaxRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxRule"/> class.
        /// </summary>
        /// <param name="style">The style applied to matching text.</param>
        /// <param name="caseInsensitive"><see langword="true" /> to compile patterns case-insensitively; otherwise, <see langword="false" />.</param>
        public SyntaxRule(ConsoleStyle style, bool caseInsensitive)
        {
            Style = style;
            CaseInsensitive = caseInsensitive;
        }

        /// <summary>
        /// Gets the style applied to matching text.
        /// </summary>
        /// <value>
        /// The highlight style.
        /// </value>
        public ConsoleStyle Style { get; }

        /// <summary>
        /// Gets a value that indicates whether the rule uses case-insensitive matching.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if matching ignores case; otherwise, <see langword="false" />.
        /// </value>
        public bool CaseInsensitive { get; }

        /// <summary>
        /// Gets the single-line regex patterns for the rule.
        /// </summary>
        /// <value>
        /// The compiled regex patterns.
        /// </value>
        public List<Regex> Patterns { get; } = [];

        /// <summary>
        /// Gets or sets the multiline start pattern.
        /// </summary>
        /// <value>
        /// The compiled start pattern, or <see langword="null" /> when the rule is single-line.
        /// </value>
        public Regex? Start { get; set; }

        /// <summary>
        /// Gets or sets the multiline end pattern.
        /// </summary>
        /// <value>
        /// The compiled end pattern, or <see langword="null" /> when the rule is single-line.
        /// </value>
        public Regex? End { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the rule spans multiple lines.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if both start and end patterns are set; otherwise, <see langword="false" />.
        /// </value>
        public bool IsMultiline => Start is not null && End is not null;
    }

    /// <summary>
    /// Describes a complete syntax definition parsed from a nanorc file.
    /// </summary>
    internal sealed class SyntaxDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxDefinition"/> class.
        /// </summary>
        /// <param name="name">The syntax definition name.</param>
        public SyntaxDefinition(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the syntax definition name.
        /// </summary>
        /// <value>
        /// The name declared by the nanorc <c>syntax</c> line.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the file name patterns matched by this syntax.
        /// </summary>
        /// <value>
        /// The compiled file matching patterns.
        /// </value>
        public List<Regex> FileMatches { get; } = [];

        /// <summary>
        /// Gets the highlighting rules for this syntax.
        /// </summary>
        /// <value>
        /// The parsed syntax rules.
        /// </value>
        public List<SyntaxRule> Rules { get; } = [];

        /// <summary>
        /// Gets or sets the comment prefix token (e.g., <c>//</c>, <c>#</c>, or <c>&lt;!--</c>).
        /// </summary>
        /// <value>
        /// The opening comment token, or <see langword="null" /> when no comment style is defined.
        /// </value>
        public string? CommentPrefix { get; set; }

        /// <summary>
        /// Gets or sets the block comment close token (e.g., <c>--&gt;</c>).
        /// When set, <see cref="CommentPrefix"/> is the open token and this is the matching close token.
        /// </summary>
        /// <value>
        /// The closing comment token, or <see langword="null" /> for line-comment style.
        /// </value>
        public string? CommentSuffix { get; set; }
    }

    /// <summary>
    /// Loads and resolves syntax definitions embedded in the editor assembly.
    /// </summary>
    internal sealed class SyntaxLibrary
    {
        private const string ResourcePrefix = "Nano.LocalSyntax.";
        private const string ResourceSuffix = ".nanorc";
        private static readonly Dictionary<string, string[]> CandidateAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["adoc"] = ["asciidoc"],
            ["asc"] = ["asciidoc"],
            ["ac"] = ["autoconf"],
            ["asm"] = ["asm"],
            ["bash_aliases"] = ["sh"],
            ["bash_functions"] = ["sh"],
            ["bash_login"] = ["sh"],
            ["bash_logout"] = ["sh"],
            ["bash_profile"] = ["sh"],
            ["bashrc"] = ["sh"],
            ["bat"] = ["batch"],
            ["capfile"] = ["ruby"],
            ["cc"] = ["c"],
            ["cfg"] = ["ini", "conf"],
            ["changelog"] = ["changelog"],
            ["colorTest"] = ["colortest"],
            ["clj"] = ["clojure"],
            ["cljc"] = ["clojure"],
            ["cljs"] = ["clojure"],
            ["cmd"] = ["batch"],
            ["cmakelists.txt"] = ["cmake"],
            ["commit_editmsg"] = ["git"],
            ["config.ru"] = ["ruby"],
            ["coffee"] = ["coffeescript"],
            ["cpp"] = ["c"],
            ["cs"] = ["csharp"],
            ["cxx"] = ["c"],
            ["dockerfile"] = ["Dockerfile"],
            ["edn"] = ["clojure"],
            ["el"] = ["elisp"],
            ["eml"] = ["email"],
            ["env"] = ["dotenv"],
            ["erb"] = ["erb"],
            ["ex"] = ["elixir"],
            ["exs"] = ["elixir"],
            ["f"] = ["fortran"],
            ["f90"] = ["fortran"],
            ["f95"] = ["fortran"],
            ["for"] = ["fortran"],
            ["fs"] = ["fsharp"],
            ["fsproj"] = ["csproj"],
            ["fsx"] = ["fsharp"],
            ["gemini"] = ["gemini"],
            ["gemfile"] = ["ruby"],
            ["git-rebase-todo"] = ["git"],
            ["gitconfig"] = ["git"],
            ["gitmodules"] = ["git"],
            ["gmi"] = ["gemini"],
            ["gradle"] = ["gradle"],
            ["groovy"] = ["gradle"],
            ["gs"] = ["genie"],
            ["gv"] = ["dot"],
            ["h"] = ["c"],
            ["hh"] = ["c"],
            ["hcl"] = ["hcl"],
            ["hpp"] = ["c"],
            ["hs"] = ["haskell"],
            ["htm"] = ["html"],
            ["html"] = ["html"],
            ["hxx"] = ["c"],
            ["i"] = ["c"],
            ["ii"] = ["c"],
            ["ino"] = ["c", "arduino"],
            ["j2"] = ["html.j2", "html"],
            ["jade"] = ["jade"],
            ["java"] = ["java"],
            ["js"] = ["js", "javascript"],
            ["json"] = ["json"],
            ["kt"] = ["kotlin"],
            ["kts"] = ["kotlin"],
            ["less"] = ["css"],
            ["lua"] = ["lua"],
            ["makefile"] = ["makefile"],
            ["md"] = ["markdown"],
            ["mkd"] = ["markdown"],
            ["mkdn"] = ["markdown"],
            ["m"] = ["objc", "octave"],
            ["patch"] = ["patch"],
            ["php"] = ["php"],
            ["pkgbuild"] = ["pkgbuild"],
            ["profile"] = ["sh"],
            ["ps1"] = ["powershell"],
            ["psm1"] = ["powershell"],
            ["props"] = ["csproj"],
            ["pxd"] = ["cython"],
            ["py"] = ["python"],
            ["pyx"] = ["cython"],
            ["pyi"] = ["cython", "python"],
            ["rakefile"] = ["ruby"],
            ["rb"] = ["ruby"],
            ["rego"] = ["rego"],
            ["rs"] = ["rust"],
            ["rst"] = ["reST"],
            ["scss"] = ["css"],
            ["service"] = ["systemd"],
            ["sh"] = ["sh"],
            ["sls"] = ["sls"],
            ["socket"] = ["systemd"],
            ["swift"] = ["swift"],
            ["tag_editmsg"] = ["git"],
            ["targets"] = ["csproj"],
            ["tf"] = ["hcl"],
            ["timer"] = ["systemd"],
            ["toml"] = ["toml"],
            ["ts"] = ["ts", "javascript"],
            ["tsx"] = ["ts", "javascript"],
            ["twig"] = ["html"],
            ["vagrantfile"] = ["ruby"],
            ["vbproj"] = ["csproj"],
            ["xml"] = ["xml"],
            ["xdefaults"] = ["xresources"],
            ["xresources"] = ["xresources"],
            ["yaml"] = ["yaml"],
            ["yml"] = ["yaml"],
            ["zig"] = ["zig"],
            ["zsh"] = ["zsh"]
        };

        private readonly Assembly _assembly;
        private readonly Dictionary<string, string> _resourcesByKey;
        private readonly Dictionary<string, SyntaxDefinition?> _cache = new(StringComparer.OrdinalIgnoreCase);

        private SyntaxLibrary(Assembly assembly, IEnumerable<string> resourceNames)
        {
            _assembly = assembly;
            _resourcesByKey = resourceNames
                .Where(name => name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
                .Select(name => new { Key = GetResourceKey(name), Name = name })
                .Where(resource => resource.Key.Length > 0)
                .GroupBy(resource => resource.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a lazy syntax library from embedded nanorc resources.
        /// </summary>
        /// <returns>
        /// A syntax library that parses embedded definitions on demand.
        /// </returns>
        public static SyntaxLibrary LoadEmbedded()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return new SyntaxLibrary(assembly, assembly.GetManifestResourceNames());
        }

        /// <summary>
        /// Finds the syntax definition that matches a file path.
        /// </summary>
        /// <param name="path">The file path to match, or <see langword="null" /> to use the default syntax.</param>
        /// <returns>
        /// The matching syntax definition, the default syntax definition, or <see langword="null" /> when none is available.
        /// </returns>
        public SyntaxDefinition? FindForFile(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                string fileName = Path.GetFileName(path);
                foreach (string key in CandidateKeys(fileName))
                {
                    SyntaxDefinition? syntax = LoadSyntax(key);
                    if (syntax is null || syntax.FileMatches.Count == 0)
                    {
                        continue;
                    }

                    if (syntax.FileMatches.Any(regex => regex.IsMatch(fileName)))
                    {
                        return syntax;
                    }
                }
            }

            return LoadSyntax("default");
        }

        private SyntaxDefinition? LoadSyntax(string key)
        {
            if (_cache.TryGetValue(key, out SyntaxDefinition? cached))
            {
                return cached;
            }

            if (!_resourcesByKey.TryGetValue(key, out string? resourceName))
            {
                _cache[key] = null;
                return null;
            }

            using Stream? stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                _cache[key] = null;
                return null;
            }

            using var reader = new StreamReader(stream);
            SyntaxDefinition? syntax = NanorcParser.Parse(reader.ReadToEnd());
            _cache[key] = syntax;
            return syntax;
        }

        private static IEnumerable<string> CandidateKeys(string fileName)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string candidate in RawCandidateKeys(fileName))
            {
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private static IEnumerable<string> RawCandidateKeys(string fileName)
        {
            string trimmed = fileName.Trim();
            if (trimmed.Length == 0)
            {
                yield break;
            }

            foreach (string alias in AliasesFor(trimmed))
            {
                yield return alias;
            }

            yield return trimmed;
            yield return trimmed.TrimStart('.');

            string extension = Path.GetExtension(trimmed).TrimStart('.');
            if (extension.Length > 0)
            {
                foreach (string alias in AliasesFor(extension))
                {
                    yield return alias;
                }

                yield return extension;
            }

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(trimmed).TrimStart('.');
            if (nameWithoutExtension.Length > 0)
            {
                foreach (string alias in AliasesFor(nameWithoutExtension))
                {
                    yield return alias;
                }

                yield return nameWithoutExtension;
            }
        }

        private static IEnumerable<string> AliasesFor(string key)
        {
            if (CandidateAliases.TryGetValue(key, out string[]? aliases))
            {
                foreach (string alias in aliases)
                {
                    yield return alias;
                }
            }
        }

        private static string GetResourceKey(string resourceName)
        {
            string key = resourceName;
            int prefixIndex = key.IndexOf(ResourcePrefix, StringComparison.Ordinal);
            if (prefixIndex >= 0)
            {
                key = key[(prefixIndex + ResourcePrefix.Length)..];
            }

            if (key.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                key = key[..^ResourceSuffix.Length];
            }

            return key;
        }
    }

    /// <summary>
    /// Produces styled highlight spans for document lines.
    /// </summary>
    internal sealed class SyntaxHighlighter
    {
        private readonly SyntaxDefinition? _syntax;
        private readonly Dictionary<int, (string Text, List<HighlightSpan> Spans)> _singleLineCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxHighlighter"/> class.
        /// </summary>
        /// <param name="syntax">The syntax definition to apply.</param>
        public SyntaxHighlighter(SyntaxDefinition? syntax)
        {
            _syntax = syntax;
        }

        /// <summary>
        /// Gets the line comment prefix for the active syntax (e.g., <c>//</c>, <c>#</c>, or <c>&lt;!--</c>).
        /// </summary>
        public string? CommentPrefix => _syntax?.CommentPrefix;

        /// <summary>
        /// Gets the block comment close token (e.g., <c>--&gt;</c>); when set, <see cref="CommentPrefix"/> is the open token.
        /// </summary>
        public string? CommentSuffix => _syntax?.CommentSuffix;

        /// <summary>
        /// Highlights a single document line.
        /// </summary>
        /// <param name="document">The document to highlight.</param>
        /// <param name="lineIndex">The zero-based line index.</param>
        /// <returns>
        /// The highlight spans for the requested line.
        /// </returns>
        public IReadOnlyList<HighlightSpan> Highlight(DocumentBuffer document, int lineIndex)
        {
            Dictionary<int, List<HighlightSpan>> range = HighlightRange(document, lineIndex, 1);
            return range.TryGetValue(lineIndex, out List<HighlightSpan>? spans) ? spans : [];
        }

        /// <summary>
        /// Highlights a range of document lines.
        /// </summary>
        /// <param name="document">The document to highlight.</param>
        /// <param name="firstLine">The first zero-based line index.</param>
        /// <param name="lineCount">The number of lines to highlight.</param>
        /// <returns>
        /// A map of line indexes to highlight spans.
        /// </returns>
        public Dictionary<int, List<HighlightSpan>> HighlightRange(DocumentBuffer document, int firstLine, int lineCount)
        {
            var spansByLine = new Dictionary<int, List<HighlightSpan>>();
            if (_syntax is null || lineCount <= 0 || firstLine >= document.LineCount)
            {
                return spansByLine;
            }

            int startLine = Math.Max(0, firstLine);
            int endLine = Math.Min(document.LineCount - 1, firstLine + lineCount - 1);

            for (int lineIndex = startLine; lineIndex <= endLine; lineIndex++)
            {
                string text = document.LineAt(lineIndex).ToString();
                if (text.Length == 0)
                {
                    _singleLineCache.Remove(lineIndex);
                    continue;
                }

                if (_singleLineCache.TryGetValue(lineIndex, out var cached) && cached.Text == text)
                {
                    if (cached.Spans.Count > 0)
                    {
                        // Copy the cached list so multiline spans added below do not mutate the cache.
                        spansByLine[lineIndex] = new List<HighlightSpan>(cached.Spans);
                    }
                    continue;
                }

                List<HighlightSpan> spans = GetList(spansByLine, lineIndex);

                for (int ruleIndex = 0; ruleIndex < _syntax.Rules.Count; ruleIndex++)
                {
                    SyntaxRule rule = _syntax.Rules[ruleIndex];
                    if (rule.IsMultiline)
                    {
                        continue;
                    }

                    foreach (Regex pattern in rule.Patterns)
                    {
                        AddMatches(text, pattern, rule.Style, ruleIndex, spans);
                    }
                }

                // Store a snapshot so multiline passes that mutate spansByLine[lineIndex]
                // cannot corrupt the cached single-line spans.
                _singleLineCache[lineIndex] = (text, new List<HighlightSpan>(spans));
            }

            for (int ruleIndex = 0; ruleIndex < _syntax.Rules.Count; ruleIndex++)
            {
                SyntaxRule rule = _syntax.Rules[ruleIndex];
                if (rule.IsMultiline)
                {
                    AddMultilineSpans(document, startLine, endLine, rule, ruleIndex, spansByLine);
                }
            }

            // Sort each line's spans by rule declaration order so later-declared rules paint last
            // and win over earlier ones — matching GNU nano's "last matching rule wins" semantics.
            foreach (List<HighlightSpan> lineSpans in spansByLine.Values)
            {
                lineSpans.Sort(static (a, b) =>
                    a.Priority != b.Priority
                        ? a.Priority.CompareTo(b.Priority)
                        : a.Start.CompareTo(b.Start));
            }

            return spansByLine;
        }

        private static void AddMatches(string text, Regex pattern, ConsoleStyle style, int priority, List<HighlightSpan> spans)
        {
            try
            {
                foreach (Match match in pattern.Matches(text))
                {
                    if (match.Success && match.Length > 0)
                    {
                        spans.Add(new HighlightSpan(match.Index, match.Length, style, priority));
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
            int priority,
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
                            spans.Add(new HighlightSpan(start.Index, Math.Max(0, end - start.Index), rule.Style, priority));
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
                            spans.Add(new HighlightSpan(0, spanEnd, rule.Style, priority));
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

    /// <summary>
    /// Parses nano syntax highlighting definitions into editor syntax definitions.
    /// </summary>
    internal static class NanorcParser
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(25);

        /// <summary>
        /// Parses nanorc syntax text.
        /// </summary>
        /// <param name="text">The nanorc file content.</param>
        /// <returns>
        /// The parsed syntax definition, or <see langword="null" /> when no syntax declaration is present.
        /// </returns>
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
                else if (definition is not null && line.StartsWith("comment ", StringComparison.Ordinal))
                {
                    List<string> tokens = ParseValues(line["comment ".Length..]);
                    if (tokens.Count > 0)
                    {
                        int pipeIndex = tokens[0].IndexOf('|');
                        if (pipeIndex >= 0)
                        {
                            definition.CommentPrefix = tokens[0][..pipeIndex];
                            definition.CommentSuffix = tokens[0][(pipeIndex + 1)..];
                        }
                        else
                        {
                            definition.CommentPrefix = tokens[0];
                        }
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
