# Execute contract and Shell integration

Profiles: `wb31`, `morphos320`. Goal steps: CC01 and CC10.
Recorded: 2026-08-30; updated 2026-08-31. Status: **classic template and
resident use verified; raw-tail transport and bounded directive state are
implemented; original fixtures and native command parity remain open**.

The [2026-10-04 native size optimization](../execute-size-optimization-20261004.md)
reduces the shipping MC68000 HUNK from 6,780 to 6,376 bytes (5.96%). The shipping
entry passes 151 supplied Exec/DOS cases on each of 68000/020/040, including
template prompting and Workbench startup. Peak observed stack is 328 bytes
against the unchanged 4,096-byte fixture limit. These are native vector-model
results; remaining original guest, I/O fault, capacity and ownership gaps are
documented separately and are not closed by the size change.

## Reference identity

| Profile | Reference | Version, size and SHA256 |
| --- | --- | --- |
| `wb31` | `Workbench3.1:C/Execute`, original Disk 2 ADF, file-header block 244 | `execute 37.11 (14.5.91)`, 4,432 bytes; `27c2f578b63857ab5fbe03cdf621d553408cc670b9f07d9988ce224d22b57ecd` |
| `morphos320` | `MorphOS/C/Execute`, official 3.20 ISO block 175404 | `Execute 50.7 (19.12.07)`, 7,934 bytes; `c142679a1c3327e2b68211649efc7592b0384f5db9c2c36e1e218354643c00d4` |

