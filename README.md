# CopperOS

CopperOS is a work-in-progress collection of Amiga-compatible operating system components. It includes a Shell, external commands, system libraries and a native desktop, along with tools to build and test 68k artifacts and development disk images. Compatibility work uses Workbench 3.1 and MorphOS 3.20 as reference targets where applicable.

The components are primarily written in C#. Host-side tests exercise their behavior on .NET; native builds use CopperSharp68k to produce freestanding 68k code that calls Amiga system interfaces. CopperOS works alongside projects such as CopperStart and CopperScreen for system services and emulation.

## Project status

CopperOS is under active development, not a finished operating system distribution. Implementation and compatibility coverage vary by component. Host tests and successful image generation do not by themselves establish compatibility on a real system. In particular, the command distribution's shipping manifest is currently empty, and the MUI library is a development implementation that does not yet claim full MorphOS compatibility.

See the component documentation and goal files below for scope and qualification details.

## Getting started

You need the .NET 10 SDK and access to the packages used by the projects, including the pinned `CopperSharp.Sdk.Amiga` package. From the repository root, run the host-side command tests:

```powershell
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj -c Release
```

This builds and tests the command model on the host; it does not build a bootable image. Other test projects live under `tests/`, and `CopperOS.sln` contains the solution projects. Some native execution and integration tests require additional local toolchains, ROMs, emulators or reference assets; consult their READMEs before running them.

Native artifact builds use a sibling `CopperSharp68k` checkout by default. For example, the MUI builder supports `CopperSharp68kRoot` to select a different checkout and `CopperOSUseLocalCopperSharp=true` to use its local SDK. See the [MUI builder guide](tools/MuiMaster.LibraryBuilder/README.md) for prerequisites and commands.

Build and stage the portable 68000 system, including the shared Shell, with:

```powershell
& tools/build_system.ps1 -Python '<python executable>'
```

This stages the generated system files in `filesystem/SYS` and writes the normal
boot image to `artifacts/system/68000/System.adf`. The image uses the same shared
Shell on CopperStart and Kickstart 3.1. Native compilation, public-interface
auditing and image verification run before staging; ROMs are supplied separately
for boot tests. This native build requires sibling `CopperStart`, `CopperFS`
and `CopperSharp68k` source checkouts. `-OutputDirectory`, `-SystemRoot` and `-CopperSharpRoot` select
alternate outputs, staging trees and toolchains. See the
[system build guide](tools/ImageBuilder/README.md).

## Repository layout

- [`src/`](src/) — source for commands, libraries, system components, devices and handlers.
- [`tests/`](tests/) — host, native-root, execution and integration tests.
- [`tools/`](tools/) — native builders, disk and image tooling, and test utilities.
- [`filesystem/`](filesystem/) — staged runtime system-volume layout.
- [`docs/`](docs/) — component notes, contracts and qualification records.
- [`Goals/`](Goals/) — project plans, inventories and progress tracking.

## Further reading

- [Shell semantic core](src/System/Shell/README.md) and [DOS integration](src/System/Shell/DosREADME.md)
- [Workbench 3.1 and MorphOS command goal](Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md)
- [MorphOS MUI replacement goal](Goals/MORPHOS_MUI_REPLACEMENT_GOAL.md)
