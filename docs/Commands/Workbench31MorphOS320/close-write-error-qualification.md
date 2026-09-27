# CC02.API17: Close buffered-write error qualification

Recorded 2026-08-30. The bounded correction passes **45 actual native cases
across 68000/020/040**, 323 portable cases, 64 host cases and 14 existing
installed/provider checks. Close now preserves the first failed buffered file
write while retiring its owned resources. This extends the earlier
[Close BOOL checkpoint](close-boolean-qualification.md); it does not change
that checkpoint's inputs or turn its retained native failure into a pass.
Raw Write and unbuffered FPutC error handling remain separate work.

## Observed defect and owner correction

The unchanged BOOL-corrected DOS DLL `1c4db0fa…` reached a real ACTION_WRITE
failure, `-1` with DiskFull (221), in the native 68000 fixture. Its private
process error became 221, then successful ACTION_END cleared it. Close returned
`$FFFFFFFF` and immediate IoErr zero. Buffer, provider and wrapper were each
freed once; the borrowed NIL: witness and selected standard streams survived.
The instrumented [original failure receipt](D:/TestData/CopperOSCommands/Q/closeNativebdb470dc/receipt.json)
has SHA256 `8a23c731d691e657fc0de33c32e331520312fc70788b26292e3b425398b27dfd`.
Its 896736-byte HUNK, identical to the earlier observation, has SHA256
`4a3ee13c0ae5684207de07946b56bf7ff1297472c6d2b1dedf3da6f30ae986b9`.
This observes generated DOS, not an original Amiga error-path differential.

The provider contract now adds `DOS.Error WriteFileByte64Checked(uint,
ulong, byte)`. None means this operation wrote exactly one byte. It returns a
fresh failure from this operation, independently of sticky ProviderError.
The native provider captures the packet result before freeing scratch storage
or ending the provider call. Zero or invalid positive counts cannot silently
count as a successful one-byte write. Existing void wrappers remain available.
This is a source-level provider interface addition; guest SDK APIs, layouts and
library ABI are unchanged.

`DosCore.Close` saves the flush error before cleanup and ACTION_END. Cleanup
still retires all owned resources; a later End error cannot replace the first
flush failure. `FlushPendingOutput` stops at the failed byte. If an earlier
prefix completed, it compacts and persists only the unwritten suffix in the
already validated handle record, keeping logical position and buffer ownership.
Zero progress leaves the previous bytes and cursors unchanged. This persistence
also applies to an existing short console write; callers that return early
cannot accidentally resend the completed prefix on a later public Flush.

Production changes are limited to `DosCore`, `IDosPlatform`,
`CopperSharpNativeDosPlatform`, `DosNativeProviderLifecycle` and
`CopperSharpRomDosPlatform`, plus the checked writer methods in the host
`ConfiguredDosHostHandler`, `LegacyAdfDosHostHandler` and `DosServices.DosHostPlatform`.
The host snapshots retain the earlier Close fix and exclude concurrent RET03
retirement partials. No compiler, SDK, CPU, parser or command source changed.

## Source-matched stages and results

The [main qualification receipt](D:/TestData/CopperOSCommands/Q/closeWriteafea7960/write-error-qualification.json)
has SHA256 `cc43a7f79cc81e90646539fec2358f10f3326948997344781ef5793abed28a0d`.
It binds the complete results, commands, source deltas and consumed binaries.

The retained original contains 91 DOS sources from the BOOL checkpoint. The
adapted before stage changes only the four provider/API files required to
compile the new capability; its DosCore is unchanged. The after stage changes
only DosCore relative to that adapted before. The new API adaptation is thus
explicit, not presented as the original DLL. Each before/after pair uses one
unchanged test DLL.

| Check | Before | After |
| --- | --- | --- |
| 29 focused portable cases | 26 fail, three controls pass | 29 pass; included in 323 related portable passes |
| 15 native scenarios per CPU | 45 attempted: 30 fail, 15 pass | 45 pass, zero skipped |
| Host checked capability and controls | New API controls run after adaptation only | 64 pass: six new, 37 retained Close/callback and 21 configured-volume checks |
| Existing installed/provider classes | Earlier migrated expectations retained | 14 pass, zero skipped |

The before native set records 42 Close entries and 36 returns: some strict
provider guards stop unexpected follow-on writes, and the public-Flush case
can stop before Close. These failures are not counted as completed Close calls.
The after set records all 45 entries and returns and 17,699,481 executed CPU
instructions. Each CPU also executes an actual failed public Flush followed by
Close, proving that only the suffix is retried.

