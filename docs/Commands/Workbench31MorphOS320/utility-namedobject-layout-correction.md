# Utility NamedObject public-layout correction

Recorded 2026-08-31. Original `C:AddDataTypes` reaches a real address error
during normal Kickstart 3.1 startup. This record identifies one concrete ABI
violation in the generated Utility provider and documents the narrow correction.
It does not qualify R07, full boot, command launch, or the final provider cause
until a fresh private original replay passes.

## Original evidence

The NDK 3.1 `utility/name.h` in the licensed developer CD identifies itself as
version 39.5 and defines `struct NamedObject` with a single public field at
offset zero: `APTR no_Object` (“Your pointer, for whatever you want”). The V39
`AllocNamedObjectA` autodoc says `ANO_UserSpace` allocates user memory and that
`no_Object` points to it. The structure is otherwise opaque.

The sealed original-address-error trace records `AllocNamedObjectA` returning
`D0=A2=00213008`. The original caller then executes `MOVEA.L (d16,A5),A0` at
`00219BBA`, loading `A0=554E414D`, and immediately calls the original
`InitSemaphore`. That odd value faults at `554E4175`. The captured source had
placed its private `UNAM` magic marker at offset zero, so the observed original
caller was reading that marker instead of its requested user-space pointer.

The original trace is [source-bound and read-only](D:/TestData/CopperOSCommands/BuildSnapshots/dos-startup-address-error-host-20260830T204037Z-3ad2312c/results/private-original-dos-startup-address-error-receipt.json): one observation passes with stable inputs. Its test assembly is
`f5e1dc51ff2b3862c22b4489d1620c7f52a11064d872ae5f74cb2f2fb0cb962a`;
the frozen emulator is
`453de5eaac3fc284ce954b1b10e0d08b619c7d9100a06b583c806a412be0edbd`.

## Correction and focused test

[UtilityCore.cs](D:/Koodit/GIT/CopperStart/src/CopperStart.Utility/UtilityCore.cs)
now reserves offset zero for `no_Object`, moves the private validation marker to
offset four, and preserves the 48-byte private header footprint. Allocation
publishes the `ANO_UserSpace` allocation at offset zero; validation checks the
private marker; freeing captures and releases that public user allocation before
clearing the marker. The source SHA-256 is
`489946b8d3be04da81d69344df142eda4304891c4447f7cb94be35a9faf33508`.

The new focused test in
[UtilityCoreTests.cs](D:/Koodit/GIT/CopperStart/tests/CopperStart.Exec.Tests/UtilityCoreTests.cs)
asserts that offset zero is a mapped, distinct 48-byte user allocation and that
both it and the object are freed. Its source SHA-256 is
`8753f1d502b9cb722bc131dc39ce601a6b8599932a86f411b7b8d6431cfeb5f6`.
`dotnet test tests/CopperStart.Exec.Tests/CopperStart.Exec.Tests.csproj --no-restore --filter FullyQualifiedName~UtilityCoreTests`
passes all five focused tests.

## Private corrected-runtime diagnostic

The private project at
`D:/TestData/CopperOSCommands/Q/utilitynamedobjecta20260831/CopperStart.Utility.Private.csproj`
compiles the captured four Utility sources against the frozen Exec and SDK
assemblies. It emits `CopperStart.Utility.dll` SHA-256
`1cd25c9de5f2bacd27e5e13aa599656a1523424861c0f6d153f8fb5e17e5aa63` and
its matching PDB. The replay driver copies the historical test runtime, then
substitutes only that Utility DLL and PDB. Its receipt is
[`replay-receipt.json`](D:/TestData/CopperOSCommands/Q/utilitynamedobjecta20260831/results/replay-receipt.json),
SHA-256 `812bc445787163ca5297a1e7c2deb1d12237bf7674afdee45c8060285dfe3990`.

The historical observer is expected to fail once the old condition disappears:
it has an assertion that requires the pre-fix InitialCLI address error. One test
therefore executes and fails at boundary 67,312, while the report has
`faultObserved=false` and `invalidSemaphoreCallObserved=false`; no vector-3
address error or odd `InitSemaphore` invocation recurs. The frozen original
runtime, licensed ROM and Workbench archive remain unchanged; there is no guest
edit, injected input, requester dismissal, explicit command call, provider
rebuild or CPU-state patch.

This is diagnostic evidence only. The old observer records 64 repeated vector-8
exceptions, overflows its bounded diagnostic buffer, and then its existing
`Assert.False(machine.Cpu.State.Halted)` fails. It does not prove a clean
continuation.

## Bounded first-exception replacement

The derived observer changes only the historical observer's stop condition and
the private Utility hint path. It stops after the first startup exception rather
than retaining repeated records. Its source-bound
[`first-fault-receipt.json`](D:/TestData/CopperOSCommands/Q/utilitynamedobjecta20260831/first-fault/results/first-fault-receipt.json)
SHA-256 `e5d8dd0c6cfc7f94a94dc2f3c2b2ad0fc4486126f2d43e1b1c0a06a821893dd2`
passes one observation at boundary 709. The first exception is vector 8 from
`$00F80BBE` (opcode `$007C`) while the observed InitialCLI task is active; its
post-exception PC is `$00F80BCA`. The report has one exception, no overflow,
unchanged CPU diagnostics, listeners restored, and a write-protected,
byte-identical disk. It also confirms again that the old vector-3 boundary and
invalid semaphore call do not occur.

The vector-8 cause and its relationship to the corrected startup state require
their own source and original-code attribution. This result remains separate
from full boot, R07, command launch, resident purity, or provider-completion
credit.

An isolated Exec hypothesis is retained as negative evidence. Its task-frame
initializer was experimentally rebuilt with initial SR `$2000`, then paired
with the corrected Utility library in the same observer. The runtime binds the
new Exec SHA-256 `a71c6db76fea0969a622487877c7e84dbd6064bc8e51ac06420217be7cc1ab0b`,
yet the observer again reaches vector 8 at boundary 709 with SR `$0018`.
The initial-frame change therefore does not own this observed state and was
reverted rather than retained as a production correction. The raw rerun is at
`D:/TestData/CopperOSCommands/Q/utilitynamedobjecta20260831/first-fault-exec/results-rerun/first-fault-exec.trx`.
