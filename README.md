# CopperOS

CopperOS is a disk-based Amiga-compatible operating system project. It complements
CopperStart rather than replacing its role: CopperStart is
the Kickstart/ROM component, while CopperOS contains libraries, devices, handlers,
commands, system software, tools, documentation, and the files used to assemble a
bootable runtime filesystem.

## Repository layout

- `src/` contains maintainable source projects, grouped by AmigaOS component type.
- `filesystem/SYS/` models the runtime Amiga system volume assembled by the build.
- `tests/` contains component and integration tests.
- `tools/` contains image, disk, and test-harness tooling.
- `docs/` contains architecture, compatibility, component, and filesystem notes.

The source tree deliberately does not mirror the runtime filesystem. A component
is developed under `src/` and its build output is later staged in the appropriate
location under `filesystem/SYS/`. For example, the MUI master library is developed
under `src/Libraries/muimaster.library/`, while its eventual runtime output belongs
at `filesystem/SYS/Libs/muimaster.library`.

No implementation projects have been added yet. `CopperOS.sln` is an empty solution
ready to receive them as development begins.
