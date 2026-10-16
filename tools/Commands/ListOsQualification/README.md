# List qualification under original Kickstart DOS

This adapts the existing Search native OS harness. It does not implement or
substitute DOS services. `Guest.cs` is a normal authored CLI diagnostic;
`Host.cs` prepares an OFS floppy, calls the frozen `NativeOsSession` API, and
extracts receipts from the final disk. Baseline and candidate are loaded once
through original DOS and repeatedly invoked with native `RunCommand`.

The declared corpus has 32 cases. Four batches per CPU contain 100 calls,
including warm-up and 20 final alternating repeats at 4096/8192-byte stacks.
The frame/time limits remain 3000 frames and 120 seconds per batch. The
six-call 68000 smoke run is separate from the 300-call acceptance matrix.

`META.FIX` is produced independently through native `Lock`/`Examine`.
`FAULTS.FIX` is produced independently through native `Fault`. The fixture's
known lengths, protection, comment, and date stamp are separately checked.
`verify.py` compares raw request/receipt bytes, exact individual row bytes,
error text, returns, immediate IoErr, caller state and stable repeat memory.
Directory row order is handler-defined, so multi-entry expectations check an
exact row multiset; baseline/candidate bytes must additionally match exactly,
including ordering. `TO` cases require an empty selected-output capture and
read the closed redirected file. Contracts are declared before native runs.

From the CopperOS root, choose a **fresh** stage:

```powershell
$stage = [IO.Path]::GetFullPath('artifacts/list-os-replay-UNIQUE')
$python = 'C:/Users/ilkle/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python tools/Commands/ListOsQualification/prepare.py $stage
if ($LASTEXITCODE) { throw 'Freeze failed' }
# prepare.py pins the installed List against the accepted pressure checkpoint.
dotnet build tools/Commands/ListOsQualification/Guest.csproj -c Release `
    "-p:FrozenInputs=$stage/sdk" --artifacts-path "$stage/build/guest" -o "$stage/guest/cil"
if ($LASTEXITCODE) { throw 'Guest CIL failed' }
dotnet "$stage/compiler/CopperSharp.Compiler.Cli.dll" `
    "$stage/guest/cil/CopperOS.ListQualification.Guest.dll" `
    --entry CopperOS.ListQualification.Guest::Main --platform amiga --cpu 68000 `
    --exceptions yolo --format hunk --runtime resident --memory none `
    --peephole fixed-point --symbols off `
    --managed-assembly "$stage/guest/cil/CopperSharp.Sdk.Amiga.dll" `
    --managed-assembly "$stage/sdk/CopperSharp.Sdk.Amiga.Support.dll" `
    --managed-assembly "$stage/guest/cil/CopperSharp.Compiler.dll" `
    --output "$stage/guest/ListQualification" --compatibility-report "$stage/guest/compatibility.json"
if ($LASTEXITCODE) { throw 'Guest HUNK failed' }
dotnet build tools/Commands/ListOsQualification/Host.csproj -c Release `
    "-p:FrozenRunner=$stage/runner" --artifacts-path "$stage/build/host" -o "$stage/host"
if ($LASTEXITCODE) { throw 'Host failed' }
$hostDll = "$stage/host/CopperOS.ListQualification.Host.dll"
dotnet $hostDll --bind "$stage/runner" "$stage/runner-source-binding.json"
& $python tools/Commands/ListOsQualification/cases.py $stage
dotnet $hostDll $stage $env:COPPERSHARP_KICKSTART31_ROM 68000 "$stage/cases-smoke.json" smoke
if ($LASTEXITCODE) { throw 'Smoke did not complete' }
& $python tools/Commands/ListOsQualification/verify.py $stage smoke 68000
if ($LASTEXITCODE) { throw 'Smoke contract failed' }
foreach ($cpu in '68000','68020','68040') {
    foreach ($batch in 'part1','part2','part3','repeat') {
        dotnet $hostDll $stage $env:COPPERSHARP_KICKSTART31_ROM $cpu "$stage/cases-$batch.json" $batch
        if ($LASTEXITCODE) { throw 'Native batch did not complete' }
        & $python tools/Commands/ListOsQualification/verify.py $stage $batch $cpu
        if ($LASTEXITCODE) { throw 'Native contract failed' }
    }
}
& $python tools/Commands/ListOsQualification/control-auditor.py $stage
if ($LASTEXITCODE) { throw 'Auditor controls failed' }
pwsh -NoProfile -File tools/Commands/ListOsQualification/confirm-winuae.ps1 -Stage $stage
if ($LASTEXITCODE) { throw 'Independent confirmation failed' }
$versions = foreach ($name in 'Copper68k','CopperMod.Amiga.Lightweight','CopperSharp.Compiler.NativeOsRunner','CopperSharp.Compiler.Tests.NativeOsFixture') {
    $dll = "$stage/runner/$name.dll"
    [pscustomobject]@{ name=$name; productVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion; sha256=(Get-FileHash -LiteralPath $dll).Hash.ToLowerInvariant() }
}
$versions | ConvertTo-Json | Set-Content "$stage/runtime.json" -Encoding utf8
& $python tools/Commands/ListOsQualification/report.py $stage "$stage/report"
```

The host's `completed` flag only says the guest finished and decoded its
receipt. Acceptance additionally requires `verify.py` to pass the contracts.
Auditor controls deliberately mutate expected return/IoErr/row data and JSON
output, confirm rejection, then restore the original evidence byte for byte.
A failed or incomplete native run cannot earn command-completion credit.

The ROM remains an external user-owned input selected by
`COPPERSHARP_KICKSTART31_ROM`; no ROM bytes are copied into the repository.
WinUAE uses a disposable directory filesystem, the original Kickstart Shell,
4096-byte stack, and JIT disabled. Its six calls per CPU independently check
QUICK, NODATES metadata and TO redirection plus Shell return/Result2.

The retained experiment is `artifacts/list-os-qualification-20261016`.
Reports bind exact executed binaries, sources, PDB identities, raw requests,
raw receipts, disk hashes and decoded observations. Four engine source
checksums differ from the current dirty CopperScreen checkout. Therefore the
result qualifies the frozen engine binary, not a rebuild of that checkout.
Runner and launcher source checksums match their frozen PDB documents.

This is finite original-DOS integration, not original Workbench command parity,
actual MorphOS execution, loaded-image immutability, shipping/PURE admission,
absolute minimum-stack or payload speed qualification. Pending Ctrl-C is a
public task signal, not keyboard or mid-read injection. Allocation/read/write
failure injection and separate stderr remain outside this corpus. Early DOS
buffer/handler memory growth is recorded; only final repeated-call deltas are
required to be zero. No production image, compiler or emulator source changes
are made by qualification.
