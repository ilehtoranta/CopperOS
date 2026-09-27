# CC04 native I/O contract and next qualification slice

Recorded 2026-08-30. Source implementation:
`src/Commands/Native/NativeCommandIo.cs`; private observation entry:
`tests/Commands.NativeRoot/NativeIoProbe.cs`.

Status: the helper, probe and executor are implemented. The expanded Foundation
run `5b2854ee3ef04f71ad6faab7d95add17` passed **32 I/O invocations per CPU,
96 total**, plus the existing 168 startup/argument invocations. This is
source-bound execution of actual 68000/020/040 HUNK instructions with supplied
vectors. It does not establish actual DOS stream behavior or a command's output
policy. No command, pure flag, resident admission, or distribution artifact is
approved by this document. Exact receipt and closure evidence appear below.

## Minimal public API

```csharp
public readonly struct NativeCommandIoError
{
    public bool IsCaptured { get; }
    public int Value { get; }
}

public static class NativeCommandIo
{
    public static int ReadOnce(BPTR file, APTR buffer, int length,
        out NativeCommandIoError error);
    public static int WriteOnce(BPTR file, APTR buffer, int length,
        out NativeCommandIoError error);
    public static bool IsCtrlCPending();
}
```

Each transfer calls the real SDK DOS vector exactly once and returns the raw
signed LONG. Only the documented failure result `-1` immediately captures
`DOS.IoErr()`, before another library call can replace it. A captured zero or
unknown signed error is retained with `IsCaptured == true`. On any other result,
the helper does not inspect or reset IoErr; the record is default/uncaptured.
Its default `Value == 0` makes no claim about the process's ambient error.

The error record contains two 32-bit scalar fields and owns no resource; it may
be copied. There is no allocated object, shared mutable state, exception path,
retry loop, length rounding, stream selection, or hidden cleanup. A caller must
provide a valid native BPTR, APTR and signed length for the original DOS call.
The helper passes a zero-length request through unchanged. It does not invent
DOS errors for invalid handles, pointers or lengths, or turn a short result
into a command result code.

The caller opens DOS through `NativeCommandStartup` and keeps that lease live
through every DOS operation. Compile the eventual entry with the existing
resident runtime profile so the library base is invocation-local. The caller
owns any explicit file open/close and buffer lifetime. `DOS.Input()` and
`DOS.Output()` already expose the needed public SDK API: their raw BPTR values
are borrowed, including zero when returned. Do not substitute a console,
convert a BPTR to its shifted address for the D1 argument, or close these
borrowed streams. A query returning zero is not tested by issuing an invalid
transfer through it.

`IsCtrlCPending` issues exactly `Exec.SetSignal(0u, 0u)` and tests bit 12. It
does not clear Ctrl-C or any other received signal, set a DOS error, or choose
whether a command stops. Polling frequency, acknowledgement, partial-output
handling and cancellation return codes require each reference command's own
contract and later tests.

## Original ABI and semantic boundaries

DOS vectors use the opened `dos.library` base in A6. Exec vectors use ExecBase
from address 4. The Amiga call convention permits D0, D1, A0 and A1 to be
clobbered, including volatile registers absent from an individual signature.
The existing API09 compiler correction must remain exercised by the fixture;
do not weaken gateway clobbers or keep a value in an undocumented register.

