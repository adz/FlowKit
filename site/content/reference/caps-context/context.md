---
title: CAPS Context
type: docs
---

This page shows the source-documented `FsFlow.Caps.Context` surface: the current-user model, request context record, request identifiers, locale, metadata, and request-scoped flags.

## Context types

- type [`UserContext`](./usercontext.md): Represents the authenticated user attached to the current request. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L10)
- type [`RequestContext`](./requestcontext.md): Represents the request-scoped execution context carried through FsFlow boundaries. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L29)

## Current user

- module `CurrentUser`: Helpers for reading and shaping the current user context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L56)
- [`CurrentUser.create`](./currentuser-create.md): Creates a current-user record from explicit values. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L81)
- [`CurrentUser.fromClaimsPrincipal`](./currentuser-fromclaimsprincipal.md): Creates a current-user record from a claims principal when the identity is authenticated. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L103)
- [`CurrentUser.live`](./currentuser-live.md): Reads the current claims principal from the runtime if one is available. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L136)
- [`CurrentUser.hasRole`](./currentuser-hasrole.md): Checks whether the current user has a role. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L146)
- [`CurrentUser.claim`](./currentuser-claim.md): Reads a claim value from the current user. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L156)

## Request context

- module `RequestContext`: Helpers for building and reshaping request contexts. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L164)
- [`RequestContext.create`](./requestcontext-create.md): Creates a request context from explicit values. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L190)
- [`RequestContext.live`](./requestcontext-live.md): Creates a live request context from the current runtime state. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L215)
- [`RequestContext.withRequestId`](./requestcontext-withrequestid.md): Replaces the request identifier in a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L231)
- [`RequestContext.withCorrelationId`](./requestcontext-withcorrelationid.md): Replaces the correlation identifier in a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L240)
- [`RequestContext.withTenantId`](./requestcontext-withtenantid.md): Replaces the tenant identifier in a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L249)
- [`RequestContext.withCurrentUser`](./requestcontext-withcurrentuser.md): Replaces the current user in a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L258)
- [`RequestContext.withCulture`](./requestcontext-withculture.md): Replaces the culture in a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L267)
- [`RequestContext.withMetadata`](./requestcontext-withmetadata.md): Replaces the request metadata in a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L276)
- [`RequestContext.withFlags`](./requestcontext-withflags.md): Replaces the request-scoped flags in a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L285)

## Request identity

- module `RequestId`: Helpers for the request identifier. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L291)
- [`RequestId.get`](./requestid-get.md): Reads the request identifier from a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L298)
- [`RequestId.live`](./requestid-live.md): Returns a live request identifier from the current runtime state. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L306)
- module `CorrelationId`: Helpers for the correlation identifier. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L311)
- [`CorrelationId.tryGet`](./correlationid-tryget.md): Reads the optional correlation identifier from a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L318)
- [`CorrelationId.live`](./correlationid-live.md): Returns a live optional correlation identifier from the current runtime state. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L326)
- module `TenantId`: Helpers for the tenant identifier. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L331)
- [`TenantId.tryGet`](./tenantid-tryget.md): Reads the optional tenant identifier from a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L338)
- [`TenantId.live`](./tenantid-live.md): Returns a live optional tenant identifier from the current runtime state. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L346)
- module `Locale`: Helpers for the current culture and locale. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L351)
- [`Locale.get`](./locale-get.md): Reads the culture from a request context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L358)
- [`Locale.live`](./locale-live.md): Returns the current culture from the runtime state. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L366)

## Metadata and flags

- module `RequestMetadata`: Helpers for request metadata. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L371)
- [`RequestMetadata.empty`](./requestmetadata-empty.md): Creates an empty metadata map. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L378)
- [`RequestMetadata.fromPairs`](./requestmetadata-frompairs.md): Creates a metadata map from name/value pairs. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L386)
- [`RequestMetadata.tryGet`](./requestmetadata-tryget.md): Reads a metadata value by key if it exists. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L395)
- [`RequestMetadata.contains`](./requestmetadata-contains.md): Checks whether the metadata contains a key. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L404)
- module `RequestFlags`: Helpers for request-scoped feature flags. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L410)
- [`RequestFlags.empty`](./requestflags-empty.md): Creates an empty flag map. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L417)
- [`RequestFlags.fromPairs`](./requestflags-frompairs.md): Creates a flag map from name/value pairs. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L425)
- [`RequestFlags.tryGet`](./requestflags-tryget.md): Reads a flag value by key if it exists. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L434)
- [`RequestFlags.isEnabled`](./requestflags-isenabled.md): Checks whether the supplied request-scoped flag is enabled. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L443)

