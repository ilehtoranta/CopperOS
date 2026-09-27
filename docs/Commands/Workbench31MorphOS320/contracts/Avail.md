# Avail contract

Profiles: `wb31` and `morphos320`. Goal steps: CC01, CC08, CC11 and CC17.
Recorded: 2026-09-12.

Status: bounded native bodies for both profiles; original guest parity and
shipping gates remain open. The Workbench v40 media
candidate identifies this DOS template:

```text
CHIP/S,FAST/S,TOTAL/S,FLUSH/S
```

`CHIP`, `FAST`, and `TOTAL` select the corresponding available-memory value;
with no selector the command prints the classic chip/fast/total summary using
Exec `AvailMem`. When more than one selector is supplied, the candidate body
uses the template order (`CHIP`, then `FAST`, then `TOTAL`).

`FLUSH` is parsed but currently fails closed with `ERROR_NOT_IMPLEMENTED`.
The exact v40 expunge ordering and the MorphOS 50.6 behavior still need a
versioned guest capture before this path can claim full functionality.

## Bounded native receipt

`tools/Commands/qualify_avail_wb31_native.ps1` builds the private
`NativeWorkbench31AvailEntry` as a resident HUNK for all required CPUs and
runs the supplied DOS fixture. The fixture covers summary formatting, each
selector, selector precedence, parser failure, fail-closed `FLUSH`, Workbench
startup and missing-DOS boundaries, repeated use and interleaved callers. It
verifies `ReadArgs`/`FreeArgs`, temporary format storage, `AvailMem` flags and
stack/guest-image ownership.

Receipt: `artifacts/avail-wb31-native-20260920-boundaries/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2856 | `125aa151ea5fa9357abd16884dc59e7cfdfd15a67d6dfd4ff4ac6511aafb54e9` | 11 | 11 |
| 68020 | 2852 | `83c76b2273c71df2d5d674a674fcd6016f4a7842454273d1d8aba4904ec0d27a` | 11 | 11 |
| 68040 | 2852 | `83c76b2273c71df2d5d674a674fcd6016f4a7842454273d1d8aba4904ec0d27a` | 11 | 11 |

These are development receipts, not original-system comparisons, PURE
admission, minimum-stack qualification, packaged command approval, or a
MorphOS implementation. Remaining work includes exact diagnostics and output
spacing, the expunge operation, MorphOS grammar/behavior, real guest
execution, lifecycle/failure coverage, purity policy and packaging.

## MorphOS 3.20 source-bound entry

`NativeMorphOSAvailCommand` and `NativeMorphOSAvailEntry` now provide a
separate DOS 37 resident profile. The source-observed grammar is:

```text
CHIP/S,FAST/S,TOTAL/S,FLUSH/S,H=HUMAN/S
```

The body rejects multiple memory selectors with the original warning, probes
the public-memory `FLUSH` expunge path, serializes summary `AvailMem` calls
under `Forbid`/`Permit`, and implements the source's K/M/G/B human formatter.
The packed identity and released-source contract are frozen in
`reference-captures/avail-morphos-binary-audit-20260923.json`; parser and
post-parse failures now report `PrintFault(IoErr(), "Avail")`, while the
multiple-selector warning suppresses that diagnostic as in the source.
`tools/Commands/qualify_morphos_avail_native_entry.ps1` emits
`artifacts/avail-morphos-native-20260923-fakechipp-v3/qualification.json` and
passes twelve supplied DOS/Exec vectors per CPU (36 total), covering numeric
and human summary/selector output, selector diagnostics, `FLUSH`, parser
failure, repeat and interleaved callers.

| CPU | HUNK bytes | SHA-256 | Reachable methods |
| --- | ---: | --- | ---: |
| 68000 | 4,516 | `ffebfe8e30e9bc2cddba83ffacb7266bc0ae39b7b1a72e2807a1e3a145dcccad` | 14 |
| 68020 | 4,464 | `d2caaf2c5d36274f79bcf3a679248167fc010de4b0b409298878145cd03e2ac0` | 14 |
| 68040 | 4,464 | `d2caaf2c5d36274f79bcf3a679248167fc010de4b0b409298878145cd03e2ac0` | 14 |

This is a MorphOS native ownership receipt only. The packed 50.6 binary still
needs an executed guest comparison for exact spacing, diagnostics, expunge
results, PURE/resident lifecycle, licensing and package admission.
