# `WaitX` contract

Profile: `morphos320`. Goal stage: CC01 and CC16. Recorded: 2026-09-23.

Status: **reference contract only; no implementation or guest-parity claim**.

The selected `MorphOS/C/WaitX` member is extent `178824`, 4,230 bytes, SHA-256
`88f59afb972fb0bc410eb720b3b25e5baa50e401cb9ab1cc7ac331568e075e03`, with
embedded version `$VER: WaitX 51.1 (26.03.2018)`. The [binary audit](../reference-captures/waitx-morphos-binary-audit-20260923.json)
binds that identity and records the installer `+P` event at `hdinstall.fixc:87`.

The [MorphOS Library command index](https://library.morph.zone/Shell_Commands)
describes WaitX as waiting for an amount of time and then executing a command
line. The packed member exposes no reliable template or diagnostic strings, so
the delay grammar, nested command boundary, output forwarding, and result/
`IoErr` precedence are not inferred.

The implementation must use guest scheduler/timer APIs and the existing Shell
launch owner. It must preserve nested command streams and return state, avoid
host sleeps and busy loops, and keep all timer/process/request storage
invocation-owned. Exact parser, timing, failure, lifecycle, purity, package,
and differential gates remain open until controlled guest captures exist.
