---
name: command-create
description: "Creates a new Ctrl+T command palette entry for nEdit. Use when asked to add a new command to the editor's command palette. Asks targeted questions only when the command's behavior cannot be inferred from the request."
argument-hint: "Description of the command to add (e.g., 'sort lines alphabetically', 'insert current username', 'uppercase the selection')"
user-invocable: true
---

# Skill: Create a New nEdit Command Palette Command

## Project Context

- Commands are registered in `src/Commands/EditorCommandCatalog.cs`, inside the `CreateDefault()` method.
- Each command is a `new EditorCommand(...)` entry in the collection literal passed to the constructor.
- The constructor **auto-sorts all commands alphabetically by name** (OrdinalIgnoreCase) — no manual ordering is required.
- Private static handler methods live in the same class, below the `CreateDefault()` method.
- After writing the command, run `dotnet build src\NEdit.csproj` and confirm zero errors.

---

## `EditorCommand` Constructor

```csharp
new EditorCommand(
    name: string,                  // Display name in the palette (Title Case)
    description: string,           // One-sentence description shown below the name
    hotKey: string?,               // Keyboard shortcut label, e.g. "Ctrl+F", or null
    command: ICommand,             // RelayCommand<EditorCommandContext> with execute + canExecute lambdas
    alias: string?,                // Optional short alias the user can type (e.g. "cd"), or null
    argumentPrompt: string?,       // Prompt label when one argument is needed, or null
    useFilePicker: bool,           // true → open file browser, then return
    useNewDocument: bool,          // true → start new document, then return
    useSearch: bool,               // true → invoke the interactive Find prompt, then return
    useReplace: bool,              // true → invoke the interactive Replace prompt, then return
    useSave: bool,                 // true → invoke the Save workflow (prompts for name if needed), then return
    useExit: bool,                 // true → invoke the Exit workflow (prompts to save if modified), then return
    useGrep: bool,                 // true → open the grep search overlay, then return
    useRunFile: bool,              // true → run the current file via its registered runner, then return
    showInStatusBar: bool,         // true → appear in the bottom shortcut bar
    sortOrder: int,                // Position in the shortcut bar (lower = further left); default int.MaxValue
    shortLabel: string?)           // Short bar label (e.g. "Line #s"); falls back to Name when null
```

All parameters after `command` are optional named parameters defaulting to `null` / `false` / `int.MaxValue`.

### HotKey label format

The `hotKey` string is used in two places:
1. **Palette display** — shown right-aligned in gray as `[Ctrl+F]` next to the command name.
2. **Shortcut bar** — automatically converted to nano-style short form via `ToShortKey()`:
   - `"Ctrl+F"` → `"^F"`
   - `"Ctrl+Alt+S"` → `"^!S"`
   - `"F5"` → `"F5"`

Always use the long form (e.g. `"Ctrl+F"`) for `hotKey`; the renderer handles conversion for the bar.

### ShowInStatusBar and SortOrder

Set `showInStatusBar: true` and assign a `sortOrder` value when the command should appear in the bottom shortcut bar. The bar groups 8 commands per row. Existing SortOrder values are:

| SortOrder | Command |
|---|---|
| 10 | Commands (Ctrl+T) |
| 20 | New Document |
| 30 | Run File |
| 40 | Exit |
| 50 | Open File |
| 60 | Save File |
| 70 | Find Text |
| 80 | Replace Text |
| 90 | Cut |
| 100 | Paste |
| 110 | Copy |
| 120 | Undo |
| 130 | Redo |
| 140 | Grep |
| 150 | Toggle Line Numbers |

Use `shortLabel` when the command Name would be too long for the narrow bar slot (e.g. `shortLabel: "Line #s"` for "Toggle Line Numbers").

---

## Command Types

Identify which type fits the requested command, then apply the matching pattern.

### Type 1 — Direct (calls an existing `EditorSession` method, no extra input)

Use when the command simply calls one method on the session with no user input or text selection.

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    null,                          // or "Ctrl+X" if there is a hotkey
    new RelayCommand<EditorCommandContext>(
        context => context?.Session.TheMethod(),
        context => context?.Session.IsReadOnly == false)),  // use 'context is not null' for read-only-safe ops
```

**When `CanExecute` predicate to use:**
- Mutates the document → `context => context?.Session.IsReadOnly == false`
- Read-only safe (display, toggle, navigate) → `context => context is not null`

### Type 2 — Selection (reads or replaces the current selection)

Use when the command transforms or reads selected text.

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    null,
    new RelayCommand<EditorCommandContext>(
        MyStaticHandler,
        context => context is not null)),
```

