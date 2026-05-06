# Samples And Cookbook Harvest

The `MAF.Advanced.Patterns` showcase samples should not be copied as runnable projects. The useful migration output is documentation about consumer patterns:

- compose workflows first, then expose them through channel-specific hosts;
- wrap tool execution with policy, capability, budget, and tracing helpers;
- keep checkpointing explicit and package-local;
- label preview/RC/alpha endpoints by package channel.

Concrete sample projects can be added later, but only if they use `ANcpLua.Agents` packages directly and avoid qyl-specific configuration names.

## Script Note

`MAF.Advanced.Patterns/scripts` was local BitNet support tooling, not sample
surface. The scripts now live under `ANcpLua.Agents/scripts` and should remain
opt-in developer tooling.
