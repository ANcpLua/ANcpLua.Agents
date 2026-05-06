#!/usr/bin/env bash
# scripts/test-bitnet.sh
#
# End-to-end probe of the BitNet stack:
#   1. Start llama-server (built by setup-bitnet-local.sh) in the background.
#   2. Wait for /health (60 s deadline — first model load is slow).
#   3. POST /v1/chat/completions with a deterministic prompt.
#   4. Assert non-empty response, print it.
#   5. Tear server down.
#
# Used as an opt-in local smoke test before pushing changes that touch the
# BitNetFixture or the BitNet helper scripts. Not intended for normal CI unless
# the runner image deliberately has the large BitNet runtime/model available.
set -euo pipefail

BIN="${BITNET_BIN:-$HOME/code/bitnet-runtime/BitNet/build/bin/llama-server}"
MODEL="${BITNET_MODEL:-$HOME/code/bitnet-runtime/model/ggml-model-i2_s.gguf}"
PORT="${BITNET_PORT:-8089}"  # less likely to clash with a manually-running server on 8080
LOG="/tmp/bitnet-test.$PORT.log"

[[ -x "$BIN" ]]   || { echo "[test-bitnet] $BIN not built — run scripts/setup-bitnet-local.sh first" >&2; exit 2; }
[[ -f "$MODEL" ]] || { echo "[test-bitnet] $MODEL missing — run scripts/setup-bitnet-local.sh first" >&2; exit 2; }

cleanup() {
  if [[ -n "${SERVER_PID:-}" ]] && kill -0 "$SERVER_PID" 2>/dev/null; then
    echo "[test-bitnet] stopping server pid=$SERVER_PID"
    kill "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

echo "[test-bitnet] starting llama-server on 127.0.0.1:$PORT (log → $LOG)"
"$BIN" \
  --host 127.0.0.1 --port "$PORT" \
  -m "$MODEL" \
  --ctx-size 2048 \
  >"$LOG" 2>&1 &
SERVER_PID=$!

echo "[test-bitnet] waiting for /health (60 s deadline)..."
for i in $(seq 1 60); do
  if curl -fsS "http://127.0.0.1:$PORT/health" >/dev/null 2>&1; then
    echo "[test-bitnet] /health OK after ${i}s"
    break
  fi
  if ! kill -0 "$SERVER_PID" 2>/dev/null; then
    echo "[test-bitnet] FAIL: server died before /health came up — last log:" >&2
    tail -40 "$LOG" >&2
    exit 3
  fi
  sleep 1
  if [[ $i -eq 60 ]]; then
    echo "[test-bitnet] FAIL: /health never returned 200 in 60s — last log:" >&2
    tail -40 "$LOG" >&2
    exit 3
  fi
done

echo "[test-bitnet] sending chat completion ..."
RESP=$(curl -fsS -X POST "http://127.0.0.1:$PORT/v1/chat/completions" \
  -H 'Content-Type: application/json' \
  -d '{"model":"bitnet-b1.58-2B-4T","messages":[{"role":"user","content":"Reply with the single word: pong."}],"max_tokens":16,"temperature":0,"seed":42}')

CONTENT=$(printf '%s' "$RESP" | python3 -c '
import json, sys
data = json.load(sys.stdin)
print(data["choices"][0]["message"]["content"])
')

if [[ -z "$CONTENT" ]]; then
  echo "[test-bitnet] FAIL: empty content in response:" >&2
  printf '%s\n' "$RESP" | python3 -m json.tool >&2
  exit 4
fi

echo "[test-bitnet] PASS"
echo "[test-bitnet] model said: $CONTENT"
