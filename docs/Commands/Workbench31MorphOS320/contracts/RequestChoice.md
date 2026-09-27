# RequestChoice contract

Profile: `morphos320`. Goal step: CC24. Recorded: 2026-09-18.

The inspected MorphOS 3.20 C source is
`D:/TestData/MorphOSReferences/c-3.20-extract/c/requestchoice/requestchoice.c`
(SHA-256
`dcadad2e5982a7e4a3904bb3e0d0fa2a91a79e35ee3d9a9c41c684341d4772e6`). The
source command template is:

```text
TITLE/A,BODY/A,GADGETS/A/M,PUBSCREEN/K,TYPE/K,TIMEOUT=TIMEOUTSECS/K/N
```

The captured source requests `dos.library` version 37 and `intuition.library`
version 37. The replacement uses DOS36 for its `ReadArgs`/cleanup calls and
retains Intuition37 pending a lower-floor proof. The
command uses DOS `ReadArgs`/`FreeArgs`, joins the `GADGETS/M` vector with `|`,
and doubles percent signs in the body before passing the text to an
`EasyStruct`. A missing public-screen lock, gadget/body allocation failure,
requester construction failure, or requester result of `-1` returns
`RETURN_FAIL`, sets `ERROR_NO_FREE_STORE`, and calls `PrintFault("RequestChoice")`.
Successful requester results are printed as a signed decimal line and return
`RETURN_OK`; timeout or Ctrl-C selects result zero. The optional `TYPE/K` value
is parsed by the template but is not consumed by the inspected source body.

Without `TIMEOUT`, the body calls `LockPubScreen`, `EasyRequestArgs`, and
`UnlockPubScreen`. With `TIMEOUT`, it calls `BuildEasyRequestArgs`, unlocks the
screen before servicing the window, then uses a VBlank `timer.device`
`TR_ADDREQUEST` alongside the requester signal and `SIGBREAKF_CTRL_C` until
`SysReqHandler` returns a choice. It aborts and waits for the timer request,
frees the requester, closes the timer device, and frees the signal on every
completed timeout path; if timeout setup fails it falls back to blocking
`SysReqHandler` processing.

`src/Commands/Native/NativeMorphOSRequestChoiceCommand.cs` and
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSRequestChoiceEntry.cs` now
implement the resident body through the public Intuition, Exec, timer.device,
and DOS vectors. The independent fixture covers parser failure, Intuition-open
failure, no-timeout requester, percent-body input, timeout selection, Ctrl-C,
public-screen lock failure, temporary text allocation failure, timer-open
fallback, missing-DOS startup, Workbench startup, and repeat use. The
three-CPU receipt is
`artifacts/requestchoice-morphos-native-20260921-v2/qualification.json`;
each resident HUNK passes sixteen supplied invocations, including timer-port
and timer-request allocation fallback plus two interleaved callers, with
balanced parser, text, requester, timer, and library ownership and no
shared-image writes.
This is native adapter evidence only. Original guest UI behavior, exact
interactive lifecycle, PURE classification, Workbench correspondence,
packaging, licensing, and differential comparison remain open.

The Workbench 3.1 profile remains open: no verified Workbench 3.1 source or
original guest capture has established that its template, diagnostics, type
handling, or timeout behavior is identical to this MorphOS member.

## Workbench 3.1 bounded candidate

The Workbench media identity is `C/RequestChoice`, 1,120 bytes, version 39.4,
SHA-256
`161c03df81cc2ce46f12cba19167cf301f55e3f10a57b7f7b535937ccbc82381`. Its
captured syntax string is the four-slot candidate
`TITLE/A,BODY/A,GADGETS/M,PUBSCREEN/K`; the inventory records this as syntax
evidence only, not a completed runtime contract.

`NativeWorkbench31RequestChoiceCommand` and its DOS36 resident entry implement
that candidate through `ReadArgs`/`FreeArgs`, public Intuition requester calls,
percent escaping, gadget joining, output, parser/startup failures and
invocation-owned cleanup. The three-CPU candidate receipt
`artifacts/requestchoice-wb31-native-20260920-candidate/qualification.json`
passes eleven supplied DOS/Intuition invocations per CPU, including two
interleaved callers. The HUNK hashes are:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 4232 | `813fbefa8e48fc8940a3b566b377066d58dbd381b76b9687828fbeea80287d80` | 16 | 11 |
| 68020 | 4324 | `9938e2897462e05f30504f08515522bdddfbc443707f7dab0cc7ad8977d4cba7` | 16 | 11 |
| 68040 | 4232 | `4c7ed26afadd3e7b333c61dcca6c7d90cc60942e5642d97e8e12567769d3c11a` | 16 | 11 |

This remains a Workbench syntax/native candidate. Exact original UI behavior,
diagnostics, timeout/type policy, PURE/resident classification, lifecycle,
package placement and differential comparison remain open.
