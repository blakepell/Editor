/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit.Commands
{
    /// <summary>
    /// Parses a command-line style string into positional arguments and switch parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tokens are separated by whitespace. A token wrapped in double quotes is treated as a
    /// single positional argument with the surrounding quotes stripped. Tokens that begin with
    /// <c>-</c> or <c>--</c> are treated as switch flags; a switch may be followed by a value
    /// token (any token that is not itself a switch).
    /// </para>
    /// <para>
    /// This class uses no reflection and is safe for NativeAOT compilation.
    /// </para>
    /// </remarks>
    internal static class CommandLineParser
    {
        /// <summary>
        /// Parses the supplied input into a <see cref="ParsedCommand"/>.
        /// </summary>
        /// <param name="input">The raw command-line string to parse.</param>
        /// <returns>
        /// A <see cref="ParsedCommand"/> containing the positional arguments and switches
        /// extracted from <paramref name="input"/>.
        /// </returns>
        public static ParsedCommand Parse(string input)
        {
            var positional = new List<string>();
            var switches = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            List<string> tokens = Tokenize(input);

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                if (IsSwitch(token, out string switchName))
                {
                    // Consume the next token as the switch value when it is not itself a switch.
                    if (i + 1 < tokens.Count && !IsSwitch(tokens[i + 1], out _))
                    {
                        switches[switchName] = tokens[i + 1];
                        i++;
                    }
                    else
                    {
                        switches[switchName] = null;
                    }
                }
                else
                {
                    positional.Add(token);
                }
            }

            return new ParsedCommand(positional, switches);
        }

        /// <summary>
        /// Splits a raw input string into tokens, respecting double-quoted groups.
        /// </summary>
        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();

            if (string.IsNullOrWhiteSpace(input))
            {
                return tokens;
            }

            int i = 0;
            while (i < input.Length)
            {
                // Skip whitespace between tokens.
                while (i < input.Length && char.IsWhiteSpace(input[i]))
                {
                    i++;
                }

                if (i >= input.Length)
                {
                    break;
                }

                if (input[i] == '"')
                {
                    // Quoted token: read until the closing quote or end of string.
                    i++; // skip opening quote
                    int start = i;
                    while (i < input.Length && input[i] != '"')
                    {
                        i++;
                    }

                    tokens.Add(input.Substring(start, i - start));

                    if (i < input.Length)
                    {
                        i++; // skip closing quote
                    }
                }
                else
                {
                    // Unquoted token: read until whitespace.
                    int start = i;
                    while (i < input.Length && !char.IsWhiteSpace(input[i]))
                    {
                        i++;
                    }

                    tokens.Add(input.Substring(start, i - start));
                }
            }

            return tokens;
        }

        /// <summary>
        /// Determines whether a token is a switch flag and extracts the switch name.
        /// </summary>
        /// <param name="token">The token to test.</param>
        /// <param name="name">
        /// When this method returns <see langword="true"/>, contains the switch name without
        /// leading dashes; otherwise, an empty string.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="token"/> begins with <c>-</c>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsSwitch(string token, out string name)
        {
            if (token.StartsWith("--", StringComparison.Ordinal) && token.Length > 2)
            {
                name = token.Substring(2);
                return true;
            }

            if (token.StartsWith('-') && token.Length > 1)
            {
                name = token.Substring(1);
                return true;
            }

            name = string.Empty;
            return false;
        }
    }
}
