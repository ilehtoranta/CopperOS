# Status contract

Profiles: `wb31` and `morphos320`. Goal steps: CC01, CC11 and CC17.
Recorded: 2026-09-23.

Status is a process and CLI inspection command. The selected packed
`MorphOS/C/Status` member is extent `178472`, 3,679 bytes, SHA-256
`9d5c3aca0a9f21d91f9ad4b0565ba5ac2fa4a964af07c9540ee58e6b7d24a168`.
The MorphOS 3.20 C source member is `c/status/status.c`, 5709 bytes, SHA-256
`bb92acf52c915f3549c57cb8253a58d60ee6912e751aa509495ac5d04b12dc25`.
Its version include records `Status 50.6 (12.08.19)`. The
[status binary audit](../reference-captures/status-morphos-binary-audit-20260923.json)
binds the packed identity to that source and records the installer `+P` event
at `hdinstall.fixc:73`. The source-observed
DOS template is:

```text
PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K
```

The selected Workbench 3.1 M10 disk 2 `C/Status` member is 828 bytes with
SHA-256
`fd7b386f103bafba80add97523991389880f9b6987f4534280abb61b8193580c` and
version `status 37.2 (1.4.91)` at byte offset 615. Its candidate template is
the same five-slot string at byte offset 568. The hash-bound audit is
[status-wb31-binary-audit-20260923.json](../reference-captures/status-wb31-binary-audit-20260923.json),
backed by the disk-level capture
[wb31-disk2-c-command-captures-20260923.json](../reference-captures/wb31-disk2-c-command-captures-20260923.json).
The observed AmigaDOS protection word is `0x00000000`, so the original is not
marked PURE by this media evidence.

`PROCESS` selects a CLI number, `FULL` adds stack/global-vector/priority and
loaded-command fields, `TCB` restricts output to the task-control fields,
`CLI=ALL` requests all CLI entries, and `COM=COMMAND/K` prints matching CLI
numbers. The MorphOS legacy path compares command names with `strnicmp`, so
matching is case-insensitive. A missing command match returns `WARN`; a missing requested process
prints `Process %ld does not exist` and returns `FAIL`. Ctrl-C is polled before
each snapshot row is rendered and publishes `ERROR_BREAK` without emitting a
partial row.

MorphOS 50.6 prefers `QueryCLIDataTags`/`FreeCLIData` when DOS is at least
51.51, requesting sorted CLI data; older DOS uses the public DOS root
`CliProcList` and each process' public CLI/task fields. The replacement keeps
both paths and performs formatting after the snapshot/lookup. It does not
hold Exec list protection while doing output.

The bounded resident native entry is
`src/Commands/Native/NativeMorphOSStatusCommand.cs`, with the private startup
adapter in `tests/Commands.AddBuffersNativeRoot/NativeMorphOSStatusEntry.cs`.
`tools/Commands/qualify_morphos_status_native_entry.ps1` builds 68000/020/040
HUNKs and runs the supplied DOS/Exec fixture. Receipt:
`artifacts/status-morphos-native-20260924-com-casefold-v1/qualification.json`.
The qualifier explicitly passes `--exports none`, keeping unrelated exported
callbacks from the shared native-root assembly out of this standalone command.
The source-aligned entry preserves the pre-render Ctrl-C boundary for both the
modern CLI-data provider and the legacy public CLI-list fallback.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 6348 | `f46429d10a27aee7fa1ab8d1af8b875401a73fc53783b5eaa9d44f60d0f9e343` | 22 | 19 |
| 68020 | 6344 | `285d249c6b4e0a5663f35018af51df082d2e20c12923a24d80143f2bf923669f` | 22 | 19 |
| 68040 | 6240 | `466c73fc3e1c0f203b4c6bd7926616b006fd2939e502cae283fb44f1f0bf0e57` | 22 | 19 |

The receipt covers parser ownership, empty/one-process/three-process legacy CLI snapshots,
the DOS 51.51 `QueryCLIDataTagList` provider and tag ABI, provider-side process
and command filters (including mixed-case legacy matching), `TCB`/`FULL`, missing-process, parser failure, Ctrl-C,
startup-boundary rejection and interleaved calls. It is bounded ABI and
supplied-vector evidence only: exact Workbench behavior, a DOS 51.51
`QueryCLIDataTags` guest capture, complete task population, packed-binary
correspondence, PURE/resident lifecycle, package admission and differential
comparison remain open.

The Workbench candidate now has a separate DOS 36 resident entry and uses the
same public legacy CLI-list path, because the original profile predates the
MorphOS `QueryCLIDataTags` provider. Its receipt is
`artifacts/status-wb31-native-20260924-com-casefold-v1/qualification.json`:

| CPU | HUNK bytes | SHA-256 | reachable methods | invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 6,412 | `a49082e7d6385dfc1b471e190d61d474d4268043825b4eee475cfd77745712f2` | 23 | 16 |
| 68020 | 6,408 | `7fd9a08efa3794ce258f56770d5023b9b14de988305083620a4351b760e343ee` | 23 | 16 |
| 68040 | 6,304 | `08d3e046b717128cf0c7e04a6fb88ef1e577f32550240fd68019bb22fe8f3ebb` | 23 | 16 |

The candidate receipt is bounded supplied-vector evidence only; exact
Workbench output and CLI/task population, original guest parity,
PURE/resident lifecycle, licensing, package admission and differential
comparison remain open.
