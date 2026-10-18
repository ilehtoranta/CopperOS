# Exe2Arc admitted-window kernels — 2026-10-18

## Result

**Accepted and installed: 14,096 → 13,836 bytes**, saving another **260 bytes (1.84%)**. Format dispatch now happens once per scan window, and the fixed candidate bounds/mapping checks reuse the full admitted scratch span instead of running at every byte position.

The cross-window case falls **12,700,331 → 2,255,711 instructions (82.24% fewer)**. The complete 63-invocation vector suite falls **13,388,640 → 2,529,806 instructions (81.10% fewer)** per CPU. The cross-window peak stack falls **912 → 896 bytes**; the suite maximum stays **940 bytes**. Counts agree on MC68000, MC68020 and MC68040. These are executed instruction counts, not a wall-time or hardware-cycle speedup claim.

Installed candidate SHA-256: `85d1c3fda6beda3db95bb7e90ad74a62111bbb3f6bb498323ecec05a0c679929`. All 61 files were staged and hash-checked; only Exe2Arc changed. Installed total: **221,336 → 221,076 bytes**. Compiler passes, SDK and command options remain those of the [preceding scan qualification](exe2arc-scan-optimization-20261018.md). No compiler backend/default changes were required.

## Change

`Scan` retains range/handle/scratch admission, source-compatible window overlap, full-read checks, observations and the two match-position seeks. After a complete read it calls `FindCandidate`, whose switch selects one of five small candidate loops: RAR4, CAB, ACE, ARJ or LZH. A loop contains its selected predicate and has no per-candidate format switch. The result is a bounded index or `uint.MaxValue`; that sentinel cannot equal a real index below 102,400.

The predicates have separate checked and admitted entry paths. Standalone public header APIs still validate their supplied ranges and mapping before using the admitted predicate. The scanner uses its already-proven fixed header span. CAB's declared length/table checks and ARJ's variable-length header, CRC-tail mapping and CRC checks remain per candidate. LZH still reads byte 20 of the scan window, and still conditionally reads candidate byte 4 twice for the `lh`/`lz` alternative. No wider memory loads or speculative prefix reads were introduced.

### Bounds and lifetime proof

Let `S` be scratch start, `B = 102400`, `F` the file length, `W` the current window start, `N = min(F - W, B)` and `H` the format's admitted fixed header size.

- `HasScratch` establishes capacity at least `B`, nonzero `S`, `S <= UINT_MAX - (B - 1)` and a mapped full `B`-byte span before any I/O. The existing borrowed-buffer ownership contract requires that allocation to remain mapped until return. This lifetime is now explicit in the scanner remarks. The native adapter is readonly, refers to the command's owned workspace, and does not revoke or replace it. The command frees it after scanner completion. This is an allocation-ownership proof, not an OS mapping probe or support for a caller revoking a borrowed buffer midway through the call.
- Window admission gives `F - W > H`; every fixed `H` is below `B`. Therefore `N > H`. The reader must return exactly `N` before any candidate is inspected. Short/error reads still stop first.
- Every loop index satisfies `i <= N - H`. Consequently `i + H <= N <= B`: the fixed header lies within initialized bytes, the admitted mapping and the non-wrapping address span. Candidate addresses are nonzero. Also `F - (W + i) >= H`, proving the old fixed file-range checks. Window advancement and absolute seeks remain inside the admitted signed-LONG domain.
- LZH's scan-window byte 20 fits because its `H` is 21. ARJ's variable header can require more than its minimum prefix; those extra length, overflow, mapping and CRC checks are deliberately retained. A successful full-span mapping query entails mapped subspans; query-call counts change, payload byte-read counts/order do not.

The standalone checked APIs are not converted into unchecked public APIs. Only internal predicates accept an already-admitted span. Public offset-zero rejection and scanner offset-zero recognition remain distinct: the scanner stops at the first zero-offset marker instead of skipping to a later valid one.

## Measurements and rejected alternatives

All variants use the same frozen receiver-fixed compiler associated with CopperSharp `9eaa2aa`, matching SDK/dependencies, MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off and the same explicit code-size pass selection. The source baseline reproduced the installed 14,096-byte image byte-for-byte before changes.

| Variant | HUNK bytes | Cross-window instructions | Full vector instructions |
|---|---:|---:|---:|
| baseline | 14,096 | 12,700,331 | 13,388,640 |
| bounds | 13,568 | 3,586,817 | 3,906,043 |
| dispatch | 14,372 | 11,574,016 | 12,226,306 |
| combined | 13,836 | 2,255,711 | 2,529,806 |

`dispatch` alone grows by 276 bytes and is rejected. `bounds` alone saves 528 bytes and is a valid smaller alternative, but retains per-candidate format dispatch. `combined` adds 268 bytes relative to that experimental alternative while staying 260 bytes below the installed baseline; it removes both requested sources of overhead and executes approximately 37.1% fewer cross-window instructions than bounds-only. The intermediate bounds-only image was never installed or counted as an accepted reduction. This report retains the full-dispatch-removal version under the installed-image size ceiling.

