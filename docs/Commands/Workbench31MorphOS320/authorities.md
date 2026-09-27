# Command reference authorities and CC00 evidence

Evidence captured 2026-08-30. This is a **partial reference closure**, not a
completed command contract or implementation qualification. Reproduce the facts
with `python tools/Commands/Inventory/inventory.py verify-media` from the repository
root; see the inventory tool README for paths, schema and safety rules.

## Admitted original media

| Reference | Observed scope | Integrity |
| --- | --- | --- |
| Original Workbench v3.1 rev 40.42, M10, Install disk | `Install3.1:C`: 20 ordinary files. The full directory tree was traversed; installer/setup/startup scripts were read without execution. | ZIP SHA-256 `e320dbbcb2b8e34da7d3e37a2953c623c26755191a74c2512169b2e6f81f2e78`; ADF SHA-256 `8f54e735925d733719a321a0ddabd8fd6d1c1c3d3c0a516ed6b77dee89ac9e1e`. |
| Original Workbench v3.1 rev 40.42, M10, Workbench disk | `Workbench3.1:C`: 50 ordinary files. Twelve overlap Install, giving 58 distinct classic C names. | ZIP SHA-256 `d93611887acf91f68f5608a5b7812f03eb16f743f40451b67c93067b04c967ed`; ADF SHA-256 `a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`. |
| [Official MorphOS 3.20 ISO](https://www.morphos-team.net/morphos-3.20.iso) | `MorphOS/C`: 188 files; root `C` is empty. Traversed 772 directories. | Full 471126016-byte image now verified against [published MD5](https://www.morphos-team.net/downloads) `70b84b8c0bb1cf9b10b7062fe8809c85`; SHA-256 `3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`. |

This completes the whole-ISO integrity check that was still open when the goal
was written. It does not establish installed overlay behavior or runtime results.
The private ISO is `D:/TestData/MorphOSReferences/morphos-3.20.iso`; the two local
Workbench ZIPs remain under `D:/TestData/TestImages/`. Their full basenames and ADF
member names are recorded in `media-evidence.json`. No originals are copied into
the repository.

The union contains **200 distinct external identities**: 58 classic names and
188 MorphOS names, with 46 shared and 12 classic-only. Each identity/profile is
matched to the goal's Appendix A membership and implementation owner. Original
case remains in source records, including `Filenote` versus `FileNote`. The 31
existing Shell internal names are a separate regression dependency, not new
external commands. Non-C counterparts on another OS do not create a C-profile
entry without placement evidence.

## Pure/resident evidence is kept separate

All 70 inspected classic C file entries have their original ADF P bit clear.
Raw protection fields and R/W/E/D, P, S and A interpretations are recorded per
file. This observation does not establish NonPure design: startup and installer
scripts explicitly force resident use of seven distinct commands—Assign,
Execute, IconPos, Delete, Reboot, ExtractKickstart and FindResident.

The MorphOS ISO's `hdinstall.fixc` adds P to 88 named commands; the additions do
not replace other flags. Its SHA-256 is
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`.
`MorphOS/S/startup-sequence` temporarily makes Assign and Execute resident and
removes them later. Its SHA-256 is
`407bc8480dba6acf11c19966d67689996015aab27a6eca04d0af2d3d244168b4`.
All 88 protection additions and 18 resident add/remove statements across the
selected scripts have source hashes, line numbers and command identities in the
evidence file. Installer scripts constructed from literals are recorded as
script intent, not claimed to have run.

MorphOS ISO records contain POSIX/Rock Ridge metadata. These values are not
converted into native AmigaDOS P/S flags; those fields remain null. Absence from
the installer P list remains `unresolved-not-nonpure`. Required pure design,
installed protection flags, resident-table state and replacement-artifact purity
are independent facts. No replacement artifact is qualified by this inventory.

## Contract authorities and limitations

The [MorphOS command index](https://library.morph.zone/Shell_Commands) is a
community documentation locator. Release membership comes from the selected
media; complete options require matching binary/help/source and runtime evidence.
The [MorphOS 3.20 release notes](https://www.morphos-team.net/releasenotes/3.20)
identify changed options and features, including HDWrite additions absent from
older documentation. The [MorphOS SDK downloads](https://www.morphos-team.net/downloads)
and original 3.1 SDK/autodocs support the subsequent API audit; they are not
replaced by platform assumptions in this extractor.

The raw binary scan finds matching version tags in all 70 classic C entries and
180 of the 188 MorphOS C entries. It retains other version tags with offsets;
an absent or non-matching tag is left unresolved. MorphOS packed native files are
not decompressed. The scan finds 59 template-looking strings across the classic
entries, including duplicated disk copies, and none in the packed MorphOS files.
These are explicitly **syntactic candidates**; ReadArgs use, exact help behavior,
aliases, numeric ranges and option completeness are not inferred. No command is
run with `?`, no script is interpreted, and no hardware or filesystem operation
is performed on the reference system.

Original developer material remains available privately at
`D:/TestData/AmigaDeveloperCD.iso` (goal Appendix C records its identity). This
tool does not independently recapture or qualify those API documents. The
[MorphOS source releases](https://www.morphos-team.net/sources) are partial and
have component/file-specific licenses. Their availability does not grant blanket
reuse permission or substitute for an inventory of all commands. The selected
3.20 command-source archive hash recorded during planning is
`db099324cbf4b07de70bcafc626af0b168903a714911f840652e1a364578b5ba`; it was not an
input to the automated media scan.

## Required open closure items

The 2026-08-31 local availability capture found only the selected Install and
Workbench ZIPs in `D:/TestData/TestImages`; its exact selection and limits are
recorded in [wb31-local-media-availability-20260831.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/wb31-local-media-availability-20260831.json).

The 2026-09-16 recheck still finds only the same two admitted ZIPs in the
known input directory, with matching hashes. See
[updated local availability](reference-captures/wb31-local-media-availability-20260916.json).
This is an availability observation, not evidence that the missing four disks
contain no commands or no protection/resident changes. Their closure remains
open; other command implementation and lifecycle work can continue.

- **WB31-DISKS-3-6:** Extras, Fonts, Locale and Storage are not admitted/observed.
  Additional matching ZIPs are reported as unadmitted rather than silently used.
- **WB31-MACHINE-VARIANTS:** only the selected M10 40.42 pair has been verified.
- **WB31-INSTALLED-METADATA:** clean installed placement, flags and original
  installer/resident lifecycle need controlled observation.
- **MORPHOS320-INSTALLED-OVERLAY:** installed SYS:C/MOSSYS:C lookup, filtering,
  assigns and resulting P flags remain open, including maintenance-only tools.
- **COMMAND-RUNTIME-CONTRACTS:** templates, options, behavior and native
  comparisons are CC01/CC43 work; raw media facts cannot close them.
- **FREEZE-INDEX-MEDIA:** HunspellService is a required observed media command.
  Freeze is documented and appears in [historical 2.1 release notes](https://www.morphos-team.net/releasenotes/2.1),
  but is absent from the inspected 3.20 ISO tree. CC39 must reconcile it; it is
  explicitly outside the initial 200 count and has not been silently dropped.

## Verification

The extractor checks the two ZIP and ADF hashes, original ADF block/header
structure and checksums, complete ISO integrity and directory structure, all
200 name/profile/owner rows, source hashes, P-script membership, resident event
links and open coverage statements. `verify-media` rebuilds both factual JSON
objects and requires an exact match. Synthetic corruption and ledger-integrity
tests run without original vendor files.

`verify` succeeding means the exported facts are internally valid.
`verify --strict-complete` must return **2** while the closure items above remain;
that is the expected incomplete-coverage result, not a passing final goal gate.
