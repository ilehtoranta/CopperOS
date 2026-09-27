# `IPrefs` contract

Profiles: `wb31`, `morphos320`. Goal stage: CC20. Recorded 2026-09-22.
This is a startup-service contract; it is not a shipping qualification.

## Reference identity

| Profile | Reference | Identity | Evidence limit |
| --- | --- | --- | --- |
| `wb31` | Workbench 3.1 M10 disk 2 `C:IPrefs` | 13,848-byte HUNK, `iprefs 40.7 (2.6.93)`, SHA-256 `14341be81f06506852204a33e7bfe7eb6fa2067d7f1e55810007bd94b021f0ef` | Original binary and bounded strings only; no guest startup receipt. |
| `morphos320` | MorphOS 3.20 ISO `MorphOS/C/IPrefs` | ISO extent 175924, 43,798-byte packed member, SHA-256 `2c4f6bf00b8f917fe30675fa7c758850583dc712f64b8bf1fed40c646b38cccf` | Packed member is hash-bound; released source is behavior evidence only. |

The complete audit is in
[`iprefs-reference-audit-20260922.json`](../reference-captures/iprefs-reference-audit-20260922.json).

## Invocation and lifetime

IPrefs has no recovered `ReadArgs` template. It is a startup service with a
single long-lived owner. MorphOS first rejects a duplicate process identified
by `IPREFS_SEM_NAME`, opens the required libraries, expands `ENV:`, creates
missing `ENV:sys` and `ENVARC:sys` directories, and creates a private process
with no inherited console/current/home directory. The parent waits on a private
message port until the child reports that preference loading is complete.

The replacement must preserve this owner and completion protocol. It must not
run preference handlers in a transient command process, return success before
the child has finished, or leave a second service competing for global
preference state. Every library, notification request, message port, child
process and preference-owned resource needs an explicit failure and shutdown
path.

## Provider and preference boundary

The MorphOS source requires DOS 37, Intuition/graphics/utility/iffparse,
keymap/layers/diskfont/workbench/icon, CyberGraphX 50, query 51 and mount 51.
It dispatches IFF preference IDs through handlers for input, screen, font,
locale, palette, pointer, Workbench pattern, sound, IControl, printer,
Workbench/Reaction, serial, blanker and MorphOS extensions. Text fallbacks
cover mouse, keyboard, font, screens, IControl, Poseidon, network, user,
logging, compositing, powerbook and disk-cache configuration.

The Workbench binary imports a smaller classic surface and contains exact
diagnostic candidates for missing iffparse/locale/diskfont/datatypes, file,
keymap, font, sound and picture providers. Do not copy MorphOS preference
modules into the Workbench implementation or assume that a shared file name
has shared data layout.

## Installation validation

MorphOS validates the running boot image against `MorphOSBoot:.morphosid` and
the `MorphOS` resident version. A newer install medium or an old boot image
produces requesters and may reboot; holding either shift key launches an
asynchronous shell instead. Device, CD, OpenFirmware and machine-family tests
change the explanatory text. This behavior must be captured on supported guest
profiles before implementation, and must never be approximated with a host
environment check.

## Required qualification gates

Before admission, qualify both profiles with:

1. duplicate-service, missing-library, missing-environment and child-process
   creation failures;
2. completion-message ordering, notification startup/teardown and termination
   without leaked global state;
3. every preference file and IFF handler that belongs to the profile,
   including fallback precedence and malformed-file behavior;
4. input/keymap, font, screen reopen, graphics, datatype, printer, sound,
   network and disk-cache provider failures;
5. exact requester/diagnostic/result behavior for old/new installation media,
   including shift-qualified fallback; and
6. original guest differential, same-segment/lifecycle, PURE metadata,
   licensing and package placement evidence.

Until those gates pass, keep both `CC20.IPrefs` rows partial/open and do not
replace the continuing service with a no-op command body.
