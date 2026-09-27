# ReadArgs explicit-source parser qualification

Recorded 2026-08-30 for CC04/CC09 and the parser semantics exposed after
CC02.API15. **The unchanged 44-case native comparison passes.** This is a
bounded DOS component result, not command, MorphOS, normal CLI input, residency
or boot qualification. The [stable goal](../../../Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md)
and its command membership are unchanged.

## Accepted result

The [strict receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/readargs-test-results/private-host-readargs-receipt.json)
has SHA256 `8647612781105ce75818bbfe060e76f38ff28375e667a8b550d37d5e6f191a49`.
Its [compact paired observations](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/readargs-test-results/native-readargs-parser-summary.json)
have SHA256 `07b5ed028c519d15768b1ae3ee758ae7b89963951ce887c6396496de872c9f00`.
One test passes, with zero failures or skips. It executes 44 original and 44
generated ReadArgs observations and compares all 44 pairs exactly. The original
side also matches the earlier 44 observations. No expectation, corpus, value
copy, comparison rule or instruction limit was relaxed.

| Compared field | Mismatching pairs |
| --- | ---: |
| Returned RDArgs identity or failure | 0 |
| Immediate IoErr | 0 |
| Explicit-source cursor | 0 |
| Typed copied values | 0 |
| Whether FreeArgs was called | 0 |
| IoErr after cleanup | 0 |

Each observation uses an explicit RDA_Source. Values are copied while their
DOS-owned storage remains live. The fixed corpus SHA256 is
`88db8d1b62d47507fdbc19c0f9403c8c337fc9823cd7504a6d9e6ba58bc6884e`.
The limit remains 1,000,000 instructions per bounded execution. There are
**zero original or generated C: command invocations and zero RunCommand
invocations** in this test.

The generated DOS installs in 169,756 instructions, including 166,475 within
its HUNK. A repeated discovery takes 6,624 instructions, including 6,600 HUNK
instructions, and returns the same base `$002A48C0` without reinitialization.
Both return with SP `$0007CF04`. Code/vector immutability, native expunge and
loader cleanup pass. The already present vector-8 bootstrap exception remains
recorded; no new CPU exception occurs. Observed stack extents are not a minimum
stack qualification. All 2,367 before/after bound file identities stay unchanged.

## Exact candidate and host binding

Private candidate:
`D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f`.
It derives from the preceding E407 field-load snapshot by replacing exactly
one captured file, DosCore.cs; the other 760 source/settings files are byte
identical. The parent compiler includes the narrow instance-field repair.
The later SDK/compiler RunCommand bridge and native-entry integration are
**not** part of this parser candidate.

The unchanged DOS owner build and independent manifest-only validation pass.
The build records four differences from corresponding live paths; this is a
frozen derivative, not an assertion that every live source was compiled.

| Bound input/output | SHA256 |
| --- | --- |
| Frozen DosCore.cs, 204,790 bytes | `f908d2ff10b0b0f124b9bcfb1f22cf4befbce6da58c82c625ac2e98f27d72a3d` |
| Candidate capture receipt | `8cc4550e5ec1adbd02a2ab6e4bb6910d472dbc233fe0876afa4b1fc5d7ebc24e` |
| Normalized 761-file source inventory | `5c3b9c03dedf7a4df128dcfe9c24892a119fa7d08e68ab8776adf840c4ba9b86` |
| [Build receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/isolated-build-receipt.json) | `243d32d88592512b27430949b8da0bdc2b36031d0182afb98079d5f93879be82` |
| [68000 DOS HUNK](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/CopperStart/artifacts/native/CopperStart-DOS-Unified-68000.hunk), 873,920 bytes | `5014fce66228ab170f498474248b387c5aea40f80e39c0404c8cf1cb3280004f` |
| DOS build inputs | `459d47797548464fcda104e13c0426eda08d53845b96e6e0bb088a0307629b94` |
| DOS artifact manifest | `b305d6d03ff307bf4b0c2e9a31ecc62135a50825adea550891fa745808c90729` |
| Unchanged strict host test DLL | `7899455419ad8c5edc1025c5632061befd8526b6712db6d3c54c5c4726f01871` |
| Unchanged emulator DLL | `453de5eaac3fc284ce954b1b10e0d08b619c7d9100a06b583c806a412be0edbd` |
| Private original Kickstart 3.1 ROM | `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee` |
| Strict TRX | `b8d8c7b5715af07317d3c717ea19d52ffb39da1f53f5dfdebcb1f7c7fed9c3ba` |

The strict host remains the separately built
`readargs-host-20260830T132350Z-063f12e4` snapshot with its 119 captured test
sources and 40 pinned runtime dependencies. Its 17 gateway regressions passed
before this replay; no live emulator/provider rebuild was substituted.
The owner build also emits 020/040 assembly, but this receipt does not claim
a three-CPU DOS execution matrix merely from those outputs.

## Parser repair and retained red baseline

The [host repair receipt](D:/TestData/CopperOSCommands/ReadArgsHostRegression/20260830T143234Z-888d80ce/completed-repair-summary.json)
has SHA256 `4d172e34d185ca850823054ab12d327a851b0bab55a5ca1f0162a4eb4abdf9bd`.
The same 44-row original-derived host suite changes from 43 failed/1 passed to
44 passed. The passing run makes 87 calls, including an ordered replay of all
44 observations. All 107 focused host rows pass, plus two separately counted
exploratory installed native-family tests. These are distinct tests, not
additional original/generated pairs in the strict native receipt.

