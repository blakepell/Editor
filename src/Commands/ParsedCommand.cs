/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit.Commands
{
    /// <summary>
    /// Represents the result of parsing a command-line style string into its constituent tokens.
    /// </summary>
    internal sealed class ParsedCommand
    {
        private readonly IReadOnlyList<string> _positional;
        private readonly IReadOnlyDictionary<string, string?> _switches;

        internal ParsedCommand(IReadOnlyList<string> positional, IReadOnlyDictionary<string, string?> switches)
        {
            _positional = positional;
            _switches = switches;
        }

        /// <summary>
        /// Gets the ordered list of positional arguments parsed from the input.
        /// </summary>
        /// <value>
        /// Positional tokens in the order they appeared, with quotes stripped.
        /// </value>
        public IReadOnlyList<string> Positional => _positional;

        /// <summary>
        /// Gets the switch parameters parsed from the input.
        /// </summary>
        /// <value>
        /// A dictionary keyed by switch name (without leading dashes) mapped to the switch value,
        /// or <see langword="null" /> for flags that have no value.
        /// </value>
        public IReadOnlyDictionary<string, string?> Switches => _switches;

        /// <summary>
        /// Gets the positional argument at the specified index.
        /// </summary>
        /// <param name="index">The zero-based positional argument index.</param>
        /// <returns>
        /// The positional argument value, or <see langword="null" /> when the index is out of range.
        /// </returns>
        public string? GetPositional(int index) =>
            index >= 0 && index < _positional.Count ? _positional[index] : null;

        /// <summary>
        /// Determines whether the specified switch was present in the input.
        /// </summary>
        /// <param name="name">The switch name to look up, without leading dashes.</param>
        /// <returns>
        /// <see langword="true" /> if the switch was present; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasSwitch(string name) => _switches.ContainsKey(name);

        /// <summary>
        /// Gets the value associated with the specified switch.
        /// </summary>
        /// <param name="name">The switch name to look up, without leading dashes.</param>
        /// <returns>
        /// The switch value, or <see langword="null" /> when the switch was not present or has no value.
        /// </returns>
        public string? GetSwitch(string name) =>
            _switches.TryGetValue(name, out string? value) ? value : null;
    }
}
