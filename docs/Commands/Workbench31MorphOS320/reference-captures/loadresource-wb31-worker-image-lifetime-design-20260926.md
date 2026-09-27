# LoadResource worker image ownership design

This is a proposed replacement architecture, supported by the selected original
HUNK and local NDK contracts. It is not evidence that startup, shutdown, callback
quiescence, or original guest parity has passed. The original command has PURE
clear; splitting its executable does not change that classification.

## Evidence and public contracts

The original executable is 3,972 bytes, SHA-256
`51c8d6da726d5d1429e84f36e323218eb94750ab60b650efe768fc2907d16f38`.
The existing [startup audit](loadresource-wb31-worker-startup-audit-20260926.json)
records its `cli_Module` link detachment, `NP_Entry` startup, duplicate-worker
forwarding, initial real request, and self-unload followed by `DOS.Exit`.

The primary local API sources are in `D:/TestData/AmigaDeveloperCD.iso`:

| Source | ISO block / byte length | Relevant contract |
| --- | --- | --- |
| `NDK_3.1/Docs/Doc/dos.doc` | 17730 / 174666 | `CreateNewProc` accepts a LoadSeg BPTR through `NP_Seglist` or an entry pointer through `NP_Entry`. On a null process result, the caller still owns resources passed through tags. |
| `NDK_3.1/Includes&Libs/include_h/dos/dostags.h` | 21063 / 5155 | `NP_FreeSeglist` applies only to `NP_Seglist` and defaults to true. `NP_Entry` and `NP_Seglist` are mutually exclusive. |
| `NDK_3.1/Docs/Doc/dos.doc` | 17730 / 174666 | `LoadSeg` chains CODE/DATA/BSS allocations with BPTRs and applies relocations. `CreateProc` describes entry at the first segment's code. `Exit` recommends assembly programs return in D0 with their original SP and RTS, instead of calling DOS Exit. |
| `NDK_3.1/Includes&Libs/include_h/dos/dosextens.h` | 21052 / 16933 | `cli_Module` is at CLI+60; process flags include `PRF_FREESEGLIST`. |

The NDK 3.1 process-tag list ends at `NP_ExitData` (TAG_USER+1025).
The SDK enum also contains later tags such as UserData and StartupMessage;
their presence in that enum does not make them available under Kickstart 3.1.
Do not use those later tags to pass the worker's bootstrap context.

## One executable containing two independent roots

Compile the client and worker separately with the resident runtime profile.
Each admitted input must be a single CODE HUNK whose relocations point only
within itself. The client is the first segment; the worker is the second.
Duplicate helper code and constants where needed so neither image retains
executable addresses or constant pointers into the other.

[`combine_loadresource_hunks.py`](../../../../tools/Commands/combine_loadresource_hunks.py)
combines that narrow format. It creates a two-entry HUNK header, copies the
client records, copies the worker records while changing each worker RELOC32
target from 0 to 1, and preserves every CODE byte, relocation offset/addend and
symbol. This does not rewrite PC-relative instructions or manufacture external
references. Unexpected BSS/DATA, flags, overlays, compact relocations, extra
records, overlapping relocation fields and out-of-range pointers are rejected.
The compiler may emit CODE+BSS for other programs; that output requires a
separate reviewed extension, not an inferred layout accepted by this packer.

The packer's independent test loader maps the segments at unrelated addresses.
Small synthetic machine programs exercise a PC-relative branch, an absolute
constant pointer and an absolute helper call in each segment. These checks
prove the supported packaging transformation, not actual Amiga process startup.

The existing `HunkImage` fixture loader flattens segments without DOS allocation
prefixes or BPTR links. It cannot by itself qualify real `cli_Module` detachment
or `NP_FreeSeglist` ownership. A startup fixture needs separate allocations,
proper segment links and independent reclamation of the client and worker.

## Client-to-worker ownership transfer

1. The client parses the original three ReadArgs slots and retains their lease
   through the reply. Enter the existing Forbid/FindPort coordination. If a
   service already exists, use it and leave the newly loaded two-segment chain
   intact for ordinary command cleanup.
2. Otherwise read the first segment through `Cli()->cli_Module`; its next BPTR
   identifies the worker segment. Save that BPTR and set the first segment's
   next link to zero **before** `CreateNewProc`. This establishes separate
   ownership even if the child runs before creation returns.