| API | LVO | Register ABI | Contract used by this slice |
| --- | --- | --- | --- |
| Read | -42 | D1 BPTR, D2 buffer APTR, D3 signed length; D0 signed count | Actual bytes read; zero is the documented EOF result for an ordinary positive request; -1 is failure. No full-buffer or positive-short-result inference is added. |
| Write | -48 | D1 BPTR, D2 buffer APTR, D3 signed length; D0 signed count | Actual bytes written; -1 is failure. A positive short count or zero remains available to command-specific policy. |
| Input | -54 | No arguments; D0 BPTR | Current input query; the original autodoc explicitly forbids closing the returned handle. |
| Output | -60 | No arguments; D0 BPTR | Current output query; the original autodoc explicitly forbids closing the returned handle. |
| IoErr | -132 | No arguments; D0 signed LONG | Inspect after a documented error return. Successful calls do not generally promise a meaningful error value. |
| Flush (V36) | -360 | D1 BPTR; D0 LONG status | Not used. It has buffered input/output side effects and historical return-value limitations. |
| SetIoErr (V36) | -462 | D1 signed LONG; D0 previous signed LONG | Replaces the process result/error field and returns the previous value; it is not a Boolean success result. |
| PrintFault (V36) | -474 | D1 signed error code, D2 header pointer; D0 BOOL | Not used. Writes through buffered default output and sets IoErr to the supplied error code. |
| SetSignal | -306 (Exec) | D0 new signals, D1 affected mask; D0 previous full signal word | A zero affected mask queries the current task without changing any received signal. |

`Read` and `Write` are documented as unbuffered DOS operations. This helper does
not layer buffered I/O or automatic final flushing over them. `PrintFault`
uses buffered output and changes IoErr, so it must not be introduced into the
transfer helper or used before preserving a selected failure. Its exact prefix,
newlines, localized text and behavior on output failure are not reference
command observations in this slice. In particular, IoErr after PrintFault is
not an independently established description of an underlying write failure.

Flush is not a harmless generic cleanup operation. For buffered input it drops
the buffer and attempts to seek back to the last read position. The 3.1 NDK
also retains warnings that pre-V37 Flush returned an indeterminate value and
V37 always reported success. Its separate V39 fix concerns handles which had
never been buffered; that is not proof that every return-value limitation is
fixed in V40. No reliable disk-completion claim or automatic Flush policy is
derived from these notes. A later command-specific buffered-output slice must
measure the selected original rather than assume that Close on a borrowed
stream is acceptable.

## Licensed reference evidence and SDK audit

The originals were read in place from
`D:/TestData/AmigaDeveloperCD.iso` with the existing read-only ISO inventory
reader. No original source, executable, or documentation was copied into the
repository. Line references below use decoded text line numbers of these exact
files, not current online replacements.

| Original file within the ISO | SHA-256 |
| --- | --- |
| `NDK_3.1/Docs/doc/dos.doc` | `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2` |
| `NDK_3.1/Docs/doc/exec.doc` | `71df8eeab9f6b9873a38bea96bec34fd6e44b78aa2d67d8f1b28319c207dd2ad` |
| `NDK_3.1/Includes&Libs/fd/dos_lib.fd` | `a9b24c1d9fb1053955dfb28eb8dabe92eff5dbde10b952e67d6cee2a80c5c228` |
| `NDK_3.1/Includes&Libs/fd/exec_lib.fd` | `4da0ae2a91d0758696e0d1f72f8bc748321b5fd14e7e31d8215cda8e002348c2` |
| `NDK_3.1/Includes&Libs/include_h/dos/dos.h` | `7791a911cab18de7aa5b5e4a818d5e09439c8c16aa7b029ea18a12ada000b8c3` |

The DOS autodoc sections are Read 4078-4117, Write 5874-5908, Input 2830-2852,
Output 3803-3825, IoErr 2936-2962, SetIoErr 4932-4956, PrintFault 4023-4050,
and Flush 2160-2201. The non-consuming query is explicit in the Exec SetSignal
section, 4344-4388. The DOS header at 238-248 defines the Ctrl-C signal bit and
mask and shows the same query. FD bias plus the six-byte vector spacing gives
the LVOs above; the FD argument registers agree with the autodocs.

The consumed local SDK in `D:/Koodit/GIT/CopperSharp68k` already declares all
nine APIs with the original registers and widths:

| SDK source | Relevant declarations | SHA-256 at audit |
| --- | --- | --- |
| `Sdk.Amiga/DOS/DOS.cs` | Read 122, Write 129, Input 136, Output 140, IoErr 205, Flush 418, SetIoErr 532, PrintFault 545 | `2853598290d02a3ae19ab7ffc2bf4dafc0d4d914c51ac257c64f00f13bf0a4fe` |
| `Sdk.Amiga/DOS/DosLvo.cs` | Named DOS vector offsets | `28bea84ea5edc8161eecf2455a7dce8a711234c103a582c971a45cb03da6ff74` |
| `Sdk.Amiga/Exec/Exec.cs` | SetSignal 239; ExecBase library policy | `e75c083a9ad0187e20cd0ea20a14c8207c531f9bcf3202491af29ade83377bf7` |
| `Sdk.Amiga/Exec/ExecLvo.cs` | SetSignal 59 | `18e262a1c47df58b1577e33ebcad687adf65762c6a89f1a8c689104ab9fdc2f2` |
| `Sdk.Amiga/Exec/Enums.cs` | SignalFlags 87-97, without break-signal masks | `a552c5ef4c8949db377a899af241e5c2919bb8cf37e9e479c05f4e8b1a3ddbae` |

`DOS.Error` has signed-int storage, so its enum return type does not prevent
retaining an unknown raw LONG through an explicit int cast. `BPTR.Raw` and
`BPTR.Address` are different representations (`Sdk.Amiga/BPTR.cs:19-27`);
only the raw BPTR is the file argument. No SDK ABI change is needed for this
helper. The missing public `SIGBREAKF_CTRL_C` equivalent is recorded as an SDK
surface gap: this source uses one private, NDK-cited constant `1u << 12` while
provider provenance is being established. It does not modify the shared SDK.

Existing Shell cancellation goes through its own native platform bridge in
`src/System/Shell/DosShellNativePlatform.cs:876`; that policy is not copied or
changed. Likewise, the existing startup probe's private treatment of a short
write (`tests/Commands.NativeRoot/NativeCommandProbe.cs:53`) is not evidence
that original DOS sets an error for every short write.

## Private native observation protocol

Suite name: `command-io-vector-fixture`. Compile only
`CopperOS.Commands.NativeRoot.NativeIoProbe.Main` as the entry for this suite,
with the resident runtime profile. A valid CLI fixture passes D0 length 40 and
A0 pointing at this longword-aligned, big-endian block:

| Byte offset | Direction | Value |
| --- | --- | --- |
| 0 | Input | `0x4343494f` (CCIO) |
| 4 | Input | Operation 0 through 6 |
| 8 | Input | Raw BPTR for explicit-handle operations |
| 12 | Input | APTR buffer address |
| 16 | Input | Signed LONG transfer length |
| 20 | Output | Raw transfer result, raw queried BPTR, or poll result 0/1 |
| 24 | Output | Error capture tag 0/1 |
| 28 | Output | Captured signed error, or default zero |
| 32 | Output | Ambient IoErr immediately after the observed operation |
| 36 | Output | Old error returned by a subsequent deliberate SetIoErr |

Operations are 0 explicit ReadOnce, 1 explicit WriteOnce, 2 Input plus ReadOnce,
3 Output plus WriteOnce, 4 IsCtrlCPending, 5 Input query only, and 6 Output query
only. Query/poll operations leave the error record uncaptured. There are no
expected output values in the input block: the host independently checks the
published fields against its configured vector returns.

After the operation, the probe inspects ambient IoErr and deliberately calls
`SetIoErr(0x5a17)` before publishing the outputs. This keeps the result, error
record, block address and previous error live across additional clobbering
library calls. Offsets 32 and 36 must match the fixture's current process error;
the stored failure at 28 must still contain the immediate failure observation.
For valid protocol cases, even a transfer failure returns `RETURN_OK` with
final IoErr `0x5a17`: this is a private completion checkpoint. Native instruction
execution and field assertions, not that return code alone, determine a pass.
Malformed protocol or a Workbench launch is rejected through the normal
startup owner without performing a transfer. No real command adopts this
binary protocol or its result policy.

## Finite native fixture matrix: 32 invocations per CPU

Run each row below once on each of 68000, 68020 and 68040, with eight requested
bytes unless the row specifies zero length. Successful/short calls deliberately
leave or change ambient IoErr to a nonzero fixture value; only the configured
raw result establishes whether the helper captures it. Values are vector
fixtures, not observations of an original DOS implementation.

