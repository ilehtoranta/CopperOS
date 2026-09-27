# GuessBootDev contract

Profile: `wb31`. Goal step: CC22. Status: partial source/media and bounded
native evidence only; this is not a shipping or PURE admission.

The Workbench 3.1 installation image contains `C/GuessBootDev`, a 680-byte
resident installer helper with version `$VER: guessbootdev 39.3 (6.8.92)` and
SHA-256
`b0f61f0852e877b4e7b219ca2ea5a1d55bd9c5b9c5e13b643d5ca3b10d9dcf88`.
The captured command template is:

```text
BOOTDISKNAME
```

The installer invokes the command as `GuessBootDev >ENV:BootDev` and consumes
the printed device name as the boot-device value. The bounded implementation
opens DOS at its DOS36 capability floor, retains the utility lease at its
existence floor (the replacement calls no utility vector), and opens
expansion at its V33 boot-node ABI floor. It parses the optional
`BOOTDISKNAME` with Kickstart `ReadArgs`, and
keeps the result array, RDArgs lease, 300-byte name buffer, process window
sentinel, locks, boot-node traversal, and all cleanup invocation-local.

It locks `SYS:` and the requested boot volume, compares the locks with
`SameLock`, scans the expansion boot-node list for filesystem nodes, ignores
disabled or malformed filesystems, selects the highest signed boot priority,
and prints the selected BSTR device name followed by `:\n`. When no usable
candidate is selected it falls back to `NameFromLock(SYS:)` and prints that
name followed by `:\n`. Normal completion retains the original helper's
`RETURN_FAIL` result convention; parser and library failures preserve their
DOS `IoErr` through resident cleanup.

## Bounded native receipt

`tools/Commands/qualify_workbench31_guessbootdev_native_entry.ps1` builds the
resident entry and runs sixteen supplied Exec/DOS/utility.library/
expansion.library vectors per CPU. The vectors cover highest-priority and
signed-priority selection, disabled and unusable nodes, lock fallback,
missing locks, parser and library-open failures, startup guards, missing DOS,
and interleaved callers. Receipt:
`artifacts/guessbootdev-wb31-native-20260921-v2/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Vectors |
| --- | ---: | --- | ---: | ---: |
| 68000 | 3,820 | `11c115d5826daf74351326be8956bb2377b173ca196870cf2c96d063c0684d56` | 13 | 16 |
| 68020 | 3,872 | `511a9621e4a3a8c9b05b471acbe9c1e9668a533809b67180c6003f331c33bc73` | 13 | 16 |
| 68040 | 3,816 | `60eef3667f58b0cdaf9e04b428afcc3c25df53e095df2eec75750d4a55f2c1f2` | 13 | 16 |

All three compatibility reports have no managed allocation sites, runtime
features/helpers, external native targets, exception regions, or fatal
machine-fault sites. The fixture reports no shared-image writes and balanced
invocation-owned cleanup. This is bounded ABI evidence only.

## Required before admission

- Confirm the original DOS, utility, and Expansion minimum versions, verify
  the DOS36/utility0/expansion33 capability floors against the ROM API
  authority, and confirm exact `ReadArgs` grammar, boot-node offsets,
  diagnostics, result level, and
  `IoErr` precedence on a disposable Workbench 3.1 guest.
- Compare the candidate against the original binary and guest for every
  requested-lock state, signed priority ordering, filesystem startup and
  environment variant, disabled node, fallback, failure, and mixed-caller
  case.
- Prove resident reuse, repeated and interleaved calls, stack bounds,
  process-window restoration, lock and library cleanup, and installer launcher
  behavior while preserving the original PURE/resident classification.
- Bind the helper to the installation-media profile and complete
  licensing/dependency review. Extracted installer media contents must not be
  redistributed.
