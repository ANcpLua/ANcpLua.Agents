#!/usr/bin/env bash
# scripts/setup-bitnet-local.sh
#
# Reproducible local setup for a BitNet llama-server used by ANcpLua tests.
# Idempotent: re-runs are no-ops once everything exists.
#
#   1. Clones microsoft/BitNet (recursive — pulls their llama.cpp fork that
#      supports the i2_s tensor type which mainline llama.cpp removed).
#   2. Downloads the BitNet b1.58-2B-4T GGUF from HuggingFace and verifies SHA256.
#   3. Generates LUT kernels matched to the 2B-4T model dimensions
#      (3200 hidden / 8640 ffn — produces qgemm_lut_3200_8640 and friends).
#   4. CMake configure + build llama-server with BitNet kernels enabled
#      (BITNET_ARM_TL1 on Apple Silicon, BITNET_X86_TL2 on x64).
#
# Final output: commands/env vars for running an external server that
# ANcpLua.Agents.Testing.BitNetFixture can probe via BITNET_URL.
set -euo pipefail

WORK_DIR="${BITNET_WORK_DIR:-$HOME/code/bitnet-runtime}"
MODEL_NAME="BitNet-b1.58-2B-4T"
GGUF_URL="https://huggingface.co/microsoft/bitnet-b1.58-2B-4T-gguf/resolve/main/ggml-model-i2_s.gguf"
GGUF_SHA256="4221b252fdd5fd25e15847adfeb5ee88886506ba50b8a34548374492884c2162"

# Pinned microsoft/BitNet commit. Bump intentionally + re-run this script after verifying
# the codegen + cmake flow still works for the 2B-4T model. Override at the shell to test
# floats: BITNET_COMMIT=HEAD scripts/setup-bitnet-local.sh
BITNET_COMMIT="${BITNET_COMMIT:-01eb415772c342d9f20dc42772f1583ae1e5b102}"

require() {
  command -v "$1" >/dev/null 2>&1 || { echo "[setup-bitnet] missing required tool: $1" >&2; exit 2; }
}
require git
require cmake
require curl
require python3
require shasum

mkdir -p "$WORK_DIR"
cd "$WORK_DIR"

# ── 1. Clone microsoft/BitNet at the pinned commit ─────────────────────────
if [[ ! -d BitNet/.git ]]; then
  echo "[setup-bitnet] cloning microsoft/BitNet @ $BITNET_COMMIT ..."
  git clone https://github.com/microsoft/BitNet.git
  ( cd BitNet && git checkout --quiet "$BITNET_COMMIT" && git submodule update --init --recursive )
else
  cd BitNet
  current=$(git rev-parse HEAD)
  if [[ "$current" != "$BITNET_COMMIT" && "$BITNET_COMMIT" != "HEAD" ]]; then
    echo "[setup-bitnet] BitNet at $current — checking out pinned $BITNET_COMMIT"
    git fetch --quiet origin "$BITNET_COMMIT" || git fetch --quiet origin
    git checkout --quiet "$BITNET_COMMIT"
    git submodule update --init --recursive
  else
    echo "[setup-bitnet] BitNet already at $BITNET_COMMIT"
  fi
  cd ..
fi

# ── 2. Fetch + verify model ────────────────────────────────────────────────
mkdir -p model
if [[ ! -f model/ggml-model-i2_s.gguf ]]; then
  echo "[setup-bitnet] downloading model GGUF (~1.2 GB) ..."
  curl -fL --retry 3 --retry-delay 2 -o model/ggml-model-i2_s.gguf "$GGUF_URL"
fi

actual_sha=$(shasum -a 256 model/ggml-model-i2_s.gguf | awk '{print $1}')
if [[ "$actual_sha" != "$GGUF_SHA256" ]]; then
  echo "[setup-bitnet] FAIL: model SHA256 mismatch" >&2
  echo "  expected: $GGUF_SHA256" >&2
  echo "  actual:   $actual_sha" >&2
  exit 1
fi
echo "[setup-bitnet] model SHA256 verified"

# Symlink into BitNet's expected layout so its tooling finds the GGUF
mkdir -p "BitNet/models/$MODEL_NAME"
ln -sf "$WORK_DIR/model/ggml-model-i2_s.gguf" "BitNet/models/$MODEL_NAME/ggml-model-i2_s.gguf"

# ── 3a. Patch ggml/src/CMakeLists.txt to compile the giant LUT at -O0 ──────
# The generated ggml-bitnet-lut.cpp expands the LUT inline; -O3 needs >2 GB
# working set per clang invocation and OOMs on memory-constrained hosts —
# even -O1 spins for hours under paging. -O0 finishes in seconds; the LUT
# is data tables so optimizer ROI on this TU is negligible. Override with
# `BITNET_LUT_OPT=-O3 scripts/setup-bitnet-local.sh` on a beefy machine.
# (The override has to live next to the ggml target — source-file properties
#  are scoped per CMakeLists.)
LUT_TARGET_CMAKELISTS="BitNet/3rdparty/llama.cpp/ggml/src/CMakeLists.txt"
LUT_PATCH_MARKER="# ANcpLua.Agents: lower LUT optimization"
LUT_DEFAULT_OPT="${BITNET_LUT_OPT:--O0}"
if ! grep -qF "$LUT_PATCH_MARKER" "$LUT_TARGET_CMAKELISTS"; then
  echo "[setup-bitnet] patching $LUT_TARGET_CMAKELISTS to compile LUT at $LUT_DEFAULT_OPT"
  python3 - "$LUT_TARGET_CMAKELISTS" "$LUT_PATCH_MARKER" "$LUT_DEFAULT_OPT" <<'PYEOF'
