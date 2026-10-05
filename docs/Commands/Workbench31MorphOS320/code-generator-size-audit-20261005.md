# Code generator size audit — 2026-10-05

## Conclusion

**There is concrete avoidable code-generation overhead.** Natural C# operations
still produce redundant initialization, address staging, byte normalization and
register moves. The current size policy also leaves some speed-oriented choices
in place. These are compiler improvement opportunities that do not require
removing command behavior.

This audit does **not** attribute the entire Workbench size gap to the compiler.
That requires matched implementations and inspection of the original binaries.
The Workbench image is unavailable on this host; reference sizes and hashes come
from the preserved verified receipts. No comparison to PPC MorphOS originals is
made.

No command or compiler implementation was changed. The encoded alternatives
below are static size sketches, not qualified fixes or published executables.

## Reproduction and scope

The audit reads all **61 installed MC68000 resident HUNK commands**, checks their
bytes against the accepted frozen artifacts, and decodes their mapped instruction
regions using Capstone 5.0.6. All regions decode completely. This establishes
encoding coverage, not semantic validation or absence of inline data in arbitrary
programs.

- Compiler: clean `b73f3794246be849706492df0bab2e2ba24636ec`, with matching SDK.
- Settings: resident HUNK, YOLO exceptions, memory disabled, fixed-point
  peepholes, symbols disabled, existing per-command size settings.
- Execute: the accepted **6,376-byte** executable, SHA256
  `676e9db33b760a8fc82e92c1b74d4f99ee15868ce7bb14bab05c0e2ba58cb060`.
- The unrelated dirty primary compiler checkout is outside this audit.
- [Machine-readable measurements](code-generator-size-audit-20261005.json)
  include image/map/compiler/source hashes, all command sizes, pattern locations,
  encoded alternatives and the compiler comparison receipts.
- Local scripts, complete disassemblies and isolated builds are retained in
  `artifacts/codegen-size-audit-20261004/`; the audit began before midnight.

## Where the bytes go

| Installed set | Bytes | Share of executable bytes |
| --- | ---: | ---: |
| Mapped instruction regions | 227,566 | 91.9% |
| Constant regions | 13,696 | 5.5% |
| HUNK records, relocations and alignment | 6,422 | 2.6% |
| **Total** | **247,684** | **100%** |

Across the instruction regions, register-to-register moves occupy **31,128
bytes**, explicit stack references **69,018 bytes**, and internal BSR instructions
**8,464 bytes**. These categories overlap. They include useful work and must not
be read as removable-byte estimates.

| Command | Executable | Instructions | Register moves | Stack references | Workbench 3.1 bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Beep | 268 | 192 | 22 | 52 | — |
| Check2090 | 516 | 424 | 30 | 124 | 220 |
| Reboot | 524 | 450 | 40 | 120 | 84 |
| Execute | 6,376 | 5,974 | 894 | 1,244 | 4,432 |
| Dir | 7,820 | 7,556 | 1,372 | 2,742 | 3,440 |
| Eval | 9,980 | 9,776 | 1,306 | 2,786 | 2,084 |
| List | 10,648 | 9,922 | 1,750 | 5,266 | 5,108 |
| Search | 10,760 | 10,564 | 1,794 | 5,616 | 2,476 |
| Copy | 15,520 | 13,314 | 1,590 | 2,772 | 5,580 |

List and Search are particularly useful next samples: **over half their
instruction bytes explicitly reference the stack**. This identifies where to
investigate aggregate transport and spilling; it does not prove those accesses
can be removed.

Execute contains no managed allocation sites, framework features or runtime
helpers. Its remaining gap is **1,944 bytes / 43.9%**. Its internal calls occupy
196 bytes, so the BSR opcodes alone do not explain the gap. Calls also influence
caller staging, preservation and separate helper bodies.

## What the existing optimizer already achieves

Three fresh compilations use the **same accepted Execute assembly and identical
dependencies**. Only the stated optimization settings change.

| Compiler settings | Executable bytes | Instruction bytes |
| --- | ---: | ---: |
| Size policy on, peepholes disabled | 8,156 | 7,758 |
| Size policy off, fixed-point peepholes | 6,460 | 6,062 |
| **Shipping: size policy on, fixed-point peepholes** | **6,376** | **5,974** |

