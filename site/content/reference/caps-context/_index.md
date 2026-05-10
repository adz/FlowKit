---
title: "CAPS Context"
type: docs
weight: 132
---

# CAPS Context

`FsFlow.Caps.Context` packages the execution-scoped facts that app code often needs alongside logging, auditing, and authorization:

- request id and correlation id
- tenant id
- current user
- locale and culture
- request metadata
- request-scoped flags

The package keeps those values explicit and testable. Production code can snapshot the current runtime state, while tests can construct a fixed context record directly.

## Reference

- [Context API](./context.md): the source-documented module and type surface for the package.
