---
title: "Flow.provideLayer"
linkTitle: "provideLayer"
weight: 2327
---

Runs a layer flow first, then runs a downstream flow with the layer&#39;s output as its environment.

## Signature

<div class="fsdocs-usage">
<code><span>Flow.provideLayer&#32;<span>layer&#32;flow</span></span></code>
</div>

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `layer` | <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-flow-3.html">Flow</a>&lt;<span>'input,&#32;'error,&#32;'environment</span>&gt;</span></code> | A flow that provides the environment required by the downstream flow. |
| `flow` | <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-flow-3.html">Flow</a>&lt;<span>'environment,&#32;'error,&#32;'value</span>&gt;</span></code> | The flow to run with the provided environment. |

## Returns

| Type | Description |
| --- | --- |
| <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-flow-3.html">Flow</a>&lt;<span>'input,&#32;'error,&#32;'value</span>&gt;</span></code> | A flow that requires only the input environment of the layer. |

## Remarks


 Use this at composition boundaries where one flow builds the environment needed by another
 flow. Ordinary workflow code should usually consume an environment directly with
 <code>Flow.read</code>; <code>provideLayer</code> is for deriving or provisioning an environment before a
 downstream workflow starts.
 

## Examples

<pre class="fssnip highlighted"><code lang="fsharp"> <span class="k">let</span> <span data-fsdocs-tip="fs1" data-fsdocs-tip-unique="1" class="id">layer</span> <span class="o">=</span> <span class="id">Flow</span><span class="pn">.</span><span class="id">succeed</span> <span class="s">&quot;test&quot;</span>
 <span class="k">let</span> <span data-fsdocs-tip="fs2" data-fsdocs-tip-unique="2" class="id">flow</span> <span class="o">=</span> <span class="id">Flow</span><span class="pn">.</span><span class="id">env</span> <span class="o">|&gt;</span> <span class="id">Flow</span><span class="pn">.</span><span class="id">provideLayer</span> <span data-fsdocs-tip="fs1" data-fsdocs-tip-unique="3" class="id">layer</span>
</code></pre>
<div popover class="fsdocs-tip" id="fs1">val layer: obj</div>
<div popover class="fsdocs-tip" id="fs2">val flow: obj</div>



