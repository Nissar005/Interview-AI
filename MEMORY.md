# Persistent Memory

The memory layer uses SQLite with an explicit table for stored facts, preferences, and project-specific notes.

## Flow

1. User text is normalized.
2. Memory instructions are detected in the request stream.
3. A record is stored in SQLite.
4. Relevant memory records are retrieved by query.
5. The AI request includes the relevant memory context.

## Explicit memory commands

Examples of supported commands:

- "Remember that I prefer concise answers."
- "Keep this in mind: I want AWS examples."
- "Forget that I prefer AWS examples."
