# Exe2Arc shipping-entry runtime fixture

The shared `Commands.NativeExecution` runner and this standalone project run
the same fixture against a relocated MC68000 resident HUNK executable.
Use `exe2arc-native-entry-vector-fixture` for full runtime qualification.
The existing `exe2arc-frontend-native-smoke` suite retains its two parser cases.
`exe2arc-type-diagnostic-native-probe` isolates diagnostic failures for negative
controls; it is not the full admission suite.

The full suite makes 63 invocations per image and CPU, including:

- Explicit and automatic ZIP, ACE, RAR, CAB, ARJ, LhA and LZH extraction using
  independently constructed, inert input bytes and expected output bytes.
- TYPE matching, unknown types, no-match and unsafe-position diagnostics,
  generated/literal/fallback output names and bounded path handling.
- Parser, allocation, input/output, seek, examination and transfer failures;
  short transfers; incomplete-output deletion after closing its handle.
- ZIP Ctrl-C before a record, after a local record and after the EOCD record,
  preserving the command's partial-output and reported-byte-count behavior.
- CLI argument boundaries, missing DOS, Workbench startup and message reply.
- Repeated and instruction-interleaved invocations through one protected image.
  Peers have distinct DOS bases, handles, processes, stacks and allocations.

Every invocation checks its return, IoErr, output and extraction bytes, exact
allocation ownership/size/release, file/message ownership, restored entry SP,
stack guards and the unchanged resident image. Cleanup vectors deliberately
overwrite IoErr. Public vectors clobber volatile registers and CCR. The main
stack is 4 KiB; an interleaved peer also exercises an 8 KiB stack.

The vectors use the public DOS register ABI, including seek modes -1/0/1 and
the LONG argument vector for VPrintf. Input archive bytes are never executed.
PrintFault calls are checked by error code and null header; original DOS's
rendered fault text is outside this fixture's scope.

This is supplied-vector qualification. It does not provide original MorphOS
or Workbench command parity, a real DOS parser/filesystem handler, or
CopperScreen execution. Exe2Arc has one MorphOS-derived command body; Workbench
launch handling is tested without implying a separate Workbench command body.

For frozen toolchain comparisons, component tests, negative controls, receipts
and guarded installation, use `tools/Commands/qualify_exe2arc_runtime.py`.