Add a private static handler method in `EditorCommandCatalog`:

```csharp
private static void MyStaticHandler(EditorCommandContext? context)
{
    if (context is null)
    {
        return;
    }

    string? selectedText = context.GetSelectedText();
    if (selectedText is null)
    {
        context.Session.SetStatus("Select text first", alert: true);
        return;
    }

    // Transform selectedText...
    string result = /* ... */;
    context.ReplaceSelection(result, "Status message shown after replacement");
}
```

For operations that may fail (e.g., parsing), wrap the transform in a `try/catch` and call `context.Session.SetStatus(message, alert: true)` on error.

### Type 3 — Argument (needs one string from the user)

Use when the command needs a single value typed by the user (path, search term, number, etc.).

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    null,
    new RelayCommand<EditorCommandContext>(
        MyStaticHandler,
        context => context is not null),
    alias: "shortname",            // omit if no alias is needed
    argumentPrompt: "Label"),      // shown as "Label: " in the prompt
```

The handler reads the value from `context.Argument`:

```csharp
private static void MyStaticHandler(EditorCommandContext? context)
{
    if (context is null)
    {
        return;
    }

    string? input = context.Argument?.Trim();
    if (string.IsNullOrWhiteSpace(input))
    {
        context.Session.SetStatus("Usage hint here", alert: true);
        return;
    }

    // Use input...
}
```

### Type 4 — Special Workflow (delegates back to `EditorLoop`)

Use when the command needs an interactive multi-step prompt already implemented in `EditorLoop`.

| Flag | Workflow delegated to |
|---|---|
| `useSearch: true` | Interactive Find/Search prompt |
| `useReplace: true` | Interactive Find-and-Replace prompt (two prompts) |
| `useSave: true` | Save workflow (prompts for filename if the buffer is new) |
| `useFilePicker: true` | File browser overlay |
| `useNewDocument: true` | New document workflow (prompts to save if modified) |
| `useExit: true` | Exit workflow (prompts to save if modified) |
| `useGrep: true` | Grep search overlay |
| `useRunFile: true` | Run current file using its registered runner |

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    "Ctrl+X",
    new RelayCommand<EditorCommandContext>(
        _ => { },
        context => context is not null),   // or IsReadOnly == false for write ops
    useSearch: true),                       // replace with whichever flag applies
```

---

## Clarifying Questions

Ask **only** when the following cannot be determined from the user's request. One question at a time; stop as soon as you have enough to proceed.

| Unknown | Question to ask |
|---|---|
| Hotkey | "Does this command have a keyboard shortcut? If so, what is it? (e.g. `Ctrl+F`)" |
| Show in status bar? | "Should this command appear in the bottom shortcut bar? If so, what short label should it show (e.g. 'Find')?" |
| Mutates document? | "Does this command modify the document content, or is it read-only (navigate, display, toggle)?" |
| Needs selected text? | "Does this command operate on the currently selected text?" |
| Needs one user argument? | "Does this command need the user to type a single value (like a search term or path)?" |
| Maps to special workflow? | "Should this trigger the built-in save / search / replace / file-picker / new-document / exit / grep / run-file workflow?" |
| Handler logic unclear | "What should happen to the text — can you describe the transformation step by step?" |

Do **not** ask about names, descriptions, or colors. Do **not** ask questions whose answers are obvious from the request.

---

## Existing `EditorSession` Methods

These can be called directly in a Type 1 command without a separate handler:

| Method | Effect |
|---|---|
| `InsertGuid()` | Inserts a new GUID at the cursor |
| `InsertDate()` | Inserts the current local date |
| `InsertDateTime()` | Inserts the current local date and time |
| `InsertText(string)` | Inserts arbitrary text at the cursor |
| `TrimCurrentLine()` | Trims whitespace from the current line |
| `TrimAllLines()` | Trims all lines |
| `TrimAllLinesLeadingSpace()` | Trims leading whitespace from all lines |
| `TrimAllLinesTrailingSpace()` | Trims trailing whitespace from all lines |
| `RemoveEmptyLines()` | Removes blank lines |
| `ConvertTabsToSpaces()` | Expands tabs to spaces |
| `ToggleLineNumbers()` | Toggles line number display |
| `ShowCurrentDirectory()` | Shows cwd in the status bar |
| `Search(string)` | Runs a non-interactive search |
| `ReplaceAll(string, string)` | Replaces all occurrences |
| `Undo()` | Undoes the last edit |
| `Redo()` | Redoes the last undone edit |
| `Cut()` | Cuts selected text or current line |
| `Copy()` | Copies selected text or current line |
| `Paste()` | Pastes from clipboard |
| `SetStatus(string, bool alert)` | Sets the status bar message |
| `SetStatusSuccess(string)` | Sets a success status message |