Portable cases cover fresh errors despite earlier ProviderError, first-error
selection, owned and borrowed buffers, zero progress, completed prefixes,
public Flush retries and early-return callers. Native cases cover normal and
empty output, End errors, negative/zero/invalid byte-write counts, failure at
different prefix lengths, borrowed buffers, prior unrelated errors and a real
injected one-byte Exec allocation failure. Packets, D1 handle/D0 result,
immediate IoErr, outer nonvolatile registers and stack, guards and exactly-once
resource retirement are checked. Each case uses a fresh installed DOS image;
Exec and external packet handling are fixtures, while DOS vectors execute
generated instructions. No DOS Close or parser substitute is used.

| Bound artifact | SHA256 |
| --- | --- |
| Adapted before DOS | `d8321abf3d16975c275f0afa0168477eeb05303b8fae5a91996c22ffe301a8e8` |
| After DOS | `9de45c8d7d38417ed20837f99eb5681e5f5af63b1c2a3b81d680b287c560882c` |
| Unchanged portable test DLL | `430fee2d2322c32f3ce9355769951c36ec6740716b92f4160b9594afe02b22f4` |
| Unchanged native test DLL | `726db302ae30646344f2ecfebb093aa3bb9533bdef80c4bdcd9dfef19bc627ce` |
| After 68000 HUNK, 897996 bytes | `2c80d1804d1107c00d5440eebec5e30d7ed3d7019a39b50ce0bf91e83d457f7a` |
| After 68020 HUNK, 899760 bytes | `eab517bdba7e928d5da2c1d74089817756ebc13f25a1f38955981df9885e1be2` |
| After 68040 HUNK, 893172 bytes | `aad9e275721e1c9cd5d4da9a512d2ec061fa4ef98a377122641993b0f7d3cb28` |

Native builds use the frozen compiler `814b2ade…`, SDK `805ceb33…`, Support
`bf578cff…`, Exec `82981a0d…` and CPU `29613e4c…`. The CPU source binding retains
the qualified `db60` M68kCore snapshot, excluding later ambient CPU edits.
Each native run persists HUNK, map and compatibility output before execution,
binds the actual loaded assemblies, and requires the exact four-assembly
DOS/Exec/SDK/Support closure with no fatal sites, runtime helpers/features,
managed allocation sites or external targets. These are Freestanding,
peephole-disabled DOS component builds, not pure/resident command approval.

The main audit revalidates 1317 captured files and seven PE/PDB pairs with 385
matching source documents. The existing installed/provider selection has a
separate [14-case receipt](D:/TestData/CopperOSCommands/Q/closeWriteafea7960/installed-regressions01/receipt.json)
(`72b6c55eb072f74b735a1fff1edeb14a2dff7b987fbd7f8fa2ab51b8e0a481b6`)
and [closing audit](D:/TestData/CopperOSCommands/Q/closeWriteafea7960/installed-regressions01/closing-audit.json)
(`138a7ab1cfa3b3db97547e4476ce1a8563df781946be32129a871c5e976cd366`).
That audit revalidates 458 inputs and another test PE/PDB with 11 source
documents. The unmodified older native fixtures do not retain their emitted
HUNKs; their checks supplement the separately retained 15-by-three matrix.

## Reproduction and remaining limits

The private scripts retained alongside the receipts build only captured source
trees with explicit frozen references. The native runner's six invocations are
`run_native_write_matrix.py before|after M68000|M68020|M68040`, one phase and CPU
per invocation. Portable tests use `build_write_error_before.py` and
`build_write_error_after.py`; host controls use `build_host_checked_writes.py`.
`run_installed_write_regressions.py` runs the three unchanged installed/provider
classes. Each receipt records the expanded build and VSTest commands. Existing
attempt directories are preserved rather than overwritten.

The incorrect original hash setup attempt `closeNative39fded5b` executed no
tests. The first PDB audit used an incorrect assumed TestSDK17.8 generated-source
path and remains a setup failure; the follow-up admits only the exact
PDB-referenced path and checksum, without changing production or test binaries.
Both original native failure observations and every adapted-before failure
remain retained.

This slice does not qualify raw Write or unbuffered FPutC provider failures,
nor change generic error mapping in other buffered callers. It establishes no
full original filesystem/handler equivalence, physical host I/O fault matrix,
task retirement or actual Exec switching, shared-image residency, distribution
command behavior, shipping HUNK, or pure flag. The first-error behavior is
source- and fixture-qualified here; original Close failure precedence still
requires separate reference observations.
