# Exec wait result ownership regression

This project compiles the complete production emulator and links the real test
classes; it does not compile a substitute Exec implementation. Ten component
cases exercise host Signal/Wait and port delivery, and four unchanged shared
Exec cases check native signal state transitions.

Two additional host component cases cover public StackSwap return-address
transfer and FindTask searches of real synthetic ready/wait lists (16 total).
These call the production services against controlled guest structures; they
do not themselves execute a CPU return or establish retirement quiescence.

Two AddTask rows also verify default exit-entry selection and preservation of
an explicit finalizer (18 total). The actual default-exit execution is covered
separately by the combined native DOS run; these rows inspect frame publication.

Six stack-bound rows inspect the real retirement predicate (24 total). They
distinguish an empty, separately owned system stack from stale values outside
its range, and retain cleanup barriers for live supervisor/user references and
unknown bank pointers. They use controlled structures, not a scheduler handoff.

The component fixtures use controlled guest Tasks, a suspension callback and
explicit task selection. They do not execute a CPU context switch, native DOS,
an original ROM, the Shell or a command. They cannot qualify boot, PURE,
minimum-stack or shipping gates.

Run from the CopperOS root, choosing a fresh output directory:

```powershell
obj/dotnet-sdk-10.0.301/dotnet.exe test tools/Commands/ExecWaitResumeRegression/ExecWaitResumeRegression.csproj -c Release --artifacts-path artifacts/exec-wait-new/build --logger 'trx;LogFileName=result.trx' --results-directory artifacts/exec-wait-new/results
```

The 2026-09-09 comparison runs the same compiled 14-test assembly with preserved
old production owners and corrected owners. Old owners fail eight cases and
pass six; corrected owners pass all 14. It covers consumed wait results, stale
native-frame writes during host scheduling, ordinary and port waits, a second
signal arriving after wake, nonmatching/preposted signals, and the native-frame
result path. See
[the bound comparison](../../../artifacts/exec-host-wait-resume-20260909/qualification.json).
The first two build failures were missing test namespace imports, retained in
their separate logs; they are not production failures.

The build reports the existing NU1902 advisory for
`Microsoft.Build.Tasks.Git` 10.0.300. This runtime change does not resolve that
dependency advisory.
