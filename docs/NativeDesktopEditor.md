# Native Desktop Editor

Editor is the third view in the native Desktop process, sharing Home/Files'
window, renderer and single IDCMP drain/reply/Wait loop. Its development profile
uses a 512 KiB ROM with keyboard acknowledgement in VBlank and an ordered-key
GUI provider on the 512 KiB Chip + 512 KiB Slow A500 profile.
All document I/O goes through native DOS; no host filesystem or UI service is
used. The shipping ROM/media pin is separate from this development overlay.

## Text and memory

One document owns a 16,936-byte allocation: a 544-byte header/scratch area, a
16,384-byte text buffer, a one-byte oversized-read probe and alignment padding.
The header retains cursor/viewport state, dirty state, the source filename,
directory display path and an owned duplicate of the source directory lock.
Files keeps its own lock and selection independently. Editing keeps no open
file handle. The [Save follow-up](NativeDesktopSave.md) also retains a shared
source-file lock and a bounded source snapshot; its native qualification
is recorded with the [Save development profile](NativeDesktopSave.md).

The format is byte-oriented Latin-1 text. Tab, LF and CR are accepted; NUL,
other C0 controls, DEL and C1 controls are rejected during open. File bytes and
mixed line endings are preserved. Newline insertion uses the first detected
line-ending style, defaulting to LF for new/empty documents. CRLF is one unit
for cursor movement, Backspace and Delete. Tabs occupy four-column stops.

The application owns an original fixed-width 8×8 TextFont and bitmap/CharLoc
tables in 2,816 bytes. ASCII lowercase has its own glyphs; Latin-1 letters and
their accents preserve case. Some non-letter Latin-1 symbols use a box glyph;
their stored bytes are preserved. UTF-8/Unicode conversion is not implemented.
The public SetFont call temporarily binds this font for Editor painting and
restores the window's original font afterward. The font remains owned until
Desktop shutdown, when it is restored/detached before its memory is freed.

Native byte decoding and glyph dispatch use explicit unsigned values. The
compiled MC68000 checks exercise high-bit Latin-1 values as well as ASCII, so
host-only font tests are not mistaken for native rendering coverage.

## Controls

Home's Editor tile shows the existing document or creates an empty one. Files'
Enter/click opens the selected file. Editor displays 16 lines × 72 columns,
with horizontal scrolling for long lines. Ordinary keys insert text, Return
inserts a newline, Tab inserts a tab, Backspace deletes the preceding unit and
Delete removes the following one. The 16 KiB cap refuses an entire insertion
without changing bytes or cursor, including a two-byte CRLF insertion.

Arrows move the caret; Shift-Up/Down moves by 16 logical lines, Shift-Left/Right
and Ctrl-A/E move to line start/end, and Ctrl-T/B move to document start/end.
Clicking text places the caret. Up/Down preserve the preferred visual column
across short lines. The old/new caret rows and affected text rows are repainted;
viewport changes and refresh repaint the complete visible area. Short drawing
runs yield cooperatively so physical key releases can be drained during custom
font painting. Native buffer scans and overlapping shifts also yield at
128-byte boundaries; the allocation-free buffer probes leave that native
scheduling option disabled.

Escape returns to Files, or Home if Files has not been opened. Header Home/Files
switches views while retaining the document. New/Ctrl-N requests a new document.
Replacing a dirty document or exiting from any view offers Save, Discard or
Cancel; Escape cancels that decision. A failed replacement keeps the previous
dirty document even after Discard was selected. Ctrl-S saves; Shift-Ctrl-S opens
Save As. See the [save protocol and recovery guide](NativeDesktopSave.md).

## Transactional open and rollback

Open allocates a candidate, duplicates the source drawer lock, copies its source
metadata, temporarily borrows that lock as CurrentDir for Open(MODE_OLDFILE),
and restores the previous CurrentDir immediately, including on Open failure.
It reads bounded chunks until EOF, handles short reads, and reads at most one
byte past the 16 KiB limit into the reserved probe byte. A read error, oversize
document, unsupported control byte or failed Close rejects the candidate.

