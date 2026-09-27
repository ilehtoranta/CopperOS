# `ShowConfig` contract

Profile: `morphos320`. Goal stage: CC01 and CC17. Recorded: 2026-09-23.

Status: **reference contract only; no implementation or guest-parity claim**.

The selected `MorphOS/C/ShowConfig` member is extent `178220`, 15,811 bytes,
SHA-256 `8bbc8f036854515ed3d9d919e0149208222290a89e92e92551bf24b86c67b711`,
with embedded version `$VER: ShowConfig 50.17 (1.2.2015)`. The [binary audit](../reference-captures/showconfig-morphos-binary-audit-20260923.json)
binds that identity. The selected `hdinstall.fixc` contains no literal
protection event for this member, so its installed PURE/resident classification
is unresolved.

The [MorphOS Library command index](https://library.morph.zone/Shell_Commands)
describes ShowConfig as reporting software versions, hardware, and registered
user information. The packed member exposes no reliable template or
diagnostics. Do not infer providers, fields, output layout, or parser behavior.

The implementation must query guest configuration providers, keep provider and
output ownership invocation-local, and report unavailable fields using captured
reference behavior. Exact grammar, providers, output, lifecycle, differential,
purity, and package gates remain open.
