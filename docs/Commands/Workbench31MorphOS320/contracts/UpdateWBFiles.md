# UpdateWBFiles contract

Profile: `wb31`. Goal step: CC22. Status: partial media/static contract only;
no implementation, guest, PURE, package, or shipping claim.

The Workbench 3.1 installation image contains `Install3.1:C/UpdateWBFiles`, a
6,764-byte Amiga HUNK with version `$VER: updatewbfiles 40.1 (1.4.93)` and
SHA-256 `3aeb41230cae46822a764a1d1e0187b3f34b3c7870c4cba6ab3443d3c48c2de3`.
The member is on the selected `Install3.1` disk and has protection word zero;
that metadata does not establish a non-PURE runtime design.

No verified `ReadArgs` template is present. The binary's operation surface is
identified by the literal `NEWWB:` source assign and these destination files:

```text
Prefs/Env-Archive/Sys/wbpattern.prefs
Prefs/Env-Archive/Sys/wb.pat
Prefs/Env-Archive/Sys/win.pat
Prefs/Env-Archive/Sys/pointer.prefs
Prefs/Env-Archive/Sys/pointer.ilbm
Prefs/Env-Archive/Sys/palette.prefs
Prefs/Env-Archive/Sys/palette.ilbm
Prefs/Env-Archive/Sys/font.prefs
Prefs/Env-Archive/Sys/screenfont.prefs
Prefs/Env-Archive/Sys/sysfont.prefs
Prefs/Env-Archive/Sys/wbfont.prefs
.backdrop
.newbackdrop
```

IFF literals (`FORM`, `ILBM`, `BMHD`, `BODY`, `CMAP`, `GRAB`, `PREF`, `PRHD`,
`FONT`, `PTRN`, `PNTR`, and `PALT`) show that the utility parses and converts
Workbench preference/icon data rather than copying arbitrary host files. The
binary opens `dos.library`, `iffparse.library`, `icon.library`, and
`graphics.library`. Exact source/destination selection, missing-file policy,
file protection/date preservation, backdrop replacement, IFF validation,
partial-update rollback, and result/`IoErr` precedence remain unobserved.

## Required before implementation admission

- Capture invocation tail behavior and the complete update matrix on a clean
  Workbench 3.1 installation, including absent, malformed, old, and newer
  preference/icon files.
- Map IFFParse, icon.library, graphics.library, and DOS ownership to public
  guest APIs; do not parse or write host paths.
- Preserve invocation-local buffers and all opened files/parsed IFF handles;
  define cleanup and partial-update behavior before implementing mutation.
- Use synthetic disposable `NEWWB:` trees and byte-level readback to prove
  exact converted IFF chunks, file metadata, and failure behavior, followed by
  original guest differential, resident/PURE, and package qualification.
- Do not redistribute extracted Workbench files or preference payloads.

