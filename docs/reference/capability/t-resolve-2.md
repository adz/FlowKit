---
title: "Resolve"
linkTitle: "Resolve<dep, value>"
---

Request token for projecting a value from a dependency.

## Remarks


 Builders read the dependency from the environment, apply the projection, and then reuse the
 existing lift/bind behavior for the projected value. If the projection returns a
 <code>Result</code>, <code>Async</code>, <code>Task</code>, <code>ValueTask</code>, <code>ColdTask</code>, <code>option</code>, or
 <code>voption</code>, the existing workflow rules still apply.
 