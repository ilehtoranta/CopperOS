# Original Workbench guest command capture

This diagnostic runs an authored 68000 probe through the original Workbench
startup CLI. The probe invokes a command through public DOS `SystemTagList`,
captures its output in guest RAM, and publishes a retained record through a named
public Exec port. The host reads saved chip/slow RAM only. It supplies no DOS
services and performs no live guest-bus reads, hooks, reflection, or PC/vector writes.

## Reproduce a bounded capture

1. Build the authored HUNK using the [native probe instructions](native/README.md).
   Keep its build receipt: compilation/provenance is separate from guest execution.
2. Follow the [disposable ADF preparation instructions](README-prepare-adf.md).
   This creates a new image outside the repository, inserts the authored probe and
   one startup invocation before EndCLI, and independently verifies the changes.
   Original ROM/ADF inputs remain untouched. Use a new token and derivative for
   each command case; never overwrite historical outputs.
3. Run the capture wrapper against that prepared image. From the repository root:

```powershell
$probeRunnerRoot = 'artifacts/workbench31-clean-runner-slowram-20260926-8c39d4a2'
$probeBuild = 'artifacts/workbench31-guest-probe-native-20260926-v2'
$probeMedia = 'D:/TestData/CopperOS-Diagnostics/<fresh-prepared-case>'
$probeCapture = 'artifacts/<fresh-capture-name>'

python -B tools/Commands/Workbench31GuestProbe/capture_probe.py `
  --runner "$probeRunnerRoot/runner-build/bin/Workbench31PassiveRunner/release/Workbench31PassiveRunner.dll" `
  --runner-qualification "$probeRunnerRoot/qualification.json" `
  --rom 'D:/TestData/ROM/kickstart-3.1-a500.rom' `
  --original-adf '<external original Workbench disk 2 ADF>' `
  --derived-adf "$probeMedia/probe.adf" `
  --probe "$probeBuild/CopperProbe" `
  --derivative-receipt "$probeMedia/probe-prepare.json" `
  --expected-token 20260926 --frames 2400 --output $probeCapture `
  --source tools/Commands/Workbench31GuestProbe `
  --source tools/Commands/Workbench31PassiveRunner `
  --source "$probeRunnerRoot/current-source" `
  --source "$probeBuild/build-receipt.json" `
  --source src/Commands/Common/NativeCommandArguments.cs `
  --source CopperOS.Portable.props

python -B tools/Commands/Workbench31GuestProbe/analyze_probe.py `
  --capture "$probeCapture/capture-receipt.json" `
  --expected-token 20260926 --output "$probeCapture/probe-analysis.json"
```

Use the token and probe artifact actually bound by the preparation receipt. The
retained runner path above identifies the qualified 2026-09-26 binary; do not
replace it with an unbound rebuild. See the [private runner notes](../Workbench31PassiveRunner/README.md)
for its public API and isolated-build boundary. No new engine build is needed to
reuse that retained binary.

`capture_probe.py` requires a fresh output directory under an existing parent.
It crossbinds the successful preparation receipt, original/derived/probe hashes,
invocation and token. It verifies the retained runner qualification, ten runtime
files and 21 engine source/PDB document matches, then hashes media, dependencies
and selected sources before/after execution. Extra source hashes are current
provenance, not independent proof that those sources produced a binary. Keep all
selected source trees stable during a capture. Process exit zero, the full frame
schedule and unchanged inputs are required; failures retain a rejected receipt.
There is no baseline-fingerprint requirement for intentionally changed media.

`analyze_probe.py` verifies snapshot identities and follows `Exec.PortList` to
`CopperOS.CommandProbe.v1`. It rejects a missing final marker, incomplete/failed
stage, bad layout, wrong token, inconsistent ownership, output overflow/truncation,
and any change/disappearance after a complete record. Stage 1 can legitimately
have token zero before ReadArgs; it gives no completion credit. The report includes
per-frame provenance, exact output hex/SHA-256/Latin-1 text, and command return code.
The [native documentation](native/README.md) defines the record and stage/error enums.

## Compare an experimental C-command replacement

The preparation tool can replace one existing C command in a separate
derivative; see its [replacement instructions](README-prepare-adf.md). Keep the
original capture/analysis and every historical artifact unchanged. The
backward-compatible Version path uses `--version-replacement '<candidate
HUNK>'`. Other commands use `--command-replacement '<candidate HUNK>'` and
`--command-replacement-path C/Break` (for example). The chosen C path must match
the command text in the startup invocation. A candidate option is mandatory
when the prepared receipt declares a replacement and rejected for an
original-only receipt. Candidate bytes and the retained derivative snapshot
must agree; original metadata preservation and
experimental/nonshipping/non-PURE classifications are checked explicitly.

After capturing and analyzing both sides:

