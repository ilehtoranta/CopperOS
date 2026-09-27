# DOSList contract

Profile: morphos320. Goal steps: CC01 and CC11. Recorded: 2026-09-02.

Status: partial release-source contract plus bounded resident native evidence;
no packed-binary or shipping claim. Official 3.20 `c/doslist/doslist.c` is 21,044
bytes with SHA-256 `0ea42d62af32eb6dd455aa28ace38ebf68370b64bca2c519776b8e3f2b976683`.
It defines this DOS ReadArgs template:

```text
NAME,ADDRESS/N,DEVICES/S,VOLUMES/S,ASSIGNS/S,VERBOSE/S
```

The template establishes source-observed selection, numeric-address and verbose
modes. Source inspection observes `AttemptLockDosList` with a read lock for
each selected device, volume, and assign pass; every successful pass iterates
with `NextDosEntry`, detects break, and releases the exact matching read lock
with `UnLockDosList`. Parser storage is released with `FreeArgs`; parse and
command errors route through `PrintFault`.

Source inspection alone does not prove packed-binary correspondence, exact
runtime output, complete error/IoErr behavior, resident purity, source reuse
rights, or runtime parity. The eventual implementation must use public DOS
list-locking/list traversal APIs and release every lock on all exits.

`NativeDosListTraversal` is the first shared implementation component. It
attempts one exact public-DOS read lock, iterates with `NextDosEntry`, polls
Ctrl-C, publishes `ERROR_BREAK`, and releases the matching lock on every
acquired path. Its [qualification script](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_doslist_traversal_native.ps1)
passes six direct supplied DOS-vector calls per CPU: lock refusal, empty and
populated lists, interruption, and repeat/interleaved callers. The two-method
resident HUNKs have no managed runtime features/helpers, external targets,
exception regions, fault sites, or shared-image writes: 68000 is 580 bytes,
SHA-256 `5cefd6abe9af9e877670fcca00e17adcc4eb3e01985492f4ebbca23e436cfa58`;
68020 is 588 bytes, SHA-256
`9d883441c9cbba90f6d24960eccf08f1295adec2e719775164b69c05bc3e5ab2`;
and 68040 is 580 bytes, SHA-256
`1360d1ee46b3196eb2cfed4dd1e489e01abec60fdfe3be830a54a3ff5ef8d867`.
The command body now preserves this template, executes the source-observed
device/volume/assign pass order through public `AttemptLockDosList`,
`NextDosEntry`, and matching `UnLockDosList` calls, supports exact
case-insensitive `NAME` and numeric `ADDRESS/N` selection, and emits the
source-common node and mounted-state rows. Names and message ports are queried
independently through public `GetDosObjectAttrTagList`; attribute failure
preserves fallback node output and mounted-row behavior. Embedded and signal-
port process lookup uses `Exec.TypeOfMem` and public task/message-port fields.
Its resident entry opens `dos.library` version 51 and rejects Workbench or
malformed startup input. The
[latest native qualification receipt](../../../artifacts/doslist-morphos-native-20260923-volume-v1/qualification.json)
passes 34 supplied-vector invocations on each 68000/020/040 target, including
zero/one/three-node device, volume and assign lists, filtering, missing
attributes, mounted/unmounted rows, source-ordered device and volume
`VERBOSE` details, DOS `DateToStr` success/failure, volume disk type and
linked lock rows, deferred and null assign targets, and assign lock/list rows.
Exact original guest comparison, PURE/resident lifecycle, source reuse rights,
package admission and differential behavior remain open.
