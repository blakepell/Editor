# Repository Guidelines

## Project Structure & Module Organization

ApexGate Editor is a single-project C# terminal editor. The solution entry is `Editor.slnx`; the buildable project is `src/Editor.csproj`, which targets `.NET 10` and emits the executable assembly name `ae`. Core source files live directly in `src/`: `Program.cs` starts the app, `EditorApp.cs` wires the runtime, `EditorLoop.cs` handles input, `EditorSession.cs` owns editor state, `DocumentBuffer.cs` and `LineBuffer.cs` model text, and `Renderer.cs` draws the terminal UI. Syntax highlighting resources are `.nanorc` files under `src/Syntax/` and are embedded automatically. Generated `bin/` and `obj/` directories should stay untracked.

## Build, Test, and Development Commands

- `dotnet build src/Editor.csproj` builds the editor.
- `dotnet run --project src/Editor.csproj -- path/to/file.txt` runs the editor against an optional file.
- `dotnet publish src/Editor.csproj` produces the AOT publish output configured by the project.
- `dotnet build Editor.slnx` builds through the solution file.

There is currently no automated test project in this repository.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Follow the existing style: file-scoped project header comments, block-scoped `namespace Editor`, XML documentation on types and important members, four-space indentation, PascalCase for types/methods/properties, camelCase for locals and parameters, and `_camelCase` for private fields. Prefer `internal` types unless an API must be public; `IConsoleDriver` is the primary abstraction boundary. Keep terminal rendering routed through `Renderer` and frame buffering APIs.

## Testing Guidelines

Because no test suite exists yet, validate changes with `dotnet build src/Editor.csproj` and a manual run using representative files. For editing behavior, check file loading, cursor movement, save behavior, undo/redo, selection, and syntax highlighting. If adding tests later, create a dedicated test project outside generated folders, name test files after the target type, and favor focused unit tests for `DocumentBuffer`, `LineBuffer`, `UndoStack`, and syntax parsing.

## Commit & Pull Request Guidelines

Recent commits use short, title-style subjects such as `Variables`, `Formatting`, and `XML comments`. Keep commit messages concise and imperative or noun-based, with extra detail in the body when behavior changes. Pull requests should describe the user-visible change, list build/manual validation performed, link related issues when available, and include terminal screenshots or short recordings for rendering/UI changes.

## Agent-Specific Instructions

Check `.github/copilot-instructions.md` before larger edits; it documents architecture, undo grouping, safe saves, layout rules, and syntax highlighting conventions. Do not hard-code terminal row positions; derive layout through `EditorLayout`.