The bounded correction preserves incoming IoErr on success, consumes/drains
the observed explicit source line, returns all-one bits for /S, recognizes
plain required-keyword spacing, backfills observed positional strings after
/M, accepts the measured numeric magnitude boundary and reports the observed
unterminated-quote error. Public ReadItem, public StrToLong and DosCliCore are
unchanged. The Shell buffer adapter owns an LF-terminated copy when needed;
the public explicit-source API retains the supplied bounds. RunCommand must
not infer LF insertion from that adapter's contract.

Historical failed receipts remain failed:

| Candidate | Retained result |
| --- | --- |
| `9a9a6904...` before the field-width repair | 44 original observations, 25 complete generated observations; result copying fails on the next /M case. All 25 complete pairs differ. Receipt `18c39884481edffd7634e97deff70ca934ed234809547eaa6f8e41efa31d0959`. |
| `e40796f5...` after the field-width repair, before parser repair | 44 original and 44 generated observations; 43 differing pairs. Installation and cleanup pass. Receipt `040acbb21326a9c659316827f1cd96699c708a5218b6dd2678e0c5c8cd53ca74`. |
| `5014fce6...` after the one-file parser repair | 44 original and 44 generated observations; 44 exact pairs. Separate new receipt, with no relabeling of either failure. |

The [reference execution record](reference-execution-paths.md) retains those
captures and their provenance. Wider numeric bounds, typed /M backfill,
defaults, interactive prompting and behavior beyond the finite corpus remain
unqualified. Normal NULL-D3 input, nested command context, return/abort/reset
ownership and real filesystem/output are separate work under the
[launch contract](native-command-launch-contract.md) and
[input-context notes](runcommand-input-context-implementation-notes.md).

## 2026-09-05 extension: full Copy template

The new bounded comparison passes **56 pairs**: the unchanged 44-case corpus plus 12 cases using Copy's complete 275-character, 24-slot template. Both tests pass with zero failures/skips. See [the bound observations and input identities](../../../artifacts/readargs-required-scan/qualification.json) and [the native test result](../../../artifacts/readargs-required-scan/native-full-follows.trx).

Generated HUNK SHA256: `A1611E8F1D78555200AB581E92E7B148C25B59CEB79E4EA3283F92841710372A`. The isolated source/build root is `C:/D-drive/TestData/CopperOSCommands/BuildSnapshots/readargs-full-follows-20260905T134320Z`. It preserves the accepted baseline and earlier failed candidates. The owner build, manifest, source and managed-input checks passed. This is the accepted baseline with the parser changes; it does not qualify the complete current live DOS tree.

The implementation removes filename-length validation from templates, uses the existing mapped string scanner, and avoids restarting entry decoding during sequential searches and final validation. Backfill retains its right-to-left order but first determines whether any missing required positional string exists. All lengths/cursors remain invocation-local. No allocation or shared cache was added. The general scanner's existing MaximumIoLength bound remains; arbitrary-length template parity is not claimed.

Copy coverage includes empty and positional inputs, multiple sources with explicit TO, quotes, short/long aliases, mode/metadata switches, negative/invalid/overflow BUFFER, and success after a parse failure. Every pair compares returned identity, immediate IoErr, cursor, typed values, FreeArgs admission and cleanup IoErr. The existing native-vector, image-immutability, installer and expunge checks remain active. The instruction limit remains 1,000,000 per ReadArgs call. Original output was not substituted for generated execution.

Live host checks also pass 80 ReadArgs/DosCore tests and 28 DosCliCore tests, including /F interactions. These host checks are separate from the native result. This extension does not close CC04 or CC13: normal streams, command execution, full live owner integration, filesystem behavior, MorphOS PPC execution, complete options, command pure/resident and shipping still require evidence. The full live native build previously failed resolving CopperSharpRomMemoryPlatform::.ctor; that issue is not waived by the isolated result.

## 2026-09-05 live owner integration

The **full live DOS owner build now passes the same 56 native parser pairs** (44 baseline plus 12 Copy). This supersedes the isolated-only limitation for the tested parser component. [Native results](../../../artifacts/readargs-live-owner/live-native-parser.trx) and [bound observations/artifact capture](../../../artifacts/readargs-live-owner/qualification.json) identify the exact run. HUNK SHA256: `A553D004C017FE56CB94DAD9A5D0AA9024251E133A34ACB15216AD06187297E3`. Owner source closure: 179 files, `AC6AFFAF2E04FC7C1FBEF6762F1A74ACD263F3481FC826820B36EF0E463895EA`.

The live build needed a compiler dependency-loading fix, not a change to the DOS constructor calls: reflection now loads dependencies referenced by a declaration from explicitly supplied managed assembly paths. The fresh-process regression passes declared dependencies and rejects an undeclared one; 17 existing compiler tests also pass. The owner's exact root-count check now follows its current one-entry/two-export profile. All other native compatibility, export, map, source and managed-input checks remain active. Earlier failed runs and isolated candidates remain preserved.

The capture freezes native outputs/reports/manifests, not the entire live source/managed build tree. Normal input streams, command execution, filesystems, full boot, MorphOS PPC execution, command pure/resident and shipping remain unqualified. No complete command gate is closed by this parser result.
