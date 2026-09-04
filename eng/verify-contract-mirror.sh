#!/usr/bin/env bash
set -euo pipefail
repo="${LUMIO_ARCH_REPO:-../LumioGameEngineArchitecture}"
origin="$(tr -d '\r\n' < contract/ORIGIN)"
if [[ -z "$origin" || ! -d "$repo/.git" ]]; then
  echo "BLOCKED: set LUMIO_ARCH_REPO to the architecture repository" >&2
  exit 2
fi
for file in account-port-v1.json platform-port-v1.json; do
  tmp="$(mktemp)"
  git -C "$repo" show "$origin:engine/wire/$file" > "$tmp"
  cmp -s "$tmp" "contract/$file" || { echo "contract/$file differs from $origin" >&2; exit 1; }
  rm -f "$tmp"
done
echo "contract mirror verified at $origin"
