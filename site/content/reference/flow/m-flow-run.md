---
title: "Flow.run"
linkTitle: "run"
type: docs
---

<div class="fsdocs-usage">
<code><span>run&#32;<span>environment&#32;flow</span></span></code>
</div>

Executes a flow with the provided environment and the default cancellation token.

## Remarks

Uncaught exceptions become <code>Cause.Die</code>; cancellation becomes <code>Cause.Interrupt</code>.

## Parameters

- `environment`: <code>'env</code>
- `flow`: <code><span><a href="https://adz.github.io/FsFlow/reference/FsFlow/fsflow-flow-3.html">Flow</a>&lt;<span>'env,&#32;'error,&#32;'value</span>&gt;</span></code>

## Examples

<pre class="fssnip highlighted"><code lang="fsharp"> <span class="k">let</span> <span onmouseout="hideTip(event, 'fs1', 1)" onmouseover="showTip(event, 'fs1', 1)" class="id">flow</span> <span class="o">=</span> <span class="id">Flow</span><span class="pn">.</span><span class="id">read</span> <span class="pn">(</span><span class="k">fun</span> <span class="id">env</span> <span class="k">-&gt;</span> <span class="s">$&quot;Hello, {</span><span class="id">env</span><span class="s">}!&quot;</span><span class="pn">)</span>
 <span class="k">let</span> <span onmouseout="hideTip(event, 'fs2', 2)" onmouseover="showTip(event, 'fs2', 2)" class="id">result</span> <span class="o">=</span> <span class="id">Flow</span><span class="pn">.</span><span class="id">run</span> <span class="s">&quot;World&quot;</span> <span onmouseout="hideTip(event, 'fs1', 3)" onmouseover="showTip(event, 'fs1', 3)" class="id">flow</span>
 <span class="c">// result = Effect that resolves to Success &quot;Hello, World!&quot; on both .NET and Fable</span>
</code></pre>
<div class="fsdocs-tip" id="fs1">val flow: obj</div>
<div class="fsdocs-tip" id="fs2">val result: obj</div>



