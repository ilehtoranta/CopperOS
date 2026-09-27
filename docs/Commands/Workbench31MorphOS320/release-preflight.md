# CC08 release-record preflight

Added 2026-09-08. **Infrastructure in progress; no shipping admission.**

`tools/Commands/verify_command_release.py` provides a read-only preflight for one
proposed release record or a selected record in the existing schema-version-1
`build-manifest.json`. It does not change that manifest, stage files, set P,
build an image, or change stock Kickstart Resident admission.

The current manifest has no qualified shipping artifacts. Development and
qualification-only records must remain inadmissible. The verifier never falls
back to those records when the requested release artifact is absent.

## What is implemented

- Select command, profile, CPU, distribution and image path independently from
  the record. Require exact agreement and external-command inventory membership.
- Reject duplicate command/profile/CPU/distribution identities and
  case-insensitive destination collisions in the submitted manifest.
- Pin the selected inventory with SHA-256. Verify current local artifact, source,
  compiler, SDK, input-manifest, input-file and dependency-ABI hashes. Missing
  files or stale hashes fail; there are no timestamp or filename shortcuts.
- Require artifact byte count, a HUNK header, native entry identity, disabled
  mandatory FPU, minimum-stack metadata, and an exact NUL-terminated `$VER:` tag
  in the artifact. A header check is **not** complete HUNK or instruction validation.
- Require explicit public Exec/DOS dependency identities and minimum versions.
  This is metadata validation, not dependency-closure or service success proof.
- Derive known original purity requirements from the inventory. Clearing the
  proposed P bit cannot bypass an observed pure/resident requirement. Requesting
  P also requires purity gates. An unresolved original classification does not
  become non-pure because its currently observed requirement flag is false.
- Require the complete release-gate list below. A status string, boolean, file
  hash alone or unknown report schema cannot discharge a gate.

## Record extension to build-manifest v1

The existing `qualified_shipping_artifacts` list remains the only manifest
selection source. `--record` can inspect a separate proposed JSON object without
adding anything to that list. A record must have `release_record_version: 1`;
older qualification records are deliberately not upgraded automatically.

| Fields | Required value or meaning |
| --- | --- |
| `command` | Canonical lowercase inventory `id`, not a synthetic probe name. |
| `profile`, `cpu` | `wb31` / `morphos320`; `68000` / `68020` / `68040`. |
| `distribution`, `installed_path` | `normal-runtime` / `installation-media`; image-relative `C/<original profile spelling>`. Actual installed selection still needs its placement gate. |
| `shipping` | Must be `true` for a proposed release; it is a request, not evidence. Development-only status is rejected. |
| `path`, `sha256`, `bytes` | Current local executable path, lowercase SHA-256, positive size. |
| `entry`, `output_format`, `fpu` | Native `Type::Method` identity, `hunk`, `disabled`. Script records and additional artifact formats need dedicated adapters; they are not treated as HUNKs. |
| `version_id`, `minimum_stack_bytes` | Exact command-specific `$VER:` text; positive four-byte-aligned stack size. Neither proves stack qualification. |
| `source`, `compiler`, `sdk`, `input_manifest` | Each an object with exactly `path` and `sha256`. Relative local paths resolve against `--root`; absolute private evidence paths are allowed. |
| `inputs` | Nonempty list of distinct hash-bound `path`/`sha256` objects. Completeness remains a reproducible-build gate requirement. |
| `dependencies` | Nonempty list of objects with exactly `id`, nonnegative integer `minimum_version`, and hash-bound `abi`. Public `exec.library` and `dos.library` entries are mandatory. |
| `amiga_protection` | Integer byte containing Amiga protection bits; P is bit 5. Not Windows filesystem attributes. |
| `required_pure`, `pure_admission` | Explicit policy/request metadata. A true admission boolean is insufficient without independently validated purity and resident reports. |
| `evidence` | Map from gate names to objects with exactly `report_type` and a hash-bound `report`. |

JSON duplicate keys are rejected. A proposed record cannot supply a separate,
less strict expected profile or destination: selection comes from the caller.

## Evidence coverage and remaining admission requirements

Every record needs these gates:

```text
original-metadata-placement  full-option-semantics  native-static
reproducible-build  native-startup  kickstart31-execution
copperstart-execution  reference-differential  resource-lifetime
minimum-stack  dependency-integration
```

Required pure/resident implementations, and any proposed artifact carrying P,
also need `shared-image-purity` and `resident-lifecycle`.

The initial preflight supported no complete release gates. The subsequent
2026-09-08 adapter supports **native-static** and **reproducible-build** for the
known versioned Workbench MakeLink producer. Supply report type
`copperos-versioned-makelink-build-v1` and the hash-bound
`artifacts/workbench-makelink-filehandle-abi-20260908/qualification.json` for each of
those two gates. The adapter validates both builds and the selected CPU/artifact;
the same report cannot establish any other gate.

The current report includes the refreshed SDK FileHandle ABI sources. Its
SHA-256 is `fcebfca783daef8febb3c93fac71f47fff48510693d9d700b44b356941b00305`.
The earlier versioned build and adapter proof remain historical; their SDK/source
snapshots must not be represented as current. The refreshed three-CPU proof is
`artifacts/command-release-build-adapters-filehandle-abi-20260908/verified.json`
(SHA-256 `c4536d29b424d00cf98945096cc39ff9f427dfe7aa36faf938e8846dcc1d0f0c`).
It records 34 passing checks without skips and API/CLI rejection of every
proposal, with exactly two accepted gates and eleven remaining requirements,
including both purity gates. Clearing `pure_admission` still rejects the request.
Its separate byte-identity report proves both fresh passes match the earlier
versioned files; retained original-DOS/Version evidence therefore remains only
the earlier execution of the identical 68000 HUNK, without a new OS-run claim.

