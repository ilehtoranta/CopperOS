# Dir original DOS qualification

## Receiver size correction follow-up

`../investigate_dir_size.py --stage STAGE` captures the installed/fixed maps
and changed-method disassembly using a locally supplied Capstone 5.0.6 decoder
under `STAGE/decoder`. Build an isolated compiler baseline from `69d01d1` into
`STAGE/compiler-baseline` before this audit. Apply the portable receiver patch
in that isolated checkout and build `STAGE/compiler-candidate` separately.

`../qualify_dir_receiver_size.py prepare --stage STAGE` freezes identical
corrected command CIL/SDK inputs, compiles both compilers and runs both profile
vectors. Baseline images must reproduce the preceding format-fix checkpoint.
`native --stage STAGE` reuses the same 27-contract original-DOS corpus and caps.
Run the auditor controls and WinUAE extended confirmations as below. Run
`negative_controls` to rebind/recheck the six historical faulty images.

The dated `check` step also requires the 900-test focused compiler receipt and
the retained baseline-control receipts at `STAGE/tests`. `integrate` copies
the two new test files into the primary compiler checkout, verifies its
equivalent pending receiver source changes remain untouched, and saves the
portable patch/integration receipt. Then a normal filtered Dir build using
the candidate CLI must reproduce the candidate into `STAGE/default-build/C`.

`publish` rechecks strict size reduction, all receipts and the initial installed
inventory, stages all 61 accepted images and replaces only Dir. `report` binds
that publication and writes the dated report. These modes are checkpoint
specific; adapt dates, source/compiler provenance and receipts for another run.
The accepted checkpoint and its qualifications are documented in
`docs/Commands/Workbench31MorphOS320/dir-value-type-receiver-size-20261017.md`.

## Formatting correction follow-up

`../qualify_dir_format_fix.py` records the subsequent `%lc` correction and
rebuilt word/long-aware vector fixture. Its fresh stage keeps old buggy images
as six negative controls. Corrected-source phi-off images are the passing
controls for corrected-source phi-on candidates. Both profiles pass original
DOS and WinUAE; installation remains held for size.

```powershell
$stage = [IO.Path]::GetFullPath('artifacts/dir-format-fix-UNIQUE')
& $python tools/Commands/qualify_dir_format_fix.py prepare --stage $stage
& tools/Commands/build-c.ps1 -Command Dir -SkipCompiler `
    -CompilerAssembly "$stage/compiler/CopperSharp.Compiler.Cli.dll" `
    -OutputDirectory "$stage/default-build/C"
& $python tools/Commands/qualify_dir_format_fix.py compile_and_vectors --stage $stage
# Set COPPERSHARP_KICKSTART31_ROM to the matching, locally owned ROM.
& $python tools/Commands/qualify_dir_format_fix.py native --stage $stage
foreach ($profile in 'native','native-morphos') {
    & $python tools/Commands/DirOsQualification/control-auditor.py "$stage/$profile"
}
pwsh -NoProfile -File tools/Commands/DirOsQualification/confirm-winuae.ps1 -Stage "$stage/native" -Extended
pwsh -NoProfile -File tools/Commands/DirOsQualification/confirm-winuae.ps1 -Stage "$stage/native-morphos" -MorphOS -Extended
& $python tools/Commands/qualify_dir_format_fix.py report --stage $stage
```

Every step must succeed for this follow-up. Run WinUAE instances serially.
`prepare` snapshots/builds CIL and the vector runner against frozen SDK
references, reusing the frozen compiler and native guest/host/runtime.
`compile_and_vectors` compiles both policies, rejects the six old images and
freezes native inputs. `report` re-audits raw receipts/outputs, performs a
shared-SDK shipping replay, binds evidence and checks the installed inventory
unchanged. It writes the dated follow-up report; adapt output names and
provenance for another checkpoint. It does not install an executable.

`cases.py --extended` adds lowercase/multiple/X cases to the original 24-case
corpus. WinUAE's `-Extended` adds lowercase and multiple diagnostics. The
original mode remains available for the historical checkpoint below.

## Initial held checkpoint

This diagnostic adapts the Search original DOS harness for both Dir profiles.
Guest/host record formats and execution limits are retained. The guest adds
an isolated working directory, an empty directory and independent DOS Fault
queries. It loads baseline/candidate once, alternates native RunCommand calls,
and saves output, return/IoErr, memory and caller-state observations. The host
uses the frozen NativeOsSession runtime; it supplies no OS vector stubs.

