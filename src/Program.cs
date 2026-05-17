/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit
{
    /// <summary>
    /// Entry point for the editor application.
    /// </summary>
    internal static class Program
    {
        public const string AppName = "nEdit";

        public const string Version = "0.1";

        /// <summary>
        /// The date-based build version read from the assembly at startup (e.g. "2026.5.17.1").
        /// Set by the publish script; empty in development builds that have not been published.
        /// </summary>
        public static readonly string BuildVersion =
            typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty;

        /// <summary>
        /// Application entry point that runs the editor application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        /// <returns>Exit code indicating success or failure of the application.</returns>
        static int Main(string[] args)
        {
            return EditorApp.Run(args);
        }
    }
}