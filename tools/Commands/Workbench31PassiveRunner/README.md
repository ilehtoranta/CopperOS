# Workbench 3.1 passive readiness runner

Private diagnostic tooling, not a command parity runner or a supported-machine
certification. It executes the real supplied ROM and unchanged write-protected ADF
on Lightweight using only public APIs. It supplies no Exec/DOS functions, never
reads the guest bus from the host, never changes guest RAM, and injects no input.
Snapshots are taken between frames on the hardware owner thread. The engine's
`SlowRam` addition is a live `ReadOnlyMemory<byte>` view, like `ChipRam`.

Build the isolated engine from a copied source directory against the frozen
runner's CPU/disk dependencies. Build the private runner against that isolated
engine. Neither project has sibling project references. Always pass fresh
`--artifacts-path` directories; do not replace the original runner's binaries.

```powershell
dotnet build tools/Commands/Workbench31PassiveRunner/IsolatedEngine.csproj -c Release `
  --artifacts-path "$probeRoot/engine-build" `
  "-p:EngineSourceDirectory=$probeRoot/current-source" `
  "-p:FrozenDependencyDirectory=$frozen"
dotnet build tools/Commands/Workbench31PassiveRunner/Workbench31PassiveRunner.csproj -c Release `
  --artifacts-path "$probeRoot/runner-build" `
  "-p:EngineAssemblyDirectory=$probeRoot/engine-build/bin/IsolatedEngine/release" `
  "-p:FrozenDependencyDirectory=$frozen"
```

`Capture.ps1` accepts the runner DLL, licensed ROM/ADF paths, output directory,
frozen runner directory, copied source directory and prior baseline directory.
The retained 2026-09-26 receipt contains every concrete argument. Its hard-coded
comparison fingerprints belong only to that exact 2,000-frame baseline profile;
different media/frame counts must not reuse them as acceptance expectations.

`AnalyzeSnapshots.py <snapshots> <rom> <output-json>` reads saved bytes only and
decodes public SDK Exec/DOS layouts. Snapshot fields show structure presence and
startup progress; they do not establish output correctness, command return values,
or readiness for arbitrary command requests. List and string traversals are bounded.

The `--bind-pdb <pdb> <source-directory> <output-json>` runner mode reads portable
PDB document metadata with `System.Reflection.Metadata`. It does not use runtime
reflection or inspect a live machine. The 21 checked-in engine source documents
match both source snapshots to their respective PDBs; three generated documents
were not included in those snapshots and are reported as absent.

The CPU/output fingerprints duplicate the existing runner algorithms. Its hardware
fingerprint requires internal device state and is deliberately unavailable here.
For the retained run, exact chip-RAM/BMP equality at all 35 observation points
provides additional coverage. FPS is not measured or accepted by this workflow.
