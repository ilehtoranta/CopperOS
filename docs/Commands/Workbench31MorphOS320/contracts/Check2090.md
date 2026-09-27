# Check2090 contract

Profile: `wb31`. Goal step: CC22. Status: partial source/media and bounded
native evidence only; this is not a shipping or PURE admission.

The Workbench 3.1 installation image contains `C/Check2090`, a 220-byte HUNK
candidate with version `$VER: check2090 39.2 (12.1.93)` and SHA-256
`dd1c09556296ece881678cf5d37b9635655eee433f6887443c1bbec5d83a2462`.
The command has no user argument template in the captured binary. Its observed
operation is:

1. The captured helper requests `dos.library` and `expansion.library` version
   37. The replacement uses the verified capability floors: DOS 36 for its
   startup/cleanup calls and expansion 33 for the V33 `FindConfigDev` vector.
2. If expansion cannot be opened, return `RETURN_FAIL`.
3. Call `FindConfigDev(NULL, $202, 1)` for the A2090 controller.
4. Return `0` when no controller is present, `1` when the controller's
   `ConfigDev` byte at offset `$10` has bit 4 set, and `2` otherwise.
5. Close expansion and DOS in the owning startup path.

The candidate keeps DOS startup and cleanup in `NativeCommandStartup`, and
uses the public Kickstart `Expansion.FindConfigDev` wrapper. It has no parser,
host state, managed allocation, or persistent command state. The Workbench
entry rejects a startup message only after opening DOS, and rejects malformed
CLI argument-buffer boundaries before touching expansion.

## Bounded native receipt

`tools/Commands/qualify_workbench31_check2090_native_entry.ps1` builds the
resident entry and runs ten supplied Exec/Expansion/DOS vectors per CPU. The
vectors cover an unflagged and flagged controller, no controller, expansion
open failure, Workbench startup, malformed entry buffers, missing DOS, and
interleaved callers. Receipt:
`artifacts/check2090-wb31-native-20260921-v3/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Vectors |
| --- | ---: | --- | ---: | ---: |
| 68000 | 1,112 | `e4c13a885db7d8d20e97636fc65321b7d396b08017b5b4ec3403dac097b50c49` | 5 | 10 |
| 68020 | 1,112 | `e4c13a885db7d8d20e97636fc65321b7d396b08017b5b4ec3403dac097b50c49` | 5 | 10 |
| 68040 | 1,112 | `e4c13a885db7d8d20e97636fc65321b7d396b08017b5b4ec3403dac097b50c49` | 5 | 10 |

All three compatibility reports have one root method, no managed allocation
sites, runtime features/helpers, external native targets, exception regions,
or fatal machine-fault sites. The fixture reports no shared-image writes and
balanced invocation-owned DOS/Expansion cleanup. This is bounded ABI evidence
only.

## Required before admission

- Confirm the original DOS and Expansion minimum versions, verify the DOS36
  and expansion33 capability floors against the ROM API authority, and confirm
  exact return/result2 and `IoErr` behavior, and the ConfigDev flag
  interpretation on a disposable
  Workbench 3.1 guest.
- Compare the candidate against the original binary and guest on controller
  present, absent, flagged, expansion failure, and repeated checks.
- Prove resident reuse, repeated and interleaved calls, stack bounds, and
  process cleanup under the real launcher while preserving PURE/resident
  classification.
- Bind the helper to the installation-media profile and complete
  licensing/dependency review. Extracted installer media contents must not be
  redistributed.
