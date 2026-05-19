---
title: "Flow.service"
linkTitle: "service"
weight: 2311
---

Extracts a specific service from an environment that implements <code>IHas&lt;&#39;service&gt;</code>.

## Signature

<div class="fsdocs-usage">
<code><span>Flow.service&#32;<span>()</span></span></code>
</div>

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `()` | <code>unit</code> |  |

## Returns

| Type | Description |
| --- | --- |
| <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-flow-3.html">Flow</a>&lt;<span>'env,&#32;'error,&#32;'service</span>&gt;</span></code> | A flow containing the requested service. |

## Remarks

This is the statically honest way to access dependencies.

## Examples

<pre class="fssnip highlighted"><code lang="fsharp"> <span class="k">let</span> <span data-fsdocs-tip="fs1" data-fsdocs-tip-unique="1" class="id">flow</span> <span class="o">=</span> <span class="id">Flow</span><span class="pn">.</span><span class="id">service</span><span class="pn">&lt;</span><span class="id">IMyService</span><span class="pn">,</span> <span class="id">_</span><span class="pn">,</span> <span class="id">_</span><span class="pn">&gt;</span><span class="pn">(</span><span class="pn">)</span>
</code></pre>
<div popover class="fsdocs-tip" id="fs1">val flow: obj</div>



