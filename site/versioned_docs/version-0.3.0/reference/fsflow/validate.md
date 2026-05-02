---
title: Validate
description: Source-documented check, result, and validation helpers for FsFlow.
---

# Validate

This page shows the source-documented validation surface: pure checks, fail-fast result helpers, and the accumulating validation graph, all with source links back to the implementation.

## Diagnostics graph

- type `PathSegment`: Location markers used to describe where a diagnostic belongs in a validation graph. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L7)
- type `Path`: A path through a validation graph. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L13)
- type `Diagnostic`: A single failure item attached to a path in a validation graph. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L16)
- type `Diagnostics`: A mergeable validation graph that carries local diagnostics and nested child branches. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L25)
- module `Diagnostics`: Helpers for building, merging, and flattening validation diagnostics graphs. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L41)
- `Diagnostics.empty` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L42)
- `Diagnostics.singleton` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L48)
- `Diagnostics.merge` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L54)
- `Diagnostics.flatten` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L65)

## Fail-fast Result

- module `Result`: Helpers for fail-fast `Result` workflows and the bridge from placeholder unit failures into application errors. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L86)
- `Result.map`: Maps the successful value of a result. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L88)
- `Result.bind`: Sequences a result-producing continuation after a successful value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L97)
- `Result.mapError`: Maps the failure value of a result. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L106)
- `Result.mapErrorTo`: Replaces the unit failure from a predicate result with the supplied error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L115)
- `Result.sequence`: Runs a sequence of results until the first failure or the end of the sequence. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L122)
- `Result.traverse`: Maps a sequence with a fail-fast result-producing function. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L138)

## Accumulating Validation

- type `Validation`: An accumulating validation result that keeps the structured diagnostics graph visible. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L34)
- module `Validation`: Helpers for accumulating validation results with mergeable diagnostics. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L161)
- `Validation.toResult` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L164)
- `Validation.succeed` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L167)
- `Validation.fail` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L170)
- `Validation.fromResult` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L173)
- `Validation.map` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L178)
- `Validation.bind` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L184)
- `Validation.mapError` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L192)
- `Validation.map2` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L212)
- `Validation.apply` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L225)
- `Validation.collect` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L231)
- `Validation.sequence` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L239)
- `Validation.merge` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L242)

## Pure checks

- type `Check`: A reusable predicate result that carries a unit failure placeholder until the caller maps it into a domain-specific error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L249)
- module `Check`: Pure predicate helpers that return `Result` values with a unit error, plus the bridge functions that turn those checks into application errors. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L257)
- `Check.fromPredicate`: Builds a check from a predicate while preserving the successful value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L259)
- `Check.not`: Returns success when the supplied check fails. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L266)
- `Check.and`: Returns success when both checks succeed. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L272)
- `Check.or`: Returns success when either check succeeds. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L281)
- `Check.all`: Returns success when every check in the sequence succeeds. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L290)
- `Check.any`: Returns success when at least one check in the sequence succeeds. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L306)
- `Check.okIf`: Returns success when the condition is true. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L322)
- `Check.failIf`: Returns success when the condition is false. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L326)
- `Check.okIfSome`: Returns the value when the option is `Some`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L330)
- `Check.okIfNone`: Returns success when the option is `None`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L336)
- `Check.failIfSome`: Returns success when the option is `None`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L342)
- `Check.failIfNone`: Returns the value when the option is `Some`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L348)
- `Check.okIfValueSome`: Returns the value when the value option is `ValueSome`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L354)
- `Check.okIfValueNone`: Returns success when the value option is `ValueNone`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L360)
- `Check.failIfValueSome`: Returns success when the value option is `ValueNone`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L366)
- `Check.failIfValueNone`: Returns the value when the value option is `ValueSome`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L372)
- `Check.okIfNotNull`: Returns the value when it is not null. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L378)
- `Check.okIfNull`: Returns success when the value is null. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L382)
- `Check.failIfNotNull`: Returns success when the value is null. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L386)
- `Check.failIfNull`: Returns the value when it is null. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L390)
- `Check.okIfNotEmpty`: Returns the sequence when it is not empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L394)
- `Check.okIfEmpty`: Returns success when the sequence is empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L398)
- `Check.failIfNotEmpty`: Returns success when the sequence is empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L402)
- `Check.failIfEmpty`: Returns the sequence when it is not empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L406)
- `Check.okIfEqual`: Returns success when the values are equal. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L410)
- `Check.okIfNotEqual`: Returns success when the values are not equal. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L414)
- `Check.failIfEqual`: Returns success when the values are equal. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L418)
- `Check.failIfNotEqual`: Returns success when the values are not equal. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L422)
- `Check.okIfNonEmptyStr`: Returns the string when it is not null or empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L426)
- `Check.okIfEmptyStr`: Returns success when the string is null or empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L430)
- `Check.failIfNonEmptyStr`: Returns success when the string is null or empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L434)
- `Check.failIfEmptyStr`: Returns the string when it is null or empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L438)
- `Check.okIfNotBlank`: Returns the string when it is not blank. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L442)
- `Check.notBlank`: Returns the string when it is not blank. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L446)
- `Check.okIfBlank`: Returns success when the string is blank. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L450)
- `Check.blank`: Returns success when the string is blank. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L454)
- `Check.failIfNotBlank`: Returns success when the string is blank. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L458)
- `Check.failIfBlank`: Returns the string when it is blank. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L462)
- `Check.orElse`: Maps a unit error into the supplied application error value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L467)
- `Check.orElseWith`: Maps a unit error into an application error produced on demand. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L472)
- `Check.notNull`: Returns the value when it is not null. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L476)
- `Check.notEmpty`: Returns the sequence when it is not empty. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L480)
- `Check.equal`: Returns success when the values are equal. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L484)
- `Check.notEqual`: Returns success when the values are not equal. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L488)

