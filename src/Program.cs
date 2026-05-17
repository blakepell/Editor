/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using NEdit.Editor;
using NEdit.Memory;
using System.Text.Json;

namespace NEdit
{
    /// <summary>
    /// Entry point for the editor application.
    /// </summary>
    internal static class Program
    {
        private static readonly string SettingsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".apexgate");

        private static readonly string SettingsFile =
            Path.Combine(SettingsFolder, "nedit.json");

        /// <summary>
        /// Application entry point that runs the editor application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        /// <returns>Exit code indicating success or failure of the application.</returns>
        static int Main(string[] args)
        {
            LoadAppSettings();
            try
            {
                return EditorApp.Run(args);
            }
            finally
            {
                SaveAppSettings();
            }
        }

        /// <summary>
        /// Loads the AppSettings from <c>~/.apexgate/nedit.json</c>.
        /// Creates the folder and a default settings file if either does not exist.
        /// Registers the result as a singleton in <see cref="AppServices"/>.
        /// </summary>
        public static void LoadAppSettings()
        {
            Directory.CreateDirectory(SettingsFolder);

            AppSettings settings;

            if (!File.Exists(SettingsFile))
            {
                settings = new AppSettings();
                string defaultJson = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
                File.WriteAllText(SettingsFile, defaultJson);
            }
            else
            {
                string json = File.ReadAllText(SettingsFile);
                settings = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings)
                           ?? new AppSettings();
            }

            AppServices.AddSingleton(settings);
        }

        /// <summary>
        /// Saves the current <see cref="AppSettings"/> singleton back to
        /// <c>~/.apexgate/nedit.json</c>.
        /// </summary>
        public static void SaveAppSettings()
        {
            var settings = AppServices.GetService<AppSettings>();
            if (settings is null)
            {
                return;
            }

            Directory.CreateDirectory(SettingsFolder);
            string json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
            File.WriteAllText(SettingsFile, json);
        }
    }
}