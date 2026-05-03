module.exports = {
  docs: [
    {
      type: 'category',
      label: 'Start',
      items: [
        { type: 'doc', id: 'index', label: 'Home' },
        { type: 'doc', id: 'GETTING_STARTED', label: 'Getting Started' },
        { type: 'doc', id: 'VALIDATE_AND_RESULT', label: 'Validate and Result' },
        { type: 'doc', id: 'TINY_EXAMPLES', label: 'Common Shapes' },
      ],
    },
    {
      type: 'category',
      label: 'Core Model',
      items: [
        { type: 'doc', id: 'WHY_FSFLOW', label: 'The FsFlow Model' },
        { type: 'doc', id: 'SEMANTICS', label: 'Execution Semantics' },
        { type: 'doc', id: 'TASK_ASYNC_INTEROP', label: 'Task and Async Interop' },
        { type: 'doc', id: 'ENV_SLICING', label: 'Environment Slicing' },
        { type: 'doc', id: 'ARCHITECTURAL_STYLES', label: 'Architectural Styles' },
      ],
    },
    {
      type: 'category',
      label: 'Patterns',
      items: [
        { type: 'doc', id: 'examples/README', label: 'Runnable Examples' },
        { type: 'doc', id: 'TROUBLESHOOTING_TYPES', label: 'Troubleshooting Types' },
        { type: 'doc', id: 'BENCHMARKS', label: 'Benchmarks' },
      ],
    },
    {
      type: 'category',
      label: 'Ecosystem',
      items: [
        { type: 'doc', id: 'INTEGRATIONS_FSTOOLKIT', label: 'Replacing FsToolkit.ErrorHandling' },
        { type: 'doc', id: 'INTEGRATIONS_VALIDUS', label: 'Validus Integration' },
        { type: 'doc', id: 'INTEGRATIONS_ICEDTASKS', label: 'IcedTasks Integration' },
        { type: 'doc', id: 'INTEGRATIONS_FSHARPPLUS', label: 'FSharpPlus Integration' },
        { type: 'doc', id: 'EFFECT_TS_COMPARISON', label: 'Effect-TS Comparison' },
        { type: 'doc', id: 'INTEGRATIONS', label: 'Ecosystem Overview' },
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      items: [
        { type: 'doc', id: 'reference/index', label: 'API Reference' },
        {
          type: 'category',
          label: 'FsFlow',
          items: [
            { type: 'doc', id: 'reference/fsflow/index', label: 'Overview' },
            { type: 'doc', id: 'reference/fsflow/flow', label: 'Flow' },
            { type: 'doc', id: 'reference/fsflow/asyncflow', label: 'AsyncFlow' },
            { type: 'doc', id: 'reference/fsflow/validate', label: 'Validate' },
            { type: 'doc', id: 'reference/fsflow/taskflow', label: 'TaskFlow' },
            { type: 'doc', id: 'reference/fsflow/coldtask', label: 'ColdTask' },
            { type: 'doc', id: 'reference/fsflow/interop', label: 'Interop' },
            { type: 'doc', id: 'reference/fsflow/support-types', label: 'Support Types' },
          ],
        },
      ],
    },
  ],
};
