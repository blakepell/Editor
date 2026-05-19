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
        /// Runs the editor application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>
        /// The process exit code.
        /// </returns>
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
        /// Loads application settings and registers them with <see cref="AppServices"/>.
        /// </summary>
        /// <remarks>
        /// Creates <c>~/.apexgate/nedit.json</c> with defaults when the settings file does not exist.
        /// </remarks>
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
        /// Saves the current <see cref="AppSettings"/> singleton to disk.
        /// </summary>
        /// <remarks>
        /// Writes settings to <c>~/.apexgate/nedit.json</c>.
        /// </remarks>
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
