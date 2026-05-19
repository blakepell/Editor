/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using System.Text;

namespace NEdit.Editor
{
    /// <summary>
    /// Represents the foreground, background, and inverse state used for terminal output.
    /// </summary>
    /// <param name="Foreground">The foreground console color.</param>
    /// <param name="Background">The background console color.</param>
    /// <param name="Inverse"><see langword="true" /> to render with inverse video; otherwise, <see langword="false" />.</param>
    internal readonly record struct ConsoleStyle(ConsoleColor Foreground, ConsoleColor Background, bool Inverse = false)
    {
        /// <summary>
        /// Defines the default editor text style.
        /// </summary>
        public static readonly ConsoleStyle Normal = new(ConsoleColor.Gray, ConsoleColor.Black);

        /// <summary>
        /// Defines the title bar style.
        /// </summary>
        public static readonly ConsoleStyle Title = new(ConsoleColor.White, ConsoleColor.DarkBlue);

        /// <summary>
        /// Defines the status bar style.
        /// </summary>
        public static readonly ConsoleStyle Status = new(ConsoleColor.White, ConsoleColor.DarkBlue);

        /// <summary>
        /// Defines the shortcut key label style.
        /// </summary>
        public static readonly ConsoleStyle ShortcutKey = new(ConsoleColor.Black, ConsoleColor.Gray);

        /// <summary>
        /// Defines the shortcut description style.
        /// </summary>
        public static readonly ConsoleStyle ShortcutText = new(ConsoleColor.Gray, ConsoleColor.Black);

        /// <summary>
        /// Defines the line number margin style.
        /// </summary>
        public static readonly ConsoleStyle LineNumber = new(ConsoleColor.DarkGray, ConsoleColor.Black);

        /// <summary>
        /// Defines the selected text style.
        /// </summary>
        public static readonly ConsoleStyle Selection = new(ConsoleColor.Black, ConsoleColor.DarkCyan);
    }

    /// <summary>
    /// Defines terminal I/O operations used by the editor renderer and input loop.
    /// </summary>
    internal interface IConsoleDriver : IDisposable
    {
        /// <summary>
        /// Gets the current terminal size.
        /// </summary>
        /// <value>
        /// The terminal dimensions.
        /// </value>
        TerminalSize Size { get; }

        /// <summary>
        /// Reads the next key press from the terminal.
        /// </summary>
        /// <returns>
        /// The key press information.
        /// </returns>
        ConsoleKeyInfo ReadKey();

        /// <summary>
        /// Gets a value that indicates whether input is available.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if a key press is waiting; otherwise, <see langword="false" />.
        /// </value>
        bool KeyAvailable { get; }

        /// <summary>
        /// Enters the alternate-screen editor mode.
        /// </summary>
        void EnterEditorMode();

        /// <summary>
        /// Leaves the alternate-screen editor mode.
        /// </summary>
        void LeaveEditorMode();

        /// <summary>
        /// Begins a buffered render frame.
        /// </summary>
        void BeginFrame();

        /// <summary>
        /// Ends and flushes a buffered render frame.
        /// </summary>
        void EndFrame();

        /// <summary>
        /// Clears the terminal screen.
        /// </summary>
        void Clear();

        /// <summary>
        /// Writes styled text at the specified terminal position.
        /// </summary>
        /// <param name="row">The zero-based row.</param>
        /// <param name="column">The zero-based column.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="style">The style to apply.</param>
        void WriteAt(int row, int column, string text, ConsoleStyle style);

        /// <summary>
        /// Writes styled text at the specified terminal position.
        /// </summary>
        /// <param name="row">The zero-based row.</param>
        /// <param name="column">The zero-based column.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="style">The style to apply.</param>
        void WriteAt(int row, int column, ReadOnlySpan<char> text, ConsoleStyle style);

        /// <summary>
        /// Moves the terminal cursor to the specified position.
        /// </summary>
        /// <param name="row">The zero-based row.</param>
        /// <param name="column">The zero-based column.</param>
        void MoveCursor(int row, int column);

        /// <summary>
        /// Shows or hides the terminal cursor.
        /// </summary>
        /// <param name="visible"><see langword="true" /> to show the cursor; otherwise, <see langword="false" />.</param>
        void ShowCursor(bool visible);

        /// <summary>
        /// Selects a block cursor shape.
        /// </summary>
        void UseBlockCursor();
    }

    /// <summary>
    /// Implements <see cref="IConsoleDriver"/> with ANSI escape sequences.
    /// </summary>
    internal sealed class AnsiConsoleDriver : IConsoleDriver
    {
        private bool _entered;
        private StringBuilder? _frame;
        private readonly ConsoleColor _originalForeground = Console.ForegroundColor;
        private readonly ConsoleColor _originalBackground = Console.BackgroundColor;
        private readonly bool _originalCursorVisible = SafeGetCursorVisible();

        /// <inheritdoc/>
        public TerminalSize Size
        {
            get
            {
                int rows = Math.Max(1, Console.WindowHeight);
                int columns = Math.Max(1, Console.WindowWidth);
                return new TerminalSize(rows, columns);
            }
        }

