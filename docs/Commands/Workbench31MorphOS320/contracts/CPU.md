# `CPU` contract

Profile: `morphos320`. Goal stage: CC01 and CC17. Recorded: 2026-09-23.

Status: **reference contract only; no implementation or guest-parity claim**.

The selected `MorphOS/C/CPU` member is extent `175236`, 4,123 bytes, SHA-256
`4b66c7a17085a932cbe4255b9a77d92e508de0c7106706701fd8ac1da4a1dc9148a`, with
embedded version `$VER: CPU 50.10 (28.11.2012)`. The [binary audit](../reference-captures/cpu-morphos-binary-audit-20260923.json)
binds that identity and records the installer `+P` event at `hdinstall.fixc:10`.

The [MorphOS Library command index](https://library.morph.zone/Shell_Commands)
describes CPU as displaying CPU information. The packed member yields no
reliable template or diagnostic strings. Do not infer cache/control options,
output fields, or unavailable-feature behavior until a source or guest capture
establishes them.

The implementation must inspect guest CPU state through public guest APIs,
preserve any explicitly requested persistent control changes, and use
invocation-owned output/formatting storage. It must not report host CPU state or
claim PURE/resident status from the installer script alone. Exact parser,
provider, output, lifecycle, differential, and package gates remain open.
