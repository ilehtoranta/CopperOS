# CC02.API17: Close result and cleanup qualification

Recorded 2026-08-30. The portable and host BOOL corrections pass their bounded
checks. This checkpoint retains its native buffered-write error failure; the
separate [write-error correction](close-write-error-qualification.md) now passes
45 native cases, 323 portable checks, 64 host checks and 14 existing provider
checks. Neither record qualifies a command, a shipping artifact or a
pure/resident flag.

## Original contract

The original NDK 3.1 `DOS.DOC`, `Close` entry, specifies a BOOL result from V36
onward and retirement of an owned handle even when closing fails. It also
forbids closing inherited standard handles. The inspected developer CD has
SHA256 `5d6bfcb213f1395d4c95584dc94d0e36265be355076dae1710bd36fb4dfdbff3`;
the DOS member is at ISO extent 17730, 174666 bytes, with this entry at byte
21197. The [public Close autodoc](https://amigadev.elowar.com/read/ADCD_2.1/Includes_and_Autodocs_2._guide/node028A.html)
provides the same versioned contract.

The retained original R04 observation closed separately opened NIL: input and
output handles through DOS 40.3. Both returned `$FFFFFFFF`, followed by IoErr
zero. Its [original observation summary](D:/TestData/CopperOSCommands/BuildSnapshots/dos-streams-host-20260830T141219Z-567b5085/results/original-dos-stream-readiness-summary.json)
has SHA256 `b2bfcd42a2aaa564979115f1e53bf6a2333744314e52f7ee595c224aa83d8dd6`.
These are two retained original calls, not new command comparisons or evidence
for every original failure code.

## Changes and source boundaries

`CopperStart.Dos/DosCore.Close` now returns nonzero success and zero failure.
The existing cleanup sequence still retires the wrapper, buffer and provider
ownership even when close fails. The startup-script, Shell output and global
variable writers now interpret the BOOL result correctly. No second Shell
implementation or command-private cleanup path was added.

The host `DosServices.Close` adapter also returns zero when it cannot initialize
its DOS owner. Its private before/after build changes that one branch only;
the DOS dependency is the same corrected DLL on both sides.

The [portable capture](D:/TestData/CopperOSCommands/Q/close49367ede/capture.json)
freezes 91 DOS sources and build settings. The
[four-file delta](D:/TestData/CopperOSCommands/Q/close49367ede/source-delta.json)
is separate from the host's
[one-file delta](D:/TestData/CopperOSCommands/Q/close49367ede/host-after-source-delta.json).
The host build uses the 124 retained emulator sources from the earlier callback
checkpoint; it does not silently include later retirement changes. Every build
uses explicit private references and leaves shared output directories alone.

| Bound artifact | SHA256 |
| --- | --- |
| Fresh source-matched DOS before | `e9e8b04081be95e034456708570cb936930558e2d68566a874f877c65d0627d4` |
| DOS after the BOOL/caller changes | `1c4db0fa5b8fe3fae596af2c5c5c466faac75ed0db5f7f7f3ecf98cee29d53ef` |
| Same 30-case test DLL on both sides | `c6f36633c77709840d335e78147f55db78e316e4496904145c44405dbf1956fc` |
| Host adapter source before | `86d2a37327c9a2f5e2ace04551a1308300abb0941b1dad38e1a29f962d3af1db` |
| Host adapter source after | `72e980e1e80a9cad1639a891e493f2f2258117f1cbc7790608e7c526cc42c0f8` |
| Private emulator before | `ea39c643295b64a9cf3073982e1fb2145a93cc0d5d9574668fd30f06b1ace2f8` |
| Private emulator after | `9ab44f92657852a6d7d8e8cc625b91e6529b913c018b672aca53307937e32871` |
| Same five-case host test DLL on both sides | `47bab42824565768671e3ba92e69f24aaee63923e13630648c07ce1754407f3a` |

## Executed checks

| Checkpoint | Outcome | Receipt |
| --- | --- | --- |
| Portable owner/router before | 24 fail, six pass; all failures reach the BOOL assertion after their cleanup checks | [before](D:/TestData/CopperOSCommands/Q/close49367ede/before-receipt.json), `13a15c72302eeb9d5193a08172a6f2cd408cd03a7d99c04374cff9437d5d3989` |
| Same 30 portable cases after | 30 pass, zero skipped | [after](D:/TestData/CopperOSCommands/Q/close49367ede/after-receipt.json), `44b3a050df5c314af4c7d7df8aa84ebf40499d35f0bb0227f0b236474455295f` |
| Portable related selection, inclusive | 294 pass, zero skipped | [related](D:/TestData/CopperOSCommands/Q/close49367ede/related-receipt.json), `a19186596acd9cebed02aa6e9e99e4ea2452a82e740a1770094b2eae78d0a0c2` |
| Host adapter before | Two failed-initialization BOOL failures; three successful/stale-handle controls pass | [before host](D:/TestData/CopperOSCommands/Q/close49367ede/host-before-receipt.json), `aaead51447ffa2902873e3cbd5d1e47dd8b18be5105322d3c8927d8c3d7584c1` |
| Same five host cases after | Five pass, zero skipped | [after host](D:/TestData/CopperOSCommands/Q/close49367ede/host-after-receipt.json), `3d944e4fd35868033c2da6aa30b59e7b0cebb6ab5369c5d27eb1d7bff068fa38` |
| Host related selection, inclusive | 37 pass, including the 15 existing RunCommand callback checks | [related host](D:/TestData/CopperOSCommands/Q/close49367ede/host-related-receipt.json), `99f315f1724fee0a44bed0dc23a5f4788cd07478e5b4b6a24cdfc65bc662c962` |

The portable cases cover provider, buffered, NIL: and console success; three
provider End errors; preparation failure; null, foreign and stale handles; and
the three existing consumers on success and failure. They check exactly-once
resource release and preservation of borrowed streams. Router checks preserve
all represented registers except D0. They do not execute CPU instructions.

Twenty legacy Close assertions in ten portable/native test files and three in
`DosServicesTests` were migrated to the BOOL convention, retaining all other
test bytes. Exact changes and before copies are retained in
[portable migration evidence](D:/TestData/CopperOSCommands/Q/close49367ede/test-expectation-migrations.json)
and [host migration evidence](D:/TestData/CopperOSCommands/Q/close49367ede/host-test-expectation-migrations.json).
Migration alone does not claim execution of the three native test classes that
are outside the 294-case selection.

The [PE/PDB source audit](D:/TestData/CopperOSCommands/Q/close49367ede/pdb-audit.json)
matches eight assembly/PDB pairs and 484 source documents, with retained copies;
SHA256 `cc9373e269df972050271b808f76906861bdb10fe5e1bdd2ce3a6cbf510fc273`.
This supplements captured inputs and references. It is not native execution.

The [closing audit](D:/TestData/CopperOSCommands/Q/close49367ede/closing-audit.json)
rechecks 806 binary/evidence files, the same focused test DLLs, both source
deltas, six additional actual MSBuild input bindings and 155 existing local
document links. It preserves the original goal and build-manifest hashes;
SHA256 `8460b6a1a49ea8512d8bde5f04347b52a57d059e86cda1f309fa75dc6a42d709`.

## Retained failures and remaining gate

The private `close24d1bc64`, `close5a047e09` and `close51aaf02e` attempts remain
available. The first two failed setup/build; the third had an incorrect test
allocation-size expectation and is not the accepted regression baseline.
The accepted test uses the actual 132-byte handle size, which already includes
its owned header. Two host test build failures, missing a namespace and a bus
cycle argument, are also retained. Neither executed a test.

A subsequent actual 68000 native fixture (`Q/closeNative8071ef39`) reaches the
final failure assertion after all cleanup and ABI checks: ACTION_WRITE returns
`-1`/DiskFull, ACTION_END succeeds, yet Close returns true and immediate IoErr
zero. The portable flush loop did not observe the byte-write error before End
overwrote it. Thus the BOOL correction is necessary but insufficient. Failure
propagation, first-failure preservation, partial/zero writes and native success
controls required their own source-matched correction and execution record.
That later [write-error checkpoint](close-write-error-qualification.md) retains
this failure and its original inputs. Raw Write and unbuffered FPutC failure
handling, and original Close failure precedence, remain separate work.
Full original handler behavior and all-command compatibility remain open.
