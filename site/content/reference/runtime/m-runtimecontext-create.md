---
title: "RuntimeContext.create"
linkTitle: "create"
type: docs
---

<div class="fsdocs-usage">
<code><span>RuntimeContext.create&#32;<span>runtime&#32;environment&#32;cancellationToken</span></span></code>
</div>

Creates a runtime context from the supplied runtime services, environment, and cancellation token.

## Parameters

- `runtime`: <code>'runtime</code>
  The runtime services of type <code>&#39;runtime</code>.
- `environment`: <code>'env</code>
  The application environment of type <code>&#39;env</code>.
- `cancellationToken`: <code><a href="https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken">CancellationToken</a></code>
  The <a href="https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken">CancellationToken</a>.

## Returns

A new <a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-runtimecontext-2.html">RuntimeContext</a>.

