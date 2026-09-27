# IconPos contract

Profile: `wb31`. Goal step: CC22. Status: partial source/media and bounded
native evidence only; this is not a shipping or PURE admission.

The Workbench 3.1 installation image contains `C/IconPos`, a resident
installer helper with version `$VER: iconpos 39.1 (24.7.92)` and SHA-256
`b8591835500a15d3c20bec94d4ae52f5cc7ef25189d9b65372ac0756d7675800`.
The captured command grammar is:

```text
FILE/A,XPOS/N,YPOS/N,TYPE/K,DXPOS/N,DYPOS/N,DWIDTH/N,DHEIGHT/N,CREATE/S,FREEX/S,FREEY/S,IMAGE/K
```

The bounded implementation keeps the parser, icon-library base, DiskObject
leases, and all mutation state invocation-local. It opens `icon.library`,
uses the public `GetDiskObject`, `PutDiskObject`, `FreeDiskObject`, and
`GetDefDiskObject` vectors, and preserves DOS startup and cleanup in the
resident owner. It supports type selection, icon position and free-position
switches, drawer geometry, default-object creation, and copying image gadget
render/size fields. Unknown types, missing objects, parser failures, and
storage failures return a DOS error with `PrintFault` and `SetIoErr`.

The candidate uses the repository's DOS 36 startup convention and a numeric
case-insensitive type matcher to preserve resident/pure compatibility. Exact
original DiskObject edge semantics, diagnostic text and result/`IoErr`
precedence still require disposable Workbench guest confirmation.

## Bounded native receipt

`tools/Commands/qualify_workbench31_iconpos_native_entry.ps1` builds the
resident entry and runs fifteen supplied Exec/DOS/icon.library vectors per CPU.
The vectors cover DiskObject position/type and drawer mutation, free-position
switches, image render copying, default creation, missing icons, parser and
type failures, `PutDiskObject` failure, startup guards, missing DOS and
interleaved callers. Receipt:
`artifacts/iconpos-wb31-native-20260920-v2/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Vectors |
| --- | ---: | --- | ---: | ---: |
| 68000 | 4,104 | `353f7c253cebca0058486a9c6c3a009b79db266510a8921de47ee7fd0568be99` | 13 | 15 |
| 68020 | 4,176 | `e8bcbc8b60ed031cc53948067b2b7a59b04858b6b1e1a63e89ffcf463c54292e` | 13 | 15 |
| 68040 | 4,104 | `a8ed0a1bbfa7e86c0e003801353844e1a7da7508120f7e1aea757f297a68b2ae` | 13 | 15 |

All three compatibility reports have no managed allocation sites, runtime
features/helpers, external native targets, exception regions, or fatal
machine-fault sites. The fixture reports no shared-image writes and balanced
invocation-owned cleanup. This is bounded ABI evidence only.

## Required before admission

- Confirm the original icon.library and DOS minimum versions, exact
  `ReadArgs` grammar, type/creation rules, diagnostics, result levels, and
  `IoErr` precedence on a disposable Workbench 3.1 guest.
- Compare the candidate against the original binary and guest for every
  option family, missing/default objects, drawer and image layouts, failed
  writes, repeated calls, and mixed callers.
- Prove resident reuse, repeated and interleaved calls, stack bounds, and
  process cleanup under the real installer launcher while preserving the
  original PURE/resident classification and cleanup points.
- Bind the command to the installation-media profile and complete
  licensing/dependency review. Extracted installer media contents must not be
  redistributed.
