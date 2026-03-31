#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/examples/EffectfulFlow.AotProbe/EffectfulFlow.AotProbe.fsproj"
PUBLISH_DIR="$ROOT_DIR/artifacts/publish/EffectfulFlow.AotProbe/linux-x64"

dotnet publish "$PROJECT" -c Release -r linux-x64 -p:PublishAot=true -o "$PUBLISH_DIR"
"$PUBLISH_DIR/EffectfulFlow.AotProbe"
