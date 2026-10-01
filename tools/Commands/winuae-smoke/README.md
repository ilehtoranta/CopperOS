# WinUAE smoke test for `out/C`

Boots WinUAE with the `out/C` commands as the whole `C:` directory and runs every
case in `smoke-cases.txt` from `S:Startup-Sequence`. It answers one question: does
each binary load, run and exit on a real Kickstart, and what does it print? It is
not a parity test against the original commands.

```powershell
tools\Commands\build-c.ps1                       # out\C
tools\Commands\winuae-smoke\run-smoke.ps1        # Kickstart 3.1, all cases
tools\Commands\winuae-smoke\run-smoke.ps1 -Only Dir,eval-add -Window
tools\Commands\winuae-smoke\run-smoke.ps1 -Aros  # WinUAE's AROS ROM: harness check only
```

## Inputs

- WinUAE 6 at `C:\Program Files\WinUAE\winuae64.exe` (`-WinUAE`). `C:UAEquit` is
  copied from its `Amiga Programs` folder and ends each boot.
- A Kickstart ROM: `-KickstartRom`, else `COPPEROS_KICKSTART31_ROM`, else
  `..\..\TestData\ROM\kickstart-3.1-a500.rom` next to the repository, else
  `D:\TestData\ROM\kickstart-3.1-a500.rom`.

No disk image or Workbench file is used. The boot volume is a new host folder that
WinUAE mounts as a bootable directory filesystem (`DH0:`, volume `Workbench3.1`, see
Reference replays), with
only `C/`, `S/Startup-Sequence` and a small `smoke-input.txt`. So there is no
`LIBS:`, `DEVS:` or `FONTS:`: commands that open a disk-based library (Version,
SetFont, AddDataTypes) fail here for that reason alone.

Machine: 68000, ECS, 2 MB chip RAM, no fast RAM, one empty DD floppy drive, CPU
speed max. The empty drive is deliberate: Kickstart always mounts `DF0:`, and with
no drive behind it a `Lock("DF0:")` never returns, which is an emulator setup
artefact, not a command fault. `-StackBytes` adds a `Stack` line to the script;
by default commands run with the boot shell's 4 KB stack, like on a real machine.

## How a run works

Each case runs as `C:<Command> <NIL: >>SYS:smoke.log <Arguments>`, bracketed by
`@@BEGIN <case>` and `@@END <case> rc <RC> r2 <Result2>` lines written by the
built-in `Echo`. The script also sets `Failat 21`, so no failure stops it. Cases
run in file order and later ones use files that earlier ones created in `RAM:smoke/`.

The runner watches `smoke.log`. If it stops growing for `-StallSeconds` (default 30),
the current case is recorded as `crashed-or-hung`, WinUAE is killed, and a fresh boot
continues with the next case. A boot that never starts the script is `no-boot`.

Results go to `artifacts/winuae-smoke/<timestamp>/` (`-OutputDirectory`):
`summary.txt`, `results.json` (per case: status, RC, Result2, full output, boot
number) and `boot-N/` with each boot's volume, `smoke.log` and WinUAE config.

Status is the RC as the Shell saw it: `ok` 0, `warn` 5, `error` 10, `fail` 20. A
non-zero RC is not automatically a bug: several cases pass no arguments on purpose
to check the "required argument missing" path. Read the output column.

## Cases

`smoke-cases.txt` is `name | Command | arguments`, one per line. `SKIP: reason` as
the arguments records the command without running it (Format, Reboot, the two
requesters, WaitForNotification). Keep cases inside `RAM:` or read-only, because
the boot volume is a real host folder.

## Reference replays

`reference-replays.txt` lists capture JSONs of the original commands (repo-relative,
under `docs/Commands/Workbench31MorphOS320/reference-captures/`). Every run replays
them after the smoke cases against `out/C`; `-NoReplay` skips them, and `-Only Eval`
or `-Only ref-eval-wb31-basic` selects them like smoke cases. Two capture shapes:

- Probe script (`probe.source` + `probe.capture_text`, or `capture_text_before_nul`
  for captures whose file carried NUL padding, compared up to our first NUL too):
  the fixture script runs verbatim through its last line that writes the capture
  (usually `Echo "END"`), intervening `Echo ""` lines included, with only the
  capture target rewritten to
  `SYS:replay/<case>.txt`. The trailing `Copy`/`EndCLI` only persisted the capture
  and are dropped. The fixture must still hash to `probe.source_sha256` (LF form,
  so a `core.autocrlf` checkout is fine), otherwise the run refuses to start.
  Each probe replay gets a boot of its own, as each original probe ran on a freshly
  booted disk: probes leave `RAM:T`, assigns, residents and FailAt behind.
- Observations (`observations[]`): one case per command line, compared on output
  and return code.

Status is `match`, `mismatch` (the note names the first differing line, or the
RC) or `no-capture`. `results.json` keeps the replayed script, the expected and
actual capture, and any console output. A note says when the ROM differs from
the one the capture was taken on.

Details that make the Workbench 3.1 captures replay as they were taken:

- The capture target is whatever file the probe's first `Echo "..." >file` creates
  (`S:Shell-Startup` for most Which probes, `RAM:eval-capture` for Eval).
- The boot volume is labelled `Workbench3.1`, like the Workbench disk the captures
  ran from, because Which prints paths such as `Workbench3.1:C/Execute`.
- The Which probes look up and `Resident` `C:Execute`. If `out/C` has no Execute,
  `C/Execute` is a copy of `C:Wait` (Which only resolves it, never runs it) and those
  cases carry the note `C:Execute is a stand-in`.
- The boot volume has an empty `T/` directory, like the Workbench disk. Execute puts
  its work file in `T:` and falls back to `:T/` when `T:` is not assigned, which is
  the case in most Execute probes and in the smoke script.
- A probe's own `FailAt` applies inside it; `Failat 21` is restored after each replay.
  If a probe stops before its END line, it is still judged on its capture (note
  `script stopped before END`), and the runner gives up on it after
  `-ReplayStallSeconds` (default 10) instead of `-StallSeconds`.
- Seven Which captures end before the probe's `END` marker: the original run stopped
  there, and the captures themselves say why is not established (later FailAt 255
  probes show those same calls returning 0 or 5). Such a capture is only a prefix of
  the original output, so a capture not ending in the probe's last marker is compared
  as a prefix and reported as `prefix-match`.