The initial read handle is closed exactly once. The Save follow-up then captures
and checks an exact source snapshot and retains its shared file lock before
publication. A complete, validated, closed candidate and a usable font are required before swapping the document
pointer. Failure releases only candidate resources and returns to Files with
the old document, its Editor status, directory, selection and scrolling intact.
Files displays its own open error. For a dirty prior
document, Cancel performs no open; Discard permits a candidate replacement but
does not destroy the old state until commit.

At most two document allocations (33,872 bytes), three application-owned drawer
locks (Files plus old/new documents) and one file handle are live during open.
The Save follow-up adds source snapshots/file locks and can retain a failed save
workspace until replacement commits; its larger explicit bounds are recorded
in the [save memory table](NativeDesktopSave.md#bounded-memory).
The font is shared between document candidates. The existing 32 KiB process
stack, 256-byte root state, 8 KiB cleanup reserve and bounded Files cache remain.
Shutdown releases the reserve before freeing document/lock/font and GUI owners.

The replay entry is
[`Test-NativeCopperOsEditor.ps1`](../../CopperStart/build/Test-NativeCopperOsEditor.ps1).
It freezes source/tool inputs and verifies exact ROM/provider hashes. It keeps
freestanding compilation, compiled-buffer/glyph checks and full ROM/media
execution as distinct evidence. Performance and shipping-image admission remain
separate qualifications.

The [retained qualification](../../CopperStart/Docs/NativeCopperOsEditor.md)
passes five Editor checks, six input/text checks and three joined refresh/input
checks with zero skips. Thirty-three Editor scenes pass native bitplane
comparisons; all 11 main lifetimes return to the same 471,792-byte free baseline.
The reproduced module is 26,416 bytes and its launcher is 652 bytes. These are
development functional results; the shipping pair and latency/peak budgets retain
their separate gates.

## Input during painting

The original frozen ROM polled and acknowledged the serial keyboard in the
cooperative console task. A long drawing call could delay acknowledgement past
the keyboard's 143 ms retry interval, causing physical presses to be retransmitted.
The [hardware keyboard specification](https://www.theflatnet.de/pub/cbm/amiga/AmigaDevDocs/hard_g.html)
defines that retry and the required handshake pulse.

The development ROM receives and acknowledges in VBlank. Cooked editing stays
in the console task. While GUI capture is active, the IRQ is the sole producer
of its existing raw FIFO, including the pending console-to-GUI handoff. A full
FIFO replaces its final queued slot with the existing recovery notification,
preserving the consumer's tail. The input worker then releases uncertain held
state without retyping the dropped batch.

The input provider derives modifiers from raw keys in order, including modifiers
already held at startup. It does not translate a delayed key using the final
modifier state after later releases. The replay stalls the Desktop task while
Shift-A and Ctrl-A arrive, and fills the raw FIFO while only interrupts can run.
It checks correct case/shortcut behavior, recovery and the absence of stuck repeat.

Reproduce the bounded inputs with
[`Build-NativeDesktopInputRom.ps1`](../../CopperStart/build/Build-NativeDesktopInputRom.ps1)
and [`Build-NativeDesktopInputProvider.ps1`](../../CopperStart/build/Build-NativeDesktopInputProvider.ps1).
They compare the frozen baseline's resolved code before applying the narrow
source changes. The ROM overlay changes the console receiver and its export;
the provider overlay changes modifier initialization and ordered key processing.
Cold reset is required when changing the ROM/provider development profile.

## Save follow-up

[Native Desktop Save](NativeDesktopSave.md) implements Save/Save As, exact source
snapshots, shared file locks, recoverable replacement and Save/Discard/Cancel.
Its combined native record retains five unchanged regression facts and adds a
separate two-fact save/recovery replay, with the earlier test timeout preserved.
The five-check Editor evidence above remains historical for its original module;
shipping-image admission is still separate.
