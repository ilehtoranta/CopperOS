# MorphOS iKill contract

Profile: `morphos320`. Goal step: CC18. Status: reference contract in progress.

## Published command summary

The MorphOS Library describes `iKill` as killing a named program and trying to
free its resources. Its documented argument is:

```text
TASKNAME
```

When no argument is supplied, the pointer becomes a crosshair: left-click
selects the program to kill and right-click cancels. See the
[MorphOS Library iKill page](https://library.morph.zone/Shell_Commands/iKill).

## Evidence boundary

This establishes a documented dual mode, but not the exact MorphOS 3.20
argument template, task-name matching, selection mechanism, safety rules,
resource cleanup, messages, or result/error behavior. The selected MorphOS 3.20
ISO is available read-only outside the repository at
`D:/TestData/MorphOSReferences/morphos-3.20.iso`. Its original packed member is
18,014 bytes, SHA-256
`8c06ee672ff86c02f8f99b176435b9fe313400da2ae5f95d51bcaf2c2e10258b`, at ISO
extent 178944. Direct extraction from that ISO matches the generated inventory,
and `verify-media` passes. The C source archive does not include this command.
The member is MorphOS-packed native code (`7fMOS`); identity is confirmed, but
its implementation has not yet been decoded or guest-observed. The older
MorphOS release notes describe it as working better with multiple screens,
confirming that screen selection is part of its behavior but not specifying
the complete contract.

## Required evidence before implementation is admitted

- [x] Bind the original packed member's size/hash/extent to the frozen ISO and
  reproduce it with the read-only media verifier.
- [ ] Decode or guest-inspect the packed implementation and capture its exact
  `?` help/template, no-argument, named-task, missing-task, and ambiguous-name
  behavior.
- [ ] Probe the crosshair path on disposable MorphOS guests: left-click target,
  right-click cancel, multiple screens, windows owned by one task, desktop and
  system windows, and target exit during selection.
- [ ] Identify the original application/task targeting and cleanup APIs.
  Preserve protections against killing system-critical tasks and never infer
  that generic task termination safely releases application resources.
- [ ] Record exact output, result, `IoErr`, pointer/screen effects, and cleanup;
  then implement and qualify the complete named and interactive paths.
- [ ] Clear source rights for any reused code and place the command in the
  correct MorphOS profile image.