```powershell
python -B tools/Commands/Workbench31GuestProbe/compare_captures.py `
  --original-analysis '<original case>/probe-analysis.json' `
  --candidate-analysis '<candidate case>/probe-analysis.json' `
  --output 'artifacts/<fresh-comparison-report>.json'
```

The comparator re-decodes the hash-bound saved RAM. It accepts earlier v1 original
analyses without rewriting them. Both sides must have complete stable records,
valid image/probe/runtime provenance, the same original ADF, ROM, probe HUNK,
frozen runtime and frame count. It independently reads the selected `C/<name>`
member and `C/CopperProbe` from the derivative images, checks the candidate
substitution and original metadata, and verifies the original side still
contains the reference command.
It therefore requires the retained media, probe/candidate artifacts and runtime
evidence to remain available; it does not modify them. It reads and checks the
exact selected C member from both derivatives, whether the target is Version
or another existing C command.

Command text, raw output bytes and return values are compared exactly. Only each
case's diagnostic token may differ, and it must match that case's own invocation.
There is **no whitespace, newline, case, version or error normalization**. Caller
`postSystemIoErr` is compared separately, retaining both values. An IoErr mismatch
can have `commandResultEqual: true` but never `allObservedFieldsEqual: true`.

For target-effect checks, `compare_captures.py` accepts
`--expect-owner-task-number` with either a before/after priority pair or
`--expect-owner-task-signal-bit` or `--expect-owner-task-signal-mask`. A signal
mask such as `0xF000` requires every selected bit to be clear before the command
and set afterward; unrelated bits are retained in the raw capture but do not
change that required-mask check. The comparator requires both guests to expose
a stage-3 pre-System sample and a stable terminal sample, and checks the
long-lived probe CLI's task number and signed priority or received signal
effect. These fields are decoded from saved RAM. A mismatch or missing
pre-state makes the pair fail; output/result equality alone cannot satisfy the
requested effect.

Exit zero and `captured-case-equal` require admitted evidence and equality of all
observed compared fields. `captured-case-mismatch` or `pair-rejected` saves the
full raw reported results, comparison fields and reasons, then exits nonzero.
Failed admission never earns equality. Even a successful pair establishes only
that invocation's bounded observation; it does not qualify the whole command,
child secondary errors, resident lifetime, PURE safety, or shipping.

## Evidence and limits

The [2026-09-26 aggregate receipt](../../../artifacts/workbench31-guest-command-captures-20260926-v1/qualification.json)
binds four successful original-command captures, preparation/build/API evidence,
and 24 passing combined tool tests:

| Original command | Return | Captured output |
| --- | ---: | --- |
| `C:Version dos.library` | 0 | `dos.library 40.3\n` — 17 bytes |
| `C:Version dos.library VERSION 39` | 0 | Same 17 bytes |
| `C:Version dos.library VERSION 999` | 5 | Same 17 bytes |
| Invalid numeric VERSION argument | 20 | 42 bytes including the Shell's failure line; caller post-System IoErr 115 |

These are original command observations in a **diagnostic derivative guest**,
not original-media-only runs or original-versus-replacement parity. The runner
uses the licensed Kickstart 3.1 ROM, original OS services and write-protected ADF;
the guest probe owns its output file, memory, process and public port.

`postSystemIoErr` is the caller's immediate `IoErr` after SystemTagList, **not a
proven copy of the child's `pr_Result2`**. Output is the selected stream as seen
through System's Shell, which may append diagnostics. No independent stderr
stream or post-3.1 `SYS_Error` tag is used. A nonzero command return can still be
a complete capture; System returning -1 or a probe cleanup/read error cannot.
Output is limited to 4096 bytes, and interactive input receives EOF from `NIL:`.

One `@SELF@` token in the invoked COMMAND is expanded to the probe owner's
DOS `pr_TaskNum` using the public process layout. The probe waits 150 DOS ticks
at stage 3 before invoking that command so the ordinary 60-frame snapshot
schedule can record the pre-command owner state. More than one token or a
process without a nonzero CLI task number fails closed. This placeholder is
diagnostic-probe syntax and is passed through the derivative invocation
unchanged; it is not a Shell expansion. For example,
`C:ChangeTaskPri 42 PROCESS @SELF@` changes only the disposable probe CLI's
priority. That priority remains changed until the diagnostic guest is rebooted.
See the [Amiga ROM Kernel Reference Manual](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
for the DOS Process `pr_TaskNum` and Exec Task/Node structures.

The guest deliberately retains the probe, record and output with `Exec.Wait(0)`
until disposal/reboot. Do not send messages to its discovery port; it has no
request handler. Start a fresh guest for every case. This lifetime is diagnostic,
not a resident service implementation or PURE admission. Full OS/profile
compatibility, arbitrary command behavior, replacement parity, resident lifecycle,
and shipping qualification remain separate requirements.

Focused tooling checks use authored synthetic fixtures, not patched reference media:

```powershell
python -B -m unittest discover -s tools/Commands/Workbench31GuestProbe -p 'test_*.py' -v
```