The dated experiment is held: `OPT Z` fails its strict output expectation in
both images and profiles, and the fixed shipping image is larger than the
installed image. Keep those expectations. `native` intentionally exits with
failure after collecting the full matrix when a contract fails. `check`
records the derived acceptance decision; a successful `report` command means
the held evidence is internally consistent, not that the command passed.
There is no publication step for this held experiment.

From the CopperOS root, with the desired Python executable in `$python`:

```powershell
$stage = [IO.Path]::GetFullPath('artifacts/dir-fixed-UNIQUE')
& $python tools/Commands/qualify_dir_fixed.py prepare --stage $stage
& tools/Commands/build-c.ps1 -Command Dir -SkipCompiler `
    -CompilerAssembly "$stage/compiler/CopperSharp.Compiler.Cli.dll" `
    -OutputDirectory "$stage/default-build/C"
& $python tools/Commands/qualify_dir_fixed.py vectors --stage $stage
dotnet build tools/Commands/DirOsQualification/Guest.csproj -c Release `
    "-p:FrozenInputs=$stage/shipping-inputs" --artifacts-path "$stage/build/guest" -o "$stage/native/guest/cil"
dotnet "$stage/compiler/CopperSharp.Compiler.Cli.dll" `
    "$stage/native/guest/cil/CopperOS.DirQualification.Guest.dll" `
    --entry CopperOS.DirQualification.Guest::Main --platform amiga --cpu 68000 `
    --exceptions yolo --format hunk --runtime resident --memory none --peephole fixed-point `
    --managed-assembly "$stage/native/guest/cil/CopperSharp.Sdk.Amiga.dll" `
    --managed-assembly "$stage/shipping-inputs/CopperSharp.Sdk.Amiga.Support.dll" `
    --managed-assembly "$stage/native/guest/cil/CopperSharp.Compiler.dll" `
    --output "$stage/native/guest/DirQualification" --compatibility-report "$stage/native/guest/compatibility.json"
dotnet build tools/Commands/DirOsQualification/Host.csproj -c Release `
    "-p:FrozenRunner=$stage/native/runner" --artifacts-path "$stage/build/host" -o "$stage/native/host"
& $python tools/Commands/qualify_dir_fixed.py prepare_native --stage $stage
# Set COPPERSHARP_KICKSTART31_ROM to the locally owned, matching original ROM.
& $python tools/Commands/qualify_dir_fixed.py native --stage $stage
# Native failure is expected for this dated checkpoint. Review all raw failures.
foreach ($profile in 'native','native-morphos') {
    & $python tools/Commands/DirOsQualification/control-auditor.py "$stage/$profile"
}
# WinUAE runs must be serial. Strict failure is expected for this checkpoint.
pwsh -NoProfile -File tools/Commands/DirOsQualification/confirm-winuae.ps1 -Stage "$stage/native"
pwsh -NoProfile -File tools/Commands/DirOsQualification/confirm-winuae.ps1 -Stage "$stage/native-morphos" -MorphOS
& $python tools/Commands/qualify_dir_fixed.py check --stage $stage
```

Check every command's exit code. Preparation/build errors must stop replay;
do not classify them as the known output-contract failure. Use a fresh stage
to preserve receipts. Native execution can resume already completed batches,
but always re-audits their raw bytes. No timeout/frame caps are raised.

For the optional six-call smoke run, generate `cases.py STAGE/native` and run
the host with `CPU=68000`, `CASES_JSON=STAGE/native/cases-smoke.json`, label
`smoke`, then `verify.py STAGE/native smoke 68000`. Do this before full runs.
It is supplementary; the declared full corpus still includes these cases.

The dated `report` mode requires that smoke receipt, captures/recompiles the
normal build CIL/SDK inputs, and requires byte-identical candidate output. It
also requires `runtime.json`, recording actual FileVersionInfo and SHA-256
from the four host runtime DLLs (Copper68k, CopperMod.Amiga.Lightweight,
CopperSharp.Compiler.NativeOsRunner and CopperSharp.Compiler.Tests.NativeOsFixture).
It writes the dated report and proves the full installed inventory unchanged.
Adapt the dated destination/provenance before using it for another checkpoint.

Raw contracts are in `cases.py` and are declared before execution. The auditor
checks request/receipt structures, input bindings, exact output bytes,
return/IoErr, alternating images, caller restoration and stable repeat memory.
`control-auditor.py` uses a passing batch to prove rejection of altered return,
IoErr, output and forged JSON, then restores original bytes. Production Dir
and its vector formatter were unchanged by that initial audit; the follow-up
above corrects both.