## Compatibility surface

The `Validate` module keeps the old names as aliases over `Check`, so existing call sites can move over without losing the source-level intent.

- module `Validate`: Backward-compatible aliases for the old validation module name. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L495)
- `Validate.not` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L496)
- `Validate.and` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L497)
- `Validate.or` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L498)
- `Validate.all` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L499)
- `Validate.any` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L500)
- `Validate.fromPredicate` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L501)
- `Validate.okIf` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L502)
- `Validate.failIf` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L503)
- `Validate.okIfSome` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L504)
- `Validate.okIfNone` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L505)
- `Validate.failIfSome` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L506)
- `Validate.failIfNone` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L507)
- `Validate.okIfValueSome` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L508)
- `Validate.okIfValueNone` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L509)
- `Validate.failIfValueSome` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L510)
- `Validate.failIfValueNone` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L511)
- `Validate.okIfNotNull` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L512)
- `Validate.okIfNull` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L513)
- `Validate.failIfNotNull` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L514)
- `Validate.failIfNull` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L515)
- `Validate.notNull` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L516)
- `Validate.okIfNotEmpty` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L517)
- `Validate.okIfEmpty` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L518)
- `Validate.failIfNotEmpty` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L519)
- `Validate.failIfEmpty` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L520)
- `Validate.okIfEqual` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L522)
- `Validate.okIfNotEqual` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L523)
- `Validate.failIfEqual` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L524)
- `Validate.failIfNotEqual` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L525)
- `Validate.okIfNonEmptyStr` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L528)
- `Validate.okIfEmptyStr` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L529)
- `Validate.failIfNonEmptyStr` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L530)
- `Validate.failIfEmptyStr` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L531)
- `Validate.okIfNotBlank` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L532)
- `Validate.okIfBlank` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L533)
- `Validate.failIfNotBlank` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L534)
- `Validate.failIfBlank` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L535)
- `Validate.orElse` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L539)
- `Validate.orElseWith` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs#L542)

## Entry points

The `result {}` and `validate {}` builders are the syntax layer over the helper surface, and they stay as entry points rather than headline API types.

- `Builders.result`: The fail-fast `result { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L1179)
- `Builders.validate`: The accumulating `validate { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L1194)

## Source

- [Validate.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Validate.fs)
- [Flow.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs)
