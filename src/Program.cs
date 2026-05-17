/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using NEdit.Editor;

namespace NEdit
{
    /// <summary>
    /// Entry point for the editor application.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Application entry point that runs the editor application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        /// <returns>Exit code indicating success or failure of the application.</returns>
        static int Main(string[] args)
        {
            return EditorApp.Run(args);
        }

        /// <summary>
        /// Loads the AppSettings from the users folder.
        /// </summary>
        public static void LoadAppSettings()
        {
            //appServices.AddSingleton(new AppSettings());
        }

        /// <summary>
        /// Saves the AppSettings to the users folder.
        /// </summary>
        public static void SaveAppSettings()
        {

        }
    }
}