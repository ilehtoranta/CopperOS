# Native Desktop Save

Save and Save As extend the [native Editor](NativeDesktopEditor.md), within
the existing Desktop process and its single IDCMP drain/reply/Wait loop.
All file operations use guest DOS. The 512 KiB ROM is derived from
the Editor profile with the MODE_OLDFILE write correction (SHA-256
`07646C8512F129E6ABF5FE38C6D384930CC318C395D7668B383C3D6F49283E0C`). The exact GUI provider
is unchanged, on a 512 KiB Chip + 512 KiB Slow A500 with no Fast RAM. Shipping
firmware and media pins are unchanged.

The combined native evidence contains seven successful checks with zero skips,
including retained Editor regression and a separate writable save/recovery rerun
with fresh-ROM byte-exact persistence. The earlier default run retains its
close-and-save test timeout; the functional exit wait and clean-close settle helper were corrected. See the
[retained evidence](../../CopperScreen/docs/engine/COPPERSTART_COPPEROS_SAVE_2026-10-19.md).
This is development functional qualification, not shipping-image or performance admission.

## Controls and ownership

Ctrl-S or the Save button saves a named document. A new document opens Save As.
Shift-Ctrl-S always opens Save As: type a path, Ctrl-U clears it, Backspace/Delete
remove its final byte, Enter submits and Esc cancels. The field holds at most
255 bytes; individual components and filenames must fit 30 bytes. Input uses
the Editor's byte-oriented Latin-1 convention. Oversized names are refused,
never silently truncated to an existing filename.

Bare names resolve relative to the document's retained source directory, then
the retained Files directory for a new document, or SYS: when neither exists.
The folder shown above the document is display information; ordinary Save uses
the owned directory lock, without reconstructing its identity from a volume
display name. Paths use AmigaDOS conventions, including `/` for a parent drawer.

Save As to an existing bounded plain file asks for Y to replace or Esc to cancel.
The decision retains a snapshot and shared file lock. It is checked again before
replacement. A write-protected destination is refused; Save As can preserve the
edits under a different name. Ordinary Save also retains a shared source-file lock and an exact
snapshot of source bytes and public metadata. Editing holds no file handle.
Shared writes can still occur; changed bytes or metadata cause Save to refuse
replacement. A source that can no longer be verified requires reopen or an
explicit Save As decision.

The common unsaved-changes decision now offers Save, Discard and Cancel. A
failed Save retains that decision and displays its error. Cancel in the unsaved decision retains
the bytes, cursor and dirty state and returns to its original view. Esc in Save
As returns to that decision when one is pending. A successful
Save performs the pending close/New/Open action. Once quit is accepted, the
process replies to queued messages without accepting further edits.

## Replacement and recovery

1. Claim a fresh `CopperSave0000` through `CopperSave0031` drawer in the target
   directory with CreateDir. Existing names, including other recovery drawers,
   are skipped. Creation failure changes no document metadata.
2. Write `New` inside that drawer, in chunks of at most 1,024 bytes. Handle short
   writes, reject zero progress, check Close, then read the complete file back
   and compare its exact bytes with the document. No terminating NUL is saved.
3. Preserve the replaced file's comment and protection flags. Default empty/zero
   metadata needs no extra writes. Write `Target`, containing the destination
   leaf name, only after `New` has closed and passed verification.
4. Compare the source/confirmed target with its snapshot. Release its file locks
   for Rename, move it to `Original` inside the recovery drawer, then check that
   moved file against the snapshot again. Directory ownership stays retained.
5. Rename `New` to the destination. Rename's refusal to replace an existing name
   protects a destination that appears in the meantime. On failure after moving
   the original, attempt `Original` back to the destination. Preserve the edited
   buffer and recovery drawer on failure, including failed restoration.
6. Successful promotion commits the new filename, directory, snapshot and clean
   state. Reacquire the source file lock. If that fails, report that the saved
   file needs reopening before another ordinary Save. Remove the backup and
   manifest, then the empty drawer. A cleanup error reports a saved file with a
   retained recovery drawer; it does not pretend the committed write failed.

CreateDir reserves the application's transaction namespace. This is a recovery
protocol using multiple DOS operations, not an atomic filesystem transaction or
an exclusion mechanism against arbitrary writes inside that drawer. Source
checks cover the retained lock and the checks around rename; applications must
still cooperate with file sharing. FFS power-loss consistency needs separate
qualification.

After an error, the Editor shows the recovery drawer and its containing folder.
Do not remove a retained drawer until its files have been inspected. `New` can
contain partial data when writing failed early. `Target` is created after the
edited copy has passed Close and read-back verification. `Original`, when
present, is the moved original. After failed restoration it is left unlocked so
recovery tools can move it back. Opening `New` through Files and using Save As
can preserve the edited copy under a fresh name. A plain-text `Original` can also be copied back through Editor and Save As.
Restoring it by rename, or restoring a binary original from an explicit
overwrite, requires native DOS tools. A dedicated recovery browser is still
future work.

## Bounded memory

| Owner | Bytes |
| --- | ---: |
| Editor document, unchanged 16 KiB text cap | 16,936 |
| One source/target snapshot, including FIB, identity-lock reference and oversize byte | 16,664 |
| Save workspace | 2,208 |
| Save buffers at the largest overwrite stage: document + four snapshots + workspace | 85,800 |
| Open buffers with a retained failed Save workspace and old/candidate documents | 102,996 |
| Editor font | 2,816 |
| Files cache | 4,384 |
| Desktop stack / root state / cleanup reserve | 32,768 / 256 / 8,192 |

These are explicit application allocations, not measured whole-machine peaks.
They exclude loader/handler ownership, display and Layers structures, other OS
allocations and allocator fragmentation. Largest free blocks and complete
open/save peaks remain release measurements.

## Reproduction and remaining work

[`Test-NativeCopperOsSave.ps1`](../../CopperStart/build/Test-NativeCopperOsSave.ps1)
freezes the qualified Editor fixture, current app/test source and exact producer
inputs. Its default run includes the previous Editor/buffer/glyph and missing-GUI
checks as well as writable save/recovery. `-SaveOnly` is narrower coverage and
does not replace the complete run. See the
[producer guide](../../CopperStart/Docs/NativeCopperOsSave.md) for admission and
retained results and exact reproduction inputs.

The [recovery and busy follow-up](NativeDesktopRecovery.md) adds read-only
inspection, unsaved text copies and cooperative refresh/input/close handling.
Individual DOS calls still block. Files mutation/paging, iterator/save I/O speed,
whole-machine memory/fragmentation and latency measurements, sustained lifecycle
stress, workspace checkpoints and shipping-image admission remain. Functional
replay is not a response-time acceptance result.

The DOS contracts for [Open](https://developer.amigaos3.net/autodocs/dos.library/Open.html),
[Write](https://developer.amigaos3.net/autodocs/dos.library/Write.html),
[Close](https://developer.amigaos3.net/autodocs/dos.library/Close.html),
[CreateDir](https://developer.amigaos3.net/autodocs/dos.library/CreateDir.html),
[Rename](https://developer.amigaos3.net/autodocs/dos.library/Rename.html),
[Lock](https://developer.amigaos3.net/autodocs/dos.library/Lock.html) and
[SameLock](https://developer.amigaos3.net/autodocs/dos.library/SameLock.html)
define the native call ABI, sharing and collision behavior used here.
