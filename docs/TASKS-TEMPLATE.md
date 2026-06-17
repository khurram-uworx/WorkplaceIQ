# Task Breakdown Template

## Purpose

This document breaks a reviewed feature area, bug cluster, or roadmap slice into concrete, assignable tasks for coding agents.

Use this template when the repo needs execution-ready work items rather than prose analysis.

## How To Use

- Replace the title with the feature or workstream name.
- Keep tasks small enough that one coding agent can own them end-to-end.
- Separate decision-gate tasks from implementation tasks.
- Put documentation tasks after API-shape decisions unless the docs work is independent.
- Include acceptance criteria so agent handoffs are testable and reviewable.

## Suggested Execution Order

1. Task 1: [highest-priority decision or prerequisite]
2. Task 2: [next dependency or API-shape task]
3. Task 3: [test hardening or core implementation]
4. Task 4+: [follow-on work that can run in parallel]

## Coordination Notes

- Call out which tasks are decision gates.
- Call out which tasks can run in parallel safely.
- Identify tasks that should not begin until docs, API, or behavior decisions are settled.
- Note any shared files that may create merge conflicts.

## Task 1: [Task Name]

### Priority

[High | Medium | Low]

### Goal

[One concise statement of the desired outcome.]

### Why this exists

[Explain the gap, risk, or motivation.]

### Decision required

[Optional. Include only if this task requires an explicit product or API choice.]

### Scope

- [specific work item]
- [specific work item]
- [specific work item]

### Constraints

- [optional constraint]
- [optional constraint]

### Suggested implementation path

- [optional step or design direction]
- [optional step or design direction]

### Acceptance criteria

- [observable outcome]
- [observable outcome]
- [observable outcome]

### Files likely involved

- `[path/to/file]`
- `[path/to/file]`
- `[path/to/file]`

## Task 2: [Task Name]

### Priority

[High | Medium | Low]

### Goal

[One concise statement of the desired outcome.]

### Scope

- [specific work item]
- [specific work item]

### Acceptance criteria

- [observable outcome]
- [observable outcome]

### Files likely involved

- `[path/to/file]`
- `[path/to/file]`

## Task 3: [Task Name]

### Priority

[High | Medium | Low]

### Goal

[One concise statement of the desired outcome.]

### Scope

- [specific work item]
- [specific work item]

### Acceptance criteria

- [observable outcome]
- [observable outcome]

### Files likely involved

- `[path/to/file]`
- `[path/to/file]`

## Additional Tasks

Repeat the same task structure for as many tasks as needed.

Recommended pattern:

- keep one task per clear outcome
- separate behavior changes from docs-only cleanup where practical
- separate design-only investigation from implementation work

## Suggested Agent Handout Batches

### Batch A: decision-critical

- Task [n]
- Task [n]

### Batch B: implementation

- Task [n]
- Task [n]

### Batch C: tests and docs

- Task [n]
- Task [n]

## Final Checklist

- every task has a clear owner-sized scope
- every task has acceptance criteria
- decision-gate tasks are clearly marked
- likely files are listed to reduce agent search time
- execution order reflects real dependencies
