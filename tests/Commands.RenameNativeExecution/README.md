# Original Rename 37.2 supplied-vector executor

This isolated host executable runs the privately supplied, hash-pinned original
1140-byte command on Copper68k 1.4.0. It contains no original binary or replacement
Rename implementation. It imports `CopperOS.Portable.props` for the pinned SDK
and source-links the existing HUNK reader without changing it.

Build only this project in an agreed build window:

```powershell
dotnet build tests/Commands.RenameNativeExecution/CopperOS.Commands.RenameNativeExecution.csproj -c Release -p:CopperOSUseLocalCopperSharp=false -p:BuildProjectReferences=false --nologo
```

Invoke with exactly four arguments:

```text
<private-original.hunk> <68000|68020|68040> <report.json> rename-classic-original-vector-fixture
```

The private original must remain outside the repository. Its expected SHA256 is
`ebff9d5f1401ca67fe9542bde304c603d21366f81852716c236c5c7ca1e49579`.
Missing/wrong originals, changed sources/binaries, unknown suites/CPUs and
unexpected outcomes fail; a safe failure receipt records completed work and the
last failing case. Windows physical-file guards resolve drive and directory
aliases before checking the repository boundary. Reports cannot overwrite or
alias even an invalid input; receipt replacement never truncates an existing
hardlink. Another host platform is rejected until its physical boundary has an
equivalent implementation. The host-only negative checks use disposable invalid
files outside the repository:

```powershell
python tests/Commands.RenameNativeExecution/verify_failure_receipts.py --executor tests/Commands.RenameNativeExecution/bin/Release/net10.0/CopperOS.Commands.RenameNativeExecution.dll --private-original D:/TestData/CopperOSCommands/Workbench31/Rename-37.2.hunk --artifacts D:/TestData/CopperOSCommands/Workbench31
```

The finite matrix contains **40 expected returned cases and eight bounded guard
experiments per CPU**, on one protected shared image. Repeated and actual
instruction-interleaved callers use private state and 4 KiB/16 KiB stacks.
Only SP is mandatory at whole-image return; D0 is the command result. Public
library calls require the correct live DOS base/register arguments and preserve
library nonvolatile registers while D1/A0/A1/CCR are deliberately clobbered.

The fixture supplies ReadArgs results, pattern output, matcher records, locks,
Rename outcomes and output vectors. It verifies supplied FROM/TO/QUIET use from the
actual command, but **does not establish parser grammar, wildcard semantics,
real filesystem effects, original DOS formatting, real CLI launch or purity**.
The exact checked template is `FROM/A/M,TO=AS/A,QUIET/S`. No host file or directory
is renamed by the vector fixture.

MatchNext replaces the AnchorPath's names before Rename, including at end/error,
to test copying order. The scripts distinguish original raw FROM in direct-mode
failure output from the supplied ParsePattern buffer used by Rename. QUIET only
suppresses progress. Most cleanup vectors change ambient IoErr; the receipt
separates selected fault codes from the actual final synthetic Process result.

An owned opaque search allocation makes omitted cleanup visible. Guarded paths
stop **before** unstarted/repeated MatchEnd, freeing a live-search anchor, reading
an unchecked unterminated/empty buffer, or writing beyond its allocation. These
are separate observations, not successful commands, safe resource cleanup, or
claims that original DOS itself crashes. Remaining allocations/locks and partial
output/calls are reported without silently completing cleanup. The matrix never
defines MatchEnd idempotence or invents a production error code for those paths.

No native/reference outcome is established merely by adding these sources.
See the [Rename contract](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Rename.md)
for the evidence ledger and later real-DOS gates. A direct run's before/after
source and assembly hashes are an audit snapshot, not a source-bound rebuild or
shipping qualification receipt. Original and generated counts remain separate;
this executor always records zero generated executions and zero comparisons.

The public default **Interpreter** is selected explicitly by factory usage for
the requested CPU, with no fallback or local package override. The pinned 1.4.0
package currently stops the 68020 matrix at original opcode `$12FC` at code
offset `$26A` (`RN31-N01.component-prefix`), reporting an unsupported exact
MC68020 timing case in `Ocs68020_14MHz`. This is a failed, partial receipt, not a
Rename behavior discrepancy. Its public options provide no instruction-semantic
timing bypass; the opt-in JIT is 68040-only. Keep the 68020 case and failed
receipt for a separately owned CPU fix. No cycle timing is qualified by this
flat-bus supplied-vector executor on any CPU.
