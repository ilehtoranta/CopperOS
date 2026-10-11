# Search real-DOS qualification, 2026-10-12

**Admission fails on a shared redirected-output defect.** The frozen baseline
and SimplifyLoopPhis candidate agree on all three CPU interpreters, but both
write `Needle\n     2> ` instead of `     2> Needle\n`. Independent WinUAE
native-shell runs reproduce the same defect on 68000, 68020, and 68040.
The pass stays default-off; `out/C/Search` remains the 10,664-byte baseline.

## Native execution

A frozen NativeOsRunner boots an in-memory OFS floppy with the user-supplied
Kickstart 3.1 A500 ROM (40.63, SHA256
`8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`).
The original startup CLI launches its normal guest program using DOS LoadSeg
and RunCommand. The authored qualification program in turn loads both unchanged
Search images through native DOS, alternates those same segment instances,
and captures exact bytes through native RAM and OFS file operations.
No supplied DOS vectors, host HUNK loader, guest result injection, or copied OS
implementation participates. This is a finite original-Kickstart integration
run, not original Workbench Search binary parity or a full OS certification.

| CPU interpreter | Complete invocations | Matched pairs | Requested stack bytes | Caller state |
| --- | ---: | ---: | --- | --- |
| 68000 | 64 | 32 | 4096 / 8192 | pass |
| 68020 | 60 | 30 | 4096 / 8192 | pass |
| 68040 | 60 | 30 | 4096 / 8192 | pass |

There are 18 distinct cases per CPU: literal/NONUM/QUIET/QUICK, no match,
empty file, DOS line/file patterns, wildcard and multiple FROM inputs,
recursive ALL traversal, missing files, required-argument and unterminated-quote
errors, another positional path, pending Ctrl-C, a line longer than 8192 bytes,
and a quoted filename. Repeat cases alternate both images, options and errors
on their existing LoadSeg instances at 4 KiB and 8 KiB. Every pair agrees on
output, return code and immediate post-RunCommand IoErr. Multiple positional
strings are accepted by FROM/M; `UNKNOWN` in that case is another path rather
than an unrecognized-keyword parser error. Native NameFromLock uses `Ram Disk:`
instead of the supplied-vector fixture's fabricated `RAM:` path spelling.

All 184 complete invocations restore selected input/output,
current directory, CLI, parent stack size/base and bounds, allocated signal bits,
and DOS library open count. The final 20 calls in each complete repeat batch
have zero AvailMem delta. First-use reductions of 208 and 8320 bytes are retained
as observations, consistent with capture/OS buffer growth; they are not credited
as proof of either a leak or complete resource reclamation. No absolute minimum
stack, overflow canary, loaded-image immutability or pure/resident admission is
established. Ctrl-C is a pending public task signal, not keyboard injection.
Separate stderr, read/allocation fault injection and actual MorphOS execution
remain open; supplied-vector error coverage stays separately documented.

The combined 68000 run reached the unchanged 3000-frame limit during the
long-line section, with an incomplete disk write and no terminal record.
That failed receipt gets zero accepted command-completion credit. Two smaller
batches complete under the same 3000-frame/120-second limits, with four extra
warm-up calls. The 68020/68040 complete runs each contain 60 calls.
Boot-inclusive frames/cycles are retained as diagnostics and are not a Search
speed benchmark or exact accelerator timing qualification.

## Defect and decision

`NativeMorphOSSearchCommand.WriteLine` emits the number with buffered VPrintf
and FPuts, then emits the text with unbuffered DOS Write. Buffer flushing moves
the prefix after the text; multiple-file and ALL headings are displaced too.
The standalone NONUM case emits the expected `Needle\n`. Strict existing
numbered-line expectations are preserved: 52 observations fail
them across both images. The optimization introduces no difference in this
corpus, but agreement between two defective images does not pass correctness.

WinUAE 6.0.3.0, JIT disabled, native Kickstart shell,
4 KiB stack and a disposable directory filesystem independently capture the
same exact misordered bytes and Shell return/Result2 0/0 for both images on all
three CPU models. Its provider boundary differs from the OFS/RAM route and is
explicit in the JSON receipt. Next: repair Search's buffered/unbuffered output
ordering, freeze a new correctness baseline, and rerun this integration gate
before selecting SimplifyLoopPhis for Search.

## Publish path and provenance

The normal `build-c.ps1`/dotnet publish route with the retained frozen CLI
reproduces both images exactly: baseline 10,664 bytes
`ae44b99cba79e377c7f95fb82089d0c9ff99e9b806ee65e9e1e202f2a92ac034`,
candidate 5364 bytes
`3a86e40958b950201a96604532672d0de8a52741d233cfa72cdb49aff891f14c`.
Only private publish directories were used; no production policy or installed
binary was changed. The [JSON report](search-os-qualification-20261012.json) binds sources,
frozen runner/compiler dependencies, CIL/guest images, binary request/response
records, exact output, final disk snapshots and publish logs. Raw evidence:
`artifacts/search-os-qualification-20261012`.

Portable PDB IDs match the executed modules' CodeView IDs. All native runner
and launcher document checksums match their current source. Of 50 engine
documents, 47 match and three differ (LightweightA500Machine, LightweightDiskDma,
generated AssemblyInfo). Therefore these results qualify the frozen engine
binary identified in JSON, not the latest dirty CopperScreen source checkout.
Neither primary sibling checkout was modified.
