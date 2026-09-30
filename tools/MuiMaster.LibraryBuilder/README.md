# MUI development library builder

This host tool compiles the production `CopperOS.MuiMaster` assembly and emits a
real, relocatable `copperos-muimaster.library` HUNK file. It is the first disk-library
packaging step: the management vectors and the complete MorphOS negative-vector
range are emitted. Only the currently implemented management and class/object
vectors, IDCMP request/reject, bounded rectangle/text/image/control/menu
`MakeObjectA`, bounded synchronous `MUI_RequestA`, `MUI_RequestObjectA`,
`MUI_Redraw`, `MUI_Layout`, the native
drawing-management vectors (`MUI_ObtainPen`, clipping, refresh and
`MUI_GetRGBColor`), ASL requester vectors, and legacy `Error`/`SetError` entries
have behavior; the remaining
public slots fail closed. The legacy error entries forward to
`dos.library/IoErr()` and `SetIoErr()` as documented by MUI; their focused
host-side seam tests pass, but they have not yet been executed on MorphOS. The
ASL entries use the named owner/requester records. Public MorphOS MUI
compatibility is **not** yet claimed.
It does not stage or replace anything in `filesystem/SYS/Libs`.

From the repository root:

```powershell
dotnet run --project tools/MuiMaster.LibraryBuilder -p:CopperOSUseLocalCopperSharp=true -- 68000
dotnet run --project tools/MuiMaster.LibraryBuilder -p:CopperOSUseLocalCopperSharp=true -- 68020 D:\output\mui-68020
dotnet test tools/MuiMaster.LibraryBuilder/tests/CopperOS.MuiMaster.LibraryBuilder.Tests.csproj -p:CopperOSUseLocalCopperSharp=true
```

CPU choices are `68000`, `68020`, and `68040`. By default the artifact, diagnostic
JSON receipt, native report, and framework report go to the builder's `bin/<configuration>/net10.0/artifacts/<cpu>`
directory. An explicit output directory is supported. Rebuilding replaces that
directory's same-named development artifact and receipt. The build uses the sibling
`CopperSharp68k` compiler and local SDK; `CopperSharp68kRoot` can override its location.
Restore when switching between pinned- and local-SDK host builds; stale package
assets can otherwise copy a different SDK into the builder output. Do not use
`--no-restore` across that switch.

## Artifact boundary

The file contains one CODE hunk and one same-hunk RELOC32 group. The first four
code bytes are `MOVEQ #-1,D0; RTS`, so executing it as a command returns failure without
running compiler startup or the library initializer. A typed SDK `Resident` and
`ResidentAutoInit`, the four management entries followed by every public vector
slot from LVO `-30` through `-756`, and development strings precede the compiled
code. Unsupported public slots are typed fail-closed thunks, preserving the ABI
positions without claiming their behavior. This follows the first-code-hunk and
inert-entry requirements in
the [Exec InitResident contract](https://developer.amigaos3.net/autodocs/exec.library/InitResident.html).

Resident and AUTOINIT fields are encoded through SDK structs/codecs. The relocation
boundary uses SDK field definitions for pointer locations; it does not duplicate
Exec structure layouts. Existing compiler relocation locations **and addends** are
shifted past the prefix. Metadata points to the exported register-ABI thunks,
never managed method entry symbols. `rt_EndSkip` points to an end marker inside the
hunk. No library-specific pointer fixups or cold initialization are supplied by
a host fixture; only standard HUNK relocation is required.

Malformed, overlapping, duplicated, unaligned, out-of-range relocations and missing
or ambiguous ABI exports fail packaging before an artifact is written. Target
compilation rejects managed allocation/runtime helpers, exception regions, runtime
type descriptors, external native targets, and reachable CopperStart implementation
assemblies. The tool itself is normal managed host tooling; none of it is shipped
as 68k library code. Native compilation uses freestanding/none, no floating point,
and the existing no-exception `Yolo` mode; it does not claim the compiler's separate
Resident runtime profile.

Given identical compiled input and CPU, output is deterministic. The receipt has
no timestamp: it records input assembly hashes, the artifact hash, hunk-relative
layout/export offsets, and sizes. Inputs are hashed again after compilation; an
input changed during compilation aborts emission. The receipt and reports are
diagnostic only and are not loader inputs.
Host tests independently parse and relocate the serialized HUNK at two load bases,
then read its descriptors through the SDK and verify every management pointer.
Real native execution belongs to `tests/MuiMaster.ExecIntegration`.
