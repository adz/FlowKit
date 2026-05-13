---
title: "RuntimeContext.mapEnvironment"
linkTitle: "mapEnvironment"
---

<div class="fsdocs-usage">
<code><span>RuntimeContext.mapEnvironment&#32;<span>mapper&#32;context</span></span></code>
</div>

Maps the application environment half of a runtime context.

## Parameters

- `mapper`: <code><span>'env&#32;->&#32;'nextEnv</span></code>
  A function of type <code>&#39;env -&gt; &#39;nextEnv</code>.
- `context`: <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-runtimecontext-2.html">RuntimeContext</a>&lt;<span>'runtime,&#32;'env</span>&gt;</span></code>
  The source context.

## Returns

A new context with the mapped environment.

