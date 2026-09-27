# FindResident contract

Profile: `wb31`. Goal step: CC22. Status: partial source/media and bounded
native evidence only; this is not a shipping or PURE admission.

The Workbench 3.1 installation image contains `C/FindResident`, a 220-byte
HUNK candidate with version `findresident 40.1 (26.5.93)` and SHA-256
`de05148f716d4271287ba1c67b225606d14ff370ce0f756811d6188912dff437`.
The captured command strings identify the positional template:

```text
MODULE/A
```

The bounded implementation uses invocation-owned Kickstart DOS `ReadArgs` /
`FreeArgs`, public Exec `FindResident`, and DOS `PrintFault`, `IoErr`, and
`SetIoErr`. It does not inspect host state or retain managed/static invocation
state. The private entry receives and rejects Workbench startup messages before
parsing, opens `dos.library`, and releases the library through the common
startup owner.

The original binary requests `dos.library` version 37, but the replacement
uses a capability floor of version 36: its required `ReadArgs`, `FreeArgs`,
`PrintFault`, `IoErr`, and `SetIoErr` calls are available at that floor, while
the resident lookup itself is the Exec `FindResident` call. The candidate
returns success when `FindResident` resolves the module; missing modules and malformed result storage are represented as
`RETURN_WARN` with `ObjectNotFound` or `RequiredArgumentMissing` and a DOS
fault. Parser failures return the parser result and `IoErr` without entering
the lookup path. These result and diagnostic choices still require disposable
original-guest confirmation.

## Bounded native receipt

`tools/Commands/qualify_workbench31_findresident_native_entry.ps1` builds the
resident entry and runs eleven supplied DOS 36/Exec vectors per CPU, including
successful and missing lookups, parser and result-slot failures, Workbench and
missing-DOS startup guards, and interleaved callers. Receipt:
`artifacts/findresident-wb31-native-20260921-v4/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Vectors |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2,244 | `c39d551af9715d5fab75d90e5e65ccc90a713cd3733604f9a454747b84c0ae97` | 10 | 11 |
| 68020 | 2,244 | `a13bcf01ba42e94527bce327e1506cf5f4d52883fa06b13efdded44bdd5d2f22` | 10 | 11 |
| 68040 | 2,244 | `a13bcf01ba42e94527bce327e1506cf5f4d52883fa06b13efdded44bdd5d2f22` | 10 | 11 |

All three compatibility reports have one root method, no managed allocation
sites, runtime features/helpers, external native targets, exception regions,
or fatal machine-fault sites. The fixture reports no shared-image writes and
balanced invocation-owned cleanup. This is bounded ABI evidence only.

## Required before admission

- Confirm the original DOS minimum version, exact `ReadArgs` help/EOF grammar,
  diagnostics, result levels, and `IoErr` precedence on a disposable Workbench
  3.1 guest.
- Compare the candidate against the original binary/guest for found, missing,
  malformed, and repeated resident lookups.
- Prove resident reuse, repeated and interleaved calls, stack bounds, and
  process cleanup under the real launcher while preserving PURE/resident
  classification.
- Bind the command to the correct installation-media profile and complete
  licensing/dependency review. Extracted Kickstart or installer media contents
  must not be redistributed.
