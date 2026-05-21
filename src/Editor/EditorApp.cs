/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using NEdit.Commands;
using NEdit.Memory;
using System.Text;

namespace NEdit.Editor
{
    /// <summary>
    /// Coordinates startup, option parsing, and lifetime for the terminal editor.
    /// </summary>
    internal static class EditorApp
    {
        /// <summary>
        /// Runs the editor application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>
        /// The process exit code.
        /// </returns>
        public static int Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            if (args.Any(a => a is "-h" or "--help"))
            {
                Console.WriteLine($"{AppSettings.AppName} {AppSettings.Version}");
                Console.WriteLine("Usage: ae [--readonly] [--linenumbers] [--softwrap] [+LINE[,COLUMN]] [FILE]");
                return 0;
            }

            if (args.Any(a => a is "-V" or "--version"))
            {
                Console.WriteLine($"{AppSettings.AppName} {AppSettings.Version}");
                return 0;
            }

            var appSettings = AppServices.GetRequiredService<AppSettings>();
            var options = appSettings.Options;
            string? fileName = null;
            int startLine = 1;
            int startColumn = 1;

            foreach (string arg in args)
            {
                if (arg is "--readonly" or "-v")
                {
                    options.ReadOnly = true;
                }
                else if (arg is "--linenumbers" or "-l")
                {
                    options.LineNumbers = true;
                }
                else if (arg is "--softwrap")
                {
                    options.SoftWrap = true;
                }
                else if (arg.StartsWith('+') && arg.Length > 1)
                {
                    ParseStartPosition(arg[1..], ref startLine, ref startColumn);
                }
                else if (!arg.StartsWith('-') && fileName is null)
                {
                    fileName = arg;
                }
            }

            using var console = new AnsiConsoleDriver();
            var session = new EditorSession(DocumentBuffer.Load(fileName, options), options, console);
            session.MoveTo(Math.Max(0, startLine - 1), Math.Max(0, startColumn - 1));

            var catalog = EditorCommandCatalog.CreateDefault();
            var editor = new EditorLoop(session, new Renderer(console, catalog), catalog);

            try
            {
                console.EnterEditorMode();
                editor.Run();
                return 0;
            }
            catch (Exception ex)
            {
                console.LeaveEditorMode();
                Console.Error.WriteLine(ex);
                return 1;
            }
            finally
            {
                console.LeaveEditorMode();
            }
        }

        private static void ParseStartPosition(string value, ref int line, ref int column)
        {
            string[] pieces = value.Split(',', ':');

            if (pieces.Length > 0 && int.TryParse(pieces[0], out int parsedLine) && parsedLine > 0)
            {
                line = parsedLine;
            }

            if (pieces.Length > 1 && int.TryParse(pieces[1], out int parsedColumn) && parsedColumn > 0)
            {
                column = parsedColumn;
            }
        }
    }
}
