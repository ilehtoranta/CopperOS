# MorphOS Rename50.8 source observations

Recorded2026-09-06 for CC12.Rename.morphos320. Source-derived, not runtime-qualified.

The [official3.20 source archive](https://www.morphos-team.net/files/src/3.20/c.tar.bz2)
matches the goal's pinned SHA256. Its Rename version header identifies50.8,
27.11.04, matching the ISO member's version/date. This correspondence does not
prove a reproducible build or packed-executable equivalence.
Archive/member hashes are in `artifacts/rename-morphos-source-inventory.json`.
Rename source SHA256: `8df08364f828f1d40c9d1238c0971f2b5a947fdbe491d4d7da36c2e967421760`.

The file attributes AROS copyright1995–2001 but contains no explicit license
grant in its header; the archive contains no license/copying/legal-named member.
Resolve applicable reuse terms before adapting code. No vendor source or
disassembly has been added to the repository. Behavioral observations below
must drive independent implementation and runtime checks, not assumed classic parity.

| Area | Source-observed behavior differing from or refining Workbench |
| --- | --- |
| Startup/parser | Opens DOS37. Same FROM/A/M,TO=AS/A,QUIET/S template. Parser failure prints a Rename-headed fault and returns10; missing DOS returns20. |
| Storage | One cleared AnchorPath allocation with two2048-byte path regions; no classic four-vector layout. Allocation failure captures ambient IoErr. |
| Preflight | Ends successful initial matching before locking/examining destination. Initial failure205 has the familiar prefix. |
| Destination | Uses a DOS FIB; allocation/Examine failure prints a Rename-headed fault and returns20. Entry type>=0 identifies a directory. Nondirectory wildcard/multiple-source rejection returns20, not classic0. |
| Single source | SameLock0 selects direct mode. A single source with directory destination forces quiet even when it is a different object. |
| Direct mutation | ParsePattern capacity2048. Pattern or Rename failure captures error, prints prefix and returns20, including synthetic error0. |
| Directory name | NameFromLock120 fails; other failures or empty output fall back to supplied TO. Composition uses FilePart of matcher path and AddPart capacity2048. |
| Traversal | Rename occurs before MatchNext. AddPart failure ends the search, reports120 and sets IoErr120. Rename failure captures error and ends search before printing prefix. |
| Match termination | Nonzero matcher result ends that pattern; subsequent sources are attempted. Final found-break flag produces fault304 and return5; otherwise return0. |
| Cleanup | Unlocks destination; selected nonzero error is printed with null header and restored through SetIoErr; then frees workspace. FreeArgs and library close follow in main. |

## Required next steps

- [x] Retain an independently authored body without importing vendor implementation code.
- [ ] If future work copies/adapts vendor implementation, resolve its exact reuse terms first; this conditional requirement does not block the current independent body.
- [ ] Establish actual packed50.8 runtime/template evidence and compare against these source observations.
- [x] Specify safe ownership for initial failed matching and failure-prefix strings referenced after MatchEnd; do not recreate use-after-release risks.
- [x] Specify fallback-copy termination and exact native AnchorPath layout/allocation arithmetic from SDK evidence.
- [x] Implement separate MorphOS body using public DOS ReadArgs, MatchFirst/Next/End, Lock/Examine, SameLock, ParsePattern, NameFromLock, FilePart, AddPart and Rename.
- [ ] Exercise all differing return/quiet/break/error branches, directory mutation ordering and resident concurrency.
- [ ] Complete installed profile selection, required P qualification and packaging without selecting the Workbench implementation as a substitute.

No implementation, runtime, purity or packaging gate is closed by these observations.

## Native layout and safety decisions (2026-09-06)

Checked `CopperSharp68k/Sdk.Amiga/DOS/DosLayout.cs`, `Structures.cs` and
`DOS.cs`: classic AnchorPath size282, buffer offset280, found-break offset12;
FilePart uses LVO-870/D1 and AddPart uses LVO-882/D1/D2/D3. These public
Kickstart calls are already exposed; no replacement path parser is needed.

For a classic68k candidate, the source's allocation expression translates to
4378 bytes (282+2048+2048), destination offset2330 (282+2048), and source
path offset280 with2048-byte capacity. This is a target-layout calculation,
not an assertion about native PPC `sizeof` or identical allocation sizes.
Use named SDK constants and checked arithmetic, not guessed offsets.

Implementation rules to verify with separate candidate fixtures:

1. Track every attempted MatchFirst, successful or failed, and end it exactly
   once. End preflight before destination acquisition. No unstarted or repeated
   MatchEnd. Label added cleanup after failed initial matching as normalization.
2. Retain MorphOS ordering: mutate the selected object before advancing
   MatchNext. Do not reuse the Workbench advance-before-mutation loop.
3. Preserve a bounded copy of the source path before ending a failed search
   when it is needed for diagnostics. This requires additional invocation-owned
   storage or verified stack space; do not read released matcher-owned memory.
   Its allocation failure must have an explicit tested cleanup path.
4. Destination fallback retains the supplied TO for non120 NameFromLock failure
   or empty output. Require a terminated complete string within2048 bytes;
   reject oversized/unterminated input with120 rather than silently truncating
   into a different destination. This is an explicit malformed/oversize-input
   safety policy requiring differential documentation.
5. Use FilePart and AddPart for valid directory composition. Restore the saved
   prefix terminator before each AddPart. Check AddPart before mutation, retain
   its Rename-headed120 diagnostic/SetIoErr behavior, and keep prior effects.
6. Keep selected error and ambient cleanup error separate. Preserve parser10,
   mutation failure20 including error0, and found-break5. No shared helper may
   silently replace those policies with Workbench's return or IoErr rules.
7. Keep DOS base, parser cells, locks, anchor, buffers and formatting arguments
   invocation-owned. The existing generic arguments helper's Release restores
   errors; use it only if its semantics match the selected profile, otherwise
   release the explicit owned parser/storage without that restoration.

The [AROS public license](https://www.aros.org/license.html) describes attached
and source-tree licensing in sections5.1/5.2. Its existence alone does not
establish the exact grant for this detached MorphOS archive and its changes.
Provenance remains unresolved for copying/adapting that source; these notes do
not assert legal clearance. Public ABI/layout work and independent behavioral
qualification can continue without importing vendor implementation code.

## Independent candidate implementation and bounded qualification (2026-09-07)

`src/Commands/Native/NativeMorphOSRenameCommand.cs` now implements this profile
independently. It does not import vendor source. The private native entry opens
DOS37; generic CLI/Workbench startup policy is still a candidate policy requiring
original launcher comparison. Missing DOS leaves the ambient secondary error
untouched; the Workbench profile's direct122 write was not copied into this entry.

The command uses20 invocation-private stack bytes for ReadArgs/format cells,
then a4378-byte cleared workspace. Directory traversal acquires an additional
2048-byte diagnostic buffer before any mutation, so a failed Rename can end the
matcher before printing a saved source. Both allocation failure points are
checked, including synthetic failure/IoErr0. No compiler runtime services,
external native targets, exceptions, fatal-machine-fault sites or shared image
writes are admitted by the bounded compile/native checks.

The current checkpoint is
`artifacts/morphos-rename-body-qualified/qualification.json`:56 distinct scenarios,
each sequential and interleaved,112 invocations per CPU/336 total on68000/020/040.
Each uses a4096-byte guarded candidate stack; maximum observed candidate stack
write depth is228 bytes. Supplied DOS vectors consume no real OS stack, so this
is not a qualified minimum stack for an installed executable.

Coverage includes parser10, zero/nonzero errors, allocation/FIB/Examine failures,
preflight cleanup before locking, EntryType>=0 versus the distinct DirEntryType
field, same-lock direct fallback, forced single-source quiet, multiple-source
progress, FilePart/AddPart composition, mutation-before-next, first/second-item
failures and retained earlier effects in the supplied operation sequence,
NameFromLock fallback,2047-byte success/2048-byte rejection, found-break5 and
ambient output/fault/cleanup error behavior. PrintFault defaults to its public
IoErr side effect; explicit fault/output overrides and FALSE/IoErr0 combinations
are synthetic provider cases, not claims about a normal handler.

`artifacts/copy-boot-fixture/verified-morphos-rename-native.json` additionally
verifies the68000 candidate under original Kickstart3.1 DOS and its RAM handler:
ordinary direct rename, two-match wildcard move and AS alias all return0, four
mutations have exact paths, and independent Type reads after resident removal
return all three exact payloads. The same loaded image stays unchanged and
tracked parser/vector/FIB/lock/library cleanup balances. This is real public-DOS
execution of our candidate, not execution of or comparison with original MorphOS.
Opaque matcher allocations, OS concurrency/lifecycle, complete options and
rendered-output/secondary-error parity remain open.

Three-CPU binaries and exact report hashes are in
[development candidates](rename-development-candidates.json). These records do
not select an installed command, admit a P flag, or close a full shipping gate.
The earlier initial native receipt failed because its missing-DOS test wrongly
expected the Workbench-specific122 policy; it is retained as test history.
The earlier108-case receipts are superseded by this112-case checkpoint; the
first of those also used a nonstandard PrintFault default, corrected before the
current qualification. No production binary changed during these oracle fixes.
