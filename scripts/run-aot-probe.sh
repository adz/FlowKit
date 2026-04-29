#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/examples/FsFlow.AotProbe/FsFlow.AotProbe.fsproj"
LIBRARY_PROJECT="$ROOT_DIR/src/FsFlow.Net/FsFlow.Net.fsproj"
PUBLISH_DIR="$ROOT_DIR/artifacts/publish/FsFlow.AotProbe/linux-x64"

dotnet build "$LIBRARY_PROJECT" -c Release -f net8.0 --nologo -v minimal
dotnet publish "$PROJECT" -c Release -r linux-x64 -p:PublishAot=true -p:UsePrebuiltFsFlow=true -o "$PUBLISH_DIR"
"$PUBLISH_DIR/FsFlow.AotProbe"
