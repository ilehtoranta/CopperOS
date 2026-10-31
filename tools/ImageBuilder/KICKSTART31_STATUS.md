# Kickstart 3.1 command HDF development status

`build_kickstart31_hdf.ps1` builds the 61 MC68000 commands, the Shell and
BootShell in fresh staging, then composes an 8 MiB DOS1 FFS HDF. The image has
`C/BootShell`, `L/Shell-Seg`, `S/Startup-Sequence`, release metadata and the
61 commands. It contains no library binaries. All commands, BootShell and
Shell-Seg have `$VER` identifiers. Image and build receipts contain hashes,
source identities and explicit `shipping: false` and `runtimeQualified: false`
status.

Example, with a fresh output directory and a Python 3 executable:

```powershell
& tools/ImageBuilder/build_kickstart31_hdf.ps1 -OutputDirectory '<fresh-directory>' -Python '<python.exe>'
& '<python.exe>' tools/ImageBuilder/qualify_kickstart31_keyboard.py --image '<fresh-directory>/CopperOS.hdf' --rom '<Kickstart-3.1-ROM>' --winuae '<winuae64.exe>' --output-directory '<fresh-evidence-directory>' --restart
& tools/ImageBuilder/create_kickstart31_winuae_config.ps1 -Image '<fresh-directory>/CopperOS.hdf' -KickstartRom '<Kickstart-3.1-ROM>' -WorkingImage '<fresh-working-copy>.hdf' -Configuration '<fresh-config>.uae'
```

The Kickstart entry distinguishes BootShell's direct child from a Shell created
by DOS. DOS-created Shells receive and initialize the startup packet, use the
buffered DOS command stream, reply to the packet, and release owned streams.
The existing CopperStart path retains its own native DOS implementation.

The following checks run against private HDF copies in WinUAE with a locally
supplied Kickstart 3.1 ROM. They do not modify the input image:

- `qualify_kickstart31_keyboard.py --restart` inserts a temporary guest
  keyboard injector before the unchanged BootShell, checks the Shell task,
  types commands via `input.device`, checks outputs, return status and file
  creation/deletion, and checks a second boot and persisted data. A
  `--without-bootshell` control demonstrates that the task marker is absent.
- `qualify_kickstart31_shell.py --boot-path --system-probe --lifetime-probe`
  tests the Shell child with file input, synchronous and asynchronous
  `SYS_UserShell`, an external Eval, error return/IoErr, and public memory
  after warmup and two equal batches of asynchronous invocations. The older
  pre-fix Shell hangs or fails these controls.
- `qualify_kickstart31_newcli.py` accepts a separately supplied Workbench
  `System/CLI` HUNK for a disposable NewCLI startup test. The reference binary
  is never included in the development HDF. An earlier Shell fails this test.

The 2026-10-31 development HDF and aggregate scoped qualification receipt are
in `artifacts/kickstart31-development-stable/`. Their image SHA-256 is
`c4a33e64210ca950419a11a6c34b494224396d3ae05b5273d3d4d03cdbddb3c4`.
The WinUAE 68000 tests used Kickstart 3.1 and 2 MiB chip RAM. After warmup,
public free memory remained at 1,276,760 bytes across two batches of ten
asynchronous Shell launches. The earlier pre-fix build lost 640 bytes per batch.

The independent readback checks 61 commands, 63 versioned binaries, no
libraries, and 1,159 current source inputs against the build receipt. The
runtime tests were made on a candidate whose HDF is byte-identical to this
clean rebuild; the aggregate receipt binds their evidence to the image hash.

The test images alter startup and contain test-only programs. The results are
evidence for those observed paths, not qualification of all Shell ABI,
command options, hardware, or WinUAE configurations. The Kickstart Shell
still implements a subset of the wider CopperOS Shell: additional built-ins,
script control and argument substitution, error-stream redirection, and full
Path/CD semantics remain open. No full Workbench or original-image comparison
is claimed. On this library-free image, `Version` without arguments reports an
invalid resident library because `version.library` is absent; the guest
checks instead validate `Version C:Eval FILE` against `Eval 1.0`.
