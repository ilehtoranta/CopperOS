# `Stat` contract

Profile: `morphos320`. Goal stage: CC01 and CC17. Recorded: 2026-09-23.

Status: **reference contract only; no implementation or guest-parity claim**.

The selected `MorphOS/C/Stat` member is extent `178468`, 4,319 bytes, SHA-256
`15eaeb422d7046b7064cf8790f4bb3e3da45dd6767e7464077513c600a3955a8`, with
embedded version `$VER: stat 50.2 (21.5.05)`. The [binary audit](../reference-captures/stat-morphos-binary-audit-20260923.json)
binds that identity and records the installer `+P` event at `hdinstall.fixc:72`.

The [MorphOS Library command index](https://library.morph.zone/Shell_Commands)
describes Stat as displaying system statistics since boot. The packed member
does not expose a trustworthy template or diagnostics, so counters, units,
reset/overflow rules, output, and command-tail syntax remain unobserved.

The implementation must read guest statistics through public Exec/DOS/provider
APIs, format through borrowed streams with invocation-owned storage, and avoid
host metrics or shared writable state. Exact parser, provider, output,
lifecycle, differential, and package gates remain open.
