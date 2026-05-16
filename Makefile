# Single entry point for ANcpLua.Agents test runs + BitNet container lifecycle.
#
# `global.json` pins Microsoft.Testing.Platform as the test runner. MTP does
# not recognise the legacy VSTest flags (--logger, --filter, --nologo); the
# filter flag is --filter-method. Route through `make` rather than typing
# `dotnet test` directly so the flag shape stays in one place.

SHELL := bash
.ONESHELL:
.PHONY: help test smoke bitnet-up bitnet-down bitnet-status

BITNET_PORT  ?= 11434
TEST_PROJECT := tests/ANcpLua.Agents.Tests/ANcpLua.Agents.Tests.csproj

help:
	@echo "Targets:"
	@echo "  make test           run the full test suite (smoke test skipped by default)"
	@echo "  make smoke          run the BitNet auto-Docker smoke test (sets BITNET_SMOKE_TEST=1)"
	@echo "  make bitnet-up      start the pinned BitNet container (idempotent)"
	@echo "  make bitnet-down    stop the BitNet container"
	@echo "  make bitnet-status  show container state"
	@echo
	@echo "Env vars consumed by the fixture / scripts:"
	@echo "  BITNET_URL                override endpoint; skips auto-Docker"
	@echo "  BITNET_FIXTURE_NO_DOCKER  truthy to opt the fixture out of auto-Docker"
	@echo "  BITNET_API_PATH           default /v1"
	@echo "  BITNET_MODEL              default bitnet-b1.58-2B-4T"
	@echo "  BITNET_PORT               host port for bitnet-up (default 11434)"

test:
	dotnet test $(TEST_PROJECT)

smoke:
	BITNET_SMOKE_TEST=1 dotnet test $(TEST_PROJECT) --filter-method "*BitNetFixture_AutoDocker*"

bitnet-up:
	BITNET_PORT=$(BITNET_PORT) scripts/bitnet-docker.sh start

bitnet-down:
	scripts/bitnet-docker.sh stop

bitnet-status:
	scripts/bitnet-docker.sh status
