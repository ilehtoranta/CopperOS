# WaitForNotification contract

Profile: `morphos320`. Goal steps: CC01, CC04 and CC16.  Recorded:
2026-09-20.

Status: bounded MorphOS 3.20 native candidate only.  MorphOS documents the
grammar as:

```text
NAME/A/M, QUIET/S, CONTINUE=CNT/S
```

`NAME` supplies one or more files or directories to listen to.  Each name is
registered with DOS `StartNotify` as a signal notification using one
invocation-owned Exec signal and request workspace.  A failed registration is
fatal by default; `CONTINUE` skips that name and continues registering later
names.  If at least one request is live, the command waits for the signal or
Ctrl-C, then calls `EndNotify` for every successful registration.  `QUIET`
suppresses registration fault printing.  An invocation with no successful
registration returns failure.  Ctrl-C returns `WARN` with the break error.

The candidate uses DOS `ReadArgs`/`FreeArgs`, `StartNotify`/`EndNotify`, Exec
`AllocSignal`/`FreeSignal`/`FindTask`/`Wait`, and invocation-owned request
storage.  Its private resident entry opens DOS 37, rejects Workbench startup,
validates the argument boundary, and replies/cleans up through the existing
resident startup owner.

## Bounded native receipt

`tools/Commands/qualify_morphos_waitfornotification_native_entry.ps1` compiles
`NativeMorphOSWaitForNotificationEntry` as resident HUNK images for 68000,
68020 and 68040, then runs the supplied DOS/Exec fixture.  The twelve cases
cover one and multiple names, QUIET, registration failure, CONTINUE with a
partial or empty registration set, Ctrl-C, parser failure, Workbench startup
rejection and interleaved callers.

Receipt:
`artifacts/waitfornotification-morphos-native-0a2f332649224908b505d5f230a1dc49/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 3476 | `938dbdd55a08b8395c86ad3521ccc1813282d3cb6e0da96ecb11a4d275aea450` | 12 | 12 |
| 68020 | 3420 | `da9bf99e3f08e37fd95400eb6e2536995c542314104b424c0aede57400186054` | 12 | 12 |
| 68040 | 3372 | `c547369f49c226b95aff770e34dba8ce7922fe61fa6575998d50703c11d091d7` | 12 | 12 |

This is an adapter checkpoint, not a shipping approval.  Exact original
diagnostic/output text, packed correspondence, provider behavior outside the
fixture, original differential comparison, PURE classification, same-segment
lifecycle, package placement and licensing remain open.
