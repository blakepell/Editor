# Copilot Instructions

## Project Overview

ApexGate Editor (`ae`) is a terminal-based text editor written in C# (.NET 10), inspired by GNU nano. It targets AOT compilation (`PublishAot = true`) with a single-project solution at `src/Editor.csproj`. The assembly output name is `ae`.

## Build & Run

```sh
dotnet build src/Editor.csproj
dotnet run --project src/Editor.csproj -- [FILE]
dotnet publish src/Editor.csproj   # produces AOT binary
```

There are no automated tests in the repository.

## Architecture

The editor is composed of these layers, wired together in `EditorApp.Run()`:

| Component | Role |
|---|---|
| `AnsiConsoleDriver` / `IConsoleDriver` | Raw terminal I/O; writes ANSI escape sequences into a `StringBuilder` frame buffer, flushed once per `EndFrame()` call to eliminate flicker |
| `DocumentBuffer` | The file model — a `List<LineBuffer>` (one per line). Handles load, save, encoding detection (UTF-8/BOM), newline style detection (Unix/Windows/Mac), and range insert/delete operations |
| `LineBuffer` | Wraps `StringBuilder` for mutable single-line editing |
| `EditorSession` | All mutable editor state: cursor, selection mark, viewport scroll (`ViewTop`/`ViewLeft`), clipboard, undo stack, syntax highlighter, and options |
| `EditorLoop` | Input dispatch — maps `ConsoleKeyInfo` → `EditorSession` method calls |
| `Renderer` | Draws title bar (row 0), editor body (rows 1..n-4), status bar (row n-3), and two shortcut rows (rows n-2, n-1) |
| `UndoStack` | Snapshot-based undo/redo capped at 200 entries; stores full `string[]` line arrays before and after each edit |
| `SyntaxHighlighter` / `SyntaxLibrary` | Parses embedded `.nanorc` files at startup (lazy `Lazy<SyntaxLibrary>`); applies regex rules per-line |

**Data flow per keypress:**
`EditorLoop.HandleKey()` → `EditorSession` mutates `DocumentBuffer` → `EditorSession.EnsureCursorVisible()` → `Renderer.Render()`

## Skills

A project skill is available to assist with common tasks:

- **`nanorc-create`** (`.github/skills/nanorc-create/`) — Guides creation of a new `.nanorc` syntax highlighting file. Invoke it when asked to add syntax highlighting for a new file type. It researches well-known formats autonomously and asks targeted questions only when details cannot be determined.

## Key Conventions

**All types are `internal`.** Nothing is public except the `IConsoleDriver` interface, which is the seam for swapping the console implementation.

**`ConsoleStyle` is the styling primitive** — a `readonly record struct(ConsoleColor Foreground, ConsoleColor Background)` with static named constants (`Normal`, `Title`, `Status`, `Selection`, `LineNumber`, etc.). Pass these to every `IConsoleDriver.WriteAt()` call.

**`Position` is a `readonly record struct(int Line, int Column)`** implementing `IComparable<Position>`. Use `DocumentBuffer.Order(a, b)` to normalize any two positions into `(start, end)` order.

**Undo grouping** — consecutive printable character insertions are batched into a single undo entry using `_typingBefore`. Any non-typing operation must call `EndTypingGroup()` before modifying the document. The `WithUndo(Action)` helper wraps atomic multi-step edits.

**Safe saves** — `DocumentBuffer.Save()` writes to a temp file (`.filename.PID.tmp` in the same directory) then copies/moves over the target to avoid partial writes.

**Syntax highlighting** — add a `.nanorc` file to `src/Syntax/`. It is embedded as a resource with the logical name `Nano.LocalSyntax.<filename>.nanorc` and picked up automatically by `SyntaxLibrary.LoadEmbedded()`. The `NanorcParser` translates nano-specific regex syntax (`\<`, `\>`, POSIX classes) to .NET regex equivalents with a 25 ms timeout per match to guard against catastrophic backtracking.

**Layout** — `EditorLayout.From(TerminalSize)` computes all row indices from the terminal height. Minimum terminal height is 5 rows. Always derive layout from `EditorSession.Layout`, never hard-code row numbers.

**Frame buffering** — always call `IConsoleDriver.BeginFrame()` before drawing and `EndFrame()` after. `Renderer.Render()` manages this; don't bypass it.

**Tab expansion** — tabs are expanded visually in `EditorSession.GetVisibleLine()` using `EditorOptions.TabSize` (default 4). The underlying `DocumentBuffer` stores raw tab characters.