3. Pass `NP_Seglist=worker BPTR` and `NP_FreeSeglist=TRUE`, without `NP_Entry`.
   Preserve the original applicable startup choices: null input/output,
   current directory, home directory and window pointer; priority zero;
   no copied local variables; 3000-byte stack subject to measured requirements.
   Do not use NP_Arguments with null NP_Input. Keep any process name storage
   valid through its documented use; do not assume a freed client literal is
   safe merely because name copying seems likely.
4. On a null process result, reattach the saved tail to the original head;
   ordinary command cleanup still owns both segments. Preserve the creation
   error across cleanup. Do not also UnLoadSeg the reattached tail.
5. On success, DOS owns the worker chain. Send the actual resource request to
   the child's Process message port; no separate acknowledgment replaces it.
   The worker waits for that first request before an early return is possible,
   including startup failure. Thus the parent's process pointer remains usable
   through its first PutMsg. The parent keeps the request and ReadArgs alive
   until the final reply.
6. The child receives the request and repeats the service lookup under Forbid.
   If another worker won, forward that same request, balance the child's
   Forbid, and return from the independent worker root. DOS frees only this
   duplicate worker's chain. Do not reply as well as forwarding.
7. The primary worker publishes its own port and initializes its separate
   opened-resource registry, cache and library leases before processing the
   first request. Service discovery and request admission must remain ordered
   with shutdown. Opening libraries or waiting can yield; Forbid alone is not
   evidence that all nested DOS paths run without scheduling.

The original client closes its DOS, Utility, Locale and Graphics opens after
reply at HUNK0 0x01da/0x01e2/0x01ea/0x01f2. A detached replacement must retain
its own dependency leases rather than inheriting pointers from the client's
resident invocation context. Keep the extra lease calls explicit in parity
review; they are an ownership implementation, not observed original open counts.

## Return-based retirement and unresolved callback boundary

Once the worker is allowed to retire, it removes its published port, releases
its signal/registry/catalog/library ownership in the required order, balances
Forbid, and returns normally from its root. DOS then frees the worker SegList.
It must never UnLoadSeg its own currently executing chain and execute another
instruction from it. A duplicate worker with no installed hook can already use
this return-based model.

The primary worker requires more than an empty opened-resource list:

- The service queue must be empty under the admission lock, and no sender may
  retain a service pointer across an unprotected lookup/send window.
- The one-shot SegList cache must be empty. Device preloads remain here even
  when the opened-resource registry is empty.
- All callback code and state users must have left, including entry/return
  adapter instructions outside the managed hook body.
- A newer DOS patch may delegate to or later restore this hook. A different
  displaced vector requires retaining this hook's state/image until that
  relationship has been resolved; it does not authorize unloading.

**Current counter checks do not establish callback quiescence.** The compiler's
`EmitExportAdapter` creates its resident context and saves registers before
entering `LoadSegHook`. `FindStateAndBeginOperation` increments the count only
inside the method. `EndOperation` decrements it and calls Permit before the
method and export adapter restore their frames and RTS. A foreign task can be
preempted in either uncounted interval. Even an assembly wrapper needs a proof
covering its first instruction, scheduling and its last code reference; moving
the count alone is insufficient. `NP_FreeSeglist` fixes who frees the image
after root return, but cannot close these callback windows.

Another unresolved requirement is wakeup after foreign consumption of the last
cached device. If the worker is waiting only in WaitPort, a callback can empty
the cache without queuing another request, leaving the worker indefinitely
asleep. Use an explicit completion/recheck notification and a service loop that
can handle it, such as a separate signal in an Exec Wait/GetMsg loop. Signaling
the existing port bit without adding a message does not make WaitPort return a
request. Any notification mechanism must itself participate in lifetime proof.

Before admitting automatic primary-worker unload, add instruction-interleaved
fixtures that pause callbacks before the increment and after the decrement,
then attempt retirement, reclaim the worker allocation, and reject any later
instruction/data fetch from it. Also exercise last-cache-consumption wakeup,
foreign patch installation/removal, simultaneous clients, duplicate children,
startup failures and immediate sender request reclamation. Original Workbench
guest scheduling remains a separate required check. Until those obligations
are resolved, the implementation and goal ledger must keep primary-worker
retirement incomplete rather than claim a leak-free or safe shutdown.