import sys, pathlib
path = pathlib.Path(sys.argv[1])
marker = sys.argv[2]
opt = sys.argv[3]
text = path.read_text()
needle = "if (EMSCRIPTEN)\n    set_target_properties(ggml PROPERTIES COMPILE_FLAGS \"-msimd128\")\nendif()"
patch = (
    f"{marker}\n"
    "if (NOT DEFINED BITNET_LUT_OPT)\n"
    f"    set(BITNET_LUT_OPT \"{opt}\")\n"
    "endif()\n"
    "set_source_files_properties(\n"
    "    ../../../../src/ggml-bitnet-lut.cpp\n"
    "    TARGET_DIRECTORY ggml\n"
    "    PROPERTIES COMPILE_OPTIONS \"${BITNET_LUT_OPT}\")\n\n"
)
if needle not in text:
    raise SystemExit("[setup-bitnet] FATAL: anchor for LUT patch not found — upstream CMakeLists shape changed")
path.write_text(text.replace(needle, patch + needle))
PYEOF
fi

# ── 3b. Cherry-pick max_completion_tokens parsing into the fork's server ───
# Mirrors ggml-org/llama.cpp PR #19831 (commit 99cc2814, merged 2026-02-23).
# OpenAI deprecated `max_tokens` in favor of `max_completion_tokens` (Sept 2024,
# alongside o1 reasoning models) and the .NET OpenAI SDK 2.x emits only the
# new field. The pinned BitNet fork's llama-server is older than the merge
# and silently ignores the new field, letting generation run to context fill.
# Single-line precedence-chain change. Idempotent: skipped on re-runs.
SERVER_CPP="3rdparty/llama.cpp/examples/server/server.cpp"
if ! grep -q 'max_completion_tokens' "BitNet/$SERVER_CPP"; then
  echo "[setup-bitnet] patching BitNet/$SERVER_CPP to honor max_completion_tokens"
  python3 - "BitNet/$SERVER_CPP" <<'PYEOF'
import sys, pathlib
p = pathlib.Path(sys.argv[1])
old = '        slot.params.n_predict          = json_value(data, "n_predict",         json_value(data, "max_tokens", default_params.n_predict));'
new = (
    '        auto max_tokens                = json_value(data, "max_tokens",        default_params.n_predict);\n'
    '        slot.params.n_predict          = json_value(data, "n_predict",         json_value(data, "max_completion_tokens", max_tokens));'
)
src = p.read_text()
if old not in src:
    raise SystemExit("[setup-bitnet] FATAL: anchor for server.cpp max_completion_tokens patch not found — fork drifted")
p.write_text(src.replace(old, new))
PYEOF
fi

# ── 3c. LUT codegen ────────────────────────────────────────────────────────
cd BitNet
arch=$(uname -m)
if [[ "$arch" == "arm64" || "$arch" == "aarch64" ]]; then
  echo "[setup-bitnet] generating arm64 TL1 kernels..."
  python3 utils/codegen_tl1.py --model bitnet_b1_58-3B \
    --BM 160,320,320 --BK 64,128,64 --bm 32,64,32
  CMAKE_KERNEL_FLAG="-DBITNET_ARM_TL1=ON"
else
  echo "[setup-bitnet] generating x86 TL2 kernels..."
  python3 utils/codegen_tl2.py --model bitnet_b1_58-3B \
    --BM 160,320,320 --BK 96,96,96 --bm 32,32,32
  CMAKE_KERNEL_FLAG="-DBITNET_X86_TL2=ON"
fi

# ── 4. CMake configure + build ─────────────────────────────────────────────
if [[ ! -x build/bin/llama-server ]]; then
  echo "[setup-bitnet] configuring cmake..."
  cmake -S . -B build "$CMAKE_KERNEL_FLAG" \
    -DCMAKE_C_COMPILER=clang \
    -DCMAKE_CXX_COMPILER=clang++ \
    -DCMAKE_BUILD_TYPE=Release

  if command -v sysctl >/dev/null 2>&1; then
    JOBS=$(sysctl -n hw.ncpu)
  else
    JOBS=$(nproc 2>/dev/null || echo 4)
  fi

  echo "[setup-bitnet] building llama-server -j $JOBS (10–15 min on first run)"
  cmake --build build --target llama-server --config Release -j "$JOBS"
else
  echo "[setup-bitnet] llama-server already built — skipping"
fi

[[ -x build/bin/llama-server ]] || { echo "[setup-bitnet] FAIL: llama-server not produced" >&2; exit 3; }

# ── 5. Print env vars/commands for local test use ──────────────────────────
echo
echo "[setup-bitnet] DONE. Export these for the local runner/smoke test:"
echo
echo "  export BITNET_BIN=$WORK_DIR/BitNet/build/bin/llama-server"
echo "  export BITNET_MODEL=$WORK_DIR/model/ggml-model-i2_s.gguf"
echo "  export BITNET_MODEL_SHA256=$GGUF_SHA256   # optional for local script checks"
echo
echo "Start a server for ANcpLua.Agents.Testing.BitNetFixture probe-only mode:"
echo
echo "  $WORK_DIR/BitNet/build/bin/llama-server \\"
echo "    --host 127.0.0.1 --port 8080 \\"
echo "    -m $WORK_DIR/model/ggml-model-i2_s.gguf --ctx-size 2048"
echo
echo "  export BITNET_URL=http://localhost:8080"
