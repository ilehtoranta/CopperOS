# CopperOS mathtrans.library

This implementation provides the 17 classic FFP mathtrans vectors.
It builds as a relocatable, AUTOINIT 68000 disk library and opens the system
`mathffp.library` for basic arithmetic. It uses software FFP operations and does
not require an FPU. Deferred expunge closes the dependency and releases the base.

Build with .NET 10 and the sibling CopperSharp68k checkout:

```powershell
& tools/build_system.ps1
dotnet test tests/MathTrans/CopperOS.MathTrans.Tests.csproj -c Release -p:CopperOSUseLocalCopperSharp=true
```

The system build retains the native library beneath its `artifacts/system/68000`
build directory (identified by `system-build.json`) and
diagnostic reports, then stages the matching artifact as
`filesystem/SYS/Libs/mathtrans.library`. The direct library builder also accepts
`68020` and `68040`, without staging them as the shared system baseline.

The reference test compares the packaged implementation with `mathtrans 37.1`
from the Workbench 3.1 disk, executing both in Copper68k against the actual
Kickstart 3.1 ROM mathffp vectors. To prepare privately owned reference media:

```powershell
python tools/MathTrans.Workbench/extract_reference.py <Workbench.adf-or-zip> artifacts/mathtrans-reference/mathtrans.library
$env:COPPERSHARP_KICKSTART31_ROM = '<Kickstart-ROM>'
dotnet test tests/MathTrans/CopperOS.MathTrans.Tests.csproj -c Release -p:CopperOSUseLocalCopperSharp=true
```

Alternatively set `COPPEROS_ORIGINAL_MATHTRANS` to the extracted library. The
reference test explicitly skips when either input is missing. It compares the
17 vector results and applicable N/Z/V flags, ordinary arguments, IEEE
boundaries and exceptional inputs, domain cases, power, and the large-angle
abort boundary. Ordinary numerical comparisons use 0.0001 absolute or relative
error; larger valid angles are separately compared with host math using a
0.001 absolute bound for the sampled cases. The original's SPLog sometimes
leaves Z set for nonzero results; the implementation reproduces that behavior
for the tested range. Carry is excluded where the API leaves it undefined.

The original and the candidate both pass `MathTransProbe` as disk-loaded
libraries on WinUAE with Kickstart 3.1, including a candidate version-40 open.
Reproduce using `tools/MathTrans.Workbench/run_probe.ps1` with `-Library`,
`-KickstartRom`, `-Python`, and a fresh `-OutputDirectory`; add `-Version 40`
for the candidate. Add `-Edge` to run the task-trap and large-angle probe. Both
original and candidate pass it on a 68000 Kickstart 3.1 WinUAE configuration.

On an out-of-range `SPSincos` input, the Workbench 3.1 implementation writes
the shifted input as the cosine and returns the caller's incoming D6 in D0,
with N/Z derived from that value. The implementation reproduces this quirk.
For `SPSinh`, the original calls mathffp division after its exponential
underflows; that raises the CPU's divide-by-zero exception. If a task trap
handler resumes execution, the result is `0xfffffffe`. The implementation uses
the same mathffp division path. `SPCosh` instead returns positive saturation
with V set and does not trap. Both the emulator comparison and WinUAE probe
exercise the underflow boundary with a resuming handler. Without such a handler,
the system's normal trap handling applies. The finite numerical sample does not
establish bit-identical results for all FFP inputs.