For operations not listed here, check `EditorSession.cs` for additional methods before writing new logic.

---

## Procedure

1. **Identify** the command's name, description, and optional hotkey from the user's request.
2. **Classify** the command into one of the four types (Direct / Selection / Argument / Special Workflow).
3. **Ask targeted questions** only when the type or behavior cannot be determined.
4. **Write** the `EditorCommand` entry inside the `[…]` list in `CreateDefault()`. If the command is the new last entry in the list, remove the trailing comma from the previous last entry; otherwise, add a trailing comma to the new entry.
5. **Write the handler** (Types 2 and 3 only) as a `private static void` method in `EditorCommandCatalog`, following the null-check → early-return pattern.
6. **Build** — run `dotnet build src\NEdit.csproj` and confirm zero compiler errors.
7. **Report** — tell the user the command name added, its type, the hotkey (if any), whether it appears in the shortcut bar, and a one-line summary of what it does.

---

## Quality Checks

Before finishing, verify:

- [ ] Entry is inside the `[…]` list in `CreateDefault()` and is syntactically valid C#
- [ ] Trailing comma convention is correct (all entries except the last have a trailing comma)
- [ ] `CanExecute` predicate matches the command's read/write nature
- [ ] Handler method (if added) follows the null-check → early-return pattern
- [ ] Handler method (if added) shows a user-facing status message on both success and error paths
- [ ] `hotKey` uses the long form (e.g. `"Ctrl+F"`), not the short form (`"^F"`)
- [ ] If `showInStatusBar: true`, a `sortOrder` value is set that doesn't collide with existing values (see table above)
- [ ] If `showInStatusBar: true` and the Name is long, a `shortLabel` is provided
- [ ] `dotnet build src\NEdit.csproj` passes with zero errors


---

## Command Types

Identify which type fits the requested command, then apply the matching pattern.

### Type 1 — Direct (calls an existing `EditorSession` method, no extra input)

Use when the command simply calls one method on the session with no user input or text selection.

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    null,                          // or "Ctrl+X" if there is a hotkey
    new RelayCommand<EditorCommandContext>(
        context => context?.Session.TheMethod(),
        context => context?.Session.IsReadOnly == false)),  // use 'context is not null' for read-only-safe ops
```

**When `CanExecute` predicate to use:**
- Mutates the document → `context => context?.Session.IsReadOnly == false`
- Read-only safe (display, toggle, navigate) → `context => context is not null`

### Type 2 — Selection (reads or replaces the current selection)

Use when the command transforms or reads selected text.

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    null,
    new RelayCommand<EditorCommandContext>(
        MyStaticHandler,
        context => context is not null)),
```

Add a private static handler method in `EditorCommandCatalog`:

```csharp
private static void MyStaticHandler(EditorCommandContext? context)
{
    if (context is null)
    {
        return;
    }

    string? selectedText = context.GetSelectedText();
    if (selectedText is null)
    {
        context.Session.SetStatus("Select text first", alert: true);
        return;
    }

    // Transform selectedText...
    string result = /* ... */;
    context.ReplaceSelection(result, "Status message shown after replacement");
}
```

For operations that may fail (e.g., parsing), wrap the transform in a `try/catch` and call `context.Session.SetStatus(message, alert: true)` on error.

### Type 3 — Argument (needs one string from the user)

Use when the command needs a single value typed by the user (path, search term, number, etc.).

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    null,
    new RelayCommand<EditorCommandContext>(
        MyStaticHandler,
        context => context is not null),
    alias: "shortname",            // omit if no alias is needed
    argumentPrompt: "Label"),      // shown as "Label: " in the prompt
