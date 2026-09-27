# `LoadMonDrvs` contract

Profile: `morphos320` (`CC20.LoadMonDrvs.morphos320`). Goal stage: CC20.
Recorded 2026-09-22. This is a packed-reference boundary, not a shipping
implementation.

## Reference identity

The MorphOS 3.20 ISO member is a 2,279-byte packed native command at extent
176144, with version tag `LoadMonDrvs 50.1 (14.7.03)` and SHA-256
`c33c5263f6edd1c1ff11738558492d5cbfe371e6117383459b4c319082dea384`. The
complete bounded audit is
[`loadmondrvs-morphos-binary-audit-20260922.json`](../reference-captures/loadmondrvs-morphos-binary-audit-20260922.json).

## Invocation boundary

The available command-reference grammar is:

```text
FROM/K,EXCEPT
```

It is a candidate until the packed command or a MorphOS guest confirms the
parser and diagnostics. Preserve DOS `ReadArgs` ownership and reject invented
switches. With no `FROM`, the default directory is `DEVS:Monitors`; `FROM/K`
selects an alternate directory and `EXCEPT` excludes one driver file name.

## Driver ownership

The command starts all discoverable monitor drivers in the selected directory.
The replacement must enumerate through guest DOS APIs, load each driver through
the guest resident/segment path, and retain only successfully initialized code.
It must not use a host directory, host dynamic loader, synthetic monitor node or
unverified ROM address. Driver ordering, duplicate handling, exact file-name
comparison and partial-failure precedence must be captured before coding the
resident body.

The bounded candidate is present in
[`NativeMorphOSLoadMonDrvsCommand.cs`](C:/D-drive/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSLoadMonDrvsCommand.cs)
with a private resident entry in
[`NativeMorphOSLoadMonDrvsEntry.cs`](C:/D-drive/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSLoadMonDrvsEntry.cs).
It uses invocation-owned `ReadArgs` results and workspace, guest
`MatchFirst`/`MatchNext`/`MatchEnd`, `NameFromLock`/`AddPart`, raw guest
`LoadSeg`/`UnLoadSeg`, resident discovery and `InitResident`; it retains a
successfully initialized segment and unloads failed discovery/initialization.
The three-CPU static receipt is
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/loadmondrvs-morphos-native-static-20260922-v1/qualification.json):
68000/020/040 HUNKs, 17 reachable methods, zero managed runtime features,
helpers, external targets, exception regions or fatal fault sites. This is
static ABI evidence only and does not establish packed or guest equivalence.

A supplied-vector resident fixture now covers default and `FROM`/`EXCEPT`
`ReadArgs` ownership, parser failure, missing DOS, matcher empty/no-match
cleanup, successful resident discovery/initialization, failed initialization
and segment unload, repeated/interleaved invocations, and zero shared-image
writes on 68000/68020/68040. Its receipt is
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/loadmondrvs-morphos-native-entry-20260922-v3/qualification.json).
This proves the candidate's invocation boundary and segment cleanup shape only;
it does not establish packed parser, provider-backed monitor effects, or guest
equivalence. The current matrix exercises case-insensitive `EXCEPT` against an
actual matched file, allows a differently named driver through `EXCEPT` using
an alternate `FROM` directory, and skips a matched directory before `LoadSeg`.

## 2026-09-25 resident-walk safety checkpoint

The candidate resident walk now validates the size word, segment start and
declared final byte through Exec `TypeOfMem`, checks arithmetic before deriving
the HUNK end, and keeps the six-byte match-word/match-tag read within each HUNK.
It follows ordinary multi-HUNK lists and uses a constant-space cycle check, so
it does not impose a fixed segment-count limit. The refreshed three-CPU receipt
is
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/loadmondrvs-morphos-native-entry-20260925-multiple-match-v7/qualification.json).
Seventeen supplied invocations per CPU cover default and alternate directory
paths, case-insensitive and nonmatching `EXCEPT`, an excluded first match
followed by a loaded second driver, directory-entry skipping, second-HUNK
resident discovery, undersized, overflowing and unmapped extents, cyclic
links, initialization and cleanup, repeat/interleaved execution, and no
shared-image writes. Static analysis reports 22 reachable methods and zero
managed runtime features or helpers, external native targets, exception
regions or fatal machine-fault sites.

These are replacement-side vectors. They do not prove that the packed command
uses the same resident walk or establish real monitor initialization, guest
equivalence, PURE metadata, installed lifecycle, licensing or package
admission. The MorphOS row remains partial/open.

The inventory records a required PURE event from `hdinstall.fixc` event 39.
That is an installed-design requirement, not proof of the packed member's
current metadata. The replacement must preserve PURE/resident constraints and
prove that a driver segment is not unloaded while a monitor node still points
to it.

## Required qualification gates

Before admission, qualify:

1. no-option default, `FROM`, `EXCEPT`, empty and malformed arguments;
2. directory lock/matcher failures, case and path handling, ordering and
   excluded-driver behavior;
3. driver `LoadSeg`/resident discovery/initialization success and every
   partial-failure cleanup path;
4. duplicate and repeated startup, concurrent callers and Ctrl-C behavior;
5. exact result, `IoErr`, diagnostic and monitor-list effects on a real
   MorphOS guest; and
6. PURE/lifecycle, installed overlay, licensing, package placement and
   differential evidence.

Until those gates pass, keep the ledger row partial/open and do not add a
host-backed or no-op loader.
