---
name: nanorc-create
description: "Creates a new nanorc syntax highlighting file for a given file type and adds it to src/Syntax/. Use when asked to add syntax highlighting for a new file type to the editor. Researches well-known file types autonomously and asks targeted questions when details cannot be determined."
argument-hint: "File type name or extension (e.g., 'TOML', 'Dockerfile', '.tf', 'Terraform')"
user-invocable: true
---

# Skill: Create a New nanorc Syntax File

## Project Context

- Syntax files live in `src/Syntax/<name>.nanorc`.
- The `.csproj` already globs all `Syntax\*.nanorc` files as `EmbeddedResource` — no project file edit is needed.
- After writing the file, run `dotnet build src/Editor.csproj` and confirm it succeeds.
- At runtime, `SyntaxLibrary.LoadEmbedded()` lazily loads all embedded `.nanorc` resources and matches files by filename (not full path) against the regex patterns in the `syntax` directive.

---

## Supported nanorc Directives

**Only these directives are parsed by `NanorcParser`.** All others (`header`, `magic`, `comment`, `tabgives`, `formatter`, etc.) are silently ignored.

```
syntax "name" "file-regex-1" "file-regex-2" ...
color  colorname "pattern"
color  colorname start="pattern" end="pattern"
icolor colorname "pattern"          # case-insensitive variant
# comment lines
```

### Available Colors

Plain and bright variants of the 8 standard terminal colors:

| Base      | Bright         |
|-----------|----------------|
| `black`   | `brightblack`  |
| `red`     | `brightred`    |
| `green`   | `brightgreen`  |
| `yellow`  | `brightyellow` |
| `blue`    | `brightblue`   |
| `magenta` | `brightmagenta`|
| `cyan`    | `brightcyan`   |
| `white`   | `brightwhite`  |

Background color syntax: `color foreground,background "pattern"` (e.g., `color ,green "[[:space:]]+$"`).

### POSIX Class Translations

The parser translates these before compiling to .NET regex:

| nanorc           | .NET equivalent   |
|------------------|-------------------|
| `\<`             | `\b`              |
| `\>`             | `\b`              |
| `[[:space:]]`    | `[\s]`            |
| `[[:blank:]]`    | `[ \t]`           |
| `[[:digit:]]`    | `[0-9]`           |
| `[[:alpha:]]`    | `[A-Za-z]`        |
| `[[:alnum:]]`    | `[A-Za-z0-9]`     |
| `[[:upper:]]`    | `[A-Z]`           |
| `[[:lower:]]`    | `[a-z]`           |

**Regex timeout is 25 ms per match** — avoid patterns prone to catastrophic backtracking (e.g., deeply nested quantifiers).

### Quoted-String Pattern Trick

Pattern values are delimited by `"`. A literal `"` inside the pattern is safe as long as the character immediately after it is not whitespace (the parser closes the string at `" ` or `"\n`). Use this to write patterns that match quoted strings:

- Simple double-quoted string: `""[^"]*""` — extracts regex `"[^"]*"`
- With escape sequences: `""(\\.|[^"])*""` — extracts regex `"(\\.|[^"])*"`

---

## Established Color Conventions

Apply these conventions automatically — do not ask the user about colors.

| Token category                  | Color            |
|---------------------------------|------------------|
| Keywords / control flow         | `cyan`           |
| Declaration keywords (class, fn)| `brightgreen`    |
| Built-in types                  | `green`          |
| Strings (quoted)                | `yellow`         |
| Numbers                         | `blue`           |
| Line comments                   | `brightblack`    |
| Block comments                  | `brightblack`    |
| Operators / punctuation         | `red`            |
| Annotations / decorators        | `magenta`        |
| Special refs / interpolations   | `brightmagenta`  |
| Boolean / null literals         | `brightcyan`     |
| Attribute / property names      | `brightcyan`     |
| XML/HTML tag range (full span)  | `green` (multiline) |
| XML/HTML tag names              | `cyan`           |
| Known element names             | `brightgreen`    |
| Attribute values                | `yellow`         |
| File header / section titles    | `brightwhite`    |
| Trailing whitespace             | `,green` bg      |
| Mixed tab/space indentation     | `,red` bg        |

---

## Layering Rule

Rules are applied in declaration order. **Later rules paint over earlier ones** at the same position on screen. Design rule sets from broad to specific:

### For XML-like / structured block formats
```
1. color white "^.+$"               # base: all text white
2. color ... start="..." end="..."  # multiline spans (tag ranges, block comments)
3. color ... "broad-pattern"        # broad inline patterns
4. color ... "specific-pattern"     # specific overrides
5. color ... start="<!--" end="-->" # comments LAST — override everything
```

### For line-oriented formats (config files, .sln, scripts)
No base color needed. Use anchored or whole-line patterns. Comments last.

---

## Research Guidance

For **well-known file types**, research autonomously without asking the user:

1. Look up the official spec, language reference, or Wikipedia article.
2. Identify all token categories present in the format:
   - Keywords and reserved words
   - String literal delimiters (single, double, triple, backtick, heredoc)
   - Comment syntax (line comment prefix, block comment delimiters)
   - Number literal formats (integer, float, hex, binary, scientific)
   - Operators and punctuation
   - Special syntax (annotations, directives, interpolations, labels, etc.)
3. Cross-reference existing `.nanorc` files in `src/Syntax/` for similar formats as stylistic reference.
4. Map each token category to a color using the conventions table above.

For **obscure or custom file types**, ask the targeted questions below.

---

## Clarifying Questions

Ask **only** when the following cannot be determined from the request or research. Keep questions minimal — one at a time if possible.

| Missing info | Question to ask |
|---|---|
| File extension(s) unclear | "Which file extension(s) should trigger this syntax? (e.g., `.tf`, `.tfvars`)" |
| Display name ambiguous | "What name should appear for this syntax? (used in the `syntax` directive)" |
| Comment syntax unknown | "How are comments written — line comments (prefix like `//` or `#`) or block comments (delimiters like `/* ... */`)?" |
| String delimiters unclear | "Are string literals delimited by double quotes, single quotes, or something else?" |
| Format structure unclear | "Is this a line-oriented format (like INI or shell scripts) or does it have significant block structure (like XML or JSON)?" |

Do NOT ask about colors — apply conventions automatically.
Do NOT ask questions whose answers are obvious from research or prior context.

---

## Procedure

1. **Identify the file type** from the user's request (name, extension, or description).
2. **Research** — if well-known, look up token categories autonomously. If unknown or ambiguous, ask the targeted questions above (one at a time).
3. **Design the rule set** — map each token category to a color using the conventions table, and decide on rule ordering.
4. **Write the file** to `src/Syntax/<name>.nanorc`. Use lowercase, hyphenated filenames matching the syntax name (e.g., `terraform.nanorc`, `docker-compose.nanorc`).
5. **Build** — run `dotnet build src/Editor.csproj` and confirm zero errors.
6. **Report** — tell the user the file created, the extensions covered, and a brief summary of the highlighting rules.

---

## Quality Checks

Before finishing, verify:

- [ ] File is at `src/Syntax/<name>.nanorc` (not anywhere else)
- [ ] `syntax` directive: name is double-quoted, file patterns are valid regexes
- [ ] Multiline spans: both `start=` and `end=` are present
- [ ] No unsupported directives (`header`, `magic`, `comment`, `tabgives`, etc.)
- [ ] No regex patterns with obvious catastrophic backtracking risk
- [ ] `dotnet build src/Editor.csproj` passes with zero errors
- [ ] A trailing-whitespace rule is included: `color ,green "[[:space:]]+$"`
