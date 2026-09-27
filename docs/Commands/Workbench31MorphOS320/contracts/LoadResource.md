# `LoadResource` contract

## Media identity and syntax

The Workbench 3.1 member is the 3,972-byte `C/LoadResource` Amiga HUNK from
the selected M10 Workbench disk. Its SHA-256 is
`51c8d6da726d5d1429e84f36e323218eb94750ab60b650efe768fc2907d16f38`; the
embedded version tag is `$VER: loadresource 40.2 (17.3.93)`. The bounded audit
records the only recovered syntax candidate:

```text
NAME/M,LOCK/S,UNLOCK/S
```

Offline disassembly now confirms that the binary passes this exact string to
DOS `ReadArgs` at code offset `0x0092`, with three result slots and a null
`RDArgs` argument. This binds the template to the parser call, but an original
guest is still needed to confirm how ReadArgs treats switches, repeated names,
and the `/M` operand in practice.

The [AmigaOS 3.1 AmigaDOS Command Reference](https://wiki.amigaos.net/wiki/AmigaOS_Manual:AmigaDOS_Command_Reference)
describes the intended resource classes and switch semantics: libraries,
devices, fonts and locale catalogs are named by path; `LOCK` keeps
libraries/fonts/catalogs resident while devices may be preloaded but cannot be
locked; `UNLOCK` permits flushing; and an invocation with no names lists
currently locked resources. This is documentation evidence, not a substitute
for the selected Workbench 3.1 guest capture, because the binary's exact state
transitions, diagnostics and catalog behavior are still unverified.

## Required behavior boundary

The command is an installer-era resource manager. The HUNK requests
`locale.library` v38, `dos.library` v39, `utility.library` v39 and
`graphics.library` v39 in that order. Static control flow checks the DOS,
Utility and Graphics opens, but does not gate startup on Locale; the
`sys/c.catalog` path is conditional on Locale being available and uses built-in
catalog strings as fallbacks. A 15-entry default-string pointer/catalog-ID
table contains ID zero (empty) and IDs `$C350`–`$C35D`; its exact runtime message selection remains
to be confirmed. The HUNK contains the classes `Library`, `Device`, `Font` and
`Catalog`. The selected file's protection metadata has the PURE bit clear, so
the replacement must not claim PURE. It still needs lifecycle evidence for
the original lock persistence and any resident registrations. The binary also
contains diskfont/graphics calls for font catalogs. The native replacement
must preserve startup ordering, repeated-name handling, lock and unlock
ownership, result/`IoErr` precedence, diagnostics, and the non-PURE
classification. These code observations are recorded in
[`loadresource-wb31-code-audit-20260925.json`](../reference-captures/loadresource-wb31-code-audit-20260925.json).

The follow-up static disassembly finds startup coordination through DOS
`Cli`/`CreateNewProc` and Exec `Forbid`/`FindPort`: invocations exchange request
messages with one shared worker process. That worker owns two separate lists.
The 16-byte list at data offset `0x0238` is protected by an Exec semaphore and
maps a DOS lock to a preloaded SegList. The `LoadSeg` hook compares incoming
paths with `SameLock`, returns a matching preloaded SegList once, consumes and
frees that mapping, and delegates unmatched names to the saved DOS vector.
Its add/remove helpers own the DOS locks, record allocation and `UnLoadSeg`
cleanup. An active-operation count participates in hook teardown checks, but
does not alone establish safe code unloading. The second list at
data offset `0x029c` records opened resource handles for `LOCK`/`UNLOCK` and
the no-name listing path. The hash-bound detail is recorded in
[`loadresource-wb31-loadseg-lifecycle-audit-20260926.json`](../reference-captures/loadresource-wb31-loadseg-lifecycle-audit-20260926.json).

This establishes the intended shared ownership structure statically, but does
not prove worker scheduling, cross-invocation persistence, concurrent startup,
or shutdown behavior at runtime. Do not replace it with a single-invocation
stub or treat supplied-vector checks as guest lifecycle evidence.

An additional hash-bound audit records that the coordinator installs an Exec
`SetFunction` replacement for DOS `LoadSeg`, saves the previous vector, and
later attempts to restore it only after the registry is empty and the active
operation count reaches zero. Both PC-relative `LEA` instructions resolve to
HUNK2 code offset `0x0000`, so teardown checks the same hook pointer that was
installed. The hook's unmatched path calls the saved vector, but the exact
guest-visible behavior and record-state meaning have not been established.
This static evidence is recorded in
[`loadresource-wb31-loadseg-lifecycle-audit-20260926.json`](../reference-captures/loadresource-wb31-loadseg-lifecycle-audit-20260926.json).
The pointer relationship is statically resolved; lifecycle and guest behavior
remain open and must be preserved by any replacement.

The corrected
[`worker-startup audit`](../reference-captures/loadresource-wb31-worker-startup-audit-20260926.json)
binds the `CreateNewProc` tags, including its 3000-byte stack, and shows that
the first message is already the real resource request. The primary worker
publishes its service port and queues that request for processing; a duplicate
worker forwards it to the existing service. Neither path sends a separate
startup acknowledgment. After successful process creation, the client detaches
the original HUNK1/HUNK2 tail from its command SegList; the worker later owns
that tail's unload. A single-HUNK replacement requires an explicit alternative
ownership implementation before the worker can survive command return.

The same audit corrects prior assumptions about teardown: the original calls
`AttemptSemaphore` and conditionally restores the vector, but the helper
unconditionally returns one to its worker. That return is not proof that
unloading is safe when acquisition fails, a hook is active, or a newer patch
exists. The candidate explicitly retains its state in those cases. This
teardown boundary still needs original-guest and compatibility review.

The first native implementation slices are in
[`NativeWorkbench31LoadResourceLoadSeg.cs`](../../../../src/Commands/Native/NativeWorkbench31LoadResourceLoadSeg.cs)
and
[`NativeWorkbench31LoadResourceProtocol.cs`](../../../../src/Commands/Native/NativeWorkbench31LoadResourceProtocol.cs).
The former implements the bounded cache/hook path. The latter writes the
hash-bound 54-byte synchronous request with explicit Amiga offsets into
fourteen LONGs of caller-stack storage, and uses the caller's process message
port while retaining the DOS `ReadArgs` lease. The field offsets and their
source instructions are recorded in
[`loadresource-wb31-request-protocol-audit-20260926.json`](../reference-captures/loadresource-wb31-request-protocol-audit-20260926.json).
The initial packed C# message passed CLR layout checks but failed actual
68000 execution because native lowering placed `ReplyPort` at offset 20
instead of 14. The explicit writes fix that discrepancy; the host-only layout
test was removed. The client fixture now passes nineteen invocations per
68000/020/040 image, including instruction-interleaved callers, exact message
bytes, parser lifetime through reply, full result/error values and cleanup.
Receipt:
`artifacts/workbench31-loadresource-protocol-runtime-20260926-v4/qualification.json`.

The hook locates worker-owned state through the original Latin-1
`« LoadResource »` service port and task `tc_UserData`. Its exported entry creates
a fresh resident invocation context, so it now explicitly sets its DOS base
from retained state and restores it on return. Eleven native lifecycle
scenarios per CPU cover one-shot matching, saved-vector ABI, removal, direct
duplicate rejection, failure cleanup, newer-patch retention and a busy
`AttemptSemaphore`. DOS filename locking precedes semaphore acquisition.
Receipt:
`artifacts/workbench31-loadresource-hook-runtime-service-name-20260926-v1/qualification.json`.
The suites report no shared-image writes and balanced owned resources. They
supply public-vector responses; their probe entries are not the complete
command, and no worker scheduling or code-segment lifetime is qualified.
The original file's P-clear protection classification is unchanged.

## Opened resources and single-request worker

The hash-bound
[`resource-action audit`](../reference-captures/loadresource-wb31-resource-actions-audit-20260926.json)
now covers classification, library/device/font/catalog ownership, the separate
opened-resource registry, matcher control flow, all fifteen diagnostic table
entries and the listing/error paths. Production components are
`NativeWorkbench31LoadResourceRegistry`, `NativeWorkbench31LoadResourceActions`,
`NativeWorkbench31LoadResourceMessages` and `NativeWorkbench31LoadResourceWorker`.
They use public DOS/Exec/Utility/Graphics/Locale/Diskfont calls and caller-owned
or explicitly allocated state; no command-private parser was introduced.

| Native qualification | Cases per CPU (68000/020/040) | Receipt |
| --- | ---: | --- |
| Opened-resource registry | 10 | `artifacts/workbench31-loadresource-registry-runtime-20260926-v2/qualification.json` |
| Resource actions and diagnostics | 34 | `artifacts/workbench31-loadresource-actions-runtime-20260926-v6/qualification.json` |
| Single-request worker dispatch/receive | 31 | `artifacts/workbench31-loadresource-worker-runtime-20260926-v7/qualification.json` |
| Client lookup/worker handoff/request | 15 | `artifacts/workbench31-loadresource-launch-runtime-20260926-v2/qualification.json` |

The registry owns copied names, uses AddTail order and first case-insensitive
WORD comparison, and closes the matching resource type before unlink/free.
The actions fixture executes the installed native LoadSeg hook before its
supplied saved vector, and explicitly consumes a device's cached segment
through that hook. Library/font opens are supplied responses and do not
recursively invoke LoadSeg in this fixture. Classification, allocation/open
failures, cache/record retention and source-specific IoErr capture order are
checked. Devices remain cached without an opened-resource record, even when
LOCK is absent.

The worker fixture verifies full-LONG UNLOCK precedence and low-WORD LOCK,
538-byte AnchorPath initialization for each pattern, expanded-name actions,
first-error termination, MatchNext counts, MatchEnd before final PrintFault,
and directory/input/output restoration before ReplyMsg. Sender NAME/M arrays
and strings remain unchanged; the sender reclaims the message on reply so any
subsequent native access fails. Listing includes the original double-newline
header, and Locale lookups validate the captured ID/default pairs. The tested
requests use the original 3000-byte worker stack size. This does not qualify
maximum-stack use for every path of the future complete coordinator.

Native compilation exposed an unsupported argument assignment in the initial
worker; a local cursor now preserves the same traversal without that IL form.
Action and worker images have 32 and 31 reachable methods respectively, no
managed allocation sites, runtime helpers, external native targets, exception
regions or fatal sites. Their only framework feature is the exact native
`BPTR?.GetValueOrDefault` intrinsic required by public DOS.LoadSeg. The
qualifiers explicitly validate that binding rather than admitting arbitrary
nullable operations. Source hashes are captured before building and checked
again after execution. No shared-image writes occurred on exercised paths.

The additional
[`client/catalog audit`](../reference-captures/loadresource-wb31-client-catalog-lifecycle-audit-20260926.json)
confirms an unusual original lifetime: the lazy catalog opener stores the
`sys/c.catalog` pointer, and the close helper does not clear it. Print calls
Begin/Get/VPrintf/End; listing adds outer Begin/End calls around those nested
prints. Empty listing therefore submits one successful catalog pointer to
CloseCatalog twice; N listed records submit it N+2 times. Messages now preserves
that source call order using a caller-owned, initially cleared four-byte state
slot. Fixtures cover failed-open retries, reuse of the stored pointer, nested
closes, and action IoErr sampled after CloseCatalog where the source does so.
This closes the known call-order implementation gap; it does not establish
guest pointer validity or reference-count effects. Failed font-size StrToLong
parsing still uses cleared scratch where the original ignored failure with an
uninitialized value; that remains an explicit parity limitation.

## Worker-image transfer and remaining coordinator work

`NativeWorkbench31LoadResourceLaunch` now performs serialized FindPort,
creation when absent, and the first real request via the existing protocol.
The selected replacement design puts independent client and worker CODE hunks
in one executable. Before CreateNewProc the client detaches its next SegList
link and passes that tail with classic NP_Seglist and NP_FreeSeglist=TRUE.
Creation failure restores the link and reports the saved IoErr before Permit;
success leaves DOS owning the worker chain. Existing-service use leaves the
newly loaded chain intact. Applicable original process tags are retained,
including 3000-byte stack, zero priority, no copied variables and null streams/
directory/home/window. No post-3.1 process tags are used. The service name must
be stored in the retained worker image, not client code or stack.

The fifteen-case native launch fixture verifies tags, transfer/rollback,
Forbid-through-reply, failure/error ordering, parser lease and unchanged sender
payloads. It uses fixture-owned segment headers and supplied process/message
responses: it does not execute a child or qualify actual DOS unloading. Invalid
module guards are explicit candidate packaging checks, not claimed original
missing-CLI behavior. The original dereferences Cli/module without null guards.

The strict [`two-HUNK packer`](../../../../tools/Commands/combine_loadresource_hunks.py)
preserves CODE bytes and remaps worker-local RELOC32 targets from zero to one.
Ten independent Python tests exercise scattered relocation targets and malformed
input/overwrite rejection. Three pairs of actual compiled probes were packaged
in `artifacts/loadresource-two-hunk-packaging-probes-20260926-00db0d98/`.
Those are structural probes, **not runnable or installable C:LoadResource**.
The [ownership design](../reference-captures/loadresource-wb31-worker-image-lifetime-design-20260926.md)
binds public NDK contracts and the remaining proof obligations. Complete client
entry, child startup/publication/duplicate forwarding and process retirement
remain unfinished. Returning to DOS can avoid self-unload, but cannot by itself
protect callbacks paused before their counter increment or after decrement.
Last-cache consumption by a foreign task also needs an explicit wakeup so an
idle worker rechecks retirement instead of remaining asleep in WaitPort.

## Open gates

The source-derived resource lookup/list behavior, lock state transitions,
multiple-name result policy, catalog/font behavior, diagnostic bytes, and
cleanup order still require original guest confirmation. Captures must also determine
whether the background process and lock records survive command return, how
concurrent invocations find or create the coordinator, and which side releases
each lock during `UNLOCK` and shutdown. The exact startup dependency floors and
order are now statically bound, but runtime failure precedence is open. The
request, hook, registry, actions, dispatcher and launch transaction have bounded
native fixture evidence. Complete command/parser/startup orchestration,
guest catalog lifetime, real message
queues, interrupted/concurrent lifecycle, code-image ownership, package and
differential statuses remain open. Installation must have a single worker
owner: the direct duplicate guard cannot detect our hook buried below a
third-party patch, and arbitrary pre-install/post-teardown `tc_UserData` is
not a valid registry handle. The active count alone does not protect code
executing before its increment or after its decrement from unloading.
