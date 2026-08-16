# CopperOS Shell DOS adapter

`CopperOS.Shell.Dos` is the first concrete value-type owner for the Shell
interfaces. `DosShellPlatform<TDosPlatform>` forwards guest memory, ReadArgs,
startup-script input, redirection, CLI fields, aliases, variables, command
paths, resident management, and file lookup to CopperStart DOS. A Shell start
now publishes its guest frame in a DOS-owned CLI binding, allowing direct
`Quit`, `EndCLI`, and `EndShell` requests plus previous-result, text, numeric,
and file-existence `If` evaluation to update the frame without managed state.

The adapter now owns nested `Else`/`EndIf`/`EndSkip` control records and
`Lab`/`Skip` label records through DOS allocations; all are reclaimed when the
frame is detached. `Ask` is implemented as a bounded Y/N line read; unknown
answers fail deterministically. File-backed `Run` reaches the existing Exec
task creator when a live `ExecBase` is supplied, and terminal continuations
release their inherited DOS/Exec resources through the process-owned task
record. Resident commands bind their acquired use count to that Process and
release it on task death without unloading the shared segment. `Execute` now
runs bounded internal script lines through the guest frame engine. CopperStart
also exposes a DOS-owned fixed-width foreground-wait record for a future
non-spinning external-script handoff; it retains the parent CLI, child
continuation, frame, and next-line cursor and is reclaimed with the frame
allocation chain. `Execute` now retains its complete frame/workspace in a
DOS-owned runner while a child is pending and resumes it through non-blocking
polls. The `copperos.shell.execute-park` ABI now bridges a prepared wait
directly to the DOS-owned record and is included in the fixed-width native
qualification root; the Begin/Poll entrypoints now use a four-byte DOS-state
context. Aggregate DOS item and resident record builders use scalar codecs,
and the full Begin/Poll/Park export set is qualification-green for MC68000,
MC68020, and MC68040 with no managed allocation sites. Child Process
termination completes its matching foreground continuation before DOS release
and wakes the prepared parent wait through Exec. The native adapter now also
has a scalar file-backed `Run` launch handoff: it validates the HUNK entry,
creates a Process-sized task, publishes a DOS-owned CLI, and records the
segment list without managed records. Resident `Run` uses a scalar resident
registry lookup/acquire/release handoff and binds the use count to the child
Process. The native Execute runner now uses the same scalar image/Process/CLI
handoff for external script commands and keeps the continuation record in the
DOS frame.
Native provider callbacks still need to provide private stream/lock duplication;
the interim native handoff keeps
inherited records shared and parent-owned. The native provider now calls the
scalar DOS vectors directly for file/lock open, close, duplication, seek, byte
I/O, parent, and name operations; relative-lock, console, packet, and requester
capabilities remain explicit fail-closed boundaries. The adapter consumes
MorphOS `SIGBREAKF_CTRL_C`/`SIGBREAKF_CTRL_D` and maps them to Shell
break/Ctrl-C/Ctrl-D events without clearing unrelated signals. Requester
integration beyond this bounded form remains an explicit failure until its
remaining DOS/Exec hooks are connected. It has
no exceptions, managed collections,
delegates, host streams, or managed process state.

`tests/Shell.Dos.NativeRoot` compiles the fixed-width runner, foreground-wait
records, and full Execute exports into one local-ABI reachability assembly. The
Execute entrypoints have a four-byte DOS-state context and are qualified by
`qualify_native.ps1 -IncludeFullExecute`; the root also compiles the native Run
handoff, external script-command launch, and continuation teardown. This does
not affect the managed execution path or the DOS-owned lifecycle records.
