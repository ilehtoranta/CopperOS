# C: commands

One folder per external `C:` command. Shell built-ins (CD, Echo, If, Set, …)
are not here; they live in `src/System/Shell`.

```
<Command>/
  CopperOS.Commands.<Command>.csproj
  *.cs          portable algorithms (also compiled into the host model)
  Native/       native bodies, Workbench 3.1 and/or MorphOS variants
  Entry/        <Command>Entry.cs, the shipping [M68kEntryPoint]
  Host/         Shell-hosted IShellPlatform model (host only)
Common/         startup, ReadArgs, DOS I/O, DOS list, process-control helpers
Native/         compatibility aggregate: builds the old CopperOS.Commands.Native
                assembly from Common/ + every <Command>/Native/ for the
                private closure roots and qualify_*.ps1 scripts
CopperOS.Commands.csproj   host model used by tests/Commands
```

A folder with `Entry/` sets `RuntimeIdentifier=amiga-m68k`,
`CopperOSCommandName` and `CopperSharpEntry` in its project and ships one
MC68000 HUNK. Without an entry, the project is a plain library. Where both
variants exist, the entry calls the Workbench 3.1 body, so the binary is safe
on Kickstart 3.1.

Build every shipping command into `out/C/`:

```powershell
pwsh tools/Commands/build-c.ps1            # builds the local CopperSharp compiler first
pwsh tools/Commands/build-c.ps1 -Command Eval,Which -SkipCompiler
```

Settings shared by all command projects (resident profile, fatal exceptions,
local CopperSharp68k tree, publish copy into `out/C/`) are in
`Directory.Build.props` and `Directory.Build.targets`.

Search and List select their Workbench profile at C# build time using
`COPPEROS_WORKBENCH31_SEARCH` and `COPPEROS_WORKBENCH31_LIST`. This allows
native reachability analysis to discard MorphOS paths that cannot run through
their shipping entries. The compatibility aggregate and qualification roots
compile both profiles. Set `-p:CopperOSSpecializeWorkbench31=false` on a
command publish to compare against the shared implementation with runtime
profile selection. List passes its live parser lease by reference to avoid
repeated 24-byte argument copies. Delete selects a deletion-only object worker
through the existing generic Copy matcher traversal.

The [2026-10-04 size measurement](../../docs/Commands/Workbench31MorphOS320/size-reductions-20261004.json)
records controlled before/after builds with the same clean compiler and SDK,
original Workbench byte counts, and supplied native comparison scope.

## Executable size policy

Dir and Mount also select their Workbench profile at C# build time. Aggregate
qualification roots retain both profiles. Command argument helpers pass the
live `NativeCommandArguments` lease by reference through one release path.

`CopperOSCodeSizeOptimizations` controls the opt-in CopperSharp resident HUNK
size policy. Its shared default is `off`; command projects enable it only after
a smaller executable passes native comparison. A global MSBuild value overrides
the project selection. Compiler clients retain their existing defaults.

The [complete size report](../../docs/Commands/Workbench31MorphOS320/command-executable-sizes-20261004.json)
records the fresh baseline, accepted reductions, original Workbench ratios,
per-pass measurements, execution costs, stack depths, toolchain hashes and
qualification limits. Existing Search, List and Delete savings are included
in its starting baseline. Original-image and CopperScreen execution remain
unavailable on this host; native fixtures supply modeled Exec/DOS responses.

Reproduce using the clean sibling compiler checkout on
`codex/command-executable-size`, rather than a compiler workspace with unrelated
local changes:

```powershell
pwsh tools/Commands/build-c.ps1 -CopperSharpRoot ../CopperSharp68k-wt-command-size -OutputDirectory artifacts/size-run/final/C
python tools/Commands/measure_c.py artifacts/size-run/final --compiler ../CopperSharp68k-wt-command-size
```

Use `qualify_sizes.py` to compile captured inputs with an identical compiler
and compare native receipts on all three CPU models. The compiler accepts
`--code-size-optimizations on|off`; `--code-size-passes` selects independent
passes for qualification. `M68kCodeSizeOptions` exposes the same switches to
API clients, and cannot be combined with `M68kRomSizeOptions`.

`publish-c.ps1` validates the complete staged set against the accepted report,
backs up the old output, verifies installed hashes and then removes stale
files. Filtered builds preserve every unselected output file. Baseline images,
matching inputs and receipts are preserved under
`artifacts/command-executable-size-20261004`.
