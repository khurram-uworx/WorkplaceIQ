---
name: resolve-observation
description: "Walk through one OBSERVATIONS.md friction item, discuss and decide on a resolution, update OBSERVATIONS.md with the decision, and optionally create or update PROPOSALS.md and POTENTIAL-AI-FEATURES.md with cross-references. USE FOR: resolve friction, adopt observation, decide on observation item, close out observation, triage observation. DO NOT USE FOR: writing code changes, creating new features, general discussion."
---

# Resolve an OBSERVATIONS Item

Standard workflow for closing out an item in `docs/OBSERVATIONS.md` with a decision, linking it to `docs/PROPOSALS.md` and `docs/POTENTIAL-AI-FEATURES.md`.

## Workflow

```mermaid
flowchart TD
    A[Pick an OBSERVATIONS item] --> B[Read current state of OBSERVATIONS.md, PROPOSALS.md, POTENTIAL-AI-FEATURES.md]
    B --> C[Discuss with user: what's the right approach?]
    C --> D{Decision type?}
    D -->|Formal design needed| E[Write/update PROPOSALS.md entry]
    D -->|Simple resolution| F[Update OBSERVATIONS.md directly]
    E --> G[Update OBSERVATIONS.md: replace "What WorkplaceIQ Should Provide" with "Resolution" block linking to proposal]
    F --> H[Update OBSERVATIONS.md: add decision summary under the item]
    G --> I[Update POTENTIAL-AI-FEATURES.md: add prerequisite link to proposal where relevant]
    H --> I
    I --> J[Done]
```

## Detailed Steps

### 1. Read Current State

Read the relevant files to understand the current framing:

```powershell
# Read OBSERVATIONS item
type docs/OBSERVATIONS.md

# Read PROPOSALS for existing proposals
type docs/PROPOSALS.md

# Read POTENTIAL-AI-FEATURES for related extractions
type docs/POTENTIAL-AI-FEATURES.md
```

### 2. Discuss Decision with User

Present the observation, its frictions, and possible approaches. Ask:

- **Keep as-is?** The friction is acceptable for now.
- **Simple fix?** Update OBSERVATIONS.md directly with the decision.
- **Formal proposal?** Write a new proposal in PROPOSALS.md.

### 3. If Creating a Proposal

Add a new section to `docs/PROPOSALS.md` with this structure:

```markdown
## Proposal N: <Title>

**Addresses:** OBSERVATIONS §N (<title>)
**Supports:** POTENTIAL-AI-FEATURES <priority> (<abstraction>)

### Problem

### Proposed Solution

### API Surface (if applicable)

### Impact

#### What improves

#### What stays the same

#### What gets removed

### Migration Path

### Open Questions
```

### 4. Update OBSERVATIONS.md

Replace the "What WorkplaceIQ Should Provide" bullet list in the relevant section with:

```markdown
### Resolution

[Proposal N — <Title>](./PROPOSALS.md#proposal-n-title) for the decided approach:

- <decision point 1>
- <decision point 2>
- <etc>
```

If no formal proposal was needed, write the decision directly as a paragraph.

### 5. Update POTENTIAL-AI-FEATURES.md

Add a **Prerequisite** note at the top of any abstraction sections that depend on the proposal:

```markdown
**Prerequisite:** [Proposal N — <Title>](./PROPOSALS.md#proposal-n-title) defines the <key design>. This section assumes that proposal is accepted.
```

Also update the API surface sections if the proposal changes the interface design.

### 6. Verify Cross-References

- [ ] OBSERVATIONS.md item links to the proposal
- [ ] PROPOSALS.md entry links back to OBSERVATIONS and POTENTIAL-AI-FEATURES
- [ ] POTENTIAL-AI-FEATURES.md relevant sections link to the proposal
