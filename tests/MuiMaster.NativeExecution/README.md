# Native MUI execution runner

This host-only test tool loads a single-code HUNK and executes it using the
M68000 emulator. Success requires the guest entry to return `42`; static
compiler metadata alone does not qualify an artifact. The simulated native-root
fixtures do not prove real Intuition rendering or full public MUI integration.

```text
dotnet tests/MuiMaster.NativeExecution/bin/Debug/net10.0/CopperOS.MuiMaster.NativeExecution.dll <artifact.hunk> [maximum-instructions]
```

The default guard is 20,000,000 instructions. An optional integer from 1 through
1,000,000,000 permits an explicitly reported diagnostic budget without changing
guest code, memory, or the success predicate. Always record an override with the
result; a diagnostic pass does not imply a pass within the default guard.
Invalid arguments return 2, failure to return within the guard returns 3, an
unexpected guest result returns 4, and a caught native execution fault returns 5.

For example, the frozen corrected-compiler Radio fixture in
`artifacts/mg2824-pointer-compiler-cli/radio-construction.hunk` exhausts the
default guard but returns 42 at 20,915,717 instructions with a 100,000,000 budget.
This distinction prevents misdiagnosing a slow fixture as an infinite loop.
At 20,915,716 instructions the same artifact returns runner exit 3 even though
D0 already contains 42; at 20,915,717 it passes. The runner requires the actual
guest return, not just a matching register observed before completion.

Set `COPPEROS_TRACE_METHODS` to pipe-separated method-name fragments to observe
calls selected from the actual artifact's `.map` file. The observer prints a
bounded recent trace on failure and does not mutate guest state. D0 and A0 are
reported separately; the compiled signature determines the return register.
Unset the variable for ordinary qualification. The legacy
`COPPEROS_TRACE_DISPATCHER` switch uses historical fixed positions and must not
be used to diagnose unrelated artifacts.