        /// <inheritdoc/>
        public bool KeyAvailable => Console.KeyAvailable;

        /// <inheritdoc/>
        public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

        /// <inheritdoc/>
        public void EnterEditorMode()
        {
            if (_entered)
            {
                return;
            }

            _entered = true;
            Console.OutputEncoding = Encoding.UTF8;
            Console.TreatControlCAsInput = true;
            WriteRaw("\x1b[?1049h\x1b[?25l\x1b[2 q\x1b[2J");
        }

        /// <inheritdoc/>
        public void LeaveEditorMode()
        {
            if (!_entered)
            {
                return;
            }

            _entered = false;
            _frame = null;
            Console.ForegroundColor = _originalForeground;
            Console.BackgroundColor = _originalBackground;
            ShowCursor(_originalCursorVisible);
            WriteRaw("\x1b[0m\x1b[0 q\x1b[?1049l\x1b[2J\x1b[H");
            Console.TreatControlCAsInput = false;
        }

        /// <inheritdoc/>
        public void BeginFrame()
        {
            _frame ??= new StringBuilder(4096);
            _frame.Clear();
            _frame.Append("\x1b[?2026h"); // begin synchronized update — defer rendering until EndFrame
        }

        /// <inheritdoc/>
        public void EndFrame()
        {
            if (_frame is { Length: > 0 })
            {
                _frame.Append("\x1b[?2026l"); // end synchronized update — render now
                Console.Write(_frame.ToString());
            }

            _frame = null;
        }

        /// <inheritdoc/>
        public void Clear() => WriteRaw("\x1b[2J\x1b[H");

        /// <inheritdoc/>
        public void WriteAt(int row, int column, string text, ConsoleStyle style)
        {
            MoveCursor(row, column);
            WriteRaw(Sgr(style));
            WriteRaw(text);
            WriteRaw(Sgr(ConsoleStyle.Normal));
        }

        /// <inheritdoc/>
        public void WriteAt(int row, int column, ReadOnlySpan<char> text, ConsoleStyle style)
        {
            MoveCursor(row, column);
            WriteRaw(Sgr(style));
            WriteRaw(text);
            WriteRaw(Sgr(ConsoleStyle.Normal));
        }

        /// <inheritdoc/>
        public void MoveCursor(int row, int column)
        {
            int safeRow = Math.Max(0, row) + 1;
            int safeColumn = Math.Max(0, column) + 1;
            WriteRaw($"\x1b[{safeRow};{safeColumn}H");
        }

        /// <inheritdoc/>
        public void ShowCursor(bool visible)
        {
            if (_entered)
            {
                WriteRaw(visible ? "\x1b[?25h" : "\x1b[?25l");
                return;
            }

            try
            {
                Console.CursorVisible = visible;
            }
            catch
            {
                WriteRaw(visible ? "\x1b[?25h" : "\x1b[?25l");
            }
        }

        /// <inheritdoc/>
        public void UseBlockCursor() => WriteRaw("\x1b[2 q");

        /// <inheritdoc/>
        public void Dispose() => LeaveEditorMode();

        private static bool SafeGetCursorVisible()
        {
            if (!OperatingSystem.IsWindows())
            {
                return true;
            }

            try
            {
                return Console.CursorVisible;
            }
            catch
            {
                return true;
            }
        }

        private static string Sgr(ConsoleStyle style)
        {
            int fg = ColorCode(style.Foreground, background: false);
            int bg = ColorCode(style.Background, background: true);
            return style.Inverse ? $"\x1b[{fg};{bg};7m" : $"\x1b[{fg};{bg}m";
        }

        private static int ColorCode(ConsoleColor color, bool background)
        {
            int offset = background ? 40 : 30;
            return color switch
            {
                ConsoleColor.Black => offset + 0,
                ConsoleColor.DarkRed => offset + 1,
                ConsoleColor.DarkGreen => offset + 2,
                ConsoleColor.DarkYellow => offset + 3,
                ConsoleColor.DarkBlue => offset + 4,
                ConsoleColor.DarkMagenta => offset + 5,
                ConsoleColor.DarkCyan => offset + 6,
                ConsoleColor.Gray => offset + 7,
                ConsoleColor.DarkGray => offset + 60,
                ConsoleColor.Red => offset + 61,
                ConsoleColor.Green => offset + 62,
                ConsoleColor.Yellow => offset + 63,
                ConsoleColor.Blue => offset + 64,
                ConsoleColor.Magenta => offset + 65,
                ConsoleColor.Cyan => offset + 66,
                ConsoleColor.White => offset + 67,
                _ => offset + 7
            };
        }

        private void WriteRaw(string value)
        {
            if (_frame is not null)
            {
                _frame.Append(value);
            }
            else
            {
                Console.Write(value);
            }
        }

        private void WriteRaw(ReadOnlySpan<char> buf)
        {
            if (_frame is not null)
            {
                _frame.Append(buf);
            }
            else
            {
                Console.Write(buf);
            }
        }
    }
}
