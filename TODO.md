# TODO / Ideas

Possible TODO items.

## In Editor Script Engine

Implement Topaz JS as an in editor script engine.

## Ollama Integration

The ability to query ollama.

## The CLI Weaver (Live Pipes)

Allow users to embed live terminal commands directly into their text files. By typing something like :::tail -n 10 /var/log/syslog | grep Error:::, the editor creates an auto-updating block inline. If you are writing a post-mortem or debugging document, the log data pulls itself in automatically and freezes when you commit the file.

## Asynchronous LLM Refactoring Panes

Instead of blocking the main thread while an AI writes code, you highlight a function, press a shortcut, and type "optimize this and add type hints." The editor spins off a temporary split-pane where you can watch the AI rewrite the code in the background. You keep working in your main buffer, and simply hit Alt+Merge when the background task finishes.