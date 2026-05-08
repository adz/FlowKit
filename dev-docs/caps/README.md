# FsFlow CAPS Corrected Bundle

This bundle contains corrected capability-design documents for FsFlow.

## Files

- `CAPS_PLAN.md` — definitive corrected plan.
- `CAPS_SUMMARY.md` — comparison of all approaches and tradeoffs.
- `CAPS-BOILERPLATE.md` — explicit record/slice baseline.
- `CAPS-ISERVICEPROVIDER.md` — pragmatic `IServiceProvider` model.
- `CAPS-SIMPLE-RECORD-SRTP.md` — conservative strict SRTP record model.
- `CAPS-STRUCTURAL-ACCESSORS.md` — preferred strict model, using accessor functions instead of trait aliases.
- `CAPS-STRUCTURAL-SP-BRIDGE.md` — bridge strategy for DI integration.
- `CAPS-EXPLICIT-HYBRID.md` — explicit interface/record hybrid baseline.
- `probe.fsx` — minimal compiler probe showing the invalid trait-alias idea.

## Key correction

Earlier drafts proposed reusable “Trait Aliases” for SRTP member constraints. Compiler probing showed this is not valid F#.

Correct model:

```fsharp
module Cap =
    let inline email (env: ^env) : IEmail =
        (^env : (member Email : IEmail) env)
```

Requirements are induced by accessor functions and composed by ordinary F# composition.
