# RN1-WB.CPU020: byte immediate postincrement

Recorded 2026-08-30. The CPU correction passes **144 focused cases and all 857
selected CPU checks**. With only the CPU DLL/PDB replaced, the unchanged
original Rename fixture now observes **40 returned cases and eight separate
hazard stops on each of 68000, 68020 and 68040**. This closes the bounded
`RN1-WB.CPU020` execution obstacle. It does not implement or qualify a replacement
Rename, original DOS matching, a shipping command or pure/resident behavior.

The [qualification receipt](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/qualification.json)
has SHA256 `0d9c638191a4345e689ac584717840d5b18fab7f0c1ce17f7afa88f482c08d2c`.
All earlier failed receipts remain unchanged.

## Reproduced defect and correction

The public 68020 core rejected `12FC 002F` at PC `$0010026A`, the byte append
used by the original Rename command. The same missing addressing form also
failed in the 68030 profile. The first public-core run recorded 60 such
failures and 84 passing 68000/010/040 controls, with PC, SR, registers, stack
banks, memory, access traces and loaded CPU identity captured per case.

The instruction stores the immediate byte at the original address, advances
the destination register, and updates MOVE condition codes. A7 advances by two;
other address registers advance by one. The high extension byte is ignored.
These rules come from the [Motorola programmer's reference](https://www.nxp.com/docs/en/reference-manual/M68000PRM.pdf),
MOVE pp.4-116–118 and postincrement p.2-6. The existing 68020 cache-case formula
already gives six clocks for this form, as in the
[MC68020 manual](https://www.nxp.com/docs/en/data-sheet/MC68020UM.pdf), §8.2.6 p.8-23.

Only two production files change. `M68kAdvancedTimingInterpreter.cs` adds an
opcode kind, the exact `$F1FF/$10FC` classification, dispatch and execution.
The execution uses the existing register writer, including active-stack-bank
handling, and MOVE flag helper. `M68kTimingEngine.cs` appends the matching
internal timing key; the existing operand-shape formula resolves it. No timing
formula, 68000 interpreter, public API or JIT source changes. The 68040 dispatch
explicitly retains its existing fallback and fixed-cycle policy.

| Production source | Before SHA256 | After SHA256 |
| --- | --- | --- |
| `M68kAdvancedTimingInterpreter.cs` | `80601c759838704d9d095c2382e27b8ac79b3b74d27adb1dc9189db35ca8060d` | `c4d8652f41ce4672f5fafc810b8740b522ca38d75284a8a29f790fe601876a26` |
| `M68kTimingEngine.cs` | `b53bb1d54bfc043f407c5670992d463f1510733f0d900a5156d29a43cd550a6f` | `bde03979dc55fe6c6f7f8de233efaf6d24989d745950b09af029a869ef36a0ab` |

The private parent is the earlier qualified opcode48F9 CPU capture, retaining
`M68kCore.cs` hash `db60fa4a…`. Later ambient core changes are excluded; no
working-tree copy of that file was overwritten. A separate read-only review
confirmed the mask, byte width, stack-bank update, flags, timing-key mapping
and preserved 68040 behavior, with no actionable finding in this bounded path.

## Fixed tests, runtime and source binding

The [public-core test](D:/Koodit/GIT/MedPlayer/Copper68k.Tests/MoveByteImmediatePostIncrementTests.cs)
covers every destination register, zero/positive/negative bytes, nonzero high
extension bits, odd byte addresses and A7 user/interrupt/master stack banks.
It checks exactly one byte write, PC advancement, all registers and SR,
untouched memory, six native cycles for the zero-wait 68020 cache-case model
and the configured one-cycle 68040 profile. Models are 68000/010/020/030/040.
It does not qualify fault frames, cache misses, instruction overlap or Amiga
bus arbitration; 68030/040 hardware timing is not inferred from these results.

| Selection | Before | After |
| --- | --- | --- |
| [Focused before](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/before-receipt.json) / [after](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/after-receipt.json) | 60 failures, 84 controls pass | 144 pass, zero skipped |
| [Related before](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/related-before/receipt.json) / [after](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/related-after/receipt.json) | 60 failures, 797 pass | 857 pass, zero skipped |

Selections overlap. The related set adds the 713 retained timing, MOVE, MOVEM,
decoder, dispatch, model, address and write-ordering controls. Both pairs use
the same test DLL, SHA256
`58fbd7cedb668eed7e53d4d7aab9b3339a393398f4fd8f364bb337699d86c9d1`.
All 84 focused controls also have identical before/after architectural states,
machine/native cycles, complete memory and bus traces.

| Consumed CPU | SHA256 |
| --- | --- |
| Before, 835584 bytes | `29613e4c1f205fdc2b101678db2b0c418683c72ae1ea5562ef88191b199684f8` |
| After, 835584 bytes | `d29fcdbedf4732ee4f0209d0f2a0ff2bb250a6bda826d9a08ffbdfdaebefdd2f` |

Private test and CPU builds consume captured sources and explicit frozen
references, without rebuilding any live sibling project. A
[reproduction check](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/reproduction/receipt.json)
rebuilds byte-identical DLL/PDB pairs with 241 test and 203 CPU Compile,
reference and analyzer inputs unchanged across each rebuild. The CPU build
has two expected SourceLink warnings because the private source tree has no
Git metadata; compilation has no errors. The test build has no warnings/errors.

The [PE/PDB audit](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/pdb-audit.json),
SHA256 `df2804a8b0bedf4a1fb41581634cf392c3075d9e807a97530b45c1c2f2e9d709`,
matches four assembly pairs and all 105 source documents, including the
unchanged original-only Rename executor. That executor was not rebuilt here;
document matching does not rewrite its historical build-provenance limitation.

## Original Rename replay and remaining work

The [six replay records](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/rename-replay.json)
use the original 1140-byte `Rename 37.2` HUNK, unchanged executor
`9f2b41f845b9cd4dd157a5c97ab57ec886f4b47e86721d4200bdc3e2e54b6ad9`
and SDK `16fd5214…`. Before and after runtimes differ only in CPU DLL/PDB.
The original HUNK remains outside the repository and is not copied into these
receipts. There is no parser substitution beyond the already declared
supplied-vector fixture, no timing exception bypass and no original-byte edit.

Before correction, 68000/040 each return 40 cases and stop eight hazardous
ones; 68020 starts 17 and returns 16 before the same `$12FC` rejection. After
correction, each CPU returns 40 and observes all eight intended guard stops.
The 68000/040 returned records and hazard records are unchanged. The
[new 68020 receipt](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/rename-after-68020.json)
has SHA256 `686ddb82e40fb3fedd084eb0680e5a8ecfc22f09f23f2151f8ecff726b2d7b33`.
It includes repeated and instruction-interleaved calls on the same protected
image. A guard stop is not a returned command or successful resource cleanup.

The scripts `qualify_byte_post.py`, `run_component_followups.py`,
`audit_byte_post_pdb.ps1` and `seal_byte_post.py` are retained under
`D:/TestData/CopperOSCommands/CpuMove12FC/`; expanded build/run arguments and
immutable input copies accompany the receipts. Existing outputs are preserved.

Continue with `RN2-WB.MATCHEND` and `RN3-WB.CLI` in the
[Rename contract](contracts/Rename.md): actual MatchEnd ownership/error behavior,
safe treatment of the guarded paths and original parser/handler behavior are
still required before implementing the replacement. The MorphOS profile,
real-DOS differential, residency and package gates remain open. No completion
or purity count changes, and the goal document and build manifest are unchanged.