`verify_command_build.py` checks the actual compiler v2 native reports, including
zero allocation/runtime/helper/external-target/exception/fatal-site counts and
arrays, exact reachable assembly identities, entry, CPU, runtime and no-FPU build
arguments. It reconstructs the known producer's source snapshot and native
DLL/JSON/support input membership, verifies current hashes, and requires exact
managed build commands and separate per-pass compiler/root/SDK/output locations.
It recomputes the HUNK version transformation, compares both raw and versioned
bytes across two fresh builds, and checks the compiler logs and transform receipts.
This is observed reproducibility on one host, not a hermetic or cross-host build
claim. These are local build records, not signed attestations or a rerun performed
by the read-only preflight. Runtime behavior and all purity obligations remain
separate from compiler static checks.

Missing gates report `missing-report`; supplied but unsupported gate formats
report `unsupported-release-report-schema`. Invalid supported evidence reports
`invalid-release-report` and a validation error. Valid build gates appear in
`passed_gates`. No current record passes every required gate, so all current
release decisions still have `eligible_for_staging: false` and exit nonzero.
Structurally sound metadata with remaining gates has status `blocked`;
malformed/mismatched/stale evidence has status `rejected`. These are preflight
results, not changes to the active goal's status.

The optional `native-vector` evidence entry supports only report type
`copperos-native-vector-v1`, matching the existing `schemaVersion: 1` reports for
Workbench/MorphOS Rename and MakeLink. Its adapter checks the suite-to-command
and profile mapping, CPU, executable hash/size, current runner/core hashes,
required named startup/body cases and results, case count, execution and output
fields, bounded stack writes, distinct interleaved callers, one loaded image,
and zero reported shared-image writes. It requires the existing explicit denials
of real OS/parser/IO, reference behavior, minimum-stack and shipping/P approval.
It returns only supplied-vector/interleaving coverage and an empty
`satisfies_release_gates` list. It does not re-execute or authenticate a capture,
prove original-system behavior, or upgrade a fabricated report into evidence.

To enable real admission, implement the remaining gate-specific adapters against actual
qualifying producers and their raw evidence, with binary/profile/CPU,
compiler/options/SDK/dependency and complete contract coverage bindings. Include
negative controls that remove a required scenario or substitute another build.
Then integrate the resulting admission decision at the real image-builder owner
and verify staged bytes and actual Amiga metadata through image readback. Do not
remove these blockers merely to obtain a successful preflight exit status.

## Running the checks

From the repository root (PowerShell):

```powershell
$inventoryPath = 'docs/Commands/Workbench31MorphOS320/command-inventory.json'
$inventoryHash = (Get-FileHash -LiteralPath $inventoryPath -Algorithm SHA256).Hash.ToLowerInvariant()
C:/Python314/python.exe tools/Commands/verify_command_release.py --manifest docs/Commands/Workbench31MorphOS320/build-manifest.json --inventory $inventoryPath --inventory-sha256 $inventoryHash --command rename --profile wb31 --cpu 68000 --distribution normal-runtime --installed-path C/Rename
```

The current manifest must fail with `artifact-selection` because it has no
shipping artifacts. For a proposed record, replace `--manifest <path>` with
`--record <path>`. All output is JSON on stdout; input files are read-only.

```powershell
C:/Python314/python.exe -m unittest discover -s tools/Commands/tests -p test_command_release.py -v
```

The build adapter has an additional integration suite requiring an actual fresh
build report. Without the environment setting it explicitly skips and provides
no build acceptance evidence:

```powershell
$env:COPPER_COMMAND_BUILD_REPORT = (Resolve-Path artifacts/workbench-makelink-filehandle-abi-20260908/qualification.json).Path
C:/Python314/python.exe -m unittest discover -s tools/Commands/tests -p test_command_build.py -v
```

All 12 checks pass against the current report, including all three CPU builds,
cross-pass reuse, omitted source/native inputs, trailing build overrides and
remaining mandatory gates. Some checks inject changed parsed data after file IO
to exercise semantic validation; they explicitly do not claim end-to-end file/hash
tamper coverage. The separate 22 metadata/rejection tests also pass. Independent
review found and resolved the build isolation, omitted input membership and
trailing argument gaps before this checkpoint.

Tests exercise missing metadata/hashes, stale compiler/SDK/source/input/report
files, version bytes, profile/CPU/command/path substitution, path traversal,
inventory duplicates, manifest selection/collisions, pure-policy downgrades,
arbitrary passed/qualified claims, report-scope inflation, missing required
cases, count/result mismatches, absent concurrency, shared writes, stack
overflow and changed runners. They also inspect every current qualification-only
build record and development-candidate record and require rejection. Synthetic
test reports are never declared qualified; these tests prove rejection behavior
and metadata checks, not production command correctness.

CC08 remains open: full release evidence and qualified command staging with
image protection-field verification are still required. The earlier source
search found no production DiskBuilder; that observation is historical.
`tools/DiskBuilder/build_command_image.py` now implements the packaging entry
point with separate image readback. Its private filesystem fixtures do not
admit any command: the shipping manifest remains empty, and this preflight
still rejects every current command. A build-gate result is not an installed
qualified command or a bootable system.
