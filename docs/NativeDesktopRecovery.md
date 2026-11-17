# Native Desktop busy feedback and recovery

This continues [native Save](NativeDesktopSave.md) on the same qualified standalone
CopperStart ROM and GUI provider. The ROM remains 524,288 bytes. The development
machine remains PAL OCS MC68000 with 524,288 bytes Chip, 524,288 bytes Slow and no
Fast RAM. Shipping firmware and workspace admission are separate.

## Disk work and messages

The Desktop task copies each IDCMP message's fields and replies before handling
its action. A disk operation paints a busy footer before calling DOS. Between
bounded read/write chunks, directory entries and save stages, it yields and drains
up to 32 messages from the same port. Refresh messages repaint the retained view
and busy footer through BeginRefresh/EndRefresh. Busy input is replied and ignored,
so queued typing cannot edit a new document or activate a later directory entry.
Close requests wait for disk work to complete, then enter the usual unsaved-edit
decision or close a clean document. A close arriving during destination lookup
also takes effect if lookup ends at a Save As/overwrite prompt. Nested save/open
work shares the busy scope. There is still one task, one port and one Wait site.

DOS calls remain synchronous: the app cannot drain IDCMP while a single packet is
blocked in the handler. This implements visible feedback and cooperative handling,
not a background I/O worker, cancellation of an in-progress replacement, a faster
filesystem or response-time admission. The 32-entry Files cache remains unchanged.
The qualified DOS iterator still rescans prior directory entries.

## Recovery UI

Select a CopperSave0000 through CopperSave0031 drawer in Files and press V
or click the recovery hint in the footer.
After a failed Save or cleanup warning, click the recovery footer or press Ctrl-R
in Editor to inspect that exact retained drawer. The view owns its own directory
lock and displays the available New and Original files. Target is read as a bounded
optional leaf-name hint; missing, invalid, partial or unreadable hints are shown as
unavailable. A hint does not establish current copy completeness.

Arrows or Tab select; Enter or a row click opens a text copy. The normal 16 KiB
limit and byte validation apply. Existing dirty text uses Save/Discard/Cancel.
A successful open creates a modified, unnamed Recovered document with no archive
source lock or directory identity: Save requires a destination choice. Binary or
oversized files remain available to DOS tools. Recovery does not rename, delete,
repair or automatically overwrite any file. The UI warns that copies may be
incomplete and must be checked before saving. It does not claim atomic or
power-loss-safe replacement.

Failed allocation, drawer lock, examination or name lookup disposes only the
candidate and retains the previous Files/recovery/document state. Cancelled
replacement and failed text validation retain the old document. Every recovery
lock/workspace is released at replacement or Desktop shutdown.

## Allocation bounds

The root remains 256 bytes; two previously unused words hold busy state and the
recovery owner. One recovery workspace is 832 bytes; replacing an inspection can
briefly retain two (1,664 bytes). Busy handling adds no heap workspace. The existing
32 KiB stack, 8 KiB reserve, 4,384-byte Files cache, 16 KiB document capacity and
save snapshots remain bounded as documented in the Save guide. A retained recovery
workspace adds 832 bytes to those application bounds; inspection replacement adds
1,664 bytes while both candidates coexist. These figures exclude loader, handler,
GUI/OS ownership and fragmentation and are not whole-machine peak measurements.

## Qualification and next work

Use [Test-NativeCopperOsRecovery.ps1](../../CopperStart/build/Test-NativeCopperOsRecovery.ps1)
with the exact Save ROM/runtime and Editor GUI/frozen fixture inputs. The default
selects the previous seven Editor/Save facts plus a native recovery/busy journey;
-RecoveryOnly selects only that new journey and is narrower coverage. The script
freezes source and records hashes. It adapts only the old provider fixture's Files
footer hint, retaining its 448-byte owner layout.

The [native DOS I/O follow-up](../../CopperStart/Docs/NativeCopperOsIo.md) now
qualifies directory cursors, bitmap-word work and internal sector copies within
the same limits, retaining the original-fixture Save/recovery checks. The
[guest latency/heap study](../../CopperStart/Docs/NativeCopperOsMeasurements.md)
now measures one instrumented journey with 134,317 fields of passive/control
parity and stable post-stop heap geometry. Its modeled guest intervals expose
native I/O and drawing latency as priorities before more features. Remaining work includes
those optimizations, broader queue-pressure/installed measurements, bounded Files mutation/paging,
100-cycle and sustained stress, Shell/Demo launch-return and shipping pair/workspace
admission. A functional replay is not a throughput or response-time acceptance.

The [2026-10-20 consumer qualification](../../CopperScreen/docs/engine/COPPERSTART_COPPEROS_RECOVERY_2026-10-20.md) records eight passing facts
with zero skips, exact inputs, observed memory and the remaining release limits.

Its [subsequent I/O evidence](../../CopperScreen/docs/engine/COPPERSTART_COPPEROS_IO_2026-10-20.md)
records nine passing facts with zero skips in one default run, including the
original fixture and cold-read hashes. Earlier altered-fixture results and failed
probes remain recorded; shipping admission is unchanged.

The [subsequent latency and heap evidence](../../CopperScreen/docs/engine/COPPERSTART_COPPEROS_MEASUREMENTS_2026-10-20.md)
records the exact routine boundaries, 218,696-byte free minimum, 96,040-byte
largest-block minimum, and retained failed observer attempts. The ROM remains
512 KiB and the fitted A500 remains 1 MiB; this is not shipping acceptance.

The [native track-cache follow-up](../../CopperStart/Docs/NativeTrackCache.md)
advances only the development ROM pin; GUI/Home/Desktop bytes are unchanged.
All original-fixture save/Close/rollback and recovery checks pass. The repeated
guest study reduces 33-byte / 16 KiB replacement intervals by 14.8% / 10.7% and
preserves the same heap minima and stable post-stop geometry. Its
[consumer record](../../CopperScreen/docs/engine/COPPERSTART_COPPEROS_TRACK_CACHE_2026-10-20.md)
retains the unchanged baseline. Further I/O/drawing latency, installed response,
sustained use and shipping admission remain open.

The [MFM follow-up](../../CopperStart/Docs/NativeMfmEncoding.md) retains all
write/Close/rollback and recovery checks with unchanged program payloads.
Replacements fall to 214.138 / 679.906 modeled guest seconds for 33 bytes /
16 KiB; heap minima and all eleven post-stop heaps remain stable. The development
ROM pin advances after qualification; installed response, sustained use and
shipping admission remain separate.
