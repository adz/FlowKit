# FsFlow Documentation Guide

Source of truth for Docusaurus maintenance and documentation style.

## Audience and voice

Write for pragmatic F# devs solving dependency, async, and typed-failure problems.

- Skip category theory.
- Explain trade-offs vs `Async<Result<_,_>>` or FsToolkit.
- Prefer concise, code-first examples.
- Use direct, instructive language.
- Start with the user's problem, not the abstraction.
- Stay factual and avoid marketing, filler, or internal narrative.

## Docusaurus workflow

- Next docs live in `/docs`.
- Released docs live in `/site/versioned_docs/version-<number>/`.
- Keep Next and the relevant versioned snapshot in sync when a change applies to both.
- Sidebars live in `/site/sidebars.js` and `/site/versioned_sidebars/version-0.3.0-sidebars.json`.
- The site uses Docusaurus with a versioning plugin.

## Docs Source Of Truth

The docs system has two different kinds of pages:

- hand-written guides and landing pages live in `docs/`
- source-lifted API member pages live in `docs/reference/`

The API member pages are generated from the XML doc comments in `src/`. When you change public API wording, update the code comments first and then regenerate the reference pages.

The pipeline is:

1. Edit the public XML doc comments in `src/`.
2. Run `bash scripts/generate-api-docs.sh`.
3. Review the regenerated `docs/reference/` pages.
4. Update the hand-written hub pages and guides so they point at the new API shape.

Do not hand-edit the generated API member pages unless you are fixing a generated-doc bug. If the source comments change, the generated markdown should change with them.

### Generated content

- The "Runnable Examples" page is generated from real code in `/examples/`.
- Use `scripts/generate-example-docs.sh` to refresh it.
- Do not edit `docs/examples/README.md` or versioned equivalents directly.
- The API reference member pages under `docs/reference/fsflow/` are generated from the XML docs in `src/`.
- Update the generator in `scripts/generate-api-docs.mjs` when the reference structure changes, then rerun the script.
- The package-hub pages, reference index pages, and the rest of the guides are hand-written markdown.

### Preview and versioning

Run `bash scripts/preview-docs.sh` for a local live-reload server at `http://localhost:3000`.

For a new docs version:

1. Ensure `/docs` is current.
2. Run `cd site && npm run version 0.4.0`.
3. That creates a new snapshot in `versioned_docs`.

## Documentation rules

- Structure API pages around the package and module hierarchy.
- Use F# code blocks with syntax highlighting (` ```fsharp `).
- Include "Source-Lifted Notes" for implementation-derived insights.
- Start every page with a one-sentence summary that begins with "This page shows".
- Use small, credible examples before semantic deep-dives.
- Add an XML doc comment with an example to every public function.
- State explicitly when not to use a feature.
- Avoid FAQ-style rhetorical questions.
- Avoid justifying why a section exists.
- Avoid promises about future features as an excuse for current gaps.

## Done means

- The reader can build a working flow without opening the source code.
- The reader knows why they would choose this library over FsToolkit.
- The docs feel like a maintained product, not a design notebook.
- The change appears in Next and any relevant versioned snapshots.
