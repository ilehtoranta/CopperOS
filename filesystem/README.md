# Runtime filesystem staging

This directory describes the Amiga runtime filesystem produced by CopperOS builds.
`SYS/` represents the system volume and uses familiar Amiga directories such as
`C`, `Devs`, `L`, `Libs`, `Prefs`, `S`, `System`, and `Tools`.

Source code does not live here. Components are developed in `src/`, built, tested,
and then copied or packaged into the appropriate runtime location by build tooling.
Generated binaries should therefore be treated as staged build output rather than
as the authoritative source for a component.

Run `tools/build_system.ps1 -Python '<python executable>'` from the repository
root to build the portable 68000 system. It stages `L/Shell-Seg`, `C/BootShell`,
`C/Execute`, `C/List`, `C/Demo`, `Libs/mathtrans.library`, `S/Startup-Sequence`,
the system identity and temporary-directory marker. The normal boot image is
`artifacts/system/68000/System.adf`. Shell internals stay inside the Shell segment.

The normal and proof workflows use `build_system_native.ps1`; there is no
separate Kickstart Shell implementation. Native gates and the linked-method
public Exec/DOS audit must pass before staging. The staged files are extracted
from the verified image and checked against its receipt; unrelated SYS files
are preserved. Generated files are ignored by Git. Run the build after source
changes. CPU-specific library variants and command-release admission remain
separate. This development image does not mark the command collection released.

The output's `system-build.json` points to the retained native manifest and
its double-read source snapshot, exact inputs and library receipt. Compilation
uses that snapshot rather than moving checkouts. Each run has its own build
directory; repeated builds update the normal image and generated SYS files.

To verify the staged file with a locally owned Kickstart 3.1 ROM and WinUAE:

```powershell
& tools/MathTrans.Workbench/run_probe.ps1 -Library filesystem/SYS/Libs/mathtrans.library `
  -KickstartRom '<Kickstart-ROM>' -Python '<python.exe>' `
  -OutputDirectory artifacts/mathtrans-staged-probe -Version 40
```

The probe mounts a private copy of the staged file and records its SHA-256 in
`receipt.json`. Add `-Edge` with a fresh output directory to exercise the
large-angle and task-trap cases as well.
