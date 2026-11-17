# Native Desktop Files

Files extends the native [Desktop/Home](NativeDesktopHome.md) process and its
single IDCMP drain/reply/Wait loop. The application uses public DOS locks and
directory APIs; no host filesystem service is involved. The development profile
remains the exact 524,288-byte ROM and text GUI provider recorded by
`DesktopProfile`, on MC68000 PAL A500 with 512 KiB Chip + 512 KiB Slow RAM and no
Fast RAM. These modules do not update CopperScreen's shipping ROM/media pin.

## Browsing and controls

Home's Files tile opens `SYS:`. Files retains one shared directory lock and a
fixed cache of up to 32 entries in DOS enumeration order. Eight rows are visible;
Up/Down select a row and Left/Right move by eight rows, with selection and the
viewport clamped at both ends. Clicking selects and opens a drawer. Enter opens
the selected drawer; selecting a file currently displays the Editor prerequisite.

The header provides Home, Up, Root and Reload. Keyboard equivalents are Escape,
U/Backspace, R and F. P or clicking the path starts path editing. Ctrl-U clears
the field, Backspace/Delete removes its last byte, Enter navigates and Escape
cancels. Relative paths resolve against the retained drawer lock; DOS absolute
paths and slash-separated drawer paths are accepted. Ctrl-C and the window close
gadget exit the Desktop. Home retains Files state for the next visit.
Navigation commands act once per press; text/backspace and arrows retain
repeat. Repeats of the P shortcut do not type into the newly opened path field.
Header actions and drawer clicks remain actions while the path field has focus.

The path and edit fields each hold 255 bytes plus NUL. The display shows the last
71 bytes of a long path; entry names retain up to 107 bytes and display up to 66.
The selected compact font still folds lowercase glyphs to uppercase, although
the original name/path bytes remain intact. A listing with more than 32 entries
explicitly says that only its first 32 are shown. Path navigation can reach a
drawer outside this cache. Sorting, paging beyond the cache, volume discovery,
copy/rename/delete and opening files in Editor remain separate work.

## Transaction and resource bounds

Each cache is one cleared 4,384-byte allocation containing its lock, count,
selection, viewport, overflow flag, two path fields, an aligned FileInfoBlock
reservation and 32 fixed 112-byte records. A navigation operation allocates a
candidate, obtains a shared lock, examines the target, obtains its display path,
and enumerates at most 33 entries. The extra entry distinguishes truncation from
an exactly full directory. Only `ERROR_NO_MORE_ENTRIES` completes an enumeration;
other DOS errors discard the candidate. A file target is rejected before ExNext.
The transaction follows the public [ExNext](https://developer.amigaos3.net/autodocs/dos.library/ExNext.html),
[NameFromLock](https://developer.amigaos3.net/autodocs/dos.library/NameFromLock.html)
and [ParentDir](https://developer.amigaos3.net/autodocs/dos.library/ParentDir.html)
contracts, including the distinct root-without-parent result.

The old cache and lock stay owned until the complete candidate is ready. A failed
allocation, Lock, Examine, NameFromLock, ParentDir or ExNext leaves the old path,
entries, selection and viewport intact and displays the DOS error number. An
initial failure leaves Home usable. The temporary CurrentDir change used to
resolve a relative Lock is always restored before scanning or returning. A root
ParentDir result with IoErr zero leaves the root in place. The application never
reconstructs drawer/up/reload lock identity from a possibly ambiguous volume
display name; explicit path input follows DOS path resolution.

Navigation owns at most two caches (8,768 bytes), two directory locks and the
existing 32 KiB process stack, 256-byte root state and 8 KiB cleanup reserve.
Exit releases the reserve before closing the directory lock/cache, then follows
the existing window, screen, port and library shutdown path. Full repaint requests
survive later selection messages in the same drain. Refresh draws the active view
inside the existing BeginRefresh/EndRefresh transaction.

The selected DOS implementation rescans preceding directory entries on ExNext.
The first implementation therefore caps the cache at 32 instead of 64. This
bounds the eager scan more tightly; directory I/O responsiveness still requires
separate optimization and measurement before release. Path/status edits repaint
only their own areas, and selection changes repaint only the affected rows unless
the viewport changes.

The command loads the largest GUI HUNK before the Desktop HUNK, then registers
both as reset-lived DOS system residents. This keeps the GUI loader's transient
image and relocated code ahead of the growing application allocation. Duplicate
resident registration unloads the redundant HUNK; failure unloads unpublished
code and returns to the console.

## Native qualification

Use [`Test-NativeCopperOsFiles.ps1`](../../CopperStart/build/Test-NativeCopperOsFiles.ps1)
with the qualified Home ROM and frozen producer inputs. It freezes the current
application and replay sources, verifies exact ROM/provider identities and runs
the original floppy fixture on the bounded machine. The fixture covers Home
startup failures, Files input/scrolling/path navigation, directory transaction
failures, expose refresh, replies, shutdown and heap return. Artifact paths and
measured results are recorded in the accompanying CopperStart evidence document.

The [retained Files result](../../CopperStart/Docs/NativeCopperOsFiles.md) passes
three checks with zero skips, including two real ROM/media journeys. The final
Desktop module is 13,316 bytes and its command 652 bytes. All 11 main lifetimes
return to the same 489,576-byte free-heap baseline. Twelve Files scenes pass native
bitplane comparisons. The bounded cache and navigation peak remain 4,384 and
8,768 bytes respectively; full loader/drawing high-water and responsiveness are
separate release work.

## Editor prerequisites

The [native Editor](NativeDesktopEditor.md) now shares this process, port and
renderer. It implements the 16 KiB byte-preserving buffer, original font with
ASCII/Latin-1 letter case, cursor/viewport, editing, New/Open and dirty-document
decisions. Files opens relative to its retained directory lock and keeps its
selection, scrolling and old document through candidate allocation/I/O/validation
failures. The follow-up ROM/provider profile adds IRQ keyboard capture and
ordered modifiers during long paints; the Files-only evidence above remains a
separate historical profile.

1. Add Safe Save/Save As and destination/collision decisions. Qualify the
   actual DOS temporary/backup replacement sequence across short writes, full or
   read-only media, failed close/rename and interrupted replacement; retain a
   recoverable original rather than assuming filesystem atomicity.
2. Join Files → Editor → save → Files reload → reopen on writable media. Measure
   total/Chip/Slow free memory, largest blocks and loader/save peaks with both
   bounded states resident. Complete latency, stress, shipping-image and workspace
   update/persistence gates separately from functional native replays.
