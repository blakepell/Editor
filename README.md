# nEdit

nEdit is a terminal-based text editor written in C# for .NET. The
project builds an executable named `nedit` and focuses on a small, practical
editing experience for the console.

The editor is Nano- or Pico-like in spirit: it uses familiar terminal editor
conventions such as shortcut rows, status messages, and direct keyboard-driven
editing. It is not a GNU nano clone, and compatibility with nano behavior is not
the goal.

## Features

- Terminal UI with title, status, editor, and shortcut rows.
- File loading and saving with encoding and newline detection.
- Cursor movement, selection, cut/copy/paste, search, replace, undo, and redo.
- Optional line numbers.
- Syntax highlighting from embedded `.nanorc` syntax files.

## Syntax Highlighting

Syntax files live in `src/Syntax/` and use Nano-style `.nanorc` definitions.
They are embedded into the application at build time by `src/Editor.csproj`.

The parser supports useful Nano-style syntax declarations, but `.nanorc` support
is intended as a convenient syntax definition format, not as a promise of full
GNU nano compatibility.

## Build and Run

Build the editor:

```sh
dotnet build src/Editor.csproj
```

Run the editor:

```sh
dotnet run --project src/Editor.csproj -- path/to/file.txt
```

Publish the editor:

```sh
dotnet publish src/Editor.csproj
```

Publish the AOT native editor for `win-x64`:

```sh
dotnet publish -r win-x64 -c Release
```

## Native AOT Goal

The project is configured with `PublishAot` and aims to support native AOT
builds. This keeps startup fast and makes it possible to distribute a native
binary without requiring a full managed runtime installation.

## Repository Layout

- `Editor.slnx` - solution file.
- `src/Editor.csproj` - main project file.
- `src/*.cs` - editor source code.
- `src/Syntax/*.nanorc` - embedded syntax highlighting definitions.

## Status

This project is early and intentionally compact. There is no automated test
project yet; use `dotnet build` and manual terminal testing when changing editor
behavior.
