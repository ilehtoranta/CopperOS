# Runtime filesystem staging

This directory describes the Amiga runtime filesystem produced by CopperOS builds.
`SYS/` represents the system volume and uses familiar Amiga directories such as
`C`, `Devs`, `L`, `Libs`, `Prefs`, `S`, `System`, and `Tools`.

Source code does not live here. Components are developed in `src/`, built, tested,
and then copied or packaged into the appropriate runtime location by build tooling.
Generated binaries should therefore be treated as staged build output rather than
as the authoritative source for a component.
