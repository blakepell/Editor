using System.Text;

namespace Nano;

internal readonly record struct ConsoleStyle(ConsoleColor Foreground, ConsoleColor Background, bool Inverse = false)
{
    public static readonly ConsoleStyle Normal = new(ConsoleColor.Gray, ConsoleColor.Black);
    public static readonly ConsoleStyle Title = new(ConsoleColor.Black, ConsoleColor.Gray);
    public static readonly ConsoleStyle Status = new(ConsoleColor.White, ConsoleColor.DarkBlue);
    public static readonly ConsoleStyle ShortcutKey = new(ConsoleColor.Black, ConsoleColor.Gray);
    public static readonly ConsoleStyle ShortcutText = new(ConsoleColor.Gray, ConsoleColor.Black);
    public static readonly ConsoleStyle LineNumber = new(ConsoleColor.DarkGray, ConsoleColor.Black);
    public static readonly ConsoleStyle Selection = new(ConsoleColor.Black, ConsoleColor.DarkCyan);
}

internal interface IConsoleDriver : IDisposable
{
    TerminalSize Size { get; }
    ConsoleKeyInfo ReadKey();
    bool KeyAvailable { get; }
    void EnterEditorMode();
    void LeaveEditorMode();
    void BeginFrame();
    void EndFrame();
    void Clear();
    void WriteAt(int row, int column, string text, ConsoleStyle style);
    void MoveCursor(int row, int column);
    void ShowCursor(bool visible);
    void UseBlockCursor();
}

internal sealed class AnsiConsoleDriver : IConsoleDriver
{
    private bool _entered;
    private StringBuilder? _frame;
    private readonly ConsoleColor _originalForeground = Console.ForegroundColor;
    private readonly ConsoleColor _originalBackground = Console.BackgroundColor;
    private readonly bool _originalCursorVisible = SafeGetCursorVisible();

    public TerminalSize Size
    {
        get
        {
            int rows = Math.Max(1, Console.WindowHeight);
            int columns = Math.Max(1, Console.WindowWidth);
            return new TerminalSize(rows, columns);
        }
    }

    public bool KeyAvailable => Console.KeyAvailable;

    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

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
        WriteRaw("\x1b[0m\x1b[0 q\x1b[?1049l");
        Console.TreatControlCAsInput = false;
    }

    public void BeginFrame()
    {
        _frame ??= new StringBuilder(4096);
        _frame.Clear();
    }

    public void EndFrame()
    {
        if (_frame is { Length: > 0 })
        {
            Console.Write(_frame.ToString());
        }

        _frame = null;
    }

    public void Clear() => WriteRaw("\x1b[2J");

    public void WriteAt(int row, int column, string text, ConsoleStyle style)
    {
        MoveCursor(row, column);
        WriteRaw(Sgr(style));
        WriteRaw(text);
        WriteRaw(Sgr(ConsoleStyle.Normal));
    }

    public void MoveCursor(int row, int column)
    {
        int safeRow = Math.Max(0, row) + 1;
        int safeColumn = Math.Max(0, column) + 1;
        WriteRaw($"\x1b[{safeRow};{safeColumn}H");
    }

    public void ShowCursor(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch
        {
            WriteRaw(visible ? "\x1b[?25h" : "\x1b[?25l");
        }
    }

    public void UseBlockCursor() => WriteRaw("\x1b[2 q");

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
}
