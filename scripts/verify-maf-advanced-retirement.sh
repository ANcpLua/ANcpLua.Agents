#!/usr/bin/env bash
set -euo pipefail

repo_root="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
repo_root="$(cd "$repo_root" && pwd)"
source_repo="${SOURCE_REPO:-/Users/ancplua/framework/MAF.Advanced.Patterns}"

fail() {
  printf 'FAIL: %s\n' "$1" >&2
  exit 1
}

require_file() {
  [[ -f "$repo_root/$1" ]] || fail "missing expected file: $1"
}

reject_rg() {
  local pattern="$1"
  local scope="$2"
  if rg -n "$pattern" "$repo_root/$scope" >/tmp/maf-retirement-rg.txt; then
    cat /tmp/maf-retirement-rg.txt >&2
    fail "unexpected retired surface matched '$pattern' under $scope"
  fi
}

[[ -d "$repo_root/src" ]] || fail "expected source directory at $repo_root/src"

require_file "src/ANcpLua.Agents.Hosting.OpenAI/Facades/QylOpenAIClientExtensions.cs"
require_file "src/ANcpLua.Agents.Hosting.Anthropic/Facades/QylAnthropicAgentExtensions.cs"
require_file "src/ANcpLua.Agents.Hosting.DevUI/Facades/QylDevUIExtensions.cs"
require_file "src/ANcpLua.Agents.Testing/Conformance/Support/ConformanceConstants.cs"
require_file "docs/maf-advanced-retirement/inventory.md"
require_file "docs/maf-advanced-retirement/retirement-plan.md"

reject_rg "Qyl(A2A|AGUI|McpToolHandler|Purview|Cosmos|GitHubCopilot|CopilotStudio)" "src"
reject_rg "Microsoft\\.Agents\\.AI\\.(Hosting\\.(A2A|AGUI)|A2A|AGUI|Purview|CosmosNoSql|CopilotStudio|GitHub\\.Copilot)" "src"

if [[ -d "$source_repo/docs/maf-advanced-retirement" ]]; then
  for retired_doc in a2a.md provider-surfaces.md retirement-plan.md; do
    [[ ! -e "$source_repo/docs/maf-advanced-retirement/$retired_doc" ]] ||
      fail "source retirement doc still exists: $source_repo/docs/maf-advanced-retirement/$retired_doc"
  done
fi

printf 'PASS: MAF.Advanced.Patterns retirement extraction checks passed for %s\n' "$repo_root"
