#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARCH_REPO="${LUMIO_ARCH_REPO:-../LumioGameEngineArchitecture}"
CONTRACT_DIR="${LUMIO_CONTRACT_DIR:-$ROOT/contract}"
DRIFT_EXIT=1
BLOCKED_EXIT=2

read_origin() {
  local origin_file="$1" origin
  if [[ ! -f "$origin_file" ]]; then
    echo "BLOCKED: missing $origin_file" >&2
    return "$BLOCKED_EXIT"
  fi

  origin="$(tr -d '[:space:]' < "$origin_file")"
  if [[ ! "$origin" =~ ^[0-9a-fA-F]{40}$ ]]; then
    echo "BLOCKED: $origin_file must contain one full 40-character commit SHA" >&2
    return "$BLOCKED_EXIT"
  fi
  origin="${origin,,}"
  printf '%s\n' "$origin"
}

verify_mirror() {
  local repo="$1" contract_dir="$2" origin="$3" file tmp_dir resolved

  if ! resolved="$(git -C "$repo" rev-parse --verify "${origin}^{commit}" 2>/dev/null)"; then
    echo "BLOCKED: architecture repository cannot resolve $origin" >&2
    return "$BLOCKED_EXIT"
  fi
  if [[ "$resolved" != "${origin,,}" ]]; then
    echo "BLOCKED: architecture revision resolved to $resolved, expected $origin" >&2
    return "$BLOCKED_EXIT"
  fi

  tmp_dir="$(mktemp -d)"
  for file in account-port-v1.json platform-port-v1.json; do
    if [[ ! -f "$contract_dir/$file" ]]; then
      rm -rf "$tmp_dir"
      echo "DRIFT: missing $contract_dir/$file" >&2
      return "$DRIFT_EXIT"
    fi
    if ! git -C "$repo" show "$origin:engine/wire/$file" > "$tmp_dir/$file"; then
      rm -rf "$tmp_dir"
      echo "BLOCKED: $origin does not contain engine/wire/$file" >&2
      return "$BLOCKED_EXIT"
    fi
    if ! cmp -s "$tmp_dir/$file" "$contract_dir/$file"; then
      rm -rf "$tmp_dir"
      echo "DRIFT: $contract_dir/$file differs from $origin" >&2
      return "$DRIFT_EXIT"
    fi
  done
  rm -rf "$tmp_dir"
}

self_test() {
  local sandbox arch contract origin_a origin_b status
  sandbox="$(mktemp -d)"
  arch="$sandbox/architecture"
  contract="$sandbox/contract"
  mkdir -p "$arch/engine/wire" "$contract"

  git init --quiet -b main "$arch"
  git -C "$arch" config user.email verify-contract-mirror@example.invalid
  git -C "$arch" config user.name verify-contract-mirror
  printf 'account probe\n' > "$arch/engine/wire/account-port-v1.json"
  printf 'platform probe\n' > "$arch/engine/wire/platform-port-v1.json"
  git -C "$arch" add engine/wire
  git -C "$arch" commit --quiet -m 'probe baseline'
  origin_a="$(git -C "$arch" rev-parse HEAD)"
  cp "$arch/engine/wire/account-port-v1.json" "$contract/account-port-v1.json"
  cp "$arch/engine/wire/platform-port-v1.json" "$contract/platform-port-v1.json"

  if ! verify_mirror "$arch" "$contract" "$origin_a"; then
    echo "SELFTEST_FAIL: matching source and mirror did not pass" >&2
    rm -rf "$sandbox"
    return 1
  fi
  echo "SELFTEST baseline: exit 0"

  printf 'reverse drift\n' >> "$arch/engine/wire/account-port-v1.json"
  git -C "$arch" add engine/wire/account-port-v1.json
  git -C "$arch" commit --quiet -m 'probe source drift'
  origin_b="$(git -C "$arch" rev-parse HEAD)"
  if verify_mirror "$arch" "$contract" "$origin_b"; then
    echo "SELFTEST_FAIL: source drift was accepted" >&2
    rm -rf "$sandbox"
    return 1
  else
    status=$?
  fi
  if [[ "$status" -ne "$DRIFT_EXIT" ]]; then
    echo "SELFTEST_FAIL: source drift returned $status, expected $DRIFT_EXIT" >&2
    rm -rf "$sandbox"
    return 1
  fi
  echo "SELFTEST reverse drift: exit $DRIFT_EXIT"

  cp "$arch/engine/wire/account-port-v1.json" "$contract/account-port-v1.json"
  if ! verify_mirror "$arch" "$contract" "$origin_b"; then
    echo "SELFTEST_FAIL: repaired mirror did not pass" >&2
    rm -rf "$sandbox"
    return 1
  fi
  echo "SELFTEST repaired: exit 0"
  rm -rf "$sandbox"
  echo "CONTRACT_MIRROR_SELFTEST_OK"
}

if [[ "${1:-}" == "--self-test" ]]; then
  [[ "$#" -eq 1 ]] || { echo "usage: $0 [--self-test]" >&2; exit 2; }
  self_test
  exit $?
fi
[[ "$#" -eq 0 ]] || { echo "usage: $0 [--self-test]" >&2; exit 2; }

origin="$(read_origin "$CONTRACT_DIR/ORIGIN")"
if ! git -C "$ARCH_REPO" rev-parse --git-dir >/dev/null 2>&1; then
  echo "BLOCKED: set LUMIO_ARCH_REPO to the architecture repository" >&2
  exit "$BLOCKED_EXIT"
fi
verify_mirror "$ARCH_REPO" "$CONTRACT_DIR" "$origin"
echo "contract mirror verified at $origin"