| Base case | Operation | Supplied result and independent assertion |
| --- | --- | --- |
| R01 | ReadOnce | 8, full count; uncaptured, ambient 205 |
| R02 | ReadOnce | 3, short count; uncaptured, ambient 212 |
| R03 | ReadOnce | 0 for positive request; uncaptured, ambient 242 |
| R04 | ReadOnce | 0 for zero-length request; one Read call, uncaptured, ambient 221 |
| R05 | ReadOnce | -1; captured 205 |
| R06 | ReadOnce | -1; captured zero with tag set |
| R07 | ReadOnce | -1; captured unknown signed value -101 |
| W01 | WriteOnce | 8, full count; uncaptured, ambient 212 |
| W02 | WriteOnce | 3, short count; no retry, uncaptured, ambient 205 |
| W03 | WriteOnce | 0 for positive request; no retry, uncaptured, ambient 303 |
| W04 | WriteOnce | 0 for zero-length request; one Write call, uncaptured, ambient 242 |
| W05 | WriteOnce | -1; captured 221 |
| W06 | WriteOnce | -1; captured zero with tag set |
| W07 | WriteOnce | -1; captured unknown signed value -101 |
| B01 | Input then ReadOnce | Distinct input BPTR passed raw to D1; result 3, ambient 117 |
| B02 | Output then WriteOnce | Distinct output BPTR passed raw to D1; result 3, ambient 118 |
| B03 | Input query | Nonzero raw BPTR returned unchanged; no transfer or close |
| B04 | Output query | A different nonzero raw BPTR returned unchanged; no transfer or close |
| B05 | Input query | Raw zero returned unchanged; no fallback, transfer or close |
| B06 | Output query | Raw zero returned unchanged; no fallback, transfer or close |
| S01 | Poll | Received signals 0; false, state unchanged |
| S02 | Poll | Received signals `0x8000e110`; false, all unrelated bits retained |
| S03 | Poll | Received signals `0x00001000`; true, Ctrl-C retained |
| S04 | Poll | Received signals `0x8000f110`; true, Ctrl-C and every other bit retained |

Add exactly eight invocations using the same loaded image, without reloading
code or resetting mutable image state between them:

| Reuse scenario | Invocations | Required distinction |
| --- | --- | --- |
| Sequential I/O | 2 | Same process: Write returns -1/error zero, then Read returns 8/ambient 212. The second result must not retain the first error tag or value. |
| Repeated poll | 2 | Same process: poll `0x8000f110` twice without reseeding the signal word. Both are true and the full word is unchanged; the second ambient error is the first invocation's final `0x5a17`. |
| Interleaved I/O | 2 | Process A Read returns -1/error -101; process B Write returns 3/ambient 242. Alternate execution one instruction at a time with different 4 KiB/16 KiB stacks; each record, process error and library lease remains independent. |
| Interleaved poll | 2 | A sees `0x8000f110` and ambient -101; B sees `0x8000e110` and ambient 221. Results 1/0, full signal words and process errors remain independent under interleaving. |

Totals are 24 base + 8 reuse/interleaved = **32 per CPU, 96 new invocations**.
The two stack sizes are exercised examples, not a qualified minimum stack.

Every fixture must also assert these invariants:

- A6 matches the actual opened library, DOS calls require a currently owned
  successful DOS lease, and the owned library closes exactly once. File handles
  are borrowed and no Close/SelectInput/SelectOutput is permitted.
- Every library gateway clobbers D0, D1, A0 and A1 according to the target ABI.
  Return registers alone retain the gateway's defined result.
- Explicit BPTR, APTR and signed count arrive unchanged in D1/D2/D3. Each
  transfer occurs once. Poll uses exactly newSignals=0 and mask=0.
- There is one probe IoErr observation per valid invocation, plus exactly one
  helper IoErr call immediately after a -1 transfer. The extra observation is
  instrumentation, never evidence that the helper inspected successful IoErr.
- SetIoErr is called once for the deliberate probe mutation and once by Finish.
  The former returns the prior value; final process IoErr is `0x5a17`.
