# Normal system build

Run from the CopperOS repository root with .NET 10, Python and sibling
CopperStart, CopperFS and CopperSharp68k source checkouts:

```powershell
& tools/build_system.ps1 -Python '<python executable>'
```

The default outputs are `artifacts/system/68000/System.adf` and the generated
files in `filesystem/SYS`. `-OutputDirectory`, `-SystemRoot` and
`-CopperSharpRoot` select other output, staging and compiler roots.

The normal build and `build_system_proof.ps1` share
`tools/ImageBuilder/build_system_native.ps1`. Both compile the same Shell
sources and export roots, apply the same StackSwap gate, and use the same
bootstrap. There is no alternate Kickstart Shell implementation.

## Build and staging contract

To build and revalidate the retained complete two-platform qualification, use:

```powershell
& tools/build_system.ps1 -Verify
```

`-VerificationDirectory` selects the retained verification directory (default
`artifacts/normal-full-verification-v1`). This gate requires the original owned
reference assets, probes, tool/source snapshots and result disks identified by
its plan. It independently audits all 53 runtime records and publishes the
accepted CopperStart pair under `artifacts/system/68000/roms`, together with
`system-runtime-acceptance.json` and `verified-profile.json`. The latter marks
`evidenceReuse: true`. The built image must match the qualified image byte for
byte; changed images fail rather than inheriting acceptance. To audit an
existing normal build without rebuilding, run `tools/verify_system.ps1`.

This command revalidates recorded execution. It does not rebuild CopperStart
ROMs or launch fresh Shell/math tests. A portable one-command fresh-build and
full runtime replay remains to be integrated; the historical artifact drivers
still contain local paths. Missing qualification assets are an error.

1. Capture and double-read the required source files into a retained tree.
   Compile the library, Shell, bootstrap and commands from that snapshot.
2. Enforce the 68000 native gates and packaged Shell entry/stack requirements.
   Reject unsupported runtime sites and private CopperStart DOS services.
3. Compose the boot image from hash-bound native artifacts. Independently
   verify its boot checksum, filesystem allocation and file payloads.
4. Extract the verified image files into SYS using checked temporary files
   and individual atomic replacements. Preserve unrelated SYS files.
5. Publish the normal image and its build, image and staging receipts.

The nine staged files are `C/BootShell`, `C/Demo`, `C/Execute`, `C/List`,
`L/Shell-Seg`, `Libs/mathtrans.library`, `S/Startup-Sequence`,
`S/CopperOS-Release.json` and `T/.keep`. Internal commands remain inside
`L/Shell-Seg`.

`system-build.json` identifies the retained source snapshot and native
manifest. Native source audits check that captured tree; later checkout edits
do not change its inputs. `system-image.json` and `system-staging.json` bind
the final image and every staged file. Each build uses a new retained run
directory, while the default output names and generated SYS files are updated.

This is the normal development system build. It does not import loose SYS
files, original operating-system binaries or command-release records. Its
`shipping: false` status does not change command distribution admission.
ENV: and ENVARC: still require boot/environment configuration; the built-in
qualification fixture supplies isolated assigns for those command contracts.

## Recorded integration artifact

The November 6, 2026 normal build produced:

- Image SHA-256:
  `dcd6e2d8f8a0572d6c7369d24a29130f76af09bc5f4a3ae9173045941259868b`.
- Packaged Shell: 187,216 bytes, SHA-256
  `9b722b3afcf02ebfa655b60a796ea034165f381c2fd38328a5798269042ed879`.
- Math library SHA-256:
  `cd4294cfc38c8eba2f9ca517b8302f74f48028d2b8ec26c3195ffdc3fe860f2d`.
- Source snapshot: 1,103 files, receipt SHA-256
  `7bd3055bf6027ad6e80e8aa4722895e5355b1938b99635cd9ef9d0dd6973541d`.

The native manifest is under
`artifacts/system/68000/build-3485616003de4a0c8ff765989ce781bc/native`.
Its public-interface audit passes with exactly the seven reviewed memory and
public Exec support methods. The 33 image-builder host tests pass, including
normal-image staging, preservation of unrelated files and rejection of changed
images before replacing a staged Shell.

The earlier [shared Shell qualification](SharedShellAcceptance.md) records
the full refactor acceptance. This normal-build integration uses a fresh native
artifact and separate runtime receipts; earlier artifact hashes are not
substituted for this build.
All 128 Shell C# source files match that verified v22 baseline. The final
integration audit checks this identity as well as the new compiled artifact.

## Runtime verification

The exact input ADF boots in the Lightweight A500 emulator under both original
Kickstart 3.1 and the qualified CopperStart ROM-v3/extension profile. The host
injects keyboard events; the guest Shell creates `SYS:T/exact` containing `ok`.
The startup and staged executable/library bytes remain those of the input
image. Read-only DF1 helpers contain only the probe command.

