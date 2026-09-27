# `Time` contract

Profile: `morphos320`. Goal stage: CC01 and CC16. Recorded: 2026-09-23.

Status: **reference contract only; no implementation or guest-parity claim**.

## Reference identity

The selected MorphOS 3.20 ISO member is `MorphOS/C/Time`, extent `178504`,
4,999 bytes, SHA-256
`03e00e1a8b35a2b31f785d17c1189228a3960bf345abef4493c3ac2abbe347a5`. Its
embedded version tag is `$VER: Time 1.0 (24.05.26)`. The bounded binary audit is
[`time-morphos-binary-audit-20260923.json`](../reference-captures/time-morphos-binary-audit-20260923.json).

The [MorphOS Library command index](https://library.morph.zone/Shell_Commands)
describes `Time` as measuring the time taken to execute a command. That
description establishes purpose only. The packed member does not expose a
reliable printable template or diagnostics, so no `ReadArgs` grammar, output
format, unit, rounding rule, or command-tail precedence is claimed here.

## Required behavior boundary

The replacement must measure a guest command invocation with a public guest
timer/time source and preserve the nested command's streams, result level,
`IoErr`, and failure precedence. It must not use the host wall clock, busy-loop,
or replace the existing Shell command runner. The exact command-tail grammar
and whether timing surrounds scripts, internal commands, or external commands
must be captured before an implementation is admitted.

The installer script contains a literal `+P` event for `Time` at
`hdinstall.fixc:79`. This is design evidence, not installed AmigaDOS metadata.

## Required qualification gates

Before admission, capture and compare:

1. empty, `?`, malformed, internal, external, and script command tails with
   exact output bytes;
2. timer units, resolution, rounding, zero-duration behavior, nested return
   levels, `IoErr`, and Ctrl-C handling;
3. timer/output/allocation/launch failures and their cleanup order;
4. repeated and overlapping resident calls with no shared-image writes or
   leaks;
5. original MorphOS guest behavior, installed P metadata, package placement,
   and differential evidence.

Until these captures exist, do not add a guessed formatter, timer wrapper, or
`ReadArgs` template, and do not mark `CC16.Time.morphos320` beyond an open
contract state.
