# Eval contract

Profiles: `wb31`, `morphos320`. Goal steps: CC01 and CC10.
Recorded: 2026-08-30. Status: **templates and MorphOS release-source behavior
verified; native differential execution remains open**.

## Reference identity

| Profile | Member | Version, size and SHA256 |
| --- | --- | --- |
| `wb31` | `Workbench3.1:C/Eval`, original Disk 2 ADF, file-header block 238 | `eval 37.3 (26.3.91)`, 2,084 bytes; `e320ba5d36b788e83719a09c7481d2c91d511396787b0b534e29c496240af962` |
| `morphos320` | `MorphOS/C/Eval`, official 3.20 ISO block 175384 | `Eval 50.7 (23.4.2013)`, 31,621 bytes; `9e7306511f09d1f0bd38edb86b93d204fd65aad4b8673396e11deb7b8ccec98b` |

Local media: `D:/TestData/TestImages/Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip`
(same-base ADF member, SHA256
`a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`),
and `D:/TestData/MorphOSReferences/morphos-3.20.iso` from the
[official release](https://www.morphos-team.net/morphos-3.20.iso).
MorphOS ISO logical blocks are 2,048 bytes. Its executable is packed; source
and the embedded version agree on 50.7, but no binary/source equivalence or
native execution has been established.
The verified complete ISO SHA256 is
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.

Primary source is the official
[3.20 command source archive](https://www.morphos-team.net/files/src/3.20/c.tar.bz2),
214,038 bytes, SHA256
`db099324cbf4b07de70bcafc626af0b168903a714911f840652e1a364578b5ba`:

| Archive member | SHA256 |
| --- | --- |
| `c/eval/eval.c` | `070583f66648c925a296b40d840aa86b01214497288def0f9d747f5a7bafdcbc` |
| `c/eval/evalParser.y` | `e43ce37d001bae6012cfd22c7117c0777daccf1a861c5aedc56bf311d81b51df` |
| `c/eval/eval.notes` | `fe0f038f00456a0a37fcd07e79b37417daf4f6c617d69b58fb2c07744a211635` |

These files have AROS copyright attribution but no explicit reuse grant in
the inspected Eval headers. This contract uses behavioral observations only.
Do not copy or translate implementation source without a separate license
audit. Other components' licenses do not automatically apply to these files.

## Exact templates and argument ownership

Classic template, observed in the original binary:

```text
VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K
```

MorphOS 3.20 release source's actual `ARG_TEMPLATE`:

```text
VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K,HEX/S
```

The source's introductory SYNOPSIS omits HEX and is stale. Use the executable
macro, not that comment. Initialize five/six slots and invoke DOS ReadArgs.
VALUE1 is a required string; OP is optional; VALUE2 is a string-pointer list.
No arithmetic operand uses `/N`: DOS numeric conversion would reject valid
expression tokens and constrain them to the wrong width. TO and LFORMAT are
keyword strings. Preserve `?`/continued input, DOS quoting, redirection and
star escaping through the existing Shell/DOS owner.

MorphOS source combines VALUE1, OP when present, and each VALUE2 item in order,
inserting spaces, before expression parsing. Its expression parser is command
semantics, not a replacement for ReadArgs. Retain every RDArgs-owned value
until used; FreeArgs once; keep assembly/parser/formatting storage local to an
invocation. Own only the output file opened by TO, not borrowed stdout.

## MorphOS source-observed expression behavior

The parser semantic value is signed `long long`; release notes in `eval.notes`
identify 50.6 as the 64-bit arithmetic change. The grammar is integer-only,
supports parentheses, unary minus and bitwise NOT, and includes exponentiation
with `^`. Do not import classic width or host floating-point semantics.

| Precedence, low to high | Operators | Associativity |
| --- | --- | --- |
| 1 | left/right shift tokens | Left |
| 2 | bitwise equivalence token | Left |
| 3 | bitwise OR | Left |
| 4 | bitwise XOR token | Left |
| 5 | bitwise AND | Left |
| 6 | addition, subtraction | Left |
| 7 | multiplication, division, modulo | Left |
| 8 | unary minus, bitwise NOT | Unary |
| 9 | exponentiation `^` | Right |

The lexer recognizes decimal input, `0x`/`#x` hexadecimal prefixes, octal
forms, and a leading single quote followed by a character. Source paths exist
for `mod`/`m`/`M`/`%`, `xor`/`x`/`X`, `eqv`/`e`/`E`, `lsh`/`l`/`L`,
and `rsh`/`r`/`R`. The released 50.7 lexer compares the longer names with
`strncmp(literal, token, tokenLength)`. This accepts lowercase two-letter
prefixes (`mo`, `xo`, `eq`, `ls`, `rs`) as well as the full lowercase names.
The one-letter aliases use a case-insensitive comparison; `%` is also modulo.
Longer names are case-sensitive in the source, so uppercase multi-letter
spellings are not inferred to work. These are source-derived rules; the exact
3.20 binary behavior still needs a MorphOS guest comparison.

Important unresolved runtime cases: `08`/`09`, invalid or missing digits after
a base prefix, values outside the signed range, negative/oversized shifts,
signed overflow, division by zero, minimum-value divided by -1, malformed
tokens and deep nesting. The source delegates conversions to C scanning and
contains undefined or platform-dependent arithmetic; those outcomes are not
portable behavioral proof. Its power helper narrows operands to `long int`
and accumulates in `int`, despite the wider grammar values. Capture exponent
boundaries separately rather than assuming arbitrary 64-bit exponentiation.

## Output and return behavior observed in MorphOS source

| Case | Source-observed behavior; native confirmation still required |
| --- | --- |
| Valid expression, no format switches | Signed decimal result followed by LF |
| HEX without LFORMAT | Lowercase hexadecimal prefixed with `0x`, followed by LF |
| LFORMAT supplied | Takes precedence over HEX; no implicit final newline |
| LFORMAT conversion | Case-insensitive `%x`, `%o`, `%n`; hexadecimal output is lowercase; `%c` is delegated through a C-library length-modified character format and needs platform capture |
| Other LFORMAT text | Literal text preserved; `%%` and a trailing `%` produce `%`; unknown percent conversion preserves percent and following character |
| Format widths | Source does not consume a width after `%x`/`%o`. `%X4` appends literal `4` after the unpadded hex value. Do not use an older manual's width rule for this source profile. |
| Escapes in LFORMAT | The extra star-decoding branch is disabled; Shell/DOS has already interpreted argument escapes. Avoid double decoding. |
| TO | Open/truncate the target only after expression parsing succeeds; direct formatted output to it; close it afterward |
| ReadArgs failure | Call `PrintFault(IoErr(), "Eval")`; return FAIL/20 |
| Expression-buffer allocation failure or parser return 1 | Release argument resources; return ERROR/10 |
| Output-file open failure | Print a diagnostic naming the target to stdout; return FAIL/20 |
| Normal output path | Return OK/0; source does not test formatting or close errors |

Source output-file failure text is `Cannot open output file %s` plus LF.
Parser diagnostics are sent to stdout by the error callback. Exact generated
parser messages, C-library behavior, output/close-failure IoErr and final
IoErr after cleanup still require reference measurement. The source has no
explicit Ctrl-C polling. Do not invent break timing or promise error
propagation that the source does not check. A parser allocation failure that
returns a value other than 1 also needs capture because the caller tests only 1.

## Classic profile gaps

The original binary has diagnostic strings for an invalid LFORMAT, mismatched
parentheses, output-open failure and allocation failure. Their presence does
not establish branch statuses or exact complete output lines. The
[transcribed classic manual](https://www.jaruzel.com/amiga/amiga-os-command-reference-help/eval.html)
describes `%X`/`%O` digit widths, which differs from MorphOS release source.
Treat the transcription as a discovery lead and obtain original-binary
fixtures before freezing formatting, arithmetic width/overflow, precedence,
character handling, fractional text and errors. Never make HEX an extra
classic switch merely to share a parser.

A disposable 68000 WinUAE run of the original 37.3 binary provides bounded
classic output evidence: [`eval-wb31-basic.json`](../reference-captures/eval-wb31-basic.json)
records `C:Eval 1+2*3` output as `9`, `C:Eval 1 + 2` as `3`, and
`C:Eval 42 LFORMAT="x=%x"` as `x=A` with no final LF. A follow-up
[`eval-wb31-classic-grammar.json`](../reference-captures/eval-wb31-classic-grammar.json)
records `1+2*3+4` as `13`; `%x` renders only the low uppercase hexadecimal
digit (`15` as `F`, `16` as `0`); and both `%X2` and `%x2` render `42` as
`2A`. These are direct observations, not a general precedence or format
specification. In particular, they conflict with the current source-observed
MorphOS precedence and formatting, so the classic evaluator and LFORMAT
semantics must be captured separately rather than shared by inference.

A third [`eval-wb31-classic-numeric.json`](../reference-captures/eval-wb31-classic-numeric.json)
capture records `20-5*2` as `30`, parenthesized `1+(2*3)` as `7`, unary
`-2+3` as `1`, `0x10` and `#x10` as `16`, and `010` as `8`. It also records
`%n` decimal output plus one- and two-low-digit `%o` output (`9` as `1` and
`11`). This still establishes only selected examples; multiplication with
subtraction, parentheses, unary minus, those literal forms, and n/o conversion
widths must not be generalized to the unmeasured grammar or formatting cases.

A fourth [`eval-wb31-classic-operators.json`](../reference-captures/eval-wb31-classic-operators.json)
capture records successful `/`, `%`, `mod`, `|`, `&`, `xor`, `eqv`, `lsh`,
`rsh`, and unary `~` examples. In particular, `1|2&4` yields `0`, consistent
with the already captured left-to-right arithmetic stream, and `6 eqv 3` yields
`-6`. The same probe observes `2^3` completing with `2`; its caret grammar is
unresolved and deliberately not shared with the MorphOS exponentiation parser.
The bounded classic candidate implements only the captured non-caret operators.

The fifth [`eval-wb31-classic-caret.json`](../reference-captures/eval-wb31-classic-caret.json)
probe distinguishes prefix acceptance from an implemented caret operator:
`2^3`, `2 ^ 3`, and `2^3+4` each output `2`; `2 + 3 ^ 4` outputs `5`; and
plain trailing text after valid `2` or `2+3` is also ignored. The Workbench
candidate therefore accepts a successfully evaluated prefix and leaves its
unrecognized suffix unconsumed. It still rejects a missing right operand after
a recognized operator. `2 ** 3` outputs `0` and `2 pow 3` outputs `2`; these
are recorded parser boundaries, not generalized into a caret or exponent rule.
The candidate now matches two fresh original-guest pairs: `C:Eval 2^3` outputs
`2\n`, and `C:Eval 2 + 3 ^ 4` outputs `5\n`. Both comparisons agree on
output bytes, return 0, and caller post-System IoErr 0. These cases cover
only captured caret-prefix behavior, not general exponentiation arithmetic.

A sixth [leading-zero capture](../reference-captures/eval-wb31-leading-zero-20260928.json)
uses five fresh disposable guests. `C:Eval 08 + 1`, `C:Eval 08`, `C:Eval 08+1`,
and `C:Eval 09+1` each output `0\n`; `C:Eval 010` outputs `8\n`. All five
return 0 and leave caller post-System IoErr at 0. This supports a Workbench
prefix parse that stops after the leading zero when 8/9 follows, separately
from MorphOS's source-defined whole-token consumption. These original captures
are now paired with the candidate for all five invocations; those results are
listed below and remain case-level evidence, not a complete classic numeric
grammar.

## Public APIs, purity and required fixtures

Prefer ReadArgs/FreeArgs and DOS file/output operations; utility conversion
or arithmetic calls are appropriate only where their width and semantics
match. MorphOS expression grammar needs a command-level integer evaluator.
Do not use a host-language evaluator, execute expression text, or read host
files. The original NDK ReadArgs contract is in
`D:/TestData/AmigaDeveloperCD.iso`, `NDK_3.1/DOCS/DOC/DOS.DOC`, SHA256
`2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2`.

The inspected classic floppy protection is zero, with validated file-header
checksum. Final installed pure/resident requirements must come from CC00.
Eval is absent from MorphOS `hdinstall.fixc`'s P-addition list, whose SHA256 is
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`.
This leaves its original classification **unresolved, not non-pure**. Consult
the CC00 [manifest](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json);
ISO POSIX modes cannot supply the missing AmigaDOS metadata.
MorphOS source globals do not establish the shipped startup's data model or
resident safety. CopperOS must use invocation-owned expression and parser
state, with repeated/concurrent qualification where P is required.

| Fixture group | Required cases |
| --- | --- |
| `EV-ARGS` | Every operand slot and keyword form, VALUE2 list, missing VALUE1, empty values, quoted expression, quoted keyword data, invalid/repeated options, `?` continuation/EOF, classic HEX rejection |
| `EV-GRAMMAR` | Every operator/token spelling, each precedence boundary and associativity, parentheses, unary chains, whitespace, all numeric bases and character values; malformed counterpart for each |
| `EV-WIDTH` | Signed 32/64-bit boundaries, overflow, zero division, extreme shifts and powers, narrowing in power, invalid base digits, long/deep expressions; isolate potential reference faults in disposable guests |
| `EV-FORMAT` | Decimal/default, HEX, every conversion/case, unknown/trailing percent, percent literal, empty format, `%X4`/`%O4`, LF and star escapes, HEX+LFORMAT precedence |
| `EV-TO` | Fresh/existing target, truncation, formatted/default/HEX output, invalid expression with existing target unchanged, missing/unwritable/full target, short write and close failure |
| `EV-ERROR` | ReadArgs/buffer/parser allocation failures, malformed input, Ctrl-C before/during evaluation and output; stdout/error bytes, immediate return/IoErr, resource balance |
| `EV-RESIDENT` | Repeated and overlapping invocations using one segment; no expression/result/stream/error state leakage |

## Implemented numeric primitive; full command remains open

The independent `EvalNumericFormatter` supports signed 64-bit decimal and
unsigned bit-pattern hexadecimal/octal in caller-owned guest storage. It uses
the SDK's register-pair split intrinsics and 32-bit words: literal decimal
divisor 10, or digit extraction and shifts for bases 16 and 8. Public signatures,
buffer validation, exact output and ownership are unchanged by that arithmetic
specialization. It does not implement expression parsing, full LFORMAT, TO,
ReadArgs, diagnostics or a command entry.

Existing tests pass 61 focused cases, the same 61 under checked arithmetic,
and the full 293 command/Shell cases without skips. Source-bound native run
`e767705fdcaf42fb864c4c68c8e3d9a3` passes 540 invocations over three
reproducible 68000/020/040 HUNKs, with zero runtime helpers, fatal sites,
managed allocations, external native targets or shared-image writes. The
supplied integer/output oracles are independent of this implementation.
[Qualification report](../qualification-report.md) and
[build manifest](../build-manifest.json) bind the sources, inputs and artifacts.
These are primitive tests, with **no original Eval execution or full-command
differential pass**. All command fixture groups above remain required.

## Bounded MorphOS expression checkpoint

`EvalExpressionEvaluator` is an independently written, caller-bounded parser
for the source-observed MorphOS 50.7 integer subset. It accepts parenthesis,
unary minus and complement; right-associative power; arithmetic, bitwise and
shift operators; decimal, `0x`/`#x` hexadecimal, octal, and byte-character
literals. It has no DOS argument parsing, formatting, output, file, global, or
resident state. The evaluator uses the source-observed lowercase full-word
aliases and case-insensitive one-letter forms; native execution must still
verify every lexer quirk.

Undefined or platform-dependent source paths are deliberately represented as
explicit failures: arithmetic overflow, a zero divisor, out-of-range shifts,
and 32-bit power overflow do not wrap or invoke host behavior. The focused
host suite has 23 cases for precedence, associativity, numeric bases, aliases,
malformed input, and each explicit failure outcome. This does not establish
source overflow, power, case, or parser-diagnostic parity.

`EvalLFormatFormatter` adds bounded source-observed LFORMAT presentation for
`%n`, `%x`, and `%o`: conversion names are ASCII case-insensitive, hex is
lowercase, no width is consumed, and percent/trailing/unknown forms remain
literal. It performs a sizing pass before writes and deliberately rejects `%c`
until its C-library behavior is captured. Five focused cases cover these forms,
capacity, and overlap; the formatter has no implicit LF, TO, or command I/O.

The private `Commands.EvalExpressionNativeRoot` reaches the evaluator through
a fixed guest control record. It lowers parser arithmetic through 32-bit lanes
and `M68kRuntime` split/combine intrinsics: bitwise operations, checked
add/subtract, signed divide/remainder, checked multiply, literal accumulation,
and shifts. The generic `IsMapped` call resolves when the native compile
includes the SDK support assembly.

On 2026-09-01, resident `memory=None` HUNK compilation produced static
artifacts for all required CPUs: M68000 `24D6C900C62AA8C2E7C784B61369EE2717720DD663597165C56A6B169336EE36`
(12,400 bytes), M68020 `7BF38E698002F7E70DB52343667B84FC52C180B581964D639A411958A1A075E8`
(12,712 bytes), and M68040 `FD96719373B22215B7F2718B218672302AC68C69C7220C758CE4C1F9ABD52FBB`
(12,352 bytes). Each compatibility report records zero managed allocation
sites, fatal machine-fault sites, runtime helpers/features, and external native
targets. This is static native reachability only: no instruction fixture,
original-command comparison, command entry, DOS I/O, or resident lifecycle
claim follows from it.

## Bounded external entry checkpoint

`EvalCommand` now supplies a caller-workspace external command core for the
exact Workbench 3.1 and MorphOS 3.20 `ReadArgs` templates. It assembles the
source-observed expression argument sequence, invokes the bounded evaluator,
uses the existing decimal/HEX/LFORMAT formatters, writes the borrowed output
stream or a `TO`-opened stream, and releases RDArgs on every result path. Six
host integration cases verify both exact templates (including the 36-byte
classic workspace), decimal output, operand and `/M` reconstruction, HEX and
LFORMAT output, `TO` open/close behavior, and the ReadArgs/FreeArgs pair. This
does not claim full outer argument combinations, diagnostic bytes, TO/error
behavior, a native entrypoint, original-command parity, or resident qualification.

## Native entry checkpoint

`NativeEvalCommand` adds the matching DOS-facing body to the independent native
command project. It uses the versioned `ReadArgs` templates through the existing
invocation-owned native argument lease, allocates expression and output buffers
per call, reconstructs the `OP` and `VALUE2/M` sequence, writes either `Output`
or a `TO`-opened file, then closes/frees/releases every owned resource through a
single explicit cleanup path. `NativeEvalEntry` is a private MorphOS-profile
compiler root; it opens and closes the real DOS library through the common
startup owner and does not assume an incoming DOS base.

The native project and root build as managed inputs. The LFORMAT sizing helper
now uses high/low-word arithmetic and preserves its existing focused tests.
Resident HUNK output succeeds for 68000, 68020, and 68040: each report reaches
81 methods with zero managed allocations, machine-fault sites, helpers, and
external targets. The reports still record the `nullable-values` runtime feature,
caused by the public SDK `DOS.Open` return type used for `TO`; that remains a
static purity-audit gap.

Each resident HUNK executes through Copper68k 1.4.0 in a 20-case supplied-vector
fixture. The fixture exercises default decimal, `OP`/`VALUE2/M` reconstruction,
five lowercase two-letter operator prefixes, the MorphOS `08`/`09` whole-token
behavior, HEX, LFORMAT precedence, `TO` open/write/close, malformed expression,
ReadArgs failure, allocation failure, repeated runs, and two same-image
interleavings.
Every CPU records one image load, zero shared-image writes, balanced DOS/Exec and
RDArgs resources, and the expected output bytes. It deliberately supplies
post-ReadArgs vectors through test-only DOS gateways; it is not a real DOS parser,
Kickstart, CopperStart, Workbench, MorphOS, filesystem-handler, or original-Eval
comparison. These remain private compiler artifacts, not packaged command files,
and do not close pure/resident qualification.

Run [`qualify_eval_native_entry.ps1`](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_eval_native_entry.ps1)
to rebuild the three resident HUNKs, run all 60 supplied-vector executions, and
write a hash-bound qualification receipt. The script intentionally permits only
the currently audited `nullable-values` runtime feature; a new feature fails the
gate rather than being silently accepted.

## Workbench 3.1 native subset checkpoint

`NativeWorkbench31EvalEntry` calls a separate literal-template
`RunWorkbench31` body rather than selecting the classic template through a
resident enum comparison. This matters because the first shared-body test
observed that compiler path using the MorphOS template despite the Workbench
enum value. The isolated classic entry uses the 5-slot Workbench template and
the captured classic evaluator/LFORMAT candidate. The refreshed resident HUNK
output is:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 12,368 | `c1ac17a1981b16c16156f07a18349b68f5dac80122af702c3b4d2af399dbbaf9` |
| 68020 | 12,540 | `5f59d1bdf537ed590d8258e930cbececf8aea3e0690bc60fab024e2f12bcdede` |
| 68040 | 12,152 | `9b079b229bd45387b4be84fefbd460890188c04f985adab28e0fdcc9539b5e93` |

[`qualify_eval_wb31_native_entry.ps1`](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_eval_wb31_native_entry.ps1)
rebuilds this matrix and executes 37 supplied post-ReadArgs vectors per CPU.
The candidate fixtures now cover the exact original-captured arithmetic,
parentheses, unary minus, `0x`/`#x`/octal literals, `/`, `%`, `mod`, bitwise
operators, shifts, complement, `%n`, and the measured low-digit X/x/O formats,
along with TO, caret-prefix behavior, the captured `2*`/`2**3` zero results,
`08`/`09`, parser/allocation failures, repeats, and two interleaved callers. The reports
have zero managed allocation sites, fatal
machine-fault sites, helpers and external targets; each retains the audited
`nullable-values` feature through the TO result. All 111 executions used one
loaded image per CPU and reported zero shared-image writes. The DOS gateway
supplies decoded slots, so this does not prove actual ReadArgs, the original
caret outcome, real filesystem I/O, full original command parity, or final P/purity
approval.

The refreshed Workbench receipt is
`artifacts/cc10-eval-wb31-native-20260928-incomplete-multiply-v1/qualification.json`.
These added vectors turn prior original-only captures into candidate execution
coverage; they are not paired original/candidate guest comparisons.

This Workbench build uses the compiler's `--symbols off` option. The symbols-on
version of the same Eval root was rejected by original DOS as `bad loadfile
hunk` (return 10, caller IoErr 235). The symbols-off build is a plain, loadable
CODE HUNK. Its original/candidate guest pairs now match exactly for
`C:Eval 08 + 1` and `C:Eval 010`, including output bytes, return, and caller
post-System IoErr: `0\n`/0/0 and `8\n`/0/0 respectively. Receipts are
`artifacts/workbench31-guest-eval-08-candidate-symbols-off-20260928-v1/effect-comparison.json`
and
`artifacts/workbench31-guest-eval-010-candidate-symbols-off-20260928-v1/effect-comparison.json`.
These prove only those two cases. The HUNK remains a private resident-root test
artifact; it has not passed the release manifest, disk-image packaging, full
option/behavior, or P/resident lifecycle gates.

- [ ] Capture native 50.7 behavior and reconcile every source-dependent case.
- [ ] Freeze the classic width, format, grammar and errors independently.
- [ ] Complete all fixtures and installed P/resident evidence before closing
  CC01/CC10. These specifications are not executed tests.

## 2026-09-22 current-source entry rerun

The current receipts are `artifacts/cc10-eval-morphos-native-20260922-v1/qualification.json`
and `artifacts/cc10-eval-wb31-native-20260922-v1/qualification.json`. MorphOS
Eval passes 14 supplied-vector calls per CPU with current HUNK hashes
`e545a3eeedb1a818fbd4b335717c7a5fe256b8ebb2d6eaecf94db9b4c7fd72c8`,
`29bab44a11e6e119d45aac20fc264afceadb6a24b6ed8b08000a34cd987f29d9`, and
`0026d34ff2bc6d01af758b2f3b3b284801c9d21991559c21172517410447c4ac` for
68000/020/040. Workbench Eval passes 12 per CPU with current HUNK hashes
`ad1324f069eb79c707a8b94f8f46799a67c4435270a40b482bcf5ad82cbc659f`,
`38367f56d2722db9bf21aeab3c1b373e91d67c113750eab4514a3712010f4333`, and
`02efd1bda7341b5b68be468e03145442653ad32d7f6ccf05da4257a2287c1e95`.
These supplied-vector reruns do not close real-DOS, original-reference,
packaging, or PURE/resident admission gates.

## 2026-09-27 bounded MorphOS operator prefixes

The selected MorphOS source archive's `Eval 50.7` lexer accepts lowercase
two-letter prefixes for its word operators. The inspected source identities are
`evalParser.y` SHA-256
`e43ce37d001bae6012cfd22c7117c0777daccf1a861c5aedc56bf311d81b51df` and
`eval.c` SHA-256
`070583f66648c925a296b40d840aa86b01214497288def0f9d747f5a7bafdcbc`.
The evaluator now accepts `mo`, `xo`, `eq`, `ls`, and `rs` in addition to the
full lowercase names and one-letter case-insensitive aliases, while rejecting
uppercase two-letter prefixes. Portable evaluator tests pass 46/46.

The resident MorphOS entry receipt
`artifacts/cc10-eval-morphos-native-20260927-operator-prefix-v1/qualification.json`
passes 19 supplied post-ReadArgs vectors per CPU (57 total) across 68000/020/040.
Each HUNK reports 92 reachable methods, only the previously audited
`nullable-values` feature, zero fixture leaks, and zero shared-image writes.
This closes only the candidate's bounded lexer slice: the DOS parser and
original 3.20 binary were not executed, and PURE/resident lifecycle, licensing,
packaging, and guest differential gates remain open.

## 2026-09-28 MorphOS leading-zero lexer edge

The source-hash-verified `evalParser.y` has a distinct path for `08`/`09`:
after declining its hex/octal cases, the lexer calls `sscanf(..., "%lli")`,
then consumes the rest of the decimal-looking token with `isdigit`. The
base-detecting conversion reads the initial zero as an octal zero, then the
whole token allows expression evaluation to continue. MorphOS source-shaped
`08 + 1` therefore evaluates to 1. Workbench's original binary instead returns
0 for `08 + 1`, `08`, `08+1`, and `09+1`; its `010` control returns 8. The
Workbench evaluator now stops after the leading zero for `08`/`09`, while the
MorphOS evaluator consumes the digit run. Focused portable tests pass 50/50.

The refreshed resident-entry receipt is
`artifacts/cc10-eval-morphos-native-20260928-leading-zero-v1/qualification.json`.
It passes 20 supplied post-ReadArgs vectors per CPU (60 total) for 68000/020/040;
each HUNK has 92 reachable methods and retains only `nullable-values`, while
the fixture reports no leaks or shared-image writes. The 68000/020/040 HUNK
SHA-256 values are
`81093d0b22ad42b2f6302e8bfb1836aa0e236bcb545770af1e99e5d4a0bee556`,
`bcc7a87514bac87e95c8543a36f01d53c5b0f71c7b2562dbca1f2234ff2991f3`, and
`d52a6cff0aa94aa4f807e0e954fb3d5953d27a5ef63e20bc9bf3a0423ec34119`.
This is source-derived candidate evidence only; the original MorphOS binary,
real DOS `ReadArgs`, full grammar, guest parity, and PURE/resident admission
remain open.

## 2026-09-28 Workbench symbol-free candidate guest pairs

The Workbench resident-entry qualifier now compiles with `--symbols off` and
records `hunkSymbols: off` in its receipt. The resulting candidate HUNK is
byte-identical to the symbol-free experimental HUNK that original Workbench
successfully loaded; the symbols-on form of this Eval root returned
`bad loadfile hunk` before entering the command. This is a result for this
artifact, not a general rule about every HUNK symbol table.

The original/candidate guest comparator reports `captured-case-equal` with
`allObservedFieldsEqual: true` for eighteen cases:

| Invocation | Exact output |
| --- | --- |
| `C:Eval 08 + 1` | `0\n` |
| `C:Eval 010` | `8\n` |
| `C:Eval 20-5*2` | `30\n` |
| `C:Eval 1+(2*3)` | `7\n` |
| `C:Eval 1\|2&4` | `0\n` |
| `C:Eval 6 xor 3` | `5\n` |
| `C:Eval ~1` | `-2\n` |
| `C:Eval 20/5` | `4\n` |
| `C:Eval 1 lsh 4` | `16\n` |
| `C:Eval 16 rsh 2` | `4\n` |
| `C:Eval 9 LFORMAT="p=%o2"` | `p=11` (no trailing LF) |
| `C:Eval 6 eqv 3` | `-6\n` |
| `C:Eval 20 mod 6` | `2\n` |
| `C:Eval 2^3` | `2\n` |
| `C:Eval 2 + 3 ^ 4` | `5\n` |
| `C:Eval 2+` | `2\n` |
| `C:Eval 2*` | `0\n` |
| `C:Eval 2 ** 3` | `0\n` |

Every pair also matches return 0 and caller post-System `IoErr` 0. The first
fifteen pairs use the earlier symbols-off HUNK; the final three cases use the
refreshed HUNK listed above. The three added leading-zero pairs also use the
earlier loadable symbols-off HUNK, SHA-256
`5d51031ab980e334e5cd2064974d7ed9eeeabbf524a2558bdb2f7a09ce51f74e`. Their
hash-bound comparison receipts are:

- `artifacts/workbench31-guest-eval-08-candidate-symbols-off-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-010-candidate-symbols-off-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-20minus5times2-candidate-symbols-off-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-paren-candidate-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-bitwise-stream-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-xor-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-complement-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-divide-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-left-shift-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-right-shift-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-lformat-octal-width-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-eqv-candidate-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-mod-candidate-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-caret-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-caret-after-add-candidate-20260928-v1/effect-comparison.json`
- `artifacts/workbench31-guest-eval-trailing-star-candidate-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-double-star-candidate-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-trailing-plus-candidate-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-08-alone-candidate-symbols-off-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-08plus1-no-spaces-candidate-symbols-off-20260928-v2/effect-comparison.json`
- `artifacts/workbench31-guest-eval-09plus1-no-spaces-candidate-symbols-off-20260928-v2/effect-comparison.json`

Three further fresh pairs now match `C:Eval 08`, `C:Eval 08+1`, and
`C:Eval 09+1`. Each emits `0\n`; return and caller post-System `IoErr` are
both zero. Their receipts are included in the case list above.

There are 21 distinct invocation cases and 23 passing comparison receipts:
`C:Eval 08 + 1` has a duplicate passing capture, while `C:Eval 2+` matches
both HUNK identities. The table counts each invocation case once. These
comparisons establish only the listed captured cases. Other captured examples,
diagnostics, the complete ReadArgs/option contract, resident/PURE admission,
licensing and production packaging remain unfinished.
