# FsFlow Tasks

This file is the active queue for `scripts/ralph-loop-tasks.sh`.
Keep completed work out of this file.
Keep settled design decisions in `dev-docs/decisions/`.
Keep live product and architecture direction in `dev-docs/PLAN.md`.

The numbered items below are intentionally linear so the ralph loop can move through them one at a time.

1. [ ] Publish one reference page per public API surface in `FsFlow`.
   - Split `Check`, `Diagnostics`, `Validation`, `Result`, `Flow`, `AsyncFlow`, and the runtime helpers into their own pages.
   - Lift the page content from the XML doc comments so the rendered docs and IDE experience stay in sync.
   - Keep each page in the FsToolkit-style format: short summary, explaining example, member map, and source link.
   - Use versioned source links for release pages and `main` links for next/unreleased docs.

2. [ ] Publish one reference page per public API surface in the task surface of `FsFlow`.
   - Split `TaskFlow`, `ColdTask`, `TaskFlow.Runtime`, `TaskFlowSpec`, `Capability`, `Layer`, and the builder extensions into their own pages.
   - Lift the page content from the XML doc comments so the rendered docs and IDE experience stay in sync.
   - Keep the main package hub concise and route readers into the member pages.
   - Make sure task interop pages and runtime helper pages are reachable from the side menu.

3. [ ] Rework the docs navigation around the new API page structure.
   - Add side-menu entries for every public API page.
   - Keep package hubs as landing pages, not as the only place where API names are visible.
   - Make the navigation mirror the public surface, with task pages living alongside the core pages.

4. [ ] Rewrite the validation docs for the graph-based model.
   - Lead with `Check`, `Diagnostics`, `Validation`, and applicative `validate {}`.
   - Treat `Validate` as compatibility-only terminology in the narrative docs.
   - Document the path-aware diagnostics graph and the bridge from `Check` into typed errors.

5. [ ] Refresh the rest of the guides to match the current public surface.
   - Update `GETTING_STARTED`, `WHY_FSFLOW`, `SEMANTICS`, `TASK_ASYNC_INTEROP`, and the integration pages.
   - Remove stale `Validate`-as-primary language.
   - Keep the examples aligned with the current source-documented API pages.

6. [ ] Move settled docs and product-shape decisions out of `PLAN.md`.
   - Record the final rules in `dev-docs/decisions/`.
   - Keep `PLAN.md` focused on unresolved or genuinely live direction.

7. [ ] Verify versioned docs and next-branch links.
   - Keep `site/versioned_docs/version-0.3.0` aligned with the release snapshot.
   - Ensure unreleased docs point source links at `main` and released docs point at the matching version.
   - Check the docs site still reads coherently in both the current and versioned trees.

8. [ ] Fix documentation contrast issues in light and dark modes.
   - Address the low contrast of secondary text and sidebars in both themes.
   - Ensure the site remains readable and accessible across all color modes.
