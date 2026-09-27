# `ConClip` contract

Profiles: `wb31`, `morphos320`. Goal stage: CC01 and CC24. Recorded:
2026-09-23.

Status: **persistent-service reference contract; no implementation or shipping
claim**.

## Reference identity

The Workbench member is the 2,432-byte `C/ConClip` HUNK from the selected M10
disk, SHA-256
`770ccf4ab4c7ede61b327d86c45322fe966e107ae7ef806e04341786a2dd75cc`, tagged
`conclip 40.1 (9.2.93)`. Its observed protection word is zero, so PURE is not
assumed. The MorphOS member is the 5,123-byte packed
`MorphOS/C/ConClip` at ISO extent 175256, SHA-256
`5521f081d16b46800c3c46c78b2f53883fd26ff2fbeeb6e9dcdc3f42e6879b4d`, tagged
`ConClip 50.4 (02.10.2025)`. Both identities and the source boundary are
recorded in [`conclip-reference-audit-20260923.json`](../reference-captures/conclip-reference-audit-20260923.json).
The private `console.device` ABI required by the source is bound to the NDK
3.1 FD and recorded in
[`conclip-console-private-api-audit-20260923.json`](../reference-captures/conclip-console-private-api-audit-20260923.json).

## Invocation boundary

The Workbench binary contains the candidate template:

```text
CLIPUNIT=UNIT/N,OFF/S
```

The released MorphOS source uses the same template. `UNIT/N` selects the
clipboard unit and `OFF` sends `SIGBREAKF_CTRL_C` to the named worker port.
The template is a source/binary candidate, not a complete parser qualification;
interactive help, defaults, malformed numeric input, and diagnostic text still
require guest captures.

`ConClip` is a service command. A normal invocation starts a worker process
named `ConClip rendezvous task`; it does not copy or paste data synchronously
before returning. `OFF` controls an existing worker and must not start a new
one. A replacement must not turn this command into the one-shot clipboard
behavior of `Clip`.

The released MorphOS startup explicitly requests `NP_CodeType=MACHINE_PPC`
for the worker. The current native CopperSharp command toolchain emits 68k
resident bodies only, so a conforming MorphOS service needs an admitted PPC
worker artifact and its cross-architecture ownership/cleanup qualification.
Replacing that worker with an unqualified 68k process would change the
original resident design.

The current native control-plane slice is
`NativeConClipControl.TryStopExistingWorker`: it protects the Exec port list,
looks up `ConClip.rendezvous`, reads `mp_SigTask`, and signals
`SIGBREAKF_CTRL_C`. It deliberately has no normal-start path and therefore
does not constitute a command entry or a shipping implementation.

## Persistent worker contract

The MorphOS source opens `dos.library`, `iffparse.library`, `utility.library`
and `intuition.library` at version 37 in the worker. It creates the named
`ConClip.rendezvous` message port, installs the Intuition string-edit hook, and
installs the console-device snip hook when `console.device` is available. The
worker waits on the port and `SIGBREAKF_CTRL_C`, dispatches copy, paste and snip
messages, replies each message, restores the previous edit hook, removes the
snip hook, closes the device and libraries, deletes the port, and restores its
CLI module segment before retirement.

The native binding must use the captured private vectors: `GetConSnip` at
`-54`, `SetConSnip` at `-60`, `AddConSnipHook` at `-66`, and `RemConSnipHook`
at `-72`. `SetConSnip` and both hook calls take their pointer in `A0`; the
getter returns an `APTR`, and the setter returns a `LONG`. These vector and
ABI facts are resolved. They do not replace guest qualification of the call
sequence or the failure paths.

Copy writes `FORM FTXT/CHRS` clipboard data through `iffparse.library` and
`clipboard.device`; paste reads the same chunk and inserts bounded text into
the supplied string-edit work buffer. The source uses only guest pointers and
invocation or worker-owned storage. No host clipboard, host thread, or host
wall-clock substitute is allowed.

## Required qualification gates

Before either profile can ship, qualify:

1. ReadArgs ownership and exact `UNIT`/`OFF` parser behavior, including empty,
   help, malformed and duplicate invocations;
2. worker creation, duplicate-start refusal, `OFF` signaling, startup failure,
   process retirement and the race between `OFF` and worker creation;
3. hook installation/restoration, console-device absence, clipboard open/IFF
   allocation/read/write/parse failures, bounded paste truncation, and message
   reply ordering;
4. repeated service lifetime and interleaved copy/paste/snip callers with no
   leaked ports, hooks, devices, libraries, messages or shared-image writes;
5. original Workbench and MorphOS guest behavior, installed protection and
   package placement, plus byte-level differential clipboard/output evidence.

Until these observations exist, do not add a transient `ConClip` body, claim
PURE/resident compatibility, or count either profile as shipping.
