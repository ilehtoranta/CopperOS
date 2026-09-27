# Execute pre-scan and temporary-input ABI

Status: resident temporary-input scanner and base rollback coverage implemented;
original-guest and full native fault qualification remain open. Owners:
`CopperOS.Shell` and `CopperStart.Dos`. Profiles: `wb31`, `morphos320`.

## Required behavior

When the first source line is a dot command, Execute prepares a unique `T:`
artifact before dispatching command lines. The temporary stream retains every
source line; the resident dispatcher resolves dot directives when it reaches
them. A script without a first-line dot command retains the existing direct-
input fast path.

The artifact is runner-owned. It is closed and removed on successful EOF,
parse/transform failure, break, pending-child termination, runner allocation
failure and CLI teardown. Original input remains borrowed until the transform
has closed successfully; a failed transform must never publish partial input.

## Bridge additions

The DOS-owned `DosShellScriptRunnerRecord` is now v5/124 bytes. It owns a
256-byte `ScriptTemporaryPath` buffer and a dynamically sized, NUL-terminated
`ScriptSourcePath` copy in both Shell adapters. The latter retains every path
length already accepted by Execute (up to 65,535 bytes), so the pre-scan can
reopen the original source for its second pass without managed script storage.
Its release path frees both buffers with the other runner workspace.
`DosShellScriptRunnerCodec.TrySetTemporaryInput` and its bridge wrapper now
publish a pre-opened replacement reader only when it uses that runner's exact
owned path buffer and a bounded NUL-terminated path. The bridge still needs to
own close/delete ordering around this atomic record update.
`ShellScriptFrameCodec.TryReplaceInput` now performs the matching frame
transition, resetting line/offset only when no buffered input record is live.
`IShellScriptPlatform.TryPublishScriptInput` now binds the two updates in each
Shell adapter after checking the active runner/frame/source relationship. It is
publish-only: the pre-scan transaction must still close or restore source input
and remove the temporary artifact.

Runner release now deletes a published temporary path after it closes the active
owned input, while continuing cleanup if deletion reports an error. The scanner
transaction still owns closing the original source before it publishes the
replacement reader. It reopens `ScriptSourcePath` to emit the source-ordered
temporary stream.

The implemented transactional platform operation is:

```text
TryPublishScriptInput(cli, frame, source, transformedHandle,
                      transformedPath, transformedLength)
```

It validates the runner/frame relationship, closes the old owned source only
after the new reader is valid, records all ownership in the DOS runner and
resets the frame cursor to line 1/offset 0. `TryDeleteScriptPath` removes only
an unpublished artifact after its writer/reader has closed. Published artifact
deletion remains runner-release work.

The implementation uses `IShellPlatform.Write` for exact byte writes and rejects
every short write. The bounded name constructor uses the live frame address and
never a shared `T:Execute.tmp` name. Native DOS `IoErr` is retained by the DOS
calls; its exact original diagnostic behavior remains qualification work.

## Shell pre-scan algorithm

1. Open a separate probe reader from `ScriptSourcePath` and read its first
   source line without touching the active runner input. If it is not a dot
   command, close the probe and retain the direct stream unchanged. The current
   DOS line bridge advances its input handle and cannot use the frame offset to
   rewind a probe.
2. For a dot source, close the probe and create the unique `T:` output through
   the existing DOS open-output path; never consume the runner's original
   reader before the replacement transaction is ready.
3. Reopen `ScriptSourcePath` and write every source line and its newline to the
   temporary stream exactly once. On any
   error, close/delete the provisional artifact and leave the original runner
   unpublished.
4. Reopen/replace the active runner input transactionally and dispatch the
   source-ordered temporary stream. The scanner itself never executes Shell
   commands.

The directive example with `.BRA`/`.KET` after an earlier `ECHO` is an
acceptance case: the first `ECHO` retains the original delimiters; later lines
observe the changed pair.

## Acceptance cases

- no-dot source never creates `T:` output;
- dot source produces one unique artifact and cleans it on every terminal path;
- changed delimiters/defaults affect only subsequent source lines;
- concurrent runners receive distinct artifacts;
- write/open/reopen/delete faults retain the original stream or clean the
  provisional artifact, with no leaked handles;
- a pending child retains only the transformed input and cleanup remains
  balanced after resume/cancel.

This design does not claim the original temporary filename, diagnostics, return
level or `IoErr`; those remain fixture work.
