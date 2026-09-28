# Disposable diagnostic ADF preparation

`prepare_probe_adf.py` prepares a **diagnostic derivative**, never an original-reference capture or a shipping image. It accepts only the hash-bound Workbench 3.1 disk 2 ADF and a separately authored probe with an explicit expected SHA-256. Original media is read-only throughout. No bootability, native command parity, PURE admission, or successful guest execution follows from a prepared receipt.

Run from the repository root with Python 3.11+ and installed `amitools==0.8.1`:

```powershell
C:/Python314/python.exe -B tools/Commands/Workbench31GuestProbe/prepare_probe_adf.py `
  --source-adf '<external original Workbench disk 2 ADF>' `
  --source-sha256 a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985 `
  --probe '<authored CopperProbe HUNK>' `
  --probe-sha256 '<expected lowercase SHA-256>' `
  --output-directory D:/TestData/CopperOS-Diagnostics/<fresh-name> `
  --command 'C:Version dos.library' --token 20260926
```

The output must be a nonexistent immediate child of `D:/TestData/CopperOS-Diagnostics`, outside the repository. Existing outputs and aliased input files are rejected. An unsuccessful attempt is retained for inspection with `preparation-failed.json`; choose a new directory for another attempt. The tool does not remove or reuse failed directories.

For a paired Version diagnostic, the backward-compatible `--version-replacement` and `--version-replacement-sha256` options remain limited to a `C:Version` invocation and the original member hash `dc2f55cd48b37bd1efecdbf07d38757463129dd9076ea6bf1b5d967f2189f224`.

For another existing C command, use the generic replacement options and name the same target in the probe invocation:

```powershell
python -B tools/Commands/Workbench31GuestProbe/prepare_probe_adf.py `
  --source-adf '<external original Workbench disk 2 ADF>' `
  --source-sha256 a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985 `
  --probe '<authored CopperProbe HUNK>' --probe-sha256 '<expected lowercase SHA-256>' `
  --output-directory D:/TestData/CopperOS-Diagnostics/<fresh-name> `
  --command 'C:Break' --token 20260927 `
  --command-replacement '<candidate HUNK>' `
  --command-replacement-sha256 '<expected lowercase SHA-256>' `
  --command-replacement-path C/Break
```

The generic mode replaces exactly one existing ordinary `C/<name>` member on the disposable derivative. The named path must exist in the audited disk inventory, have an empty comment, and match the command being invoked. The candidate is independently checked as a narrow single CODE HUNK and retained as `command-replacement.hunk`. It must not alias the original ADF, the probe input, or the output directory.

Version substitution preserves exact original metadata, including the protection word, comments, owner fields, and date. The audited disk's `C/Version` protection word is **0**, so no PURE bit is added. A synthetic test separately exercises retaining a preexisting P bit. Retaining a protection bit for a controlled diagnostic does not certify candidate reentrancy, resident lifetime, PURE admission, or shipping eligibility. Original and candidate runs keep the same `C:Version ...` command text, so Shell diagnostics require no path normalization.

The successful directory contains:

- `probe.adf`: the derivative medium.
- `probe.hunk`: the exact authored input snapshot.
- `startup-original` and `startup-patched`: exact startup byte snapshots.
- `probe-prepare.json`: hashes, writer identity, operation record, and independent validation.
- `version-replacement.hunk`: exact candidate snapshot, only in legacy Version substitution mode.
- `command-replacement.hunk`: exact candidate snapshot, only in generic C-command substitution mode.

The probe receives one quoted `COMMAND` value and a decimal `TOKEN/N` value. The builder uses Amiga Shell `*` escaping, rejects line breaks/control characters, and inserts one line directly before the unique executable `EndCLI`, preserving the existing line terminator and every original startup byte. Tokens deliberately use the nonnegative signed LONG subset `0..2147483647`; the receipt contains that integer, which is also the corresponding unsigned 32-bit value.

