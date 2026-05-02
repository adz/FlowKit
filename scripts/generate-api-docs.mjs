#!/usr/bin/env node

import fs from 'node:fs';
import path from 'node:path';

const repoRoot = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const githubBase = 'https://github.com/adz/FsFlow/blob/main';

const targets = [
  path.join(repoRoot, 'docs', 'reference'),
  path.join(repoRoot, 'site', 'versioned_docs', 'version-0.3.0', 'reference'),
];

const pageSpecs = [
  {
    outPath: ['fsflow', 'flow.md'],
    title: 'Flow',
    description: 'Source-documented synchronous and async workflow surface in FsFlow.',
    intro:
      "This page shows the source-documented `Flow` and `AsyncFlow` surface, with source links on every member so the reference stays tied to the code.",
    sourceFiles: ['src/FsFlow/Flow.fs'],
    sections: [
      {
        title: 'Flow',
        symbols: ['type:Flow', 'module:Flow', 'Flow.run', 'Flow.succeed', 'Flow.value', 'Flow.fail', 'Flow.fromResult', 'Flow.fromOption', 'Flow.fromValueOption', 'Flow.orElseFlow', 'Flow.env', 'Flow.read', 'Flow.map', 'Flow.bind', 'Flow.tap', 'Flow.tapError', 'Flow.mapError', 'Flow.catch', 'Flow.orElse', 'Flow.zip', 'Flow.map2', 'Flow.localEnv', 'Flow.delay', 'Flow.traverse', 'Flow.sequence'],
      },
      {
        title: 'AsyncFlow',
        symbols: ['type:AsyncFlow', 'module:AsyncFlow', 'AsyncFlow.run', 'AsyncFlow.toAsync', 'AsyncFlow.succeed', 'AsyncFlow.fail', 'AsyncFlow.fromResult', 'AsyncFlow.fromOption', 'AsyncFlow.fromValueOption', 'AsyncFlow.orElseAsync', 'AsyncFlow.orElseAsyncFlow', 'AsyncFlow.fromFlow', 'AsyncFlow.fromAsync', 'AsyncFlow.fromAsyncResult', 'AsyncFlow.env', 'AsyncFlow.read', 'AsyncFlow.map', 'AsyncFlow.bind', 'AsyncFlow.tap', 'AsyncFlow.tapError', 'AsyncFlow.mapError', 'AsyncFlow.catch', 'AsyncFlow.orElse', 'AsyncFlow.zip', 'AsyncFlow.map2', 'AsyncFlow.localEnv', 'AsyncFlow.delay', 'AsyncFlow.traverse', 'AsyncFlow.sequence'],
      },
      {
        title: 'Builder entry points',
        manual:
          'The builder entry points are the syntax layer on top of the module surface. Keep using the modules when you want the actual API members.',
        symbols: ['Builders.result', 'Builders.flow', 'Builders.asyncFlow', 'Builders.validate'],
      },
    ],
  },
  {
    outPath: ['fsflow', 'validate.md'],
    title: 'Validate',
    description: 'Source-documented check, result, and validation helpers for FsFlow.',
    intro:
      'This page shows the source-documented validation surface: pure checks, fail-fast result helpers, and the accumulating validation graph, all with source links back to the implementation.',
    sourceFiles: ['src/FsFlow/Validate.fs', 'src/FsFlow/Flow.fs'],
    sections: [
      {
        title: 'Diagnostics graph',
        symbols: ['type:PathSegment', 'type:Path', 'type:Diagnostic', 'type:Diagnostics', 'module:Diagnostics', 'Diagnostics.empty', 'Diagnostics.singleton', 'Diagnostics.merge', 'Diagnostics.flatten'],
      },
      {
        title: 'Fail-fast Result',
        symbols: ['module:Result', 'Result.map', 'Result.bind', 'Result.mapError', 'Result.mapErrorTo', 'Result.sequence', 'Result.traverse'],
      },
      {
        title: 'Accumulating Validation',
        symbols: ['type:Validation', 'module:Validation', 'Validation.toResult', 'Validation.succeed', 'Validation.fail', 'Validation.fromResult', 'Validation.map', 'Validation.bind', 'Validation.mapError', 'Validation.map2', 'Validation.apply', 'Validation.collect', 'Validation.sequence', 'Validation.merge'],
      },
      {
        title: 'Pure checks',
        symbols: ['type:Check', 'module:Check', 'Check.fromPredicate', 'Check.not', 'Check.and', 'Check.or', 'Check.all', 'Check.any', 'Check.okIf', 'Check.failIf', 'Check.okIfSome', 'Check.okIfNone', 'Check.failIfSome', 'Check.failIfNone', 'Check.okIfValueSome', 'Check.okIfValueNone', 'Check.failIfValueSome', 'Check.failIfValueNone', 'Check.okIfNotNull', 'Check.okIfNull', 'Check.failIfNotNull', 'Check.failIfNull', 'Check.okIfNotEmpty', 'Check.okIfEmpty', 'Check.failIfNotEmpty', 'Check.failIfEmpty', 'Check.okIfEqual', 'Check.okIfNotEqual', 'Check.failIfEqual', 'Check.failIfNotEqual', 'Check.okIfNonEmptyStr', 'Check.okIfEmptyStr', 'Check.failIfNonEmptyStr', 'Check.failIfEmptyStr', 'Check.okIfNotBlank', 'Check.notBlank', 'Check.okIfBlank', 'Check.blank', 'Check.failIfNotBlank', 'Check.failIfBlank', 'Check.orElse', 'Check.orElseWith', 'Check.notNull', 'Check.notEmpty', 'Check.equal', 'Check.notEqual'],
      },
      {
        title: 'Compatibility surface',
        manual:
          'The `Validate` module keeps the old names as aliases over `Check`, so existing call sites can move over without losing the source-level intent.',
        symbols: ['module:Validate', 'Validate.not', 'Validate.and', 'Validate.or', 'Validate.all', 'Validate.any', 'Validate.fromPredicate', 'Validate.okIf', 'Validate.failIf', 'Validate.okIfSome', 'Validate.okIfNone', 'Validate.failIfSome', 'Validate.failIfNone', 'Validate.okIfValueSome', 'Validate.okIfValueNone', 'Validate.failIfValueSome', 'Validate.failIfValueNone', 'Validate.okIfNotNull', 'Validate.okIfNull', 'Validate.failIfNotNull', 'Validate.failIfNull', 'Validate.notNull', 'Validate.okIfNotEmpty', 'Validate.okIfEmpty', 'Validate.failIfNotEmpty', 'Validate.failIfEmpty', 'Validate.okIfEqual', 'Validate.okIfNotEqual', 'Validate.failIfEqual', 'Validate.failIfNotEqual', 'Validate.okIfNonEmptyStr', 'Validate.okIfEmptyStr', 'Validate.failIfNonEmptyStr', 'Validate.failIfEmptyStr', 'Validate.okIfNotBlank', 'Validate.okIfBlank', 'Validate.failIfNotBlank', 'Validate.failIfBlank', 'Validate.orElse', 'Validate.orElseWith'],
      },
      {
        title: 'Entry points',
        manual:
          'The `result {}` and `validate {}` builders are the syntax layer over the helper surface, and they stay as entry points rather than headline API types.',
        symbols: ['src/FsFlow/Flow.fs::Builders.result', 'src/FsFlow/Flow.fs::Builders.validate'],
      },
    ],
  },
  {
    outPath: ['fsflow', 'support-types.md'],
    title: 'Support Types',
    description: 'Source-documented runtime support types in FsFlow.',
    intro:
      'This page shows the support types that stay close to the runtime helpers without taking over the main workflow story.',
    sourceFiles: ['src/FsFlow/Flow.fs'],
    sections: [
      {
        title: 'Logging',
        symbols: ['type:LogLevel', 'type:LogEntry'],
      },
      {
        title: 'Retry policy',
        symbols: ['type:RetryPolicy', 'module:RetryPolicy', 'RetryPolicy.noDelay'],
      },
    ],
  },
  {
    outPath: ['fsflow-net', 'taskflow.md'],
    title: 'TaskFlow',
    description: 'Source-documented task workflow surface in FsFlow.Net.',
    intro:
      'This page shows the source-documented task-oriented surface: the runtime context, cold task helper, task flow module, and the task-specific runtime helpers.',
    sourceFiles: ['src/FsFlow.Net/TaskFlow.fs', 'src/FsFlow.Net/Runtime.fs'],
    sections: [
      {
        title: 'Runtime context',
        symbols: ['src/FsFlow.Net/Runtime.fs::type:RuntimeContext', 'src/FsFlow.Net/Runtime.fs::module:RuntimeContext', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.create', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.runtime', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.environment', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.cancellationToken', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.mapRuntime', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.mapEnvironment', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.withRuntime', 'src/FsFlow.Net/Runtime.fs::RuntimeContext.withEnvironment'],
      },
      {
        title: 'ColdTask',
        symbols: ['type:ColdTask', 'module:ColdTask', 'ColdTask.run', 'ColdTask.create', 'ColdTask.fromTaskFactory', 'ColdTask.fromTask', 'ColdTask.fromValueTaskFactory', 'ColdTask.fromValueTaskFactoryWithoutCancellation', 'ColdTask.fromValueTask'],
      },
      {
        title: 'TaskFlow',
        symbols: ['type:TaskFlow', 'module:TaskFlow', 'TaskFlow.run', 'TaskFlow.runContext', 'TaskFlow.toTask', 'TaskFlow.succeed', 'TaskFlow.fail', 'TaskFlow.fromResult', 'TaskFlow.fromOption', 'TaskFlow.fromValueOption', 'TaskFlow.orElseTask', 'TaskFlow.orElseAsync', 'TaskFlow.orElseFlow', 'TaskFlow.orElseAsyncFlow', 'TaskFlow.orElseTaskFlow', 'TaskFlow.fromFlow', 'TaskFlow.fromAsyncFlow', 'TaskFlow.fromTask', 'TaskFlow.fromTaskResult', 'TaskFlow.env', 'TaskFlow.read', 'TaskFlow.readRuntime', 'TaskFlow.readEnvironment', 'TaskFlow.map', 'TaskFlow.bind', 'TaskFlow.tap', 'TaskFlow.tapError', 'TaskFlow.mapError', 'TaskFlow.catch', 'TaskFlow.orElse', 'TaskFlow.zip', 'TaskFlow.map2', 'TaskFlow.localEnv', 'TaskFlow.delay', 'TaskFlow.traverse', 'TaskFlow.sequence'],
      },
      {
        title: 'Task runtime helpers',
        symbols: ['module:TaskFlow.Runtime', 'TaskFlow.Runtime.cancellationToken', 'TaskFlow.Runtime.catchCancellation', 'TaskFlow.Runtime.ensureNotCanceled', 'TaskFlow.Runtime.sleep', 'TaskFlow.Runtime.log', 'TaskFlow.Runtime.logWith', 'TaskFlow.Runtime.useWithAcquireRelease', 'TaskFlow.Runtime.timeout', 'TaskFlow.Runtime.timeoutToOk', 'TaskFlow.Runtime.timeoutToError', 'TaskFlow.Runtime.timeoutWith', 'TaskFlow.Runtime.retry'],
      },
      {
        title: 'TaskFlowSpec',
        symbols: ['type:TaskFlowSpec', 'module:TaskFlowSpec', 'TaskFlowSpec.create', 'TaskFlowSpec.run'],
      },
      {
        title: 'Capabilities and layers',
        symbols: ['module:Capability', 'Capability.MissingCapability', 'Capability.service', 'Capability.runtime', 'Capability.environment', 'Capability.serviceFromProvider', 'type:Layer'],
      },
      {
        title: 'Entry points',
        manual:
          'The task-specific builder entry points stay as syntax on top of the module surface, while the extension modules handle the extra task and async interop shapes.',
        symbols: ['Builders.asyncFlow', 'Builders.taskFlow', 'module:TaskFlowBuilderExtensions', 'module:AsyncFlowBuilderExtensions'],
      },
    ],
  },
  {
    outPath: ['fsflow-net', 'coldtask.md'],
    title: 'ColdTask',
    description: 'Source-documented delayed task helpers used by FsFlow.Net.',
    intro:
      'This page shows the delayed task helper surface used by `TaskFlow`, with source links so the cold/hot distinction stays anchored to the implementation.',
    sourceFiles: ['src/FsFlow.Net/TaskFlow.fs'],
    sections: [
      {
        title: 'Core shape',
        symbols: ['type:ColdTask', 'module:ColdTask', 'ColdTask.run'],
      },
      {
        title: 'Creation helpers',
        symbols: ['ColdTask.create', 'ColdTask.fromTaskFactory', 'ColdTask.fromTask', 'ColdTask.fromValueTaskFactory', 'ColdTask.fromValueTaskFactoryWithoutCancellation', 'ColdTask.fromValueTask'],
      },
    ],
  },
  {
    outPath: ['fsflow-net', 'interop.md'],
    title: 'Interop',
    description: 'Source-documented task and async interop helpers for FsFlow.Net.',
    intro:
      'This page shows the interop helpers that bridge task-based boundaries to sync and async boundaries when that is the honest runtime shape.',
    sourceFiles: ['src/FsFlow.Net/TaskFlow.fs'],
    sections: [
      {
        title: 'TaskFlow bridges',
        symbols: ['TaskFlow.fromFlow', 'TaskFlow.fromAsyncFlow', 'TaskFlow.orElseTask', 'TaskFlow.orElseAsync', 'TaskFlow.orElseFlow', 'TaskFlow.orElseAsyncFlow', 'TaskFlow.orElseTaskFlow'],
      },
      {
        title: 'Builder extensions',
        manual:
          'The builder extension modules are the supported customization surface. The builder types themselves stay out of the narrative.',
        symbols: ['module:TaskFlowBuilderExtensions', 'module:AsyncFlowBuilderExtensions'],
      },
    ],
  },
];

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

function readLines(filePath) {
  return fs.readFileSync(filePath, 'utf8').split(/\r?\n/);
}

function cleanXmlDocText(text) {
  return text
    .replace(/<c>([\s\S]*?)<\/c>/gi, '`$1`')
    .replace(/<paramref name="([^"]+)"\s*\/>/gi, '`$1`')
    .replace(/<see cref="([^"]+)"\s*\/>/gi, (_match, cref) => {
      const withoutPrefix = cref.replace(/^[A-Z]:/, '');
      const lastSegment = withoutPrefix.split(/[.:]/).pop() ?? withoutPrefix;
      return `\`${lastSegment.replace(/`[0-9]+/g, '')}\``;
    })
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