The shared outer scanner body shrinks **892 → 686 bytes**, with a separate **424-byte** window dispatcher/loops. Removed checked candidate helpers offset the additional loop bodies. `TryValidateCandidate<NativeExe2ArcIo>` is no longer reachable in the shipping executable; it remains in source for standalone checked uses. Saved disassembly confirms the format switch precedes the candidate loops. This is source specialization with measured generated output, not automatic compiler loop unswitching or a general alias-analysis transformation.

## Qualification

- **241 component tests pass.** New regressions cover all five forward-scanned formats, two-window seeking/overlap, exact ordered guest-byte read traces, the highest non-wrapping scratch address, rejection before any I/O on mapping failure, and zero-offset stopping before a later marker. Existing tests retain malformed signatures, variable ARJ CRC ranges, LZH window-level behavior, source-specific EOF admission, short reads, failed seeks, IoErr capture and guards. The legacy source control is rejected by exactly five mapping-count regressions; all other component expectations are unchanged.
- **378 shipping-entry vector calls pass:** 63 calls per image on every CPU. Every output/payload byte, return code, IoErr, allocation/release, handle, cleanup order, preserved state, Workbench message ownership and repeated/instruction-interleaved behavior matches. Requested stacks stay 4 KiB/8 KiB; peak stack remains below the original limits. Copper68k **1.5.1**, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
- **342 native original-DOS calls pass:** all 32 authored contracts for each image and CPU, including automatic/explicit formats, naming, parser/scanner failures, Ctrl-C, cross-window and large payloads. Caller state and repeat free-memory equality pass. Ten receipt/JSON auditor controls are rejected.
- **366 independent WinUAE calls pass:** the entire 32-contract corpus for each image on each CPU, including combined cross-window/multi-chunk batches. Exact raw requests, inputs, output, payloads, return/IoErr and cleanup are audited. WinUAE **6.0.3.0**, JIT disabled, same original Kickstart 3.1 A500 ROM SHA-256 `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.

The unchanged Search-based native host/guest use the frozen Copper68k **1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63** snapshot. Native cases preserve the 3,000-frame/120-second limits, with cross-window and multi-chunk images in separate native boots. WinUAE preserves its 120-second limit and combined batches. No expected output, resource ownership or failure contract was relaxed.

### Interleaving fixture correction

The original comparator initially rejected the two interleaved ZIP/RAR rows, although each invocation passed its own output/resource assertions. The faster RAR now finished first; `Complete` appended observations in completion order, and the pairwise comparator compared that RAR observation with ZIP's earlier baseline row. The fixture now buffers each completed observation at its request index and publishes the group in request order. All per-invocation assertions, scheduling, clobbers, guards, fault cases and stack checks are unchanged. Both baseline and candidate were rerun on all CPUs with the same rebuilt runner. The auditor checks all 63 rows, exact comparable observations, runner/core/image hashes and ZIP/RAR request order. Earlier rejected comparisons and runner-build iterations remain preserved.

## Delivery and reproduction

Changed implementation: `Exe2ArcForwardScanner.cs` and `Exe2ArcHeaderProbe.cs`. Test changes: ordered vector observations, optional test-byte-read tracing and `Exe2ArcWindowKernelTests.cs`. The repository source was rebuilt with frozen dependencies/compiler and reproduced the qualified 13,836-byte executable exactly before installation. Exe2Arc's existing explicit optimization selection is unchanged; no other command source or installed file changed.

- Experiments, compiler/SDK/source/CIL snapshots, component and vector receipts: `artifacts/exe2arc-window-kernel-20261018`.
- Original DOS: `artifacts/exe2arc-window-os-20261018`.
- Independent WinUAE: `artifacts/exe2arc-window-winuae-20261018`.
- `tools/Commands/qualify_exe2arc_windows.py build --stage <stage> --sources <source-tree> --label <fresh-label>` rebuilds source variants against that stage's frozen inputs. `audit` and `publish` require `--stage`, `--os-stage` and `--winuae` evidence roots. The experiment recipe is retained and hashed in the stage. The audit verifies all source/toolchain identities, tests, positive/negative receipts and final inventory; publish preserves the complete old set and replaces only Exe2Arc after staging.

No new actual MorphOS PPC/Workbench GUI launch, native fault injection, original-OS instruction/peak-stack instrumentation or CopperScreen run is claimed. Exe2Arc has no genuine Workbench 3.1 counterpart size ratio. Workbench startup and injected failures remain covered by the vector suite; original DOS and independent WinUAE cover the real CLI/filesystem paths.

[Machine-readable qualification and hashes](exe2arc-window-kernels-20261018.json).
