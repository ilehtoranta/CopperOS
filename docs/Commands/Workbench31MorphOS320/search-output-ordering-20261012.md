# Search output ordering checkpoint, 2026-10-12

Search now flushes DOS's buffered prefixes and headings before raw `Write`.
Redirected numbered output is exactly `     2> Needle\n`, and directory/file
headings precede their matching text. The corrected baseline and the opt-in
SimplifyLoopPhis candidate pass this finite correctness checkpoint on 68000,
68020 and 68040. **SimplifyLoopPhis was disabled at this checkpoint.** The later
[normal-build policy checkpoint](search-policy-enabled-20261012.md) enables it
after verifying the default selection on all three CPUs.

The [previous checkpoint](search-os-qualification-20261012.md) remains a
retained failure. Its baseline and candidate agreed on incorrect output; the
new buffered vector fixture also rejects the old candidate before its first
raw write. Expected numbered output was preserved throughout the correction.

## Results

| CPU interpreter | Original DOS invocations | Workbench vectors, baseline + candidate | MorphOS source vectors, baseline + candidate | Independent WinUAE |
| --- | ---: | ---: | ---: | --- |
| 68000 | 64 | 22 + 22 | 49 + 49 | exact output, return/Result2 0/0 |
| 68020 | 64 | 22 + 22 | 49 + 49 | exact output, return/Result2 0/0 |
| 68040 | 64 | 22 + 22 | 49 + 49 | exact output, return/Result2 0/0 |

All 192 original-DOS invocations pass exact output and return/IoErr contracts,
including literal, NONUM, QUIET, QUICK, patterns, wildcard and multiple FROM,
recursive ALL, long lines, quoted paths, parser/path errors and pending Ctrl-C.
Each CPU executes the same two batches under unchanged 3000-frame/120-second
limits. Alternating baseline/candidate calls use the same loaded segments at
4096- and 8192-byte RunCommand stacks. Caller stream/directory/CLI/stack/signal
and DOS-library state is restored; the final 20 calls per CPU have zero
AvailMem delta. Initial OS/capture buffer growth is retained as an observation,
not credited as complete resource reclamation.

The native route uses original Kickstart 3.1 A500 40.63 DOS ReadArgs, LoadSeg,
RunCommand, RAM files and OFS receipts. Binary output bytes are decoded from
the saved disk, independently checked against the generated request corpus,
and compared with the output contract. WinUAE 6.0.3.0 independently repeats
the numbered-line check on all three CPU models with JIT disabled, a native
shell, a 4096-byte stack and its directory filesystem provider.

All 426 vector invocations pass output, return/IoErr, resource ownership,
interleaving and image-immutability checks. The fixture now buffers VPrintf,
FPuts and PutStr, requires an explicit successful Flush before raw Write, and
covers Flush failure, Write failure and two-byte short writes. Workbench
reports FAIL/221 on either output failure. The shared MorphOS candidate keeps
its existing per-file continuation policy: a match recorded before output
returns OK/0 after successful traversal, while raw writes stop and file,
parser, matcher and workspace resources are released. This is a check of that
existing candidate policy, not original MorphOS output-error parity.
Search's 11 host tests also pass; their receipt is retained from the first
corrected attempt, whose production source is identical.

## Frozen images and decision

| Profile | Baseline HUNK | Opt-in candidate HUNK | Reduction |
| --- | ---: | ---: | ---: |
| Workbench syntax candidate | 10,704 bytes | 5,396 bytes | 5,308 bytes |
| MorphOS source candidate | 20,780 bytes | 10,496 bytes | 10,284 bytes |

Workbench baseline SHA-256:
`4da177348834cb28569327c80cae19a32b8bbc86567d872476a07618a2ef5a52`.
Candidate SHA-256:
`ea87b54b65cb92aa9b97f27768d6c423e2e9c8da1c4e92b136cfe18ea61c7548`.
The normal build-c/dotnet publish path reproduces both images byte for byte.
The baseline excludes SimplifyLoopPhis; the candidate adds only that pass.
Fresh CIL and SDK dependencies are frozen once per profile and compiled with
the unchanged frozen compiler from the register-pressure investigation.
No compiler change or production policy selection is part of this fix.
`out/C/Search` remains the previously installed baseline; the next normal
source build produces the corrected baseline without enabling the new pass.

The [JSON receipt](search-output-ordering-20261012.json) binds frozen inputs,
sources, compiler/runner dependencies, HUNKs/maps, request/response bytes,
disk snapshots, vector observations, independent WinUAE results and the
old-image negative control. Raw evidence is
`artifacts/search-output-ordering-final-20261012`; reproduction and auditing
tools are in [SearchOsQualification](../../../tools/Commands/SearchOsQualification/README.md).
The first corrected attempt remains in `artifacts/search-output-ordering-20261012`:
its newly added MorphOS failure tests incorrectly expected Workbench's FAIL/221
result. The final fixture corrects those profile-specific result/traversal
expectations. Production images are byte-identical between those attempts.

This closes the observed buffered/raw output-ordering defect and passes the
recorded finite correctness gate. It establishes neither original Workbench
Search binary parity nor actual MorphOS execution, shipping/PURE admission,
an absolute minimum stack or a payload speed improvement. Pending Ctrl-C uses
public SetSignal, not keyboard or mid-read injection. Real failure injection
and separate stderr remain outside the original-DOS corpus. The runner's
retained PDB binding still identifies three engine-source differences in the
dirty CopperScreen checkout; results qualify the frozen engine binary, and
neither primary sibling checkout was modified.
