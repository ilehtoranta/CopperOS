# CLI contract

Profile: `morphos320`. Goal steps: CC01 and CC10. Recorded: 2026-09-02.

Status: **identity and pure-design evidence plus a bounded host-launch
candidate; no native command entry or runtime-parity claim**. `C:CLI` is a
MorphOS external command with its own launch and window contract. It is not an
alias for the Shell's `NewCLI` or `NewShell` internal commands.

## Reference identity

| Item | Evidence |
| --- | --- |
| Distribution | [Official MorphOS 3.20 ISO](https://www.morphos-team.net/morphos-3.20.iso), local `D:/TestData/MorphOSReferences/morphos-3.20.iso` |
| Member | `MorphOS/C/CLI`, ISO logical block 175232, 2,886 bytes; block size 2,048 |
| Member SHA-256 | `369516c8ab540bd7e35dec5b1bea16b8e3c650c151d1a5ce8ebeafc219de07dd` |
| Embedded version | `CLI 50.2 (20.5.04)`; unexecuted packed-binary version tag at byte 2,844 |
| Executable inspection | `morphos-packed-native`; no plaintext template candidate was found. That absence does not define its syntax. |
| Workbench 3.1 | Absent from the inspected Workbench and Install `C:` inventories. Do not include it in the exact `wb31` profile. CC00 still owns the remaining-disk closure. |
| Pure/resident | **Required pure by installer design**: ISO `hdinstall.fixc`, line 9, adds P to CLI. Installed AmigaDOS flags and resident use have not been observed. |

The complete ISO SHA-256 is
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.
`hdinstall.fixc` is 2,478 bytes at ISO block 6348, SHA-256
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`.
Its P addition preserves other flags; it neither proves an installed protection
bit nor makes CLI resident. ISO POSIX mode bits are not AmigaDOS protection
evidence. The source record is in the CC00
[inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json)
and [media evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json).

## Implementation boundary

The existing Shell's child-process support remains the only candidate owner for
process creation, CLI records, inherited streams, current directory, command
tail, and foreground completion. A future external entry must call that owner
through a documented public DOS/Intuition boundary; it must not create a second
CLI registry, scheduler, or Shell parser. The command must retain a distinct
implementation path until captures show that its options, window semantics,
and result propagation equal one of the internal commands.

No `ReadArgs` template is admitted yet. Do not invent one from later AmigaOS
manuals, current MorphOS documentation, or a familiar NewCLI syntax. Once the
packed binary or controlled help establishes a template, use real DOS
`ReadArgs`/`FreeArgs` for its outer grammar and preserve any non-template
window specification as an explicit command grammar.

The [MorphOS Library CLI page](https://library.morph.zone/Shell_Commands/CLI)
currently says “No Template” and describes a new interactive CLI in a newly
opened window rather than a tab. This is a useful secondary-source capture
lead, not proof of the 3.20 packed executable's accepted arguments, errors,
or window contract. In particular, it does not authorize sharing NewCLI's
`WINDOW,FROM` template. The current bounded `CliCommand` therefore admits only
the zero-tail launch documented by that lead and returns an explicit incomplete
error for any nonempty tail. That admission rule is a CopperOS safety boundary,
not a claim about the original parser.

All launch state must be invocation-owned until ownership transfers to the
created process. In particular, template slots, window/title strings, command
tails, inherited handles, locks, and pending completion records cannot reside
in the shared resident image. A launch failure must release everything before
returning; a successful pure launcher must remain safe when invoked repeatedly
or concurrently from one loaded SegList.

The candidate records `ShellLaunchKind.Cli`, separate from `NewCli` and
`NewShell`, then calls the existing `TryCreateShell` owner with inherited
streams and current directory. It neither parses a template nor opens a second
registry/console on its own. Five focused host cases cover zero-tail launch,
the current nonempty-tail boundary, owner launch failure, continuation startup,
and failed-launch completion. Native command entry, actual window behavior,
and original compatibility remain open.

## Required captures and gates

- [ ] `CLI-SYNTAX`: exact `?` text/template, aliases, defaults, quoted and
  missing arguments, and whether `ReadArgs` is used.
- [ ] `CLI-WINDOW`: window specification/default geometry, public-screen and
  console behavior, title, close action, and failure diagnostics.
- [ ] `CLI-LAUNCH`: command/script/interactive modes, inherited versus newly
  opened streams and directory, foreground/background completion, and child
  result/IoErr propagation.
- [ ] `CLI-LIFETIME`: allocation/open/create/hand-off failures, close/break
  behavior, repeated runs and instruction-interleaved runs from one resident
  image.
- [ ] `CLI-PURITY`: installed P metadata, resident lifecycle if any, and a
  three-CPU replacement artifact bound to the required pure behavior.
- [ ] `CLI-PARITY`: controlled MorphOS 3.20 comparisons for output, process
  state, window side effects, statuses, diagnostics, and cancellation.

These are completion gates, not inferred behavior. No original binary or
complete command output is committed by this contract.
