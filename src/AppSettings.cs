namespace NEdit
{
    /// <summary>
    /// Application settings and configuration for nEdit.
    /// </summary>
    public partial class AppSettings
    {
        public const string AppName = "nEdit";

        public const string Version = "0.1";

        /// <summary>
        /// The date-based build version read from the assembly at startup (e.g. "2026.5.17.1").
        /// Set by the publish script; empty in development builds that have not been published.
        /// </summary>
        public static readonly string BuildVersion =
            typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty;

    }
}