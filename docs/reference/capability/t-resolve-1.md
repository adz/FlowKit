---
title: "Resolve"
linkTitle: "Resolve<dep>"
---

Request token for binding a whole dependency inside a workflow.

## Remarks


 Use this token when a workflow needs the dependency itself rather than a value projected from
 that dependency. The <code>flow {}</code> builder and its internal compatibility helpers
 read it from any environment that implements <code>Requires&lt;&#39;dep&gt;</code>.
 