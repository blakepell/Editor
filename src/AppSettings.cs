using NEdit.Editor;
using System.Text.Json.Serialization;

namespace NEdit
{
    /// <summary>
    /// Application settings and configuration for nEdit.
    /// </summary>
    public class AppSettings
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
        /// Editor options that are persisted across sessions. Loaded from
        /// <c>~/.apexgate/nedit.json</c> on startup and saved on exit.
        /// </summary>
        public EditorOptions Options { get; set; } = new();
    }

    /// <summary>
    /// Source-generated JSON serializer context for <see cref="AppSettings"/>.
    /// Required for AOT-safe serialization when <c>PublishAot = true</c>.
    /// </summary>
    [JsonSerializable(typeof(AppSettings))]
    internal partial class AppSettingsJsonContext : JsonSerializerContext
    {

    }
}