Kickstart receipts are in `artifacts/system-shared-shell-exact-ks-v1` and
`artifacts/system-shared-shell-ks-matrix-v1`. Its enforcing built-in corpus
and original-V40 comparison are in `artifacts/system-shared-shell-ks-builtins-v2`.
The five process/script cases and full built-in corpus pass. The comparison
matches 57 shared V40 contracts and separately passes three retained extensions.

CopperStart's exact boot and dependency receipt is in
`artifacts/system-shared-shell-exact-copper-v2`; its command cases are in
`artifacts/system-shared-shell-copper-matrix-v1`. Control flow with 128 padding
lines, CLI path isolation, NewCLI/EndShell, owned streams and System have
passed. The full built-in corpus also passes, including physical console reset,
all 60 candidate contracts and zero remaining Shell, child or background tasks.

The final independent aggregate is
`artifacts/system-shared-shell-integration-acceptance.json`, produced by
`artifacts/audit_normal_system_integration.py`. It passes all 15 checks with
no errors: the normal build/staging gate, two unchanged-image boots and six
integration cases per platform. It independently rereads guest output files,
checks ROM/emulator and fixture/result identities, confirms task retirement,
rehashes every captured source and staged file, and reruns the native
public-interface audit. All runtime processes have terminated successfully.

The first Kickstart built-in run was observation-only because its command
omitted `--verify-shared`. Its outputs are retained under the matrix directory;
the separate `system-shared-shell-ks-builtins-v2` enforcing run and original
comparison are the acceptance evidence. The read-only live-stack/heap capture
during CopperStart's long corpus confirmed ordinary frame progress and the
final retirement interval; no emulator source or timing was changed.

### Math dependency on CopperStart

The earlier ROM-v3 profile lacked mathffp.library; its `RESULT 20` smoke result
is retained as the initial failure. The replacement CopperStart profile now
installs the software FFP owner, the ROM disk-library loader, and the resumable
68000 divide exception entry. The shared Shell needs no platform adapter.
Layers startup also identifies libraries by their public names before reading
Layers-owned state, so FFP's private kernel pointers are not misinterpreted.

The same normal `artifacts/system/68000/System.adf` listed above now passes the
full math probe under both CopperStart and original Kickstart 3.1. Its startup,
Shell, staged mathtrans bytes and source ADF remain unchanged. The enforcing
receipts are:

- `artifacts/mathffp-normal-ks-v4/qualification.json`.
- `artifacts/mathffp-normal-copper-v5/qualification.json`.
- Independent two-ROM audit: `artifacts/mathffp-normal-image-acceptance.json`.

The accepted CopperStart main ROM is
`artifacts/mathffp-rom-v3/CopperStart-DiskBoot.rom`, SHA-256
`222ae453eac25bf7b6430acf25eaea765f15251d041f5c44402c10f762346d32`.
Its desktop extension is
`artifacts/mathffp-rom-v7/CopperStart-DesktopExtension.rom`, SHA-256
`7222d9d163ef2772db7e510e3f1b9f4f79fdd6d245046ba430108276a3b0f7f0`.
Both are built by CopperStart's production ROM scripts, using retained source
snapshots `artifacts/mathffp-rom-source-v3` and `mathffp-rom-source-v7`.
The main bank does not contain the desktop FFP/loader bodies; these execute in
the extension. The extension uses the frozen v4 compiler producer; its sources
are identical in the retained v7 snapshot. No emulator source was changed.

Both boots load V40 `SYS:Libs/mathtrans.library`, check all seventeen vectors,
pass six overflow/trap stages, and produce identical raw results and defined
CCR bits for twenty nontrivial samples. This includes large trig arguments,
out-of-range Sincos's caller cosine result, extreme negative sinh's resumable
divide exceptions, and cosh saturation. The probe also rejects minimum version
41, balances two opens, expunges, and reloads the same staged file.
The read-only DF1 images contain only the identical probe command. The audit
rehashes and independently reads both result ADFs, compares the expected probe
bytes, and confirms all sixteen installed FFP JMP targets, twelve kernel
pointers and the divide exception trampoline point into ROM.

The FFP suite passes three tests with the owned original ROM and final boot
capture explicitly enabled (`artifacts/mathffp-tests-accepted.log`). It covers
recorded raw words/flags, a 512-case reference digest, 2,048 direct original-ROM
comparisons, three resumable divide-by-zero cases, shared export register
preservation, lifecycle, and 1,536 additional calls through the actual
boot-installed ROM vectors. The native foreign-library scan regression passes
separately (`artifacts/mathffp-layers-tests-v3.log`). The seventeen current image
builder tests and the new 68000 probe-alignment regression also pass.

Reproduce the focused boot check from the CopperOS root:

```powershell
python tools/ImageBuilder/qualify_unmodified_shell31.py --image artifacts/system/68000/System.adf --rom artifacts/mathffp-rom-v3/CopperStart-DiskBoot.rom --extension-rom artifacts/mathffp-rom-v7/CopperStart-DesktopExtension.rom --probe-dll artifacts/mathffp-exact-probe/bin/ExactBootProbe/release/ExactBootProbe.dll --mathtrans-full --output-directory artifacts/math-replay-copper
python tools/ImageBuilder/qualify_unmodified_shell31.py --image artifacts/system/68000/System.adf --rom path/to/owned/kickstart-3.1-a500.rom --probe-dll artifacts/mathffp-exact-probe/bin/ExactBootProbe/release/ExactBootProbe.dll --mathtrans-full --output-directory artifacts/math-replay-ks
```

The accepted runtime scope is a 68000 Lightweight A500 emulator with 2 MiB Chip
RAM. The focused math check above is supplemented by the full updated-ROM
Shell replay recorded below. Neither qualification claims physical-hardware
coverage. The library's existing transcendental
accuracy limits still apply; identical samples here compare the CopperOS
library on the two operating systems. Arithmetic reference comparisons use the
original Kickstart mathffp library. See CopperStart `Docs/NativeMathFfp.md` for
build commands and numerical contracts. A task without an installed divide
trap handler currently stops on that unhandled exception; default Guru alert
presentation is outside this qualification.

## Complete normal-image verification with updated CopperStart

The November 6, 2026 full replay passes on the normal image identified above,
using the updated CopperStart main ROM `222ae453...` and desktop extension
`7222d9d1...`, and the owned original Kickstart 3.1 ROM `8c8a0cf0...`.
The full hashes are retained in
`artifacts/normal-full-verification-v1/plan.json` and the math receipts.
The packaged Shell and `SYS:Libs/mathtrans.library` are unchanged.

The independent final receipt is
[`artifacts/normal-full-verification-v1/acceptance.json`](../artifacts/normal-full-verification-v1/acceptance.json),
produced by `artifacts/audit_normal_full_verification.py`. It passes 60
checks with no errors and binds 53 runtime result records:

- Twenty CopperStart Shell cases and 31 Kickstart cases. These cover all 31
  built-in command identities, quoting, script substitution and control flow,
  error redirection, Path with 65 arguments, CLI path isolation, CD, System,
  resident commands, Run, NewCLI/NewShell, inherited Prompt, ASK, recovery,
  stream ownership, native ABI and runner ownership under allocation failure.
- Two additional boots of the untouched normal ADF. After the normal
  `C:BootShell` startup, the guest executes the complete 60-contract built-in
  corpus and all 17 math vectors, then exits with no remaining Shell, child or
  background Shell tasks. Their actual result disks and traces are in
  `exact-corpus/copper-ready.*` and `exact-corpus/kickstart-grouped.*` beneath
  the verification directory. CopperStart supplies read-only test assets via
  temporary public DOS C:/S: assigns; SYS: remains the original boot image.

The command fixtures preserve the normal staged binaries. CopperStart's
matrix also preserves the normal startup; Kickstart's supplemental HDF matrix
uses guest keyboard-injection startup helpers. The separate unchanged-ADF
runs establish normal startup on both profiles. Test helpers and observers are
hash-bound, and no guest state is patched to make commands pass.

The built-in comparison matches all 57 shared original-V40 contracts and
separately passes three retained extensions. CopperStart passes all 60 values
and physical console reset. The full owner probe reaches `0x3fff`, including
real allocation failure 103, on both systems. Four Kickstart lifetime fixtures
retain equal warmed memory samples within each fixture. The host command and
ownership regressions pass 1,177 tests with no skips or failures.

Fresh `math-copper` and `math-kickstart` receipts plus `math-acceptance.json`
verify loading V40 from `SYS:Libs`, all 17 public vectors, 20 identical raw
result/defined-CCR samples, six overflow/trap stages, version rejection,
multiple opens and expunge/reload. They include large trig arguments,
out-of-range SPSincos, extreme negative sinh exceptions and cosh saturation.
The 16 installed FFP vectors, 12 arithmetic kernels and divide exception
entry all point into CopperStart ROM. These cross-platform samples compare
the CopperOS mathtrans implementation; its documented numerical tolerances and existing
original-library comparison remain applicable.

The final audit independently rereads guest files and rechecks the source
snapshot, native public-interface gate, SYS staging, original reference result
disks, ROM/emulator identities and packaged HUNK relocation at two addresses.
The accepted scope is 68000 emulation with 2 MiB Chip RAM: CopperStart in the
Lightweight emulator, Kickstart's command matrix in WinUAE, and both exact-ADF
corpora in Lightweight. This accepts the recorded complete Shell coverage,
without claiming exhaustive language conformance or physical-hardware testing.