The external writer process receives only the derivative directory and captured metadata, never the original ADF path. It snapshots and hashes the installed `amitools` Python sources and runs them with isolated Python, without cached bytecode. It opens the existing copy, adds `C/CopperProbe`, replaces `S/Startup-Sequence`, and restores affected metadata. It never formats or creates a filesystem or installs boot code. The probe has ordinary protection, with PURE and SCRIPT clear; the original startup SCRIPT protection is restored.

The source's affected entries have empty comments. The tool rejects a different shape because `amitools 0.8.1` has a nonempty-comment mutator failure. Unrelated comments are untouched and checked. The installed block-device writer restores the captured root block after the volume closes: root entries do not change, and this preserves original reserved bytes and stale name padding. The exact original DOS1 root word 126 equals the bitmap block (881); this is recorded as residue, not followed or counted as an additional allocation. The source also has 49 free bits beyond the actual image bounds; these are preserved and never counted as usable space.

Independent validation uses the repository's read-only Inventory parser plus explicit block ownership and bitmap checks. It requires the exact original tree plus the probe, original boot blocks, exact root block, restored metadata, exact injected startup, unchanged unrelated payloads and allocations, and preserved bitmap padding. It rejects changes in free-to-free blocks, unrelated data slack, unrelated extension blocks, and reserved header bytes. Only C/S directory hash tables and the required sibling hash-chain links may vary, in addition to new/replaced file blocks and the validated allocation bitmap. A selected replacement additionally permits that one C member's original/new file blocks and changed size/allocation, while verifying exact candidate contents and original metadata. Without an explicit replacement option, a changed original command is rejected as an unrelated-file mutation.

Receipt consumers must require `schema_version: 1`, `kind: "workbench31-diagnostic-probe-adf"`, and `status: "prepared"`. The agreed input bindings are `source_adf`, `derived_adf`, and `probe`, each containing `path`, `bytes`, and `sha256`; `source_before_sha256` and `source_after_sha256` must equal the source identity. `invocation` contains `command`, integer `token`, exact `line`, and guest path `C/CopperProbe`. All inputs, output, startup snapshots, installed writer sources/Python, builder, HUNK parser, and independent reader are hash-bound. Prepared receipts still explicitly mark guest execution, shipping, original-reference status, and PURE admission false.

The additive schema-1 `replacement` field is null without substitution. In candidate mode it contains the exact `guest_path` (for example `C/Version` or `C/Break`), `original_command` (guest `path`, `bytes`, `sha256`, `metadata`), `candidate` (input identity), `snapshot` (retained identity), `hunk_structure`, `original_metadata_preserved: true`, `experimental: true`, `pure_admission: false`, and `shipping: false`. Capture must explicitly bind the supplied candidate to both candidate and snapshot identities. `tool_sources_before` and `tool_sources_after` bind builder, Inventory reader, and HUNK reader; a source change during preparation fails before publishing a prepared receipt.

Focused tests use only hand-authored synthetic DOS1 bytes; no original media is patched by tests:

```powershell
C:/Python314/python.exe -B -m unittest discover `
  -s tools/Commands/Workbench31GuestProbe -p test_prepare_probe_adf.py -v
```

The tests cover exact LF/CRLF insertion and quoting, malformed/ambiguous startup, command/token rejection, narrow HUNK bounds, independent allocation bounds, bitmap mismatch, path/alias/overwrite denial, actual isolated installed-writer roundtrips, metadata/protection and padding preservation, and raw-block tamper rejection. Candidate tests force an extension block, retain original P/date metadata, and reject wrong candidate bytes, altered metadata, and substitution without the explicit option. A generic named-command test verifies target, path, hash, and alias checks. Source-edit detection is tested using a temporary tool-source fixture. The structural HUNK check accepts one plain CODE HUNK with the strict reader's supported relocations/symbols; it is not an entry-point or CPU qualification.
