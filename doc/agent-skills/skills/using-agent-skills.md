# Using Agent Skills

Use this guide when choosing which skill applies to a task.

## Selection Flow

```text
Task arrives
  -> unclear goal?            interview-me
  -> need variants?           idea-refine
  -> new feature/spec?        spec-driven-development
  -> need tasks?              planning-and-task-breakdown
  -> implementing code?       incremental-implementation
  -> writing tests?           test-driven-development
  -> code review?             code-review-and-quality
  -> debugging?               debugging-and-error-recovery
  -> shipping?                shipping-and-launch
```

## Operating Rules

- Surface assumptions before non-trivial work
- Stop when requirements conflict instead of guessing
- Verify with build, tests, or runtime evidence
- Keep scope narrow unless the user asks for more