```

The handler reads the value from `context.Argument`:

```csharp
private static void MyStaticHandler(EditorCommandContext? context)
{
    if (context is null)
    {
        return;
    }

    string? input = context.Argument?.Trim();
    if (string.IsNullOrWhiteSpace(input))
    {
        context.Session.SetStatus("Usage hint here", alert: true);
        return;
    }

    // Use input...
}
```

### Type 4 — Special Workflow (delegates back to `EditorLoop`)

Use when the command needs an interactive multi-step prompt already implemented in `EditorLoop`.

| Flag | Workflow delegated to |
|---|---|
| `useSearch: true` | Interactive Find/Search prompt |
| `useReplace: true` | Interactive Find-and-Replace prompt (two prompts) |
| `useSave: true` | Save workflow (prompts for filename if the buffer is new) |
| `useFilePicker: true` | File browser overlay |
| `useNewDocument: true` | New document workflow (prompts to save if modified) |

```csharp
new EditorCommand(
    "Command Name",
    "One-sentence description.",
    "Ctrl+X",
    new RelayCommand<EditorCommandContext>(
        _ => { },
        context => context is not null),   // or IsReadOnly == false for write ops
    useSearch: true),                       // replace with whichever flag applies
```

---

## Clarifying Questions

Ask **only** when the following cannot be determined from the user's request. One question at a time; stop as soon as you have enough to proceed.

| Unknown | Question to ask |
|---|---|
| Hotkey | "Does this command have a keyboard shortcut? If so, what is it? (e.g. `Ctrl+F`)" |
| Mutates document? | "Does this command modify the document content, or is it read-only (navigate, display, toggle)?" |
| Needs selected text? | "Does this command operate on the currently selected text?" |
| Needs one user argument? | "Does this command need the user to type a single value (like a search term or path)?" |
| Maps to special workflow? | "Should this trigger the built-in save / search / replace / file-picker / new-document workflow?" |
| Handler logic unclear | "What should happen to the text — can you describe the transformation step by step?" |

Do **not** ask about names, descriptions, or colors. Do **not** ask questions whose answers are obvious from the request.

---

## Existing `EditorSession` Methods

These can be called directly in a Type 1 command without a separate handler:

| Method | Effect |
|---|---|
| `InsertGuid()` | Inserts a new GUID at the cursor |
| `InsertDate()` | Inserts the current local date |
| `InsertDateTime()` | Inserts the current local date and time |
| `InsertText(string)` | Inserts arbitrary text at the cursor |
| `TrimCurrentLine()` | Trims whitespace from the current line |
| `TrimAllLines()` | Trims all lines |
| `TrimAllLinesLeadingSpace()` | Trims leading whitespace from all lines |
| `TrimAllLinesTrailingSpace()` | Trims trailing whitespace from all lines |
| `RemoveEmptyLines()` | Removes blank lines |
| `ConvertTabsToSpaces()` | Expands tabs to spaces |
| `ToggleLineNumbers()` | Toggles line number display |
| `ShowCurrentDirectory()` | Shows cwd in the status bar |
| `Search(string)` | Runs a non-interactive search |
| `ReplaceAll(string, string)` | Replaces all occurrences |
| `SetStatus(string, bool alert)` | Sets the status bar message |
| `SetStatusSuccess(string)` | Sets a success status message |

For operations not listed here, check `EditorSession.cs` for additional methods before writing new logic.

---

## Procedure

1. **Identify** the command's name, description, and optional hotkey from the user's request.
2. **Classify** the command into one of the four types (Direct / Selection / Argument / Special Workflow).
3. **Ask targeted questions** only when the type or behavior cannot be determined.
4. **Write** the `EditorCommand` entry inside the `[…]` list in `CreateDefault()`. If the command is the new last entry in the list, remove the trailing comma from the previous last entry; otherwise, add a trailing comma to the new entry.
5. **Write the handler** (Types 2 and 3 only) as a `private static void` method in `EditorCommandCatalog`, following the null-check → early-return pattern.
6. **Build** — run `dotnet build src\NEdit.csproj` and confirm zero compiler errors.
7. **Report** — tell the user the command name added, its type, the hotkey (if any), and a one-line summary of what it does.

---

## Quality Checks

Before finishing, verify:

- [ ] Entry is inside the `[…]` list in `CreateDefault()` and is syntactically valid C#
- [ ] Trailing comma convention is correct (all entries except the last have a trailing comma)
- [ ] `CanExecute` predicate matches the command's read/write nature
- [ ] Handler method (if added) follows the null-check → early-return pattern
- [ ] Handler method (if added) shows a user-facing status message on both success and error paths
- [ ] `dotnet build src\NEdit.csproj` passes with zero errors
