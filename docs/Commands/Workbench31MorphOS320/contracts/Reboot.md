# Reboot contract

Profiles: `wb31` and `morphos320`. Goal step: CC21. Recorded: 2026-09-17.

The MorphOS 3.20 source archive identifies the AROS-derived `Reboot` command as
version 50.3 (`23.5.09`). The archived `c/reboot/reboot.c` is 955 bytes with
SHA-256 `9e3cc8dd4f9fba123fb3507f86333c09164cf05e26b64ec237125d1c10990fc5`.
The packed ISO member is now bound by
[`reboot-morphos-binary-audit-20260923.json`](../reference-captures/reboot-morphos-binary-audit-20260923.json):
extent `178088`, 1,206 bytes, SHA-256
`6173f32fed7ab0cff36fb0c0f5e87f907525877ae67469e001f511c822199024`.
The command has no options. It calls DOS `ReadArgs` with an empty template,
releases the returned parser state when present, checks the pending Ctrl-C
signal, reports `ERROR_BREAK` and returns `RETURN_ERROR` when interrupted, and
otherwise calls Exec `ColdReboot`. The source deliberately ignores a failed
empty-template `ReadArgs` result and still follows the signal/reboot path,
preserving that parser `IoErr` if the reboot call returns. The source's normal
return value after a returning reboot call is the literal `666`; DOS-open
failure returns `RETURN_FAIL` without a diagnostic. It has no Workbench startup
message path.

`src/Commands/Native/NativeMorphOSRebootCommand.cs` implements this public
DOS/Exec boundary. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSRebootEntry.cs`.

The Workbench 3.1 Disk 1/2 HUNK exposes an empty-template Reboot syntax
candidate. `NativeWorkbench31RebootCommand` keeps a separate DOS 36 startup
entry and shares the public `ReadArgs`/Ctrl-C/`ColdReboot` body only as a
bounded implementation candidate. Classic reboot transition and return
behavior still require an original Workbench capture.

## Bounded native receipt

`tools/Commands/qualify_morphos_reboot_native_entry.ps1` compiles resident
68000/020/040 HUNKs and executes ten supplied DOS/Exec vectors per CPU (30
total): normal and ignored-tail calls, Ctrl-C, parser and DOS-open failure,
Workbench and entry-boundary rejection, and interleaved repeat calls. The
durable receipt is
`artifacts/reboot-morphos-native-20260923-parity-v2/qualification.json`.

Each HUNK has no managed allocation sites, runtime helpers, external native
targets, exception regions, or fatal machine-fault sites, and the fixture
reports no leaked resources or shared-image writes. This is adapter evidence
only: a real reboot transition, original guest parity, PURE/resident reuse,
package admission, and differential comparison remain open.

The Workbench candidate is qualified by
`tools/Commands/qualify_workbench31_reboot_native_entry.ps1`; receipt
`artifacts/reboot-wb31-native-20260923-parity-v2/qualification.json` records
ten supplied DOS/Exec invocations on each resident 68000/020/040 HUNK. This
remains syntax-candidate evidence only and leaves real reboot, PURE/resident,
packaging and differential gates open.
