# Hermes.C# - WinUI 3 Desktop AI Agent

A clean-room C# implementation of an AI coding agent with WinUI 3 desktop interface.

## Features

### 9 Core Pillars
- **Persistent Memory** - YAML frontmatter, LLM relevance scoring, freshness warnings
- **Dream System** - 10-min auto-consolidation, 4-phase prompt engineering
- **Agent Teams** - Worktree isolation, inter-agent messaging, team orchestration
- **Coordinator Mode** - Multi-worker orchestration, mode matching
- **Transcript-First** - JSONL persistence, write-before-execute, crash-proof resume
- **Task V2** - Dependencies, cron scheduling, priority management
- **Buddy System** - Mulberry32 PRNG, 4 rarities, AI soul, ASCII renderer
- **Skills System** - Markdown+YAML skills, built-in skill library
- **Granular Permissions** - Rule DSL, 5 permission modes

### Tools (6/19 Implemented)
- ✅ bash - Execute shell commands
- ✅ read_file - Read file contents
- ✅ write_file - Create/overwrite files
- ✅ edit_file - Precise text replacement
- ✅ glob - Pattern-based file search
- ✅ grep - Content search with regex

## Build

```bash
# Build core library
cd Hermes.CS\src
dotnet build Hermes.Core.csproj

# Build desktop app
cd Hermes.CS\Desktop\HermesDesktop
dotnet build
```

## Run

```bash
cd Hermes.CS\Desktop\HermesDesktop
dotnet run
```

## Configuration

Create `%LOCALAPPDATA%\hermes\config.yaml`:

```yaml
llm:
  provider: openai
  model: qwen3.5
  baseUrl: http://localhost:11434/v1
  apiKey: ""
```

## Project Structure

```
Hermes.CS/
├── src/                    # Core library
│   ├── agents/            # Agent teams, coordination
│   ├── buddy/             # Buddy system
│   ├── coordinator/       # Multi-agent orchestration
│   ├── Core/              # Base models, interfaces
│   ├── dream/             # Auto-consolidation
│   ├── LLM/               # LLM clients
│   ├── memory/            # Persistent memory
│   ├── permissions/       # Permission system
│   ├── skills/            # Skills system
│   ├── tasks/             # Task management
│   ├── Tools/             # Tool implementations
│   ├── transcript/        # Session persistence
│   └── Hermes.Core.csproj
├── Desktop/
│   └── HermesDesktop/     # WinUI 3 desktop app
│       ├── Models/
│       ├── Services/
│       ├── Views/
│       └── HermesDesktop.csproj
└── README.md
```

## Tech Stack

- **.NET 10** - Latest .NET runtime
- **WinUI 3** - Windows App SDK 1.8
- **C# 13** - Modern C# features
- **Clean-Room** - No copied code from Python or leaked sources

## License

MIT
