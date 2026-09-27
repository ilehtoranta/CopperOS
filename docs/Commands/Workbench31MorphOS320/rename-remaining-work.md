# Rename evidence audit and remaining work

Audit date: 2026-09-17. Scope: CC12.Rename.wb31 and CC12.Rename.morphos320.
This does not close either profile or any shipping gate.

Current Workbench candidate: [development records](rename-development-candidates.json).
The latest native run is `artifacts/workbench-rename-examine-qualified/qualification.json`:
103 invocations per CPU, 309 total, on 68000/020/040 with explicit 4096-byte candidate
stacks. The recorded original-DOS fixtures cover ordinary moves, parser/duplicate
failure, nondirectory rejection, partial failure with readback, and two overlapping
resident processes. The latter are 68000/RAM-handler evidence, not every handler.

The [contract](contracts/Rename.md) remains authoritative for expected behavior.
Historical original-only observations and guarded hazards remain distinct from
replacement behavior and its explicit safety normalizations.

| Contract rows | Current evidence and remaining requirement |
| --- | --- |
| A01–A03 | The dedicated original-DOS `RunCommand` observer now executes with the licensed V4063 ROM and private 37.2 reference. The fixture binds the `FROM/A/M,TO=AS/A,QUIET/S` template, three result slots, parser-failure cleanup and exact empty-line/unterminated-quote errors. Four invocations (original/generated image × two inputs) and two comparisons pass; failed parses call no `FreeArgs` and retain the observed `UnLock(NULL)` common-cleanup vector. Receipt: `artifacts/rename-parser-edges-20260917/qualification.json`. Real positional `/M` binding, TO/AS aliases, duplicates, quoting, escapes, Latin-1 and option-like names still need original/candidate comparison. |
| A04 | QUIET progress suppression, diagnostics under QUIET and nonquiet progress covered in bounded fixtures. Complete grammar/value comparison remains open. |
| A05 | Interactive help, continuation, EOF and cancellation remain open. |
| L01–L02 | Candidate library ownership, missing-DOS122 and early break304 covered on all CPUs. Real signal clearing/timing comparison remains open. |
| L03 | Candidate Workbench message handling tested; original launcher parity remains open. |
| M01–M02 | Four AllocVec failures and parser failure116/zero covered with cleanup. Candidate extra FIB allocation failures covered separately. Real low-memory behavior and original cleanup normalization remain distinct. |
| D01 | Initial MatchFirst error selection,205 prefix and output poisoning covered. |
| D02 | Direct missing target and duplicate destination covered. Examine(FALSE)/IoErr222 direct fallback now has candidate vectors; wider actual existing-target/Examine failure comparison remains open. |
| D03 | Original/candidate multiple/wild sources rejected for regular destination with preserved result0/diagnostic; added cleanup explicitly normalized. |
| D04 | Source-lock failure and SameLock0/1/-1 dispatch covered by supplied vectors. |
| D05 | Distinct BPTRs with SameLock0 covered. Actual case-only/self-rename handler outcomes remain open. |
| P01 | Parsed-source forwarding covered; real pattern quoting/escape behavior remains open. |
| P02 | ParsePattern failure120/zero and unterminated output safely rejected; original guarded hazards retained. |
| N01–N03 | Colon/slash prefixes, valid255-byte composition, overflow, empty/failed/unterminated NameFromLock covered. Real filesystem capacity/normalization comparison remains partial. |
| W01 | Multiple source entries and a single source yielding two distinct matches now have candidate vectors (multimatch-qualified). Original/candidate wildcard two-match and AS alias effects now compare successfully on original DOS; second-match failure203 within one pattern now has candidate vectors; broader real wildcard forms remain open. |
| W02–W04 | Anchor overwrite before mutation, nonzero MatchNext232/304/103 and later MatchFirst205 covered. Real signal/error timing remains partial. |
| W05 | Directory matches, trailing slash, empty basename and `*` configuration remain open. |
| E01 | Duplicate direct failure and later directory mutation failure covered; broaden first-directory-item errors. |
| E02 | Direct and directory `Rename(FALSE)`/`IoErr0` cases are covered as explicitly synthetic provider combinations for both profile bodies. The hash-bound policy receipt checks prefix bytes, no common fault, preserved primary result0 and interleaved ownership; real handler status combinations remain open. |
| E03 | Existing target and persisted partial success covered on original RAM handler. Cross-volume/access/device/race outcomes remain open. |
| E04 | Failed VPrintf with IoErr poisoning and parser/PrintFault ambient changes are covered for both profiles on all three CPUs. `artifacts/rename-error-policy-20260917/qualification.json` binds the sequential cases and source receipts; broaden other branches and real-provider comparison. |
| R01–R02 | Candidate search/parser/vector/lock ownership tested; redundant/omitted original MatchEnd differences documented. Opaque matcher allocation leak freedom is not established by command-owned counts. |
| R03 | Candidate parser-failure and successful-cleanup `IoErr` mutations now pass for both profiles on all three CPUs, with final secondary errors kept visible and primary results unchanged. The new receipt covers the bounded cases; other cleanup APIs and original final-Process comparison remain open. |
| R04 | Supplied interleaving on all CPUs and overlapping original-DOS68000 callers pass. Minimum real-system stack and complete PURE admission remain open. |
| R05 | Bounded real-provider comparisons exist; full arguments/output/secondary-result/effects differential remains incomplete. |
| RN50-C01–C02 | MorphOS50.8 identity/P plus version-matched official source observations now recorded in [source contract](rename-morphos50-source-contract.md). Independent MorphOS body now compiles and passes112 native invocations per CPU plus bounded original-Kickstart-DOS success/readback. Original MorphOS execution/full differential, complete resident qualification and packaging remain open; see the source contract checkpoint. |

## Execution order from this checkpoint

- [x] Add single-pattern multi-match candidate vectors, including mutation of saved names and later errors (Workbench multimatch/multifailure receipts).
- [x] Close E02, E04 and R03 supplied-vector gaps without changing original error policy (`artifacts/rename-error-policy-20260917/qualification.json`).
- [x] Add the original/candidate real ReadArgs harness for the bounded A01/A03 parser-error cases; run it with the licensed ROM and private Workbench Rename reference before claiming the rows.
- [ ] Run the bound harness for A01–A05 and P01, including positional `/M`, aliases, quoting, escapes, help, continuation and pattern forwarding; preserve separate receipts.
- [ ] Run real wildcard, directory/self/case-only and relevant handler-error cases for D05/W05/E03.
- [ ] Establish original Workbench launcher and real-system stack behavior; finish purity ownership/metadata audit.
- [x] Implement the independent MorphOS50.8 body from the recorded behavioral contract, including native branch/boundary checks.
- [ ] Establish original MorphOS50.8 executable correspondence and complete profile differential; source correspondence and original Kickstart execution do not close this requirement.
- [ ] Complete profile differential and packaging gates before admitting either command or installed PURE metadata.

These steps supplement the full goal; they do not exclude other C: commands or
reduce its completion criteria. Test totals describe executed invocations, not
completed contract rows or shipping commands.
