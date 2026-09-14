# oniDash Documentation

This directory intentionally contains a small set of canonical documents. Historical milestone reports, duplicate specifications and temporary audits are not the source of truth and should not be recreated.

## Canonical documents

| Document | Purpose |
|---|---|
| `PRODUCT-SPEC.md` | Overall product contract and platform principles |
| `ARCHITECTURE-SPEC.md` | Platform and module architecture |
| `UI-SPEC.md` | Shared visual and interaction system |
| `MUSIC-SPEC.md` | Complete Music feature/product contract |
| `MUSIC-ARCHITECTURE.md` | Music technical architecture and boundaries |
| `MUSIC-UX.md` | Music information architecture and workflows |
| `TASKLIST.md` | Ordered executable implementation backlog |
| `ROADMAP.md` | Milestones and acceptance gates |
| `DECISIONS.md` | Architecture decisions |

## Agent entry point
`AGENTS.md` at repository root is the single agent-operating-instructions file. Do not create competing `agent.md` or `AGENT-PROMPT.md` files.

## Documentation rule
When behavior or architecture changes, update the relevant canonical document. Do not add a second document merely to describe a milestone or duplicate an existing contract. Historical implementation details belong in Git history/PRs, not in the active specification set.
