---
title: "Flow.join"
linkTitle: "join"
---

Waits for a fiber to complete and returns its final outcome.

## Signature

<div class="fsdocs-usage">
<code><span>Flow.join&#32;<span>fiber</span></span></code>
</div>

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `fiber` | <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-fiber-2.html">Fiber</a>&lt;<span>'error,&#32;'value</span>&gt;</span></code> | The fiber to join. |

## Returns

| Type | Description |
| --- | --- |
| <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-flow-3.html">Flow</a>&lt;<span>'env,&#32;'error,&#32;'value</span>&gt;</span></code> | A flow that completes with the fiber&#39;s outcome. |

