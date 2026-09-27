# MorphOS FlushLib contract

Profile: `morphos320`. Goal step: CC18. Status: reference contract in progress.

## Published command summary

The MorphOS Library documents `FlushLib` as removing unused libraries from
memory, with this ReadArgs-shaped template:

```text
LIBRARY/A/M,ONCE/S,QUIET/S
```

`LIBRARY` names one or more libraries to flush, `ONCE` requests one flush per
library, and `QUIET` suppresses output. See the
[MorphOS Library FlushLib page](https://library.morph.zone/Shell_Commands/FlushLib).

## Evidence boundary

The selected MorphOS 3.20 ISO is available read-only outside the repository at
`D:/TestData/MorphOSReferences/morphos-3.20.iso`. The original packed entry is
3,755 bytes, SHA-256
`05c06f989d58d3eca7fb34d5eec2ddb0feea894b126e6857880a0542d7204140`, at ISO
extent 175432. Direct extraction from that ISO matches the generated inventory,
and `verify-media` passes. The C source archive does not include this command.
The member is MorphOS-packed native code (`7fMOS`); identity is confirmed, but
its implementation has not yet been decoded or guest-observed. The summary
does not define wildcard matching, library lookup scope, how `ONCE` differs
from the default, messages, per-library failure handling, or the precise
public API used to expunge libraries. Those details must not be guessed.

## Required evidence before implementation is admitted

- [x] Bind the original packed member's size/hash/extent to the frozen ISO and
  reproduce it with the read-only media verifier.
- [ ] Decode or guest-inspect the packed implementation and capture its exact
  `?` help/template.
- [ ] Capture help/parser forms, multiple names, ONCE/QUIET, exact-name and
  wildcard matches, zero-open and in-use libraries, missing names, repeat
  flushes, and mixed success/failure. Record output, result level, and `IoErr`.
- [ ] Establish whether expunge behavior uses standard library lifecycle APIs
  or MorphOS-specific policy, and verify it cannot invalidate active callers.
- [ ] Implement with public APIs where parity permits; test concurrent library
  opens, resident reuse, cleanup, purity, and 68000/020/040 artifacts.
- [ ] Clear source rights for any reused code and place the command in the
  correct MorphOS profile image.
