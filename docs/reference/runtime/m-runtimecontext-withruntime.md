---
title: "RuntimeContext.withRuntime"
linkTitle: "withRuntime"
---

<div class="fsdocs-usage">
<code><span>RuntimeContext.withRuntime&#32;<span>runtime&#32;context</span></span></code>
</div>

Replaces the runtime half of a runtime context.

## Parameters

- `runtime`: <code>'nextRuntime</code>
  The new runtime services.
- `context`: <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-runtimecontext-2.html">RuntimeContext</a>&lt;<span>'runtime,&#32;'env</span>&gt;</span></code>
  The source context.

## Returns

A new context with the replaced runtime services.

