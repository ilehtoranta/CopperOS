# Development command version metadata

`build_workbench_makelink_versioned.py` supplies the next bounded CC08 build
slice for Workbench MakeLink. It does not stage files or grant shipping/PURE
admission. Existing unversioned candidates and their evidence remain unchanged.

The emitted identifier is
`$VER: MakeLink 0.1 (8.9.2026) CopperOS wb31`. This identifies the CopperOS
development implementation and its behavior profile; it does not reuse the
original command's 37.4 version. The standard embedded identifier format is
documented in the [AmigaOS UI Style Guide](https://wiki.amigaos.net/wiki/UI_Style_Guide_Shell#Embedded_Version_IDs).

## Supported HUNK transformation

Inspection of CopperSharp's CLI and `Compiler/Output/HunkWriter.cs` found no
dedicated executable version-tag emitter. The writer emits a plain CODE HUNK,
optional RELOC32/SYMBOL records, and END for this candidate. The separate
`append_hunk_version.py` tool therefore appends read-only identifier bytes to
that specific, completely parsed subset. The record layouts and relocation
alignment follow the [Amiga ROM Kernel Reference Manual: DOS, chapter 11](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf).

The tool requires an independently supplied input SHA-256 and new output and
receipt paths. It rejects memory flags, multiple hunks, resident names,
unsupported/out-of-order/repeated records, trailing bytes, truncation, duplicate
or overlapping relocations, odd/out-of-bounds relocation locations, relocation
addends outside the original CODE, and invalid symbol records. These restrictions
are the tool's accepted subset, not a claim that other valid HUNK forms are invalid.

Only allocation/CODE lengths at file offsets 20 and 28 change. The original CODE
bytes and complete relocation/symbol/END tail remain byte-identical. A leading
NUL, the printable version ID, a terminating NUL and zero longword padding are
inserted at the original CODE end. For the current MakeLink tag this is 48 bytes.
No instruction, relocation, writable global, entry point, startup path or library
call is added. Existing code offsets and symbols remain valid. Original compiler
map/static reports describe the raw HUNK; the append receipt describes the new
size and identity. Do not relabel a raw compiler map as a map of the versioned file.

The appender proves structural preservation. Native/real-OS execution, resident
lifecycle, version discovery through the real `Version` command, and release
qualification must still be associated with the resulting HUNK's new hash.
An unchanged body does not automatically transfer old execution reports.

## Reproduce the development build

Use a **new** output directory on every run. The driver builds the compiler and
managed root twice, with `--no-incremental` and separate `--artifacts-path`
directories, then generates all three raw HUNKs and versioned HUNKs each time.
Both raw and versioned outputs must match between passes. It uses the support
assembly selected by the root's actual restore assets, not a package-name sort.
The original compile/qualification scripts remain unchanged.

```powershell
& C:/Python314/python.exe tools/Commands/build_workbench_makelink_versioned.py `
  --dotnet C:/D-drive/Koodit/GIT/CopperOS/obj/dotnet-sdk-10.0.301/dotnet.exe `
  --runner artifacts/morphos-rename-body-qualified/runner/bin/CopperOS.Commands.NativeExecution/release/CopperOS.Commands.NativeExecution.dll `
  --output artifacts/workbench-makelink-versioned-NEW-RUN
```

The driver records exact native build arguments, compiler/SDK/root/runtime
assembly identities, restore assets, observed relevant source/configuration
files, host/runtime identity, both build logs, append receipts and native reports.
It fails if observed sources or native generation inputs change during the build.
These are two fresh builds on one host; this is not a hermetic cross-host build
claim or proof that every possible MSBuild/environment input has been captured.

The 68000, 68020 and 68040 commands use resident runtime, no managed memory,
YOLO exceptions, no FPU and disabled peephole optimization. The recorded public
dependencies are `exec.library` version 36 as the declared platform floor and
`dos.library` version 36 as explicitly requested by startup. Exec is obtained at
absolute address 4 and is not opened/version-checked by this command. The target
remains Kickstart 3.1. Dependency ABI files and the SDK's LVO/layout sources are
hash-bound; lower system versions are not qualified by this build.

Each candidate declares a 4096-byte stack budget with
`minimum_stack_qualified: false`. The unchanged existing native suite actually
configures 16384 bytes; its passing result must not be described as proof of the
4096-byte budget. Real-OS stack and resident evidence for old hashes also remain
separate. No installed path, Amiga protection byte, shipping or P admission is
assigned to these development records.

## Current bounded evidence

`artifacts/workbench-makelink-filehandle-abi-20260908/qualification.json` records two
fresh successful builds per CPU and 60 supplied-vector native invocations. All
raw outputs also equal the previously qualified unversioned candidate bytes.
The new native reports include instruction-interleaved calls through one loaded
image and report zero writes to the shared image. They do not run real DOS.

This refresh includes the current SDK FileHandle ABI sources. The qualification
report SHA-256 is `fcebfca783daef8febb3c93fac71f47fff48510693d9d700b44b356941b00305`;
the current SDK assembly SHA-256 is
`89f504c897eb79f605f110a9b4857cb12a3e84e693884512caabaf080d0017d2`.
The earlier `workbench-makelink-versioned-20260908` build and its SDK/source
snapshot are retained as history, not current-source qualification.
Both fresh passes produce raw and versioned files byte-identical to both earlier
passes for all three CPUs. Direct comparisons and repeated structural append
checks are recorded in
`artifacts/command-release-build-adapters-filehandle-abi-20260908/preservation-and-byte-identity.json`.

| CPU | Raw bytes | Versioned bytes | Versioned SHA-256 |
| --- | ---: | ---: | --- |
| 68000 | 3184 | 3232 | `12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45` |
| 68020 | 3224 | 3272 | `f2ee57c9d4f79b971d98718ec1585b8cd9780b085699abd04f7aa808789c1fd8` |
| 68040 | 3180 | 3228 | `34fdd4711ebd79aae834cc064c2cf142e7ec4581a96f9402c06a6df8c2eb566a` |

The appender has 12 unit tests with multiple adversarial cases, including every
truncated prefix of a valid fixture, malformed records, unsafe relocation
locations/addends, preservation of relocation loading at two bases, duplicate
version tags, deterministic output and no-overwrite/input-hash controls:

```powershell
& C:/Python314/python.exe -m unittest discover -s tools/Commands/tests -p test_hunk_version.py -v
```

The original appender test log and `preservation-audit.json` remain beside the
historical build report; those 12 tests were not rerun for this SDK refresh.
The current refresh reran all 34 command-build/release tests without skips and
exercised all three CPU proposals through the API and CLI. Its new `verified.json`
beside the byte-identity report accepts only `native-static` and
`reproducible-build`; eleven required gates remain, and every proposal rejects
release, including its explicit `pure_admission: false` request. No complete
CC08, command, stack or resident gate is closed by this development slice.

## Original DOS and Version checkpoint

`artifacts/workbench-makelink-versioned-20260908/version-boot-complete/verified.json`
records the earlier original-Kickstart execution of the versioned **68000** hash.
The current SDK build emits exactly that same HUNK, as established by the
byte-identity report above. This retains the existing execution capture; it does
not claim a new OS run, a current historical SDK snapshot, or cross-CPU coverage.
The unchanged
passive observer boots a fresh private Workbench derivative with the existing
32-by-250000 instruction bound. Only C:Ed and Startup-Sequence are replaced;
the verifier checks all 151 other regular files against the original archive.
No different-hash execution report is relabeled or transferred.

The five existing cases produce primary returns `0/20/20/20/0` and final errors
`205/116/205/203/205`, with exact separate success/parser/missing/duplicate/recovery
output files. Both 18-byte alias payloads are read independently after resident
removal. Command-owned direct allocations, parser storage, locks and FIBs balance;
the observed shared image remains unchanged.

Original `C:Version` 40.1 (4764 bytes, SHA-256
`dc2f55cd48b37bd1efecdbf07d38757463129dd9076ea6bf1b5d967f2189f224`)
then runs `C:Ed FILE FULL`, writing to RAM. This original implementation opens
and loads/unloads the selected HUNK while finding its identifier, without
executing the command. The verifier checks the Version caller's CODE range,
successful return, parser and output lifetimes, and the reloaded HUNK's relocated
bytes. Version's most recent metadata load replaces the observer's earlier
loaded-image hash, so the verifier associates that field with the correct load.

Its actual 27-byte output is `MakeLink 0.1\nCopperOS wb31\n`. The embedded date
is present in the disk HUNK but is not rendered by this original Version fixture;
the report does not claim otherwise. An independent `C:Type >NIL:` invocation
reads every byte, checks the file's examined size, and closes both handles.

The earlier `version-boot.trx` completed, but its console-directed Type stopped
after the first line when console output failed. That attempt is retained and
is **not** complete-output evidence. The fresh `version-boot-complete/boot.trx`
uses NIL redirection solely to permit the full readback; observer behavior and
execution bounds are unchanged. The accepted trace has 925 events, below 2048.

`verify_workbench_makelink_version_boot.py` produces the new report.
`verify_workbench_makelink_version_boot_controls.py` rejects 14 corruptions:
12 semantic mutations with mutually consistent TRX/observation captures, plus
failed-terminal-result and stale-capture controls. Results are in
`version-boot-complete/controls.json`. This remains a bounded development
checkpoint, without complete stack, startup, profile, resident or shipping admission.
