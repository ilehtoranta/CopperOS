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
