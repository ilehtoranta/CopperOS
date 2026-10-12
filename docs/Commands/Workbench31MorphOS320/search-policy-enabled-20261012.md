# Search default optimization policy, 2026-10-12

Search's default code-size pass list now includes SimplifyLoopPhis, following
the [passing correctness checkpoint](search-output-ordering-20261012.md).
The normal build selects the pass without a code-size or pass override and
produces the previously qualified 5,396-byte image. `out/C/Search` is refreshed
to that same image. Other commands' policies are unchanged, and explicit
code-size/pass overrides retain their existing behavior.

| CPU interpreter | Original DOS invocations | Baseline + normal-build vectors | WinUAE strict numbered output |
| --- | ---: | ---: | --- |
| 68000 | 64 | 22 + 22 | pass |
| 68020 | 64 | 22 + 22 | pass |
| 68040 | 64 | 22 + 22 | pass |

The 192 fresh original-DOS invocations match the previously qualified exact
output and return/IoErr contracts, including long lines, directory/file
headings, patterns, parser/path errors, Ctrl-C, repeated calls and alternating
4 KiB/8 KiB RunCommand stacks. Caller state is restored, and the final 20 calls
per CPU have zero AvailMem delta. The 132 supplied-vector invocations pass
exact output, result/error, resource ownership and image-immutability checks,
including Flush/Write failures and short writes. Independent WinUAE runs
produce `     2> Needle\n` and Shell return/Result2 0/0 on all three CPUs.

The normal build produces one MC68000 HUNK, executed unchanged on all three
CPU interpreters. Its SHA-256 is
`ea87b54b65cb92aa9b97f27768d6c423e2e9c8da1c4e92b136cfe18ea61c7548`,
byte-identical to the corrected opt-in candidate. It is 5,308 bytes smaller
than the corrected 10,704-byte baseline. This is a size result; these runs do
not establish a payload speed improvement.

Both normal publish runs use the qualified compiler payload from CopperSharp
commit `02a7fa3` and SDK tree `../CopperSharp68k-search-pressure-20261011`:

```powershell
pwsh -NoProfile -File tools/Commands/build-c.ps1 -Command Search `
  -CopperSharpRoot ../CopperSharp68k-search-pressure-20261011 `
  -CompilerAssembly artifacts/search-output-ordering-final-20261012/compiler/CopperSharp.Compiler.Cli.dll `
  -SkipCompiler
```

The first run used a private OutputDirectory for CPU verification; the second
uses the default `out/C` destination, preserves the previous image through
build-c's normal staging backup, and reproduces the same publish response and
image. Neither run passes CodeSizeOptimizations or CodeSizePasses. The primary
CopperSharp checkout still lacks SimplifyLoopPhis: it needs the compiler change
before it can build this selection. That dirty checkout and CopperScreen were
not modified. The [JSON receipt](search-policy-enabled-20261012.json) records
both commands, the compiler/SDK/input/policy hashes, exact native output and
binary receipts, vector comparisons, independent WinUAE results and installed
image identity. Evidence is in `artifacts/search-policy-enabled-20261012`.

This enables the bounded, qualified Search policy. Original Workbench Search
binary parity, actual MorphOS runtime execution, shipping/PURE admission,
absolute minimum stack and keyboard/mid-read break behavior remain outside
this checkpoint. The frozen runner's retained engine-source drift is carried
forward explicitly; the executed engine binary is the qualified boundary.
