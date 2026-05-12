# FsFlow Tasks

This file is the active queue for `scripts/ralph-loop-tasks.sh`.
Keep completed work out of this file.
Keep settled design decisions in `dev-docs/decisions/`.
Keep live product and architecture direction in `dev-docs/PLAN.md`.

1. [ ] Decide the public defect model for `Cause.Die`.
   - If FsFlow should follow a ZIO-like model, add a discoverable `Flow.die` and make the runtime capture unexpected non-cancellation exceptions as `Cause.Die`.
   - If FsFlow should keep defects as thrown exceptions, remove `Cause.Die` from the public story and document that boundary explicitly.
2. [ ] Make the runtime behavior match the chosen defect model end to end.
   - Align `Flow.run`, `AsyncFlow.run`, and `TaskFlow.run` with the same policy.
   - Preserve `Cause.Fail` for expected domain errors and `Cause.Interrupt` for cancellation.
3. [ ] Update user-facing docs after the defect policy is settled.
   - Keep `docs/core-model/semantics.md` and `docs/start/getting-started.md` aligned with the final runtime behavior.
   - Update retry and interruption guidance so it explains which causes are retried, rethrown, or translated.