With the size policy enabled, enabling peepholes saves **1,780 executable bytes
/ 21.8%**. With fixed-point peepholes, the size policy adds **84 bytes / 1.3%** of
savings. These effects are not additive. The shipping control is byte-identical
to the installed executable, and its 688 peephole rewrites converge.

The alternative builds were measured, not executed. Existing qualification of
the accepted shipping executable remains recorded in the
[Execute optimization report](execute-size-optimization-20261004.md).

## Concrete findings

### 1. Guest byte access misses compact addressing modes

`CStringLength` generates this address/read/test sequence inside its loop:

```asm
movea.l a1,a0
move.l  d2,d0
adda.l  d0,a0
moveq   #0,d0
move.b  (a0),d0
tst.b   d0
```

For a zero/nonzero test, a single **four-byte** instruction can express the
memory operation:

```asm
tst.b   0(a1,d2.l)
```

The emitter already has indexed load/store support for other operations. Its
APTR intrinsic path instead constructs the address by addition and canonicalizes
the byte result, leaving later peepholes to recover some of the simplification.
The missing opportunity is preserving the address expression and recognizing
that this consumer needs only a byte test.

The scan finds **42 specific ten-byte byte-read sequences**, nine in Execute,
that could use six-byte `MOVEQ + indexed MOVE.B` sequences when the address
temporary is dead. That condition requires liveness analysis. The full helper
sketch below combines addressing with better register assignment and testing.

Compiler locations:
`M68kCodeGenerator.Allocated.cs:4690` and `:8560`.

### 2. Small helpers spend too much on transport and normalization

The following MC68000 alternatives are encoded and decoded in the receipt.
They retain the selected checks in their sketches. They have **not** been
executed or incorporated into the compiler.

| Execute helper | Generated body | Encoded sketch | Body difference |
| --- | ---: | ---: | ---: |
| `Append` | 52 | 26 | 26 |
| `CStringLength` | 36 | 20 | 16 |
| `ToUpper` | 30 | 18 | 12 |
| `Put` | 16 | 10 | 6 |
| `IsLetter` | 20 | 18 | 2 |

For example, `Append` can express its current capacity check, byte store and
length increment without saving D2 or normalizing a value that is only stored
as a byte:

```asm
move.l  44(a0),d1
cmpi.l  #2048,d1
bcc.s   done
movea.l (a0),a1
adda.l  d1,a1
move.b  d0,1024(a1)
addq.l  #1,44(a0)
done:
rts
```

The memory `ADDQ` still rereads the length after the byte store, preserving the
current order even if the output aliases that field. `Put` admits the same
approach. A general compiler transformation must establish ordinary memory,
alias behavior, byte width, CCR use and the internal calling convention.

The sketches total **62 fewer helper-body bytes**, not 62 established executable
bytes: branch layout, sharing, preservation and HUNK rounding can change the
final result. Some savings overlap with the indexed-access finding.

### 3. Entry initialization duplicates later initialization

Execute `Run` clears its **56-byte local frame** at offsets `$06C4..$06D8`,
including its 48-byte `State`. After `Cli` and `AllocMem`, it clears that state
again at `$0732..$0740` before assigning its fields. The state address has not
been passed to those calls.

The initial clear sequence occupies **22 bytes**. The current definite-write
analysis stops at the first call, so it cannot establish that the state is
unobserved until its explicit initialization. A per-home analysis across calls
and control flow could remove the redundant initialization while retaining
required zeroes and all resource paths.

Reboot provides a smaller reproduction. Its result cell is cleared by frame
initialization, `default(Cells)`, and `cells.Unused = 0` before `ReadArgs`; its
separate parser-handle home is also cleared before being assigned the call
result. The explicit source assignments explain part of the duplication, but
the compiler should eliminate redundant writes to proven private storage.

Compiler locations: `M68kFrameInitializationAnalysis.cs:10` and
`M68kCodeGenerator.Allocated.cs:4825`.

### 4. Size mode is not consistently size-first

The 22-byte Execute entry clear uses four stores per loop iteration. If the
clear is retained, the same fourteen stores fit in a **12-byte** loop with one
store per iteration. It executes **eleven additional DBRA instructions**.
This is a concrete size/speed tradeoff compatible with the requested size
priority; it is an alternative to removing the redundant clear, not an
additional saving to count with it.

`EmitAllocatedScratchFrameClear` always emits the four-store body. The frame
clear planner also requires its proposed plan to be no slower. Neither receives
the general size policy.

