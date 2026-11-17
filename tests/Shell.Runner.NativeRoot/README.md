# Shared runner guest probe

This command exercises `ShellRunnerBuffers`, Shell-owned nested frame controls
and labels, the public CLI field helpers, packed runner cleanup,
process-local binding, and public DOS file I/O on a 68000 Amiga. The file
checks cover buffered and unbuffered writes, append to existing and missing
files, CRLF and unterminated lines, seek, EOF, invalid names and modes, and
deletion. It writes an empty `DH0:runner-ok` file on success, or
`DH0:runner-fail` on a failed check. It requires a writable helper volume named
`DH0:`. It is a focused code-generation and execution test; it does not verify
the Shell's runner discovery, command dispatch, or complete Shell behavior.

Build the managed input with the local SDK, then compile it with a frozen
CopperSharp compiler input directory (for example, the `inputs` directory
produced by `tools/ImageBuilder/build_system_proof.ps1`):

```powershell
dotnet build tests/Shell.Runner.NativeRoot/CopperOS.Shell.Runner.NativeRoot.csproj -c Release -p:CopperOSUseLocalCopperSharp=true --artifacts-path artifacts/shell-runner-probe
$p = 'artifacts/shell-runner-probe/bin/CopperOS.Shell.Runner.NativeRoot/release'
$compiler = '<proof-directory>/inputs/CopperSharp.Compiler.Cli.dll'
dotnet $compiler "$p/CopperOS.Shell.Runner.NativeRoot.dll" --entry 'CopperOS.Shell.Runner.NativeRoot.RunnerProbe::Main' --platform amiga --cpu 68000 --clr always --exceptions yolo --fpu disabled --format hunk --symbols off --runtime freestanding --memory none --peephole fixed-point --code-size-optimizations off --managed-assembly "$p/CopperOS.Shell.dll" --managed-assembly "$p/CopperSharp.Sdk.Amiga.dll" --managed-assembly "$p/CopperSharp.Sdk.Amiga.Support.dll" --managed-assembly "$p/CopperSharp.Compiler.dll" --output artifacts/shell-runner-probe/runner --compatibility-report artifacts/shell-runner-probe/runner.compatibility.json
```

Place `runner` on the helper volume, boot the image under test, run
`DH0:runner`, and inspect the newly created marker on the helper volume. Use a
fresh helper directory so an old marker cannot be mistaken for a result. The
guest also returns a nonzero command result on failure.

The same project contains `PathProbe::Main`. Compile it with the same options,
substituting `CopperOS.Shell.Runner.NativeRoot.PathProbe::Main` for the entry and
`path` for the output name. It uses public DOS vectors to exercise a temporary
CLI path, including quiet requester restoration and duplicate suppression. It
also checks rollback when a directory does not exist and writes the live path
listing to `DH0:path-list`. The probe uses the same public Exec/DOS path
platform as the native Shell. It checks `CD` and rollback for a missing path
and a file, then restores the original directory. It also closes and deletes a
temporary file through the public cleanup operations. It writes `DH0:path-ok` or
`DH0:path-fail`; it does not test the full Shell loop.

`LongPathProbe::Main` exercises a 301-entry path transaction and checks that
all old and replacement entries are released. Its synthetic lock identities
isolate list ownership from filesystem behavior. A 68000 HUNK built from this
entry passed on Kickstart 3.1 and CopperStart, including return to and exit from
the interactive Shell. The separate Path probe uses real DOS locks.
