# COPY recursive traversal acceptance trace

Status: reference-derived contract; sibling COPY and MOVE fallback have supplied-DOS normal-command receipts. Full recursive acceptance remains open.

Reference: MorphOS 3.20 c/copy/copy.c, SHA256
13be3cb51223c726aa13abbbac28e9a4545a0a36ec32621262c7bda27a69808f,
PatCopy lines 1226-1392. This supplements Copy.md without closing CC13.

## Wildcard source with ALL

Start at depth 1, first=false, with an owned destination root. Feed directory
folder, file folder/child, then folder with DIDDIR.

| Record | Work before snapshot | Transition after snapshot |
| --- | --- | --- |
| folder | None | Soft-link check; set DODIR; defer folder work and depth increment |
| folder/child | Execute folder work at depth 1, creating/entering destination folder | Increment depth to 2; clear ENTERSECOND; snapshot and defer child |
| folder DIDDIR | Copy saved child at depth 2 | Clear DIDDIR; depth becomes 1; set ENTERSECOND; obtain destination parent; release transient child lock; apply folder metadata relative to parent and restore CurrentDir |
| NoMoreEntries | None for COPY | MatchEnd, free matcher; caller retains destination root |

MOVE and DELETE additionally defer folder work at DIDDIR. At enumeration end,
that work runs after MatchEnd and before freeing matcher storage. Test two
sibling directories so restoration cannot pass merely because enumeration ends.

## Literal first-directory exception

When first=true and the record is a directory, that branch wins even over
DIDDIR. It forces VERBOSE and DODIR but schedules neither work nor depth change.
Do not merge this old-syntax branch with ordinary ALL directory entry.

## Ordering and failure invariants

- Pending work runs before depth advances or path/FIB snapshots are overwritten.
- A failed ParentDir on DIDDIR releases the transient child lock and skips
  metadata. MatchNext still runs. Only if another loop iteration is admitted
  does the saved parent failure become IoErr (InvalidLock if IoErr is zero).
  Do not override end-of-enumeration unconditionally with InvalidLock.
- Parent failure's early return preserves the reference continue, which skips
  first=false. The current helper matches this detail.
- Loop result limits allow WARN normally, only OK with ERRWARN. Deferred work
  retains its own result guards.
- Directory metadata uses the saved FIB and live exit filename. Restore CurrentDir
  even when metadata setters fail.
- Dangling-link rejection can suppress descent while leaving work pending.
- Depth wraps as an unsigned byte, matching CopyData.Deep.

## Required runtime evidence

Use NativeMorphOSCopyCommandEntry and normal ReadArgs ownership. Check actual
payload, source/destination paths, flags, lock ownership, CurrentDir restoration,
metadata order, and shared-image/resource integrity on 68000/020/040. Cover COPY
ALL, MOVE ALL, DELETE ALL, literal first-directory syntax, cancellation and parent
failure. Flat normal-command tests and isolated directory helpers do not prove
this trace. Original-system and pure/resident packaging gates remain separate.

## Current partial execution evidence

The +recursive-command suite now executes sibling COPY, cross-device MOVE
fallback, DELETE, CLONE success and metadata failure, NOPRO, PROX, nested and
literal sources, pending-file completion/failure, parent failure and interleaved
mixed modes. The current aggregate receipt is
`artifacts/qualification-copy-visible-continue/qualification.json`:
111 invocations per CPU, 333 total, including 105 recursive executions.

| Requirement | Current supplied-DOS evidence | Still open |
| --- | --- | --- |
| Sibling descent/ascent | Sibling and nested directories with distinct child payloads, lock identities and restored CurrentDir | Deeper limits and failure paths |
| Deferred metadata | Distinct saved protection/date/comment, child-before-directory order | POSIX dates in normal recursive entry |
| Recursive modes | COPY, MOVE fallback, DELETE child-before-parent | MOVE rename success and mode failures |
| Protection switches | NOPRO suppresses updates, PROX original precedence | Full combinations with CLONE and failures |
| Invocation isolation | COPY/DELETE and MOVE/CLONE interleaving | Real-OS same-SegList qualification |
| Soft-link probing | Relative Lock with matcher parent; failed probes preserve descent; buffer/device cleanup and saved IoErr restoration; positive ReadLink skips descent; normal WARN continues, ERRWARN stops; visible warning and continuation output/flush ordering | Real filesystem link behavior and remaining mode combinations |
| Matcher edge cases | Sibling/nested traversal, literal first directory, terminal matcher error/Break, pending-file error/Break and normal enumeration end; COPY parent failure with zero/nonzero IoErr and enumeration end; QUIET dangling link | Parent failure in MOVE/DELETE, other cancellation boundaries |

All rows remain partial command acceptance. The parser is supplied by the fixture;
these receipts do not establish real DOS ReadArgs grammar, original-system
behavior, pure/resident safety on the OS, or installed image correctness.
