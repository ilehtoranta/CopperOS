# `Uptime` contract

Profile: `morphos320`. Goal stage: CC01 and CC16. Recorded: 2026-09-23.

Status: **reference contract only; no implementation or guest-parity claim**.

## Reference identity

The selected MorphOS 3.20 ISO member is `MorphOS/C/Uptime`, extent `178788`,
2733 bytes, SHA-256
`1b1c87202fca5c789a1c34c700d72d879367bb9fb76bd2f23c168d60bdc60c7e`. Its
embedded version tag is `$VER: uptime 50.5 (9.2.2014)`. The bounded binary audit
is [`uptime-morphos-binary-audit-20260923.json`](../reference-captures/uptime-morphos-binary-audit-20260923.json).

The [MorphOS Library reference](https://library.morph.zone/Shell_Commands/Uptime)
describes `Uptime` as having **no template** and
displaying the time elapsed since MorphOS was rebooted. This is the invocation
boundary only; it does not establish the output bytes, localization, timer
source, rounding, or result/`IoErr` behavior.

## Required behavior boundary

The replacement must use a public Kickstart/MorphOS time source and write to
the borrowed `Output()` stream. It must not require a command tail, invent a
ReadArgs grammar, or use host wall-clock time. The implementation needs an
invocation-owned formatting buffer and must preserve the original resident/PURE
classification, cleanup order, and failure behavior once captured.

The installer script contains a literal `+P` event for `Uptime` at
`hdinstall.fixc:83`. This is design evidence, not installed AmigaDOS metadata.

## Required qualification gates

Before admission, capture and compare:

1. empty, `?`, and non-empty command tails, including exact output bytes;
2. zero, sub-second, minute, hour, day, and long-uptime formatting and rounding;
3. timer/system-time provider, allocation, output, Ctrl-C, and missing-DOS
   failures with exact result and `IoErr` precedence;
4. repeated and overlapping resident calls with no shared-image writes or leaks;
5. original MorphOS guest behavior, installed P metadata, package placement,
   and differential evidence.

Until these captures exist, do not add a guessed formatter or mark
`CC16.Uptime.morphos320` beyond an open contract state.
