---
name: asher-documentation
description: Use when documenting Asher knowledge — architecture decisions, implementation discoveries, investigations, Linux/runtime research, Electron/JSONL changes, migration decisions, experiments/PoCs, or debugging findings. Decides whether content belongs in docs/, AGENTS.md, a skill, or the Obsidian Digital Garden.
---

# Asher Documentation

How Asher knowledge should be captured and where it belongs. This is **project-specific
documentation practice**, not Vault maintenance — for the Obsidian Vault use the global
`digital-garden` skill. The current project contract lives in `AGENTS.md`; this skill does not
restate it.

## Where information belongs

| Destination | Owns |
|-------------|------|
| `docs/` | Authoritative Asher technical reference (platform contracts, Electron architecture, Linux build/packaging) |
| `AGENTS.md` | Concise current project contract only |
| `.opencode/skills/` | Operational/specialized agent knowledge (`asher-jsonl-protocol`, `asher-electron`, `asher-testing`) |
| Digital Garden | Durable conceptual knowledge: discoveries, reasoning, relationships, learning |

Canonical-source principle: when information already has a home, **link or reference it** rather
than duplicating large blocks. The Garden is not a mirror of the repository, and a Vault note does
not replace repository documentation.

## Classify before writing

Tag the content, and never present a lower tier as established architecture:

- **Current** — matches the shipped implementation.
- **Historical** — a previous architecture kept for context.
- **Deferred** — an intended direction, not yet implemented (see `AGENTS.md`).
- **Experimental / PoC** — an investigation not yet part of supported architecture.
- **Hypothesis** — an explanation still requiring validation.

When a discovery changes project understanding, check whether existing notes/docs now contradict it
and update only the affected ones.

## Decision process

```text
Implementation detail only?          -> repository/code documentation
Reusable technical knowledge?        -> docs/ or a specialized skill
Important architectural decision?    -> document decision + rationale (docs/ or AGENTS.md)
Discovery / research worth keeping?  -> Digital Garden
Still uncertain?                     -> record as investigation/hypothesis, not fact
Already documented?                  -> update/link the canonical source, do not duplicate
```

Do not create a new note automatically — search first.

## Source hierarchy

When resolving conflicting information, prefer:

```text
current source code
  -> current repository documentation
  -> AGENTS.md
  -> Asher specialized skills
  -> Digital Garden notes
  -> historical / experimental material
```

Historical notes are not "wrong" merely for differing from the current implementation — they may
accurately record an earlier state. When uncertainty remains, report it rather than silently picking
an interpretation.

## Digital Garden workflow (when the Vault is the target)

1. The global `digital-garden` skill governs Vault maintenance.
2. Search for existing Asher notes before creating one.
3. Prefer updating the canonical concept note over adding a duplicate.
4. Preserve meaningful wikilinks and established terminology.
5. Relate Asher concepts where useful; make small targeted edits.
6. Do not copy whole repository documents into the Vault.
7. Never run destructive cleanup automatically.

Conceptual categories to reuse when the Vault does not already express them: Asher architecture,
runtime/modding, Linux investigation, Electron, JSONL, Harmony, development tooling, architectural
decisions, experiments/discoveries. These are concepts, not mandatory folders; do not assume a Vault
path or impose structure.

## Documentation drift

When code changes invalidate existing knowledge:

1. Identify the canonical technical documentation.
2. Identify possibly affected Garden notes.
3. Update only notes whose content is actually stale.
4. Preserve notes that document genuine history; mark superseded approaches explicitly.
5. Do not silently rewrite history to match the current architecture — make the history/current
   distinction clear.

## Skill interaction

- JSONL change: `asher-jsonl-protocol` + this skill (+ `digital-garden` if the Vault is the target).
- Electron architecture change: `asher-electron` + this skill (+ optionally `digital-garden`).
- Linux runtime discovery: this skill + `digital-garden`; consult `Asher.Linux/README.md` and
  `docs/Cross-Platform-Architecture.md`.

Do not duplicate the bodies of those skills.
