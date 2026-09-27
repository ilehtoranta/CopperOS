# System dependency capability-floor audit

This audit separates the minimum version observed in a captured command from
the lowest version that supplies the API used by the replacement. Startup code
must use the capability floor. The captured request remains a differential
test input and is not, by itself, a requirement.

| Dependency | Replacement use | Verified floor | Captured request | Evidence or follow-up |
| --- | --- | ---: | ---: | --- |
| `dos.library` | `ReadArgs`, `FreeArgs`, `PrintFault`, `IoErr`, `SetIoErr`, and classic file/lock calls | 36 | often 37 | Kickstart DOS authority; lower only after the same calls and semantics are verified below 36 |
| `utility.library` | `UMult32`/`UDivMod32` in Workbench `Date` | 36 | 0 | NDK utility authority marks the date/math vectors V36 |
| `utility.library` | Lease retained but no utility vector called by `GuessBootDev` or `ExtractKickstart` | 0 | 37 / 36 | `0` means no minimum; utility.library itself first appears in V36 |
| `expansion.library` | `FindConfigDev` in `Check2090` | 33 | 37 | NDK expansion authority marks `FindConfigDev` V33 |
| `expansion.library` | `MakeDosNode`/`AddDosNode` in MorphOS `Mount` | 33 | 37 | NDK expansion public ABI; refreshed Mount receipt uses V33 |
| `expansion.library` | Boot-node base traversal in Workbench `GuessBootDev` | 33 | 37 | V33 expansion ABI and stable boot-node layout; keep layout evidence with the command contract |
| `asl.library` | File requester vectors in MorphOS `RequestFile` | 36 | 37 | NDK ASL authority marks requester functions V36 |
| `intuition.library` | Workbench `RequestChoice` requester vectors | 33 | 33 | Existing Workbench entry and public vector authority |
| `intuition.library` | MorphOS `RequestChoice` extended requester flow | 37 | 37 | Lower floor remains unresolved until the additional `BuildEasyRequestArgs`/`SysReqHandler` semantics are version-bound |
| `icon.library` | Workbench `IconPos` DiskObject vectors | 0 | captured higher request | Public vectors are used with an existence floor; verify oldest provider before shipping |
| `icon.library` | MorphOS `Format`/`Mount` icon helpers | 37 | 37 | Keep 37 until icon.library introduction/revision evidence is recorded |
| `locale.library` | MorphOS `Date` locale formatting | 38 | 38 | `FormatDate` is a V38 locale vector |

The same two-column record is required for devices, resources, versioned
providers, and any new ROM vector introduced by a command. A floor must not be
lowered from a captured request without an authority declaration, disassembly
evidence, or a guest probe showing that the consumed call, structure, and
semantics are present.
