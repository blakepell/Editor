using NEdit.Collections;
using NEdit.Editor;
using System.Text.Json.Serialization;

namespace NEdit
{
    /// <summary>
    /// Provides application settings and configuration for nEdit.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Defines the display name for the application.
        /// </summary>
        public const string AppName = "nEdit";

        /// <summary>
        /// Defines the product version displayed by the editor.
        /// </summary>
        public const string Version = "0.1";

        /// <summary>
        /// Gets the date-based build version read from the assembly at startup.
        /// </summary>
        /// <value>
        /// The assembly version set by the publish script, or an empty value for unpublished development builds.
        /// </value>
        public static readonly string BuildVersion =
            typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty;

        /// <summary>
        /// Gets or sets the editor options that are persisted across sessions.
        /// </summary>
        /// <value>
        /// The options loaded from <c>~/.apexgate/nedit.json</c> on startup and saved on exit.
        /// </value>
        public EditorOptions Options { get; set; } = new();

        /// <summary>
        /// Gets or sets a buffer of recently used files.
        /// </summary>
        public CircularObservableCollection<string> RecentFiles { get; set; } = new(20);
    }

    /// <summary>
    /// Provides source-generated JSON serialization metadata for <see cref="AppSettings"/>.
    /// </summary>
    /// <remarks>
    /// This context keeps settings serialization compatible with AOT publishing.
    /// </remarks>
    [JsonSerializable(typeof(AppSettings))]
    internal partial class AppSettingsJsonContext : JsonSerializerContext
    {

    }
}
