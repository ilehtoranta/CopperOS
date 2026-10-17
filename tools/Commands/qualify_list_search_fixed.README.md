# List/Search fixed-compiler qualification

`qualify_list_search_fixed.py` freezes the already qualified Version allocator
compiler and the retained List/Search diagnostics. It does not build a dirty
compiler or emulator checkout. Defaults refer to the 2026-10-17 experiment;
keep those inputs or explicitly adapt a new checkpoint to new provenance.
Use a fresh absolute stage path. Historical artifacts are local and ignored;
the checked-in report records their hashes, not their availability elsewhere.

From the repository root, with the Python runtime selected as `$python`:

```powershell
$stage = [IO.Path]::GetFullPath('artifacts/list-search-fixed-UNIQUE')
& $python tools/Commands/qualify_list_search_fixed.py prepare --stage $stage
& tools/Commands/build-c.ps1 -Command List,Search -SkipCompiler `
    -CompilerAssembly "$stage/compiler/CopperSharp.Compiler.Cli.dll" `
    -OutputDirectory "$stage/default-build/C"
& $python tools/Commands/qualify_list_search_fixed.py vectors --stage $stage
# Set COPPERSHARP_KICKSTART31_ROM to the locally owned, matching original ROM.
& $python tools/Commands/qualify_list_search_fixed.py native --stage $stage
& $python tools/Commands/ListOsQualification/control-auditor.py "$stage/native/List"
& $python tools/Commands/SearchOsQualification/control-auditor.py "$stage/native/Search"
& $python tools/Commands/SearchOsQualification/control-buffering.py $stage `
    --image artifacts/build-c/08b639eb44b74116beee1a4d69ded016/C/Search
# Run WinUAE instances serially. These scripts use disposable directory filesystems.
pwsh -NoProfile -File tools/Commands/ListOsQualification/confirm-winuae.ps1 -Stage "$stage/native/List"
pwsh -NoProfile -File tools/Commands/SearchOsQualification/confirm-winuae.ps1 -Stage "$stage/native/Search" -RequireCorrectOutput
& $python tools/Commands/qualify_list_search_fixed.py check --stage $stage
& $python tools/Commands/qualify_list_search_fixed.py publish --stage $stage
```

Check every process exit code before continuing. Native batches run at most
two at a time, with unchanged per-run timeout/frame caps. Publication requires
strictly smaller images, the complete CPU/corpus matrix, raw-receipt checks,
WinUAE output checks and all negative controls. It preserves the other 59
installed files. Do not rerun `prepare` or `publish` into an existing result.

For this dated checkpoint `report` additionally freezes the normal build CIL
and SDK inputs, independently recompiles them with the frozen compiler,
requires byte-identical installed images, and writes the dated JSON report.
The stage's `runtime.json` records actual FileVersionInfo and hashes from the
native host payloads; it must be captured before this report step. For a new
checkpoint update the dated report destination and record current runtime
identities rather than inheriting an old identity claim.

Search's reusable `SearchOsQualification/verify.py STAGE BATCH CPU` retains
the corrected historical report's exact output contracts while checking raw
requests and receipts. It deliberately does not inherit that report's
hardcoded historical image and policy assertions. Historical reports remain
unchanged. `control-buffering.py` accepts `--image`, freezes its bytes inside
the new stage and binds their origin before executing the negative control.