The ordinary inliner uses `M68kTargetCostModel.Accept`, whose score combines
cycles, bytes and pressure. Cycles are multiplied by loop depth, while MC68000
bytes receive a weight of two. The estimator assigns division/remainder four
bytes even when a general 32-bit operation emits a 22-byte restoring core plus
setup and optional sign handling.

The single-use size extension supplies a separate byte comparison, but the
inliner still accepts only restricted single-block scalar bodies. Branching
helpers and helpers with multiple memory operations do not become candidates
merely because their removal could reduce the total image. Execute records
zero ordinary or single-use machine inlines in all three fresh builds.

Compiler locations: `M68kCodeGenerator.FrameClearRuns.cs:53`,
`M68kFrameClearRunPlanner.cs:83`, `M68kTargetCostModel.cs:54`, and
`M68kMachineModuleOptimizer.cs:1182` / `:624`.

### 5. Local cleanup still misses simple waste

Execute `AppendNumber` contains:

```asm
; image offsets $1712 and $1714
move.l a1,d0
move.l d2,d0
```

The first instruction has no surviving effect: the second replaces D0 and the
same condition codes, while neither changes X. This is a **two-byte** missed
cleanup. The scan finds three more such adjacent pure-register overwrites in
Eval: **eight bytes across the installed set**.

It also records **233 stack-load/write-back pairs** and **23 stack self-moves
occupying 134 bytes**. These need CCR and label analysis before removal. For
example, Copy's `$0F84` stack self-move is followed by `MOVEQ`, which replaces
its flags. The existing self-move pass refuses referenced label positions;
that restriction is a possible explanation for survivors, not a proven cause
for every recorded site. Safe label preservation/retargeting needs investigation. Self-moves
whose flags are needed can sometimes become shorter stack `TST` instructions.

Compiler location: `M68kPeepholeOptimizer.cs:1865`.

### 6. Full-width division is not automatically bad code

The scan identifies 25 instances of the 22-byte unsigned restoring core.
Execute has two: full-width task-number `% 100` and decimal number formatting.
`AppendNumber` already combines quotient and remainder into **one division per
digit**. It does not repeat the operation for `% 10` and `/ 10`.

The compiler already uses bounded `DIVU.W`/`DIVS.W` and power-of-two reductions
where its range analysis permits them. Replacing arbitrary `uint` division by
one `DIVU.W` would overflow the 16-bit quotient for large values.

Useful next work is extending proven bounds across calls/branches and measuring
shared arithmetic helpers where several cores occur in one executable. The
existing core is compact; its presence alone does not establish waste.

Compiler locations: `M68kMachineArithmeticOptimizer.cs:34`,
`M68kIntegerRangeAnalysis.cs:16`, and `M68kCodeGenerator.cs:5085`.

## Reboot and the meaning of Workbench ratios

Reboot's generated command body is **140 instruction bytes**. Entry, startup
helpers and the resident wrapper occupy the remaining **310 instruction bytes**.
The selected source includes empty-template parsing, parser cleanup, Ctrl-C and
failure handling, DOS ownership, and Workbench message handling. Those operations
have real costs independently of the duplicate initialization.

Its 524/84-byte ratio therefore cannot serve as a compiler efficiency benchmark
without resolving the original command's behavior. Startup handling remains part
of the selected contract. Execute has a much larger implementation and is a
better first sample for instruction-selection improvements.

## Recommended compiler work

1. Remove proved dead register moves and redundant private stack write-backs;
   preserve CCR, address labels, relocation and branch-range behavior.
2. Preserve guest address expressions through allocation, use indexed accesses
   and memory tests, and normalize bytes only for consumers that need it.
3. Extend initialization analysis per private home across calls and control flow;
   retain exclusions for escaped storage, unsafe aliases, EH, GC and dynamic
   stack state.
4. Propagate the opt-in size policy to instruction costs and frame-clear planning.
   Prefer actual encoded-byte comparisons where estimates decide inlining or
   outlining. Keep existing compiler-wide defaults.
5. Measure restricted helper inlining/ABI specialization and shared arithmetic
   bodies. Use Execute, then List/Search and Eval as distinct reproductions.

Every implementation needs its compiler regressions and command qualification
before acceptance. Preserve the existing six branch-liveness regressions,
register/stack/relocation coverage, command outputs, ownership and Workbench
lifecycle. This audit reports static opportunities and fresh size measurements;
it establishes no new execution, stack or compatibility qualification.
