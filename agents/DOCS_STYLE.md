# Documentation Style Guide

This guide establishes the voice and structure for FsFlow documentation.

## Audience

The audience is a **senior F# developer** who wants to solve real-world problems involving dependencies, async work, and typed failures.

- They do not need the category theory behind the library.
- They do need to know the trade-offs vs `Async<Result<_,_>>` or FsToolkit.
- They value conciseness and "just show me the code" examples.

## Tone and Voice

- **Direct and Instructive:** Use "do this" instead of "it might be good to do this."
- **User-Centric:** Start with the user's problem, not the library's abstraction.
- **Fact-Based:** Prefer technical trade-offs over marketing-speak.
- **No Conversational Filler:** Avoid phrases like "you might ask," "the real question is," or "it's worth noting."
- **No Internal Narrative:** Do not describe the design process or history unless it helps the user make a decision today.

## Structural Requirements

- **Page Headers:** Every page must start with a one-sentence "When you should read this page" summary.
- **Examples First:** Use small, credible examples before deep-diving into semantics.
- **API Docstrings:** Every public function must have an XML doc comment with an example.
- **Trade-offs:** Explicitly state when NOT to use a feature.

## Forbidden Patterns

- **Rhetorical Q&A:** Avoid FAQ-style headers that sound like a transcript.
- **Justifying Content:** Do not explain *why* a section exists; just provide the value.
- **Wait-and-See:** Do not promise future features as a way to excuse current gaps.

## Done Means

- The reader can build a working flow without opening the source code.
- The reader knows exactly why they would choose this library over FsToolkit.
- The documentation feels like a maintained product, not a design notebook.
