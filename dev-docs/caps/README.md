# FsFlow CAPS Design Bundle

This bundle contains capability-design research documents for FsFlow.

Start with `NEW-APPROACH.md`. It is the current proposed direction: optional cap families for
explicit, typed, testable .NET/system effects, while user domain dependencies stay plain by default.
`CAPS_SUMMARY.md`, `CAPS_RECOMMENDED_MODEL.md`, and `CAPS_RECOMMENDED_WALKTHROUGH.md` keep the
research path that led here.

## Files

- `NEW-APPROACH.md` — current proposed direction.
- `CAPS_RECOMMENDED_MODEL.md` — compressed description of the chosen model and how the parts fit.
- `CAPS_RECOMMENDED_WALKTHROUGH.md` — step-by-step explanation for a new user.
- `CAPS_SUMMARY.md` — comparison, recommendation, and 1.0 advice.
- `CAPS_PLAN.md` — earlier corrected plan based on structural accessors; now historical.
- `CAPS-BOILERPLATE.md` — explicit record/slice baseline.
- `CAPS-ISERVICEPROVIDER.md` — pragmatic `IServiceProvider` model.
- `CAPS-SIMPLE-RECORD-SRTP.md` — conservative strict SRTP record model.
- `CAPS-STRUCTURAL-ACCESSORS.md` — structural accessor model, using accessor functions instead of trait aliases.
- `CAPS-STRUCTURAL-SP-BRIDGE.md` — bridge strategy for DI integration.
- `CAPS-EXPLICIT-HYBRID.md` — explicit interface/record hybrid baseline.
- `probe.fsx` — minimal compiler probe showing the invalid trait-alias idea.

## Current Recommendation

The recommended 1.0 direction is:

```text
Capify FsFlow-provided runtime/system effects.
Keep user domain dependencies as records/provider/env by default.
Ship optional cap-family NuGets for Core, Context, Observability, FileSystem, Console, Http, Process, ServiceProvider.
Preserve fine-grained requirements for FsFlow-provided operations.
```

The older structural-accessor documents remain useful research, especially for understanding why SRTP member
constraints are not the default public capability surface.

## Key Research Correction

Earlier drafts proposed reusable “Trait Aliases” for SRTP member constraints. Compiler probing showed this is not
valid F#. That correction still matters, even though the current recommendation now moves away from SRTP as the
main 1.0 capability model.
