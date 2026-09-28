# Authored Workbench 3.1 guest command probe

This diagnostic is a normal 68000 CLI HUNK. It runs inside an original guest using public Kickstart 3.1 APIs. Its build script does not boot an emulator, substitute OS vectors, or alter reference media.

Build into a fresh evidence directory:

```powershell
./tools/Commands/Workbench31GuestProbe/native/build_probe.ps1 -OutputDirectory ./artifacts/my-fresh-probe-build
```

The output executable is `CopperProbe`; `build-receipt.json` binds the source snapshots, compiler/SDK binaries, HUNK and compatibility report. Build success establishes native compilation only. The resident compiler profile avoids shared-image writable runtime state; it does not classify this diagnostic as a Shell-resident command or establish original-command parity.

After installing this authored HUNK into a **derivative diagnostic guest**, invoke it from a normal CLI, for example:

```text
CopperProbe COMMAND "C:Version dos.library" TOKEN 26092601
```

The template is `COMMAND/A,TOKEN/N/A`, parsed by DOS `ReadArgs`. Quote the complete command string when it contains spaces. `TOKEN/N` is a DOS LONG; the record preserves its 32 bits and the analyzer should compare them as an unsigned token. Use a unique positive token for each derivative run.

The probe opens `NIL:` with `MODE_OLDFILE` and `RAM:CopperProbe.Output` with `MODE_NEWFILE`, then calls `SystemTagList` with `SYS_Input` set to that valid NIL handle, `SYS_Output` to the RAM file handle, and `SYS_Asynch=FALSE`. Input and output are distinct and remain caller-owned during this synchronous call. The NIL input supplies EOF instead of interactive input. No separate stderr capture is claimed; no post-3.1 `SYS_Error` tag is used.

The probe captures the System return code and immediately calls `IoErr`. That error field is **post-System caller IoErr**, not a proven copy of the child's `pr_Result2`. A command return code such as 10 or 20 is a valid captured command result. System returning -1 is a probe execution failure.

After System returns, the probe flushes output, seeks to EOF and then to the beginning. The second Seek's previous-position return supplies the full output length. It reads up to 4096 bytes through public DOS `Read`, retaining partial output on failure. Output larger than 4096 bytes is flagged as truncated and fails capture qualification. It closes both handles and releases parser/library leases before committing terminal status.

## Public discovery and record layout

The probe allocates a 96-byte record and a separate 4096-byte output buffer using `AllocVec(MEMF_PUBLIC | MEMF_CLEAR)`. It obtains a port from `CreateMsgPort`, assigns node priority zero, and points `ln_Name` at the record's ASCII `CopperOS.CommandProbe.v1` string. The name occupies bytes 0–24 including NUL; bytes 25–31 are zero. `AddPort` publishes the port after initialization. The port's message list and signal task remain as initialized by Exec. A FindPort/AddPort critical section under Forbid prevents duplicate named markers; an existing marker is never modified.

Discover through Exec's public port list using the exact name. Let `record` be that port's `ln_Name` pointer and `header = record + 32`. The following are 32-bit big-endian LONGs:

| Header offset | Field | Meaning |
|---:|---|---|
| 0 | magic | `0x43505242` (`CPRB`) |
| 4 | version | 1 |
| 8 | token | Parsed TOKEN bits; zero before successful parsing |
| 12 | stage | See stages below |
| 16 | commandReturn | Signed System return; initialized to -1 |
| 20 | postSystemIoErr | Signed immediate caller IoErr after System |
| 24 | outputLength | Captured bytes, from 0 through 4096 |
| 28 | probeError | First diagnostic failure code; zero on successful capture |
| 32 | process | Current process from `FindTask(NULL)` |
| 36 | outputBuffer | Public output allocation; zero before allocation succeeds |
| 40 | outputCapacity | 4096 |
| 44 | port | Published Exec message-port pointer |
| 48 | flags | Progress/capture bitmask below |
| 52 | errorIoErr | Immediate first failing operation's IoErr, or specified non-DOS error value |
| 56 | recordBytes | 96 |
| 60 | reserved | Zero |

Stages:

| Value | Stage |
|---:|---|
| 1 | Published; DOS opening/argument parsing has not finished |
| 2 | Arguments parsed and token stored |
| 3 | Input and output files opened |
| 4 | System call running |
| 5 | System returned; result and immediate IoErr stored |
| 6 | Flushing/seeking/reading output |
| 100 | Complete capture |
| 200 | Failed capture |

Flags:

| Bit value | Meaning |
|---:|---|
| 1 | Arguments parsed |
| 2 | System invoked |
| 4 | System returned |
| 8 | Full output captured |
| 16 | Output truncated |

A complete capture requires stage 100, flags exactly 15, probeError zero, the expected token, valid mapped record/output regions, and consistent port/process ownership. Stage 200 and truncation must never be accepted as command parity. Terminal stage is written last after all result/output/flags/cleanup fields. Intermediate stages are progress only.

Probe error codes:

| Value | Meaning |
|---:|---|
| 0 | None |
| 1 | DOS V39 unavailable; errorIoErr is 122 |
| 2 | ReadArgs failed or required result pointers absent |
| 3 | Public output allocation failed; errorIoErr is zero |
| 4 | RAM output file open failed |
| 5 | System returned -1 |
| 6 | Seek failed |
| 7 | Read failed or returned premature EOF/invalid count |
| 8 | Output exceeds capacity; errorIoErr is zero |
| 9 | Output or input Close failed |
| 10 | NIL input open failed |
| 11 | Output Flush failed |

The first probe failure is preserved; later cleanup cannot replace it. Post-System return/IoErr are independent of this failure record. Record allocation failure or CreateMsgPort failure occurs before publication and cannot supply a marker. A second invocation finding an existing marker returns 20 without replacing it. The capture controller must reject missing or stale-token markers.

## Retention and termination

After terminal publication, the process loops in public `Exec.Wait(0)`. It does not return to the Shell, unload its HUNK, remove its port, or free record/output memory. This keeps all discovered pointers and code valid for the diagnostic snapshot. The record is ordinary writable allocated memory; no HUNK code or constants are used as a mutable marker.

**Do not send messages to this port.** It is a discovery marker with no request/reply handler. Stop the diagnostic by rebooting or disposing of its derivative guest. Reboot before the next probe. This deliberate lifetime is suitable for bounded disposable captures, not a resident service implementation.
