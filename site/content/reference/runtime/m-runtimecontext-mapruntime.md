---
title: "RuntimeContext.mapRuntime"
linkTitle: "mapRuntime"
type: docs
---

<div class="fsdocs-usage">
<code><span>RuntimeContext.mapRuntime&#32;<span>mapper&#32;context</span></span></code>
</div>

Maps the runtime half of a runtime context.

## Parameters

- `mapper`: <code><span>'runtime&#32;->&#32;'nextRuntime</span></code>
  A function of type <code>&#39;runtime -&gt; &#39;nextRuntime</code>.
- `context`: <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-runtimecontext-2.html">RuntimeContext</a>&lt;<span>'runtime,&#32;'env</span>&gt;</span></code>
  The source context.

## Returns

A new context with the mapped runtime services.

