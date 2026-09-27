# CC08: command fixture refresh after compiler repairs

The same fifteen private fixtures pass again using captured sources containing
the narrow-field and RunCommand stack-bridge repairs. This refresh does not
admit a shipping command, P bit, resident installation or minimum stack.

The unchanged owner is [qualify_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_native.ps1),
SHA256 `371695e0a6a2a9533b001e07a772e85dfec56aa95872012166e6de1e47c10399`.
No compiler, executor, qualification rule or acceptance bound was changed to
make these runs pass. Both private workspaces contain the same 570 captured
source/settings files. Neither builds into a live repository output directory.

| Accepted component / run | HUNKs | Generated calls | Original calls / comparisons | Stages / input checks | Source / binary / restore / host / reference files |
| --- | ---: | ---: | ---: | ---: | --- |
| Foundation / `40cf9509a3a1480a9c796649ebe50568` | 9 | 264 | 0 / 0 | 61 / 63 | 442 / 20 / 27 / 196 / 0 |
| EvalNumeric / `18c0fa3254384c3287734b3537a17510` | 3 | 540 | 0 / 0 | 25 / 27 | 528 / 20 / 33 / 196 / 0 |
| Workbench31MakeDir / `1fe8f38d9e6a497eb675ed3a94459bc5` | 3 | 117 | 114 / 114 | 25 / 27 | 443 / 20 / 27 / 196 / 1 |

Every artifact passes a second byte-identical native compile, its existing
execution matrix, zero fatal machine-fault sites and the unchanged helper
policy. Foundation retains its nine prior HUNK hashes. All three Eval and
all three MakeDir HUNK hashes change; a same-sized binary is not treated as
the previous artifact. The [build manifest](build-manifest.json) binds all
fifteen new paths, hashes, counts and reports.

| Receipt | SHA256 | Input-manifest SHA256 |
| --- | --- | --- |
| [Foundation](D:/TestData/CopperOSCommands/BuildSnapshots/commands-requalification-20260830T153022Z-8881cb48/CopperOS/tests/Commands.NativeRoot/bin/Release/net10.0/qualification/40cf9509a3a1480a9c796649ebe50568/qualification.json) | `76a5de237142bae1fbb543aa8330ad879b160ba33d0501c5c5c50682a8810002` | `fd412646b6d651425f7e40d1af39320ec60202f42643e6d5eb0eddf59ab99ded` |
| [EvalNumeric](D:/TestData/CopperOSCommands/Q/ad1d7d83/CopperOS/tests/Commands.EvalNativeRoot/bin/Release/net10.0/qualification/18c0fa3254384c3287734b3537a17510/qualification.json) | `cf73581bbaf7386c710d7f2a3046320d914c1844221f1699df9c19909ff2c0a3` | `2fc06d1470b560270728c3d794ced6eb31ef8a8e0c1e5a56ab699ea2d9735802` |
| [MakeDir](D:/TestData/CopperOSCommands/Q/ad1d7d83/CopperOS/tests/Commands.NativeRoot/bin/Release/net10.0/qualification-makedir/1fe8f38d9e6a497eb675ed3a94459bc5/qualification.json) | `896d121aa130115c090368961c4c4d044092ca24d8beda0598a2661e35f3c16b` | `c3c7f7cdec443818cdd16e23f22a46e266e39b09bc67d438eae23f56f83d6610` |

The final [cross-run audit](D:/TestData/CopperOSCommands/Q/ad1d7d83/accepted-requalification-audit.json),
SHA256 `69433b37337cabdfd2babac4d0af3a8849dc52db6fc7697e45123d2bd334a13a`,
rechecks captured sources, copied inputs, artifact evidence and the private
original MakeDir. The original remains external, 464 bytes, SHA256
`23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b`.
It is not copied into the repository or source snapshots.

## Retained failure and shorter workspace

Eval first ran in the longer Foundation workspace. Its 540 native calls passed,
but strict report validation rejected the loaded executor/core paths: Windows
reported an extended-length prefix while the expected paths lacked that
prefix. Reproducibility stages were not reached. Run
`d3df1c3aca5a4de7b8ca5d3f48500b56` remains **failed**, with receipt SHA256
`62e03dfb0b65b9004357490b7757d0bf41c2f6ed588dd3a99730ffab7d690eb0`.
Those calls are not added to accepted invocation totals.

The successful retry copied exactly the same 570 sources into a shorter
private path and rebuilt them. No comparison was normalized or disabled.
The [short-copy receipt](D:/TestData/CopperOSCommands/Q/ad1d7d83/capture-receipt.json)
is `e5bd3168ec92435f6506aa68ffbcb95da0d0ee630235ca972f438caac3c62904`;
its parent capture is
`8bf1cf7e6c5d8c0d93611fe86b14a30616ecc650548101b8b376fcc5d08dde53`.
The native compiler/SDK binaries are newly built and individually bound in
each input manifest; older DLL hashes are not substituted for these inputs.

## History and remaining scope

The [prior build manifest](D:/TestData/CopperOSCommands/Q/ad1d7d83/previous-build-manifest.json)
is retained byte-for-byte, SHA256
`fd13eb4398cb00b137a35ce5ed017a490bccf042aa93f45af96c48fb2e3541b3`.
Its Foundation, Eval and MakeDir receipts remain valid for their old inputs.
The separate original-DOS MakeDir A01/A03 and quote-variant observations bind
the older `012c05cf...` MakeDir image; they do not qualify the new MakeDir image.

The captured roots predate Exe2Arc's H2/scanner work and the DOS input lease.
They cannot qualify those later changes. Full commands, original parser and
filesystem behavior, normal launch and Exit, boot, resident registry use,
minimum stack and packaging remain independent open gates. The existing
internal I/O return-tail admission remains limited to its exact ten bytes.