- Guard the protocol prefix, output extent and payload. The helper/probe does
  not modify write payloads; the Read fixture writes only its configured
  nonnegative returned byte count. Prefix/guards remain unchanged, and all
  output fields are written and independently checked before accepting a pass.
- AllocMem/FreeMem, FreeArgs, Flush and PrintFault are unexpected in this suite.
  No helper writes code/static image bytes or owns hidden per-invocation heap
  storage. Fixture task/stack/buffer resources have separate host ownership.

The executor retains the original startup Write gateway and uses isolated
I/O-suite handlers for Read, Write, Input, Output, IoErr, SetIoErr and SetSignal.
It reuses the HUNK loader, guarded bus, library ownership and task machinery.
The existing 31 startup and 25 argument-boundary cases per CPU also pass;
their contracts were not weakened to accommodate this protocol.

## Accepted native evidence and remaining scope

Executed `pwsh -NoLogo -NoProfile -File tools/Commands/qualify_native.ps1
-Component Foundation` from the repository root. The
[accepted receipt](D:/Koodit/GIT/CopperOS/tests/Commands.NativeRoot/bin/Release/net10.0/qualification/5b2854ee3ef04f71ad6faab7d95add17/qualification.json)
has SHA-256 `ffc05feaa384f4a27bbc80349b7d1a6a77c873856c23cff1e0c66ec55b002ff3`;
its input manifest is
`6c587c66c8bd08153f82f56db50eff0acd94fdc1b673647a5158cda802aeabaf`.
All 61 stages and 63 input checks passed, binding 441 source/settings files,
20 copied binaries, 35 restore files and 196 host/runtime files. All nine
Foundation HUNKs reproduced identical bytes. Each suite/CPU uses one protected
loaded image for its sequential and interleaved calls.

| I/O CPU | Bytes | Invocations | Observed stack writes | HUNK SHA-256 |
| --- | ---: | ---: | ---: | --- |
| 68000 | 1620 | 32 | 84 | `60c575a44f89d0b19abe4b214364422b011d362fc4b199871d8f753fac6adc88` |
| 68020 | 1592 | 32 | 84 | `27c82885925ccbbc58bd2c77a5ed9bc0f389d39589227beadef1ebb72fca0b26` |
| 68040 | 1604 | 32 | 84 | `adf0256395dbf22ddc4372e2f3af9c6b7914da8f86305385cbd5879364d371bc` |

The source hashes are `0e8fca1d274b636c097621ef2bcb6a2a541cc41a3a9f86e3864a7fb8b44ba1b0`
for NativeCommandIo and
`adb1893e41b676ff5175cc2982ac4d6cf61e2fad17d4fc5034b7631ed56ae111`
for NativeIoProbe. The compiler backend is
`2083e5497e70c9c7162beb84b85e56aa19229a3e3306b5b708eecbc505fe434a`.
Value-type constructor validation and imported-call body folding were corrected
at that owner, with separate before/after regressions; see
[compiler qualification](compiler-qualification.md). No heap, SDK declaration
change or command-source workaround was introduced for either compiler defect.

There are zero managed allocations, framework features, external native targets,
exception regions, fatal-machine-fault sites and shared writable image bytes.
The compiler reports **one helper label** for a shared native return sequence;
the qualification receipt preserves that count. The gate proves its exact ten
bytes, unique code location, both real ReadOnce/WriteOnce symbols, matching
saved-register prologues and incoming branches, and absence of overlapping
relocations. It approves no external runtime dependency or general helper
namespace. The sequence returns the result, unwinds local/saved-register stack
storage and returns; it calls no service and accesses no shared data. Previous
failed and exploratory runs remain distinct in the [progress log](progress-log.md).

This gate qualifies the generated helper's tested ABI, observations, absence of
hidden stream ownership, and invocation isolation. It does not establish original
file/pipe/console behavior, real EOF or short-write causes, PrintFault text,
buffer flushing, reference-command cancellation, full command options/results,
Workbench tool semantics, or pure/resident packaging. Those remain explicit
later command/provider qualification work.