Classic ZIP in `D:/TestData/TestImages/`:
`Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip`;
same-base ADF member, SHA256
`a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`.
MorphOS reference is the [official ISO](https://www.morphos-team.net/morphos-3.20.iso)
at `D:/TestData/MorphOSReferences/morphos-3.20.iso`, block size 2,048.
The verified complete ISO SHA256 is
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.

The classic executable embeds the exact advertised template:

```text
FILE/A
```

The [MorphOS documentation](https://library.morph.zone/Shell_Commands/Execute)
reports the same template. Its `7f4d4f53` native payload has not been decoded;
the exact MorphOS binary/help template remains to confirm. The official
partial command source archive contains no Execute source.

## Script arguments must survive FILE/A

FILE is the script name, but the invocation can also supply arguments governed
by the script's `.KEY`/`.K` template. This is a separate script argument stage;
it is not justification to advertise a new `/F` option or to reject every word
after FILE. The [classic reference transcription](https://www.jaruzel.com/amiga/amiga-os-command-reference-help/execute.html)
describes this behavior; original-binary fixtures must freeze the exact raw
tail, quoting and substitution order.

The original binary also embeds directive names
`KEY,K,DEFAULT,DEF,BRA,KET,DOLLAR,DOT`. This confirms that directive processing
exists, not every abbreviation rule or parameter grammar. Capture `.DOL` and
other abbreviated spellings instead of assuming a modern template parser's
abbreviation policy.

Read-only extraction on 2026-08-31 reassembled the nine contiguous FFS data
blocks beginning at the recorded header's first-data block 245, trimmed the
result to the recorded 4,432-byte length, and reproduced the listed SHA256.
The binary additionally contains these distinct directive diagnostics:
`Invalid directive`, `More than one .KEY directive`, `Illegal KEY directive`,
`Parameters unsuitable for key "%S"`, `Illegal key`, `Missing .KEY directive`,
`Illegal data item`, `Key too long`, and `Invalid directive argument`. This is
identity and vocabulary evidence only. It does not establish matching rules,
abbreviations, output streams, return levels, IoErr, or substitution order.
The hash-bound capture metadata is retained in
[execute-wb31-binary-audit.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-binary-audit.json).

A disposable WinUAE/68000 guest fixture now executes that exact classic binary
with `.KEY filename` and `C:Execute RAM:execute-probe fallback`. Its inner
script records `fallback`, and the calling startup appends its `END` marker,
proving one positional `<filename>` substitution and return to the caller. The
hash-bound run is retained in
[execute-wb31-key-substitution.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-key-substitution.json).
An otherwise identical no-`T:` control produced the same capture, so this case
does not require a caller-installed `T:` assignment. Neither run establishes
output inheritance, temporary-file behavior, diagnostics, return level, IoErr,
directive order or MorphOS parity.

| Input layer | Required ownership and checks |
| --- | --- |
| Shell command line | Existing Shell handles command resolution, quoting, variable expansion, redirection, continuation and input ownership. Preserve its raw command/script tail boundaries. |
| Execute filename | Use the documented DOS argument/item boundary; preserve spaces, empty/missing filename behavior and `?` continuation. Do not consume script arguments as excess FILE/A arguments. |
| Script template | The script engine binds first-line `.KEY`/`.K` templates, validates required and keyword values with `ReadArgs`, records one script-wide `.DEF` default for each declared key (whitespace or `name=value` spelling), tracks `.BRA`/`.KET`, `.DOLLAR`/`.DOL` and `.DOT` state, and expands matching references on following lines. Dot-first sources pass through runner-owned temporary input while retaining every directive line, so effects remain source ordered. Original diagnostics, fault behavior and MorphOS parity remain qualification work. |
| Script lines | Existing engine owns line dispatch, nested frames, CLI state, labels/flow, failure thresholds, Quit and break handling. No second Shell implementation belongs in C:Execute. |

Required script behavior includes default substitution, bracket/dollar/dot
changes, comment/directive handling, script nesting, continuation and input
redirection, script protection and direct script invocation. Capture the
no-directive fast path versus directive expansion, T: work-file creation and
cleanup, failure when T: is unavailable, and the observable Shell state after
return. Do not demand a temporary file solely to imitate an internal algorithm
unless its existence or failure behavior is observable in the selected profile.

The AmigaDOS script manual says a script that starts with a dot command is
scanned before dispatch and transformed into a temporary `T:` file. The
hash-bound Workbench 3.1 fixture in
[execute-wb31-late-brackets.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-late-brackets.json)
establishes that `.BRA` and `.KET` after an earlier `ECHO` do not change that
earlier line. CopperOS therefore retains all directive lines in the temporary
input and lets the resident dispatcher apply them as it reaches each line.
The source/limit record is retained in
[execute-script-manual-semantics.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-script-manual-semantics.json).

The AmigaDOS manual documents one `.DEF` per formal `.KEY` name, an optional
empty value, and either whitespace or `=` between the name and value. The
resident implementation follows the source-observed empty `name=` rule. The hash-bound
[default fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-default.json)
confirms one classic whitespace `.DEF filename fallback` case: invoking the
script without a positional value emits `fallback` and returns to the caller.
A second hash-bound [equals/explicit fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-equals-explicit.json)
confirms `.DEF filename=fallback` and that a supplied positional value takes
precedence. A third [empty-equals fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-equals-empty.json)
and a fourth [empty-whitespace fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-whitespace-empty.json)
show the same bounded rule for `.DEF filename=` and `.DEF filename `: both
return success and dispatch following non-substituting lines, but suppress a
line that references the empty default. The resident expansion pass follows
that observed behavior.
A fifth [duplicate fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-duplicate.json)
overrides the manual's apparent one-definition wording for this version:
repeated declarations are accepted and the first value wins. The compact
resident default table now preserves its first record and ignores later
duplicates. A sixth [undeclared-name fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-undeclared.json)
shows that `.DEF missing fallback` is also accepted and ignored: a later
ordinary line runs and the command returns `RC=0`, `Result2=0`. The current
table now ignores undeclared names without retaining a record.
The [surplus-whitespace fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-extra-value-expansion.json)
and [surplus-equals fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-equals-extra-value.json)
show that `.DEF filename first second` and `.DEF filename=first second` are
accepted, retain `first`, and return success; the resident parser therefore
ignores later value tokens after retaining the first. Other malformed directives, diagnostics, IoErr and MorphOS
behavior remain capture work.
The [bare-DEF fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-missing-name.json)
also shows that a directive containing only `.DEF` is accepted as a no-op and
does not prevent the following ordinary line from running. The resident parser
matches that narrow no-name/no-value case; named malformed forms remain open.
The [empty-name fixture](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/reference-captures/execute-wb31-def-empty-name.json)
shows that `.DEF =fallback` returns `RC=10`, `Result2=0` and stops before the
following ordinary line. The runner already reports the same malformed status
and stops its bounded run; diagnostic text and other malformed forms remain
open.

## Existing implementation and concrete integration gaps

Reuse [ExecuteCommand.cs](D:/Koodit/GIT/CopperOS/src/Commands/ExecuteCommand.cs),
`IShellScriptPlatform`, and `ShellScriptEngine`. The wrapper already delegates
FILE/A parsing to the DOS-owned `TryReadArgs`, calls `TryExecuteScript`, and
frees its RDArgs. It must remain the shared semantic owner of this external
entry rather than be bypassed by another script interpreter.

After the empty-default change, the full Shell DOS native root was rebuilt and
compiled with its Execute exports for 68000, 68020 and 68040. Each current
compatibility report is freestanding, has zero managed allocation sites and no
non-implemented reachable members. This establishes native reachability of the
shared expansion path only; it is not a standalone `C:Execute` artifact or a
runtime/lifecycle qualification.

The inspected implementation needs qualification in these areas:

1. The bounded 2026-08-31 transport uses the DOS-owned `ReadItem` grammar to
   find the raw end of FILE, then runs `ReadArgs(FILE/A)` over only that raw
   prefix. The untouched suffix, including its leading separator, is copied
   into a 104-byte Shell frame and a 100-byte DOS runner record before
   `FreeArgs`. Focused host coverage proves quoted FILE parsing, tail transport
   and runner-tail release. It does not prove original-binary behavior.
2. The copied tail and `.KEY` template are consumed by the bounded runner, but
   pending and nested behavior still need lifecycle coverage with substituted
   lines. A retained pointer alone is not command compatibility.
3. The Pending sentinel is an internal continuation state, not a CLI return
   level. The native executable must wait/resume through the existing process
   continuation owner and return the final command result.
4. Validation/parse failures currently select ShellCommandResult values. Match
   actual return levels, diagnostics and IoErr at the native boundary rather
   than treating those internal mappings as reference evidence.
5. Caller scratch capacities and `EchoCommand.MaximumArgumentLength` do not
   establish reference filename/tail limits. Test reference boundaries and
   retain correct owned storage without arbitrary user-visible restrictions.

## Return, IoErr and cancellation

The original binary contains diagnostics for invalid filenames, open failures,
work-file creation failure, duplicate/illegal KEY directives, missing KEY,
invalid directive/data, overlong keys and invalid directive arguments. Their
presence does not determine exact complete lines, output stream, return level
or IoErr. Capture them separately; do not synthesize a common generic error.

Capture final script status at normal EOF, after failing lines under different
FAILAT values, explicit Quit levels, child-script failure and parse/directive
failure. Distinguish script return level from success in opening or scheduling
a script. Original NDK `dos.library/Execute()` returns a boolean indicating
launch success, **not the command result**; simply returning that boolean would
violate a command-level result contract.

The classic manual transcription describes Ctrl-D as ending the current
script and Ctrl-C as stopping nested execution. Treat that as a fixture lead:
verify exact signal routing, frame unwinding, cleanup, output and resulting
CLI status on both selected versions. Do not add an independent cancellation
loop that consumes signals needed by the Shell or currently running command.

## Public APIs and pure/resident evidence

Use existing Shell execution and DOS facilities (`ReadArgs`/`FreeArgs`, script
item/template operations where appropriate, `Open`/`Read`/`Close`, current CLI
and input/output state). `SystemTagList`/`Execute` calls must preserve the
existing Shell's execution context; avoid recursively spawning a fresh Shell
as a shortcut around a missing script-tail seam.

Primary API authority: `NDK_3.1/DOCS/DOC/DOS.DOC` on
[AmigaDeveloperCD.iso](D:/TestData/AmigaDeveloperCD.iso), SHA256
`2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2`.
The original startup example receives the command-tail length in D0 and
pointer in A0, then opens dos.library through Exec. Incoming A6 is not a
contractual DOSBase. Keep native startup, ReadArgs input and RunCommand tail
handling consistent with the shared command runtime and original NDK.

Classic floppy protection is `0x00000000`, with valid FFS file-header checksum,
but original startup **explicitly forces Execute resident with PURE**:
`Workbench3.1:S/Startup-Sequence`, version `Startup-Sequence_LD 40.3 (31.8.93)`,
SHA256 `64cb5972947dba207e852ad69a1a84f0aeb84e3f8b7a1f45ffde91d61de2546c`.
It installs `C:Execute PURE` at line 13 and removes Execute at line 76.
Therefore P-clear floppy metadata cannot justify omitting resident safety.
MorphOS likewise requires pure behavior: ISO `hdinstall.fixc` line 24 adds P,
and `MorphOS/S/startup-sequence` line 17 makes Execute resident with PURE.
The installer script SHA256 is
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`
(2,478 bytes, ISO block 6348). The startup script SHA256 is
`407bc8480dba6acf11c19966d67689996015aab27a6eca04d0af2d3d244168b4`
(2,158 bytes, block 213484). These are script observations, not observed
installed metadata or a completed resident run. The CC00
[media evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json)
retains the source events.

Keep parser/template slots, script filenames, tail storage, saved CLI state
and temporary resources owned by the invocation or active Shell frame. No
shared mutable resident work area is allowed. FreeArgs and all file/lock
cleanup must be balanced on success, error, signal and pending completion.
Return without closing borrowed Shell input/output or altering another frame.

## 2026-08-31 bounded tail transport

`DosCore.TryReadItemPrefixFromBuffer` is a short-lived DOS adapter: it uses
the existing `ReadItem` implementation and reports only the raw source cursor.
It does not add a second command parser or retain caller bytes. `Execute` then
applies the original `FILE/A` template through `ReadArgs` to that prefix alone.
The suffix is copied into the active script runner and published in
`ShellScriptFrameState.ScriptArguments`/`ScriptArgumentLength`; runner release
frees the same guest allocation. This avoids retaining RDArgs-backed filename
or caller tail storage across an asynchronous return.

Focused tests cover the DOS raw boundary, the runner record's tail ownership and
release, the wrapper's quoted-file case, and `S:Startup extra` transport. The
raw suffix is `" extra"`, because ReadItem leaves the separator unread. No
fixture yet establishes complete pending lifecycle, return levels, IoErr, HUNK
startup or profile differential behavior.

## 2026-08-31 directive lexical boundary

`ShellScriptDirective` now recognizes the documented directive names and the
`.K`, `.DEF`, and `.DOL` aliases without allocating or retaining a managed
string. `ShellScriptEngine` skips the two documented comment-line forms (`. `
and `.\\`) and semicolon-leading comment lines before alias lookup or command
dispatch. Unknown dot lines fail as malformed rather than reaching arbitrary
external command lookup. Focused directive tests and the complete 448-case
command suite pass, and the DOS-native Shell project builds.

This remains only the lexical and comment boundary for `.DEF`, delimiter-changing
and dot directives. `.KEY` now has a stateful frame owner and `ReadArgs`
binding; it does not close an Execute behavior fixture or alter either Execute
ledger row.

The next ownership seam is now present: every DOS script runner allocates a
4 KiB `ScriptKeyTemplate` buffer and publishes it through the 112-byte v3
Shell frame. The 108-byte runner record releases that buffer with the frame
and raw-tail allocation. It is empty until `.KEY` binding is implemented;
therefore this ABI change alone does not parse or substitute arguments.

`.KEY`/`.K` now uses that seam: it is accepted only on the first script line,
copies its nonempty template to the runner buffer, validates the retained raw
tail with DOS `ReadArgs`, releases the returned RDArgs immediately, and skips
the directive line. Each following line reparses the retained tail with the
same template, copies matching case-insensitive `<name>` values into a
caller-owned workspace, and frees that transient RDArgs before alias and
redirection handling. `.DEF name value` appends a decoded script-wide default
record to unused capacity in the same runner-owned template buffer and uses it
when ReadArgs leaves that named value absent. This has no retained RDArgs
pointer and preserves the resident runner boundary. `.BRA` and `.KET` update
the runner-owned opening and closing delimiter bytes, so subsequent expansion
uses the selected pair. The runner also retains the `.DOLLAR`/`.DOL` separator:
when a matching ReadArgs value is null, `<name$inline>` (or the selected
separator) emits its inline default before falling back to a script-wide
`.DEF`. `.DOT` updates the runner-owned directive prefix before the next line is
lexically classified, allowing subsequent dot directives and dot comments to
use the selected character. Directive ordering, temporary-file behavior and
broader original-fixture behavior remain open.

The pending-runner coverage binds `.KEY filename/A`, dispatches a substituted
external line, retains the runner-owned template while that child is pending,
and confirms final continuation teardown reports the child result. This covers
the local ownership invariant; it is not original runtime evidence for child
scheduling, return levels or resource effects.

## Required fixture and completion gates

- [ ] `EX-TEMPLATE`: both exact templates, missing/empty/spaced filename,
  unknown item, quoted keywords, `?` continuation and EOF.
- [ ] `EX-TAIL`: filename plus positional/keyword/multiple/empty/escaped
  script arguments; `.KEY` modifiers and defaults; literal delimiters;
  prove the one-owner boundary preserves bytes and interpretation.
- [ ] `EX-DIRECTIVE`: each directive/verified alias, changed delimiters,
  duplicate/missing/illegal KEY, comments, long and malformed lines.
- [ ] `EX-FILES`: missing/unreadable script, current/path/assign locations,
  direct S-bit invocation, T: work-file create/read/close failure and cleanup.
- [ ] `EX-STATUS`: EOF, failing line, FAILAT variants, Quit levels and nested
  failures; capture complete stdout/error streams and immediate return/IoErr.
- [ ] `EX-BREAK`: Ctrl-D/C in parent/child scripts and running child command;
  frame, stream, signal and temporary-resource restoration.
- [ ] `EX-PENDING`: delayed execution after RDArgs cleanup, nested pending
  frames and final continuation; no stale pointers or exposed Pending result.
- [ ] `EX-RESIDENT`: original forced-PURE startup lifecycle, repeated and
  overlapping runs, resident use counts, removal after completion and code
  immutability on every required CPU target.

All boxes are open reference/implementation obligations. No test execution,
shipping HUNK, complete Shell behavior, or native parity is asserted here.
