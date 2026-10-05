# CString conditional-literal fix — 2026-10-06

## Cause and correction

The literal-address intrinsics discarded their SSA argument. Emission then selected the physically preceding `ldstr` token. A branch could supply a different literal to that same conversion, so the generated address depended on CIL layout rather than the selected value.

Lowering now follows the actual operand through copies and phi nodes, including loop-carried values. Literal leaves become native addresses and the original control-flow edges select among them. A separate native graph preserves managed-string layout for other consumers. Direct literals retain their previous machine graph and allocation. Unsupported provenance is diagnosed. `ValidateLiteralOperands` remains accepted for client compatibility; correctness validation is unconditional and literal-only merges are supported. Size-policy defaults and ROM policy exclusivity are unchanged.

The allocated backend supports these merges. Legacy emission paths safely reject a conversion entered through a merge; they do not silently choose a fallthrough token. This does not provide runtime string-to-C-string conversion.

## Affected commands and fixture corrections

* **ModList:** the fixture previously rendered revisions regardless of the selected format. It now checks that selection and reads exactly the arguments consumed by the format. The command also moves priority into the fourth argument cell when revision is omitted. Tests cover one and three records without revisions, Ctrl-C, and interleaved invocations of both revision profiles.
* **TaskList:** its fixture used the numeric PPC stack field to infer the 68k field's format. It now distinguishes the first field correctly and covers unknown usage for ordinary and CLI rows. The compiler fix also corrects `StateText`: a waiting task with pending signals prints `wait`; zero signals print `dead`. The earlier expected `dead` output for waiting tasks codified the compiler bug.

Both old shipping binaries fail the strengthened fixtures on all three CPUs. Candidate fixtures verify the intended source contract, rather than requiring equality with incorrect old output.

## Executable measurements

| Command | Before bytes | Corrected bytes | Change |
| --- | ---: | ---: | ---: |
| ModList | 2,360 | 2,424 | +64 |
| TaskList | 17,176 | 17,340 | +164 |
| Complete 61-command set | 244,632 | 244,860 | +228 |

The other **59 executables are byte-identical**. Reboot remains **516 bytes**, Execute **6,168**, List **10,232**, and Search **10,664**. These are correctness costs, with no size savings claimed. ModList and TaskList are MorphOS-only commands and receive no comparison with PPC originals.

## Frozen inputs and publication

Baseline compiler: `9e61ea73aa67082c2f72928ee480b09050117929`. Compiler fixes: `bf256d4` and `1234d0b` in `CopperSharp68k-wt-stack-size`, branch `codex/list-search-stack-size`. Command/fixture correction: `2580610` in CopperOS. The primary dirty CopperSharp checkout was preserved.

The complete comparison uses identical accepted assemblies and dependencies, one copied compiler payload, MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, and symbols disabled. Existing per-command size selections are retained; the compatibility-only validation flag adds no optional optimization. The corrected ModList assembly is measured separately against the same pinned dependencies and compiler. Compiler payload, SDK/input identities, flags, map metrics, method inventories, native receipts and hashes are recorded in the JSON report.

A complete source publish was also staged. Its compiler payload changed during investigation, so those exploratory images were not published. The final accepted inventory was recompiled with one frozen final compiler payload, combined with the separately qualified ModList source correction, and verified before refreshing `out/C`. All 61 published hashes match final staging. Previous output and all experiment images remain under `artifacts/cstring-fix-20261006`; no stale files needed removal.

## Qualification and costs

* **1,658 focused compiler tests passed**, including the six branch-liveness regressions and existing literal embedding tests.
* Conditional, explicit, local, nested, mixed managed/native, and loop-carried CString cases execute with default and compatibility policies, disabled/bounded/fixed-point peepholes, all three compilation targets and execution CPUs, two relocation bases, both branches, and repeated invocation. Unknown-call and null provenance are negative cases.
* **843 host profile tests passed.**
* **ModList:** 18 invocations per CPU, **54 total**. Peak stack remains **148 bytes**. Historical revision-present cases add **eight instructions per printed row**; empty/error/startup cases have unchanged counts.
* **TaskList:** 57 invocations per CPU, **171 total**. Peak stack remains **664 bytes**. Matched historical cases add between zero and **200 instructions**, the latter for 100 ready tasks. Historical cost comparisons identify corrected output separately and are not correctness oracles.
* Workbench List (32), Execute (151), Eval (37), LoadMonDrvs (17), and Reboot (10) cases per CPU compare output bytes, return values, IoErr, resource events and stack limits against their identical baseline images. MorphOS List's 31 cases per CPU also pass and its image remains 10,676 bytes.

Copper68k actual product version: **1.5.1+43a3a84f79657cb4c2b32bb58d8e510e3aec9c15**. Core SHA-256: `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Every measured invocation stays within its existing configured stack limit.

## Qualification limits

Native checks use controlled DOS/Exec vectors, not original operating-system images or real DOS parsing/I/O. CopperScreen and original-image checks remain unavailable. This is a focused compiler suite, not a claim that the full repository suite passes. An exploratory `AmigaVarArg.Raw` access produced an out-of-range guest pointer; that aggregate accessor remains outside this CString qualification. Legacy merge handling is diagnostic-only, as described above.

## Receipts

* `artifacts/cstring-fix-20261006/frozen`: preserved baseline executables and input identities.
* `final-control`: complete identical-assembly comparison and fixed compiler payload.
* `modlist-source` / `modlist-final`: separately compiled source correction and native execution receipts.
* `accepted`: complete final image/map inventory.
* `publication/receipt.json`: published hashes and previous-output inventory.
* `tests`: compiler and host TRX receipts; `native-summary.json`: instruction and stack comparisons.
