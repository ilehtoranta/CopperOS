# Workbench 3.1 / MorphOS 3.20 command inventory

`inventory.py` produces reference **facts**, not compatibility qualification.
It uses Python 3.11 or newer and only the standard library. Original media is
opened read-only, never mounted, unpacked into the repository, or executed.
It writes only the two derived JSON files selected by `--output-dir`.

Run from the repository root:

```powershell
python tools/Commands/Inventory/inventory.py fetch-morphos
python tools/Commands/Inventory/inventory.py extract
python tools/Commands/Inventory/inventory.py verify
python tools/Commands/Inventory/inventory.py verify-media
python -m unittest discover -s tools/Commands/Inventory -p 'test_*.py' -v
```

The fetch command is optional when the ISO is already available. Its default
destination is `D:/TestData/MorphOSReferences/morphos-3.20.iso`; `--target` may
select another location **outside this repository**. An existing mismatched
target is never replaced. Downloads must match both the published MD5 and the
full SHA-256 independently recorded for the selected image. Never commit the ISO.

`extract` and `verify-media` default to the two exact Workbench ZIPs under
`D:/TestData/TestImages/` and the ISO above. Override with:

```powershell
python tools/Commands/Inventory/inventory.py extract --workbench-root D:/Private/WB31 --morphos-iso D:/Private/morphos-3.20.iso
```

The selected ZIP basenames and archive/image hashes are in `WB_SOURCES`. Other
Workbench 3.1 ZIPs found beside them are reported as unadmitted references; the
tool does not guess their role or treat them as verified disk coverage. Extending
the selected baseline requires an explicit provenance review and corresponding
updates to the goal, source definitions, expected counts, and closure items.

`verify` validates the derived facts, command identities, profile membership,
owner stages, integrity evidence, script links, purity distinctions and open
coverage without opening private media. `verify-media` also re-extracts the
selected originals and compares both JSON objects, including every file hash.
Generation is deterministic: no run timestamps or host-specific paths are
inserted. The goal file's SHA-256 is recorded; editing it requires regeneration
and review even when its command table is unchanged.

Successful extraction/validation returns **0**, invalid evidence returns **1**.
`--strict-complete` returns **2** after successful validation while reference
closure items remain. This is intentional; a correct inventory is not the same
as complete six-disk, installed-system or runtime coverage.

## Export schema version 1

Both outputs are under `docs/Commands/Workbench31MorphOS320/` by default.

| File / field | Meaning |
| --- | --- |
| `command-inventory.json` → `commands[]` | One case-insensitive external identity per Appendix A row. `name` keeps the plan spelling; `id` is ASCII lowercase. `owner_stage` is read from the goal. |
| `reference_profiles.wb31` / `.morphos320` | Only profiles with an observed C-directory counterpart are present. This is not a claim that a command is absent everywhere else in the other OS. |
| Profile → `source_files[]` | Original spelling/path/source ID, byte length, SHA-256, file format, raw version tags, metadata and candidate templates. Multiple Workbench disks may supply the same identity. |
| Source file → `protection` | ADF AmigaDOS raw bits and decoded facts. R/W/E/D are denial bits; P is bit 5, S bit 6. ISO values remain null: POSIX/Rock Ridge permissions do not establish AmigaDOS flags. |
| Source file → `version` | A unique raw `$VER` tag matching the original filename, or null. All observed tags remain in `version_tags` with byte offsets. Packed code is not decompressed. |
| Source file → `template_candidates[]` | Null-terminated strings matching template syntax, with offsets and an explicit **candidate-only** confidence. No caller, profile/help behavior or actual use by ReadArgs is proved. |
| Profile → `purity` | Source script event IDs and observed design requirements. `required-pure` follows forced resident or installer +P evidence. The other classification is `unresolved-not-nonpure`. |
| `discrepancies[]` | Freeze has a required CC39 reconciliation record outside the initial 200-name count. |
| `shell_internal_names` | Existing 31 Shell-owned identities; not external work items. |
| `media-evidence.json` → `sources[]` | Whole archive/image hashes, exact selected roles, filesystem/volume facts and parse coverage. |
| `script_files[]` / `script_events[]` | Source script hashes and narrowly parsed additions/removals with line numbers. Script bodies are not exported. |
| `reference_discovery` | Selected/missing disk roles and unadmitted Workbench archive filenames. |
| Both → `coverage` / `open_closure_items[]` | Explicit partial closure. No command execution, installed metadata, option parity, or replacement purity is claimed. |

These JSON files are reproducible media snapshots. Subsequent contract and
implementation status belongs in the contracts/completion ledgers, not manual
edits to generated `contract_status`, `runtime_observed`, or qualification fields.

## Parsing and safety checks

ADF parsing verifies bounded 512-byte OFS/FFS blocks, directory/header/extension
checksums, parent identities, hash-chain cycles, file-block counts, file lengths
and OFS sequence/checksum fields. ZIP members are read in memory and their CRCs
are checked. Hard/soft-linked C entries require review rather than silent
dereferencing. The admitted media is FFS; synthetic OFS cases cover that reader.

ISO parsing validates both-endian fields, directory block boundaries, extents,
duplicate paths and bounded/cycle-checked Rock Ridge continuations. The complete
tree is inspected for Freeze. Unknown source changes, new C names and changed
installer instructions fail with an actionable error instead of quietly changing
the denominator or dropping metadata.

Synthetic corruption tests do not contain original binaries or disk fragments.
Ledger tests use the generated factual JSON and ensure that omitted required
commands, invented ISO flags, lost resident evidence and false completion claims
are rejected. Real-media reproduction remains `verify-media`.