function extractSummary(commentLines) {
  if (commentLines.length === 0) {
    return '';
  }

  const raw = commentLines
    .map((line) => line.replace(/^\s*\/\/\/\s?/, ''))
    .join('\n');

  const summaryMatch = raw.match(/<summary>([\s\S]*?)<\/summary>/i);
  const content = summaryMatch ? summaryMatch[1] : raw;
  return cleanXmlDocText(content);
}

function isAttributeLine(line) {
  return /^\s*\[<[^>]+>\]\s*$/.test(line);
}

function extractLetName(line) {
  const simple = line.match(/^\s*let\s+(?:inline\s+)?(?:private\s+)?(?:rec\s+)?(?:(``([^`]+)``)|([A-Za-z_][A-Za-z0-9_']*))(?=\s|$|[:(=<])/);
  if (simple) {
    return simple[2] || simple[3] || null;
  }

  const parenthesized = line.match(/^\s*let\s+(?:inline\s+)?(?:private\s+)?(?:rec\s+)?\(\s*(?:(``([^`]+)``)|([A-Za-z_][A-Za-z0-9_']*))\s*\)(?=\s|$|[:(=<])/);
  if (parenthesized) {
    return parenthesized[2] || parenthesized[3] || null;
  }

  return null;
}

function extractSymbols(filePath) {
  const lines = readLines(filePath);
  const symbols = new Map();
  const moduleStack = [];
  let pendingComments = [];
  let sawExclude = false;

  const popForIndent = (indent) => {
    while (moduleStack.length && moduleStack[moduleStack.length - 1].indent >= indent) {
      moduleStack.pop();
    }
  };

  const currentPrefix = () => moduleStack.map((entry) => entry.name).join('.');

  const record = (name, kind, lineNumber) => {
    if (sawExclude) {
      pendingComments = [];
      sawExclude = false;
      return;
    }

    const summary = extractSummary(pendingComments);
    const fullName = currentPrefix() ? `${currentPrefix()}.${name}` : name;
    const existing = symbols.get(fullName) ?? [];
    existing.push({
      kind,
      line: lineNumber,
      summary,
      filePath,
    });
    symbols.set(fullName, existing);
    pendingComments = [];
    sawExclude = false;
  };

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    const trimmed = line.trim();
    const indent = line.match(/^ */)?.[0].length ?? 0;

    if (trimmed.startsWith('///')) {
      pendingComments.push(line);
      if (trimmed.includes('<exclude/>')) {
        sawExclude = true;
      }
      continue;
    }

    if (trimmed === '') {
      continue;
    }

    if (isAttributeLine(line)) {
      continue;
    }

    popForIndent(indent);

    const moduleMatch = line.match(/^(\s*)module\s+([A-Za-z_][A-Za-z0-9_']*)\s*=/);
    if (moduleMatch) {
      const moduleIndent = moduleMatch[1].length;
      popForIndent(moduleIndent);
      record(moduleMatch[2], 'module', index + 1);
      moduleStack.push({ name: moduleMatch[2], indent: moduleIndent });
      continue;
    }

    const typeMatch = line.match(/^(\s*)type\s+([A-Za-z_][A-Za-z0-9_']*)/);
    if (typeMatch) {
      record(typeMatch[2], 'type', index + 1);
      continue;
    }

    const letName = extractLetName(line);
    const currentModuleIndent = moduleStack.length ? moduleStack[moduleStack.length - 1].indent : -4;
    if (letName && (pendingComments.length > 0 || indent <= currentModuleIndent + 4)) {
      const name = letName;
      record(name, 'let', index + 1);
      continue;
    }

    if (pendingComments.length > 0) {
      pendingComments = [];
      sawExclude = false;
    }
  }

  return symbols;
}

function makeSourceLink(filePath, line) {
  const relPath = path.relative(repoRoot, filePath).split(path.sep).join('/');
  return `${githubBase}/${relPath}#L${line}`;
}

function resolveSymbolDoc(symbols, qualifiedName, kindHint) {
  const docs = symbols.get(qualifiedName);
  if (!docs || docs.length === 0) {
    return null;
  }

  if (kindHint) {
    return docs.find((doc) => doc.kind === kindHint) ?? null;
  }

  if (docs.length === 1) {
    return docs[0];
  }

  return docs.find((doc) => doc.kind === 'module') ?? docs.find((doc) => doc.kind === 'type') ?? docs[0];
}

function renderItem(symbols, sourcePath, symbolRef) {
  const [sourcePrefix, rawSymbol] = symbolRef.includes('::') ? symbolRef.split('::', 2) : [null, symbolRef];
  const sourcePathKey = sourcePrefix && !sourcePrefix.includes(':') ? sourcePrefix : sourcePath;
  const symbolWithKind = sourcePrefix && sourcePrefix.includes(':') ? sourcePrefix : rawSymbol;
  const [kindHint, qualifiedName] = symbolWithKind.includes(':') ? symbolWithKind.split(':', 2) : [null, symbolWithKind];
  const doc = resolveSymbolDoc(symbols, qualifiedName, kindHint);
  if (!doc) {
    throw new Error(`Missing symbol doc for ${symbolRef} in ${sourcePathKey}`);
  }

  const label = kindHint && !qualifiedName.includes('.') ? `${kindHint} \`${qualifiedName}\`` : `\`${qualifiedName}\``;
  const summary = doc.summary ? `: ${doc.summary}` : '';
  return `- ${label}${summary} [source](${makeSourceLink(sourcePath, doc.line)})`;
}

function renderSection(spec, section, symbolsByFile) {
  const lines = [`## ${section.title}`, ''];

  if (section.manual) {
    lines.push(section.manual, '');
  }

  for (const symbol of section.symbols ?? []) {
    const [sourceAlias, symbolName] = symbol.includes('::') ? symbol.split('::', 2) : [null, symbol];
    const sourcePath = sourceAlias && !sourceAlias.includes(':') ? sourceAlias : spec.sourceFiles[0];
    const symbols = symbolsByFile.get(sourcePath);
    if (!symbols) {
      throw new Error(`Missing symbol index for ${sourcePath}`);
    }
    lines.push(renderItem(symbols, path.join(repoRoot, sourcePath), sourceAlias ? `${sourceAlias}::${symbolName}` : symbol));
  }

  if ((section.symbols ?? []).length > 0) {
    lines.push('');
  }

  return lines;
}

function renderPage(spec, symbolsByFile) {
  const lines = [
    '---',
    `title: ${spec.title}`,
    `description: ${spec.description}`,
    '---',
    '',
    `# ${spec.title}`,
    '',
    spec.intro,
    '',
  ];

  for (const section of spec.sections) {
    lines.push(...renderSection(spec, section, symbolsByFile));
  }

  lines.push('## Source', '');
  for (const sourceFile of spec.sourceFiles) {
    const relPath = path.relative(repoRoot, path.join(repoRoot, sourceFile)).split(path.sep).join('/');
    lines.push(`- [${path.basename(sourceFile)}](${githubBase}/${relPath})`);
  }
  lines.push('');

  return lines.join('\n');
}

function generate() {
  const symbolsByFile = new Map();

  for (const sourceFile of new Set(pageSpecs.flatMap((spec) => spec.sourceFiles))) {
    symbolsByFile.set(sourceFile, extractSymbols(path.join(repoRoot, sourceFile)));
  }

  for (const targetRoot of targets) {
    ensureDir(targetRoot);
    for (const spec of pageSpecs) {
      const outPath = path.join(targetRoot, ...spec.outPath);
      ensureDir(path.dirname(outPath));
      fs.writeFileSync(outPath, renderPage(spec, symbolsByFile), 'utf8');
    }
  }
}

generate();
