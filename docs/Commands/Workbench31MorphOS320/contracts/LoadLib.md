# MorphOS LoadLib contract

Profile: `morphos320`. Goal step: CC18. Status: reference contract in progress.

## Published command summary

The MorphOS Library documents `LoadLib` as loading libraries or classes into
memory, with this ReadArgs-shaped template:

```text
PATH/A/M,VERBOSE/S
```

`PATH` is one or more files to load; `VERBOSE` requests detailed output. See
the [MorphOS Library LoadLib page](https://library.morph.zone/Shell_Commands/LoadLib).

## Evidence boundary

The selected MorphOS 3.20 ISO is available read-only outside the repository at
`D:/TestData/MorphOSReferences/morphos-3.20.iso`. The original packed entry is
2,480 bytes, SHA-256
`9cd6bfa13dcf94d0f934a24bfc31861fb4c637d865ebc7f63aee2466f3e28c80`, at ISO
extent 176140. Direct extraction from that ISO matches the generated inventory,
and `verify-media` passes. The C source archive does not include this command.
The member is MorphOS-packed native code (`7fMOS`); identity is confirmed, but
its implementation has not yet been decoded or guest-observed. Do not infer
that a library open/close pair, `LoadSeg`, or a MUI class acquisition is
equivalent to the original loader. The Library syntax remains a provisional
lead until the original's `?` and runtime behavior are captured.

## Required evidence before implementation is admitted

- [x] Bind the original packed member's size/hash/extent to the frozen ISO and
  reproduce it with the read-only media verifier.
- [ ] Decode or guest-inspect the packed implementation and capture its exact
  `?` help/template.
- [ ] Capture `LoadLib ?`, missing PATH, one and multiple paths, VERBOSE,
  nonexistent files, non-library files, classes, and mixed success/failure on
  the original guest. Record output bytes, result level, and caller `IoErr`.
- [ ] Determine the actual library/class loading API and lifetime behavior;
  cover repeat calls, already-loaded objects, partial failure, and cleanup.
- [ ] Implement with public Kickstart/MorphOS APIs where they preserve the
  captured behavior; qualify pure/resident policy and the 68000/020/040 HUNKs.
- [ ] Clear source rights for any reused code and place the command in the
  correct MorphOS profile image.
