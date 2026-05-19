---
title: "IHas"
linkTitle: "IHas<service>"
weight: 1000
---

Compatibility contract for a single dependency.

## Signature

<div class="fsdocs-usage">
<code>type IHas<'service></code>
</div>

## Type Parameters

| Name |
| --- |
| `service` |

## Remarks


 Prefer nominal capability interfaces for public workflows. This helper remains for lower-level
 binding machinery that still needs to work with a single dependency directly.
 

## Examples

<pre class="fssnip highlighted"><code lang="fsharp"> <span class="k">type</span> <span data-fsdocs-tip="fs1" data-fsdocs-tip-unique="1" class="if">IDb</span> <span class="o">=</span>
     <span class="k">abstract</span> <span class="fn">Query</span> <span class="pn">:</span> <span data-fsdocs-tip="fs2" data-fsdocs-tip-unique="2" class="rt">string</span> <span class="k">-&gt;</span> <span data-fsdocs-tip="fs2" data-fsdocs-tip-unique="3" class="rt">string</span>

 <span class="k">type</span> <span class="rt">AppEnv</span> <span class="o">=</span>
     <span class="pn">{</span> <span class="prop">Database</span> <span class="pn">:</span> <span data-fsdocs-tip="fs1" data-fsdocs-tip-unique="4" class="if">IDb</span> <span class="pn">}</span>
     <span class="k">interface</span> <span class="id">IHas</span><span class="pn">&lt;</span><span data-fsdocs-tip="fs1" data-fsdocs-tip-unique="5" class="id">IDb</span><span class="pn">&gt;</span> <span class="k">with</span> <span class="k">member</span> <span data-fsdocs-tip="fs3" data-fsdocs-tip-unique="6" class="fn">x</span><span class="pn">.</span><span class="prop">Service</span> <span class="o">=</span> <span data-fsdocs-tip="fs3" data-fsdocs-tip-unique="7" class="fn">x</span><span class="pn">.</span><span data-fsdocs-tip="fs4" data-fsdocs-tip-unique="8" class="id">Database</span>

 <span class="k">let</span> <span data-fsdocs-tip="fs5" data-fsdocs-tip-unique="9" class="id">db</span> <span class="o">=</span> <span class="id">Flow</span><span class="pn">.</span><span class="id">service</span><span class="pn">&lt;</span><span data-fsdocs-tip="fs1" data-fsdocs-tip-unique="10" class="id">IDb</span><span class="pn">,</span> <span data-fsdocs-tip="fs6" data-fsdocs-tip-unique="11" class="id">AppEnv</span><span class="pn">,</span> <span data-fsdocs-tip="fs7" data-fsdocs-tip-unique="12" class="id">unit</span><span class="pn">&gt;</span><span class="pn">(</span><span class="pn">)</span>
</code></pre>
<div popover class="fsdocs-tip" id="fs1">type IDb =
  abstract Query: string -&gt; string</div>
<div popover class="fsdocs-tip" id="fs2">Multiple items<br />val string: value: &#39;T -&gt; string<br /><br />--------------------<br />type string = System.String</div>
<div popover class="fsdocs-tip" id="fs3">val x: AppEnv</div>
<div popover class="fsdocs-tip" id="fs4">AppEnv.Database: IDb</div>
<div popover class="fsdocs-tip" id="fs5">val db: obj</div>
<div popover class="fsdocs-tip" id="fs6">type AppEnv =
  { Database: IDb }
  interface obj
  override Service: IDb</div>
<div popover class="fsdocs-tip" id="fs7">type unit = Unit</div>



