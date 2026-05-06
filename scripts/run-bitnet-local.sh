#!/usr/bin/env bash
# scripts/run-bitnet-local.sh
#
# Foreground llama-server with the BitNet b1.58-2B-4T model, OpenAI-compatible.
# Pair with the fixture's probe-only mode (BITNET_URL=http://localhost:8080).
#
# Run scripts/setup-bitnet-local.sh once first to build the binary + fetch model.
set -euo pipefail

BIN="${BITNET_BIN:-$HOME/code/bitnet-runtime/BitNet/build/bin/llama-server}"
MODEL="${BITNET_MODEL:-$HOME/code/bitnet-runtime/model/ggml-model-i2_s.gguf}"
PORT="${BITNET_PORT:-8080}"
CTX_SIZE="${BITNET_CTX_SIZE:-2048}"

[[ -x "$BIN" ]]   || { echo "[run-bitnet] $BIN not built. Run scripts/setup-bitnet-local.sh first." >&2; exit 2; }
[[ -f "$MODEL" ]] || { echo "[run-bitnet] $MODEL missing. Run scripts/setup-bitnet-local.sh first." >&2; exit 2; }

# Free the port if a stale instance is squatting on it
if command -v lsof >/dev/null 2>&1; then
  existing=$(lsof -ti :"$PORT" 2>/dev/null || true)
  if [[ -n "$existing" ]]; then
    echo "[run-bitnet] killing stale process on :$PORT (pid $existing)"
    kill -9 $existing 2>/dev/null || true
    sleep 1
  fi
fi

echo "[run-bitnet] starting llama-server on 127.0.0.1:$PORT (ctx=$CTX_SIZE)"
exec "$BIN" \
  --host 127.0.0.1 --port "$PORT" \
  -m "$MODEL" \
  --ctx-size "$CTX_SIZE"
