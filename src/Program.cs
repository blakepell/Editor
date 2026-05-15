/*
 * @project  : ApexGate Editor
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace Editor
{
    /// <summary>
    /// Entry point for the editor application.
    /// </summary>
    internal static class Program
    {
        public const string AppName = "nEdit";

        public const string Version = "0.1";

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