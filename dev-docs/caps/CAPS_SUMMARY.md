# FsFlow Capability Approaches: Comparative Summary

This document compares the candidate capability approaches explored for FsFlow.

For the definitive implementation-oriented recommendation, see `CAPS_PLAN.md`.

## Rating scale

- `5` is best for ergonomics and IDE clarity.
- `5` is highest for setup burden and user-code burden.
- `Yes` / `Partial` / `No` indicates whether the approach naturally supports fine-grained capabilities.

## Summary table

| Approach | File | Ergonomics | Setup burden | User-code burden | Fine-grained | Compile-time safety | Host / DI interop | IDE clarity | Verdict |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Boilerplate records + slices | `CAPS-BOILERPLATE.md` | 2/5 | 5/5 | 4/5 | Yes | Compile-time | Medium | Good | Safe baseline, but too much wiring |
| `IServiceProvider` edge | `CAPS-ISERVICEPROVIDER.md` | 5/5 | 1/5 | 1/5 | No | Runtime only | Excellent | Excellent | Best edge ergonomics, weakest static honesty |
| Simple Record SRTP | `CAPS-SIMPLE-RECORD-SRTP.md` | 3/5 | 3/5 | 2/5 | Partial | Compile-time | Low | Good | Conservative middle ground |
| Structural Accessors | `CAPS-STRUCTURAL-ACCESSORS.md` | 4/5 | 2/5 | 2/5 | Yes | Compile-time | Low | Fair | Preferred strict core model |
| Structural Accessors + DI bridge | `CAPS-STRUCTURAL-SP-BRIDGE.md` | 4/5 | 3/5 | 2/5 | Yes | Compile-time core, runtime edge | High | Good at edge | Best integration story |
| Explicit interface/record hybrid | `CAPS-EXPLICIT-HYBRID.md` | 2/5 | 4/5 | 4/5 | Partial | Compile-time core, runtime edge | Medium | Good | Useful as a baseline, not the ergonomic winner |

## What the ratings mean

- `Ergonomics` is the overall day-to-day feel for the person writing flows.
- `Setup burden` is the amount of library and environment plumbing needed to get started.
- `User-code burden` is the amount of code a feature author needs to write for a typical capability workflow.
- `Fine-grained` means individual capabilities can stay narrow instead of forcing coarse grouped dependencies.
- `Compile-time safety` captures whether missing dependencies are caught statically or only at runtime.
- `Host / DI interop` measures how naturally the approach fits ASP.NET Core, Aspire, or similar application hosts.
- `IDE clarity` measures how understandable the inferred types and tooltips remain.

## Research history

### Boilerplate records + slices

The conventional F# baseline. Very explicit and safe, but it requires a lot of manual environment wiring.

### `IServiceProvider` edge

The pragmatic .NET host model. Very clean for app edges, but dependencies are opaque and the compiler cannot prove registrations.

### Simple Record SRTP

A conservative strict option. It keeps named environment records and still allows SRTP-based accessors.

### Anonymous traits probe

There is no standalone anonymous-traits file. The anonymous-traits idea was a probe step that helped validate the structural accessor model, but it did not survive as a separate approach.

### Structural Accessors

The preferred strict model. Inline accessors and anonymous records give low-boilerplate composition with compile-time checking.

Earlier drafts claimed this could use reusable trait aliases. Compiler probing showed that is not valid F#.

### Structural Accessors + DI bridge

The strict core plus a generated or reflective edge bridge to `IServiceProvider`. This is the best full-stack integration story.

### Explicit interface/record hybrid

A fully explicit hybrid baseline with interfaces, classes, and records. It is useful as a comparison point, but it does not beat the structural model on ergonomics.

## Recommendation

Use `Structural Accessors` for `FsFlow.Strict`, `IServiceProvider` for `FsFlow.Pragmatic`, and a bridge at the application edge when you need them to meet in the middle.

Final position:

> Strict where logic matters. Pragmatic where app hosts demand it.
