# Quote contract

Profile: `morphos320`. Goal steps: CC01 and CC10. Recorded: 2026-08-30.

Status: **partial contract; no reference execution or parity claim**. The
3.20 binary identity and its release-specific READITEM fix are primary
evidence. The full option list is currently a documentation lead. Output
escaping, invalid-input rules, and status/IoErr still need native captures.

## Reference identity

| Item | Evidence |
| --- | --- |
| Distribution | [Official MorphOS 3.20 ISO](https://www.morphos-team.net/morphos-3.20.iso), local `D:/TestData/MorphOSReferences/morphos-3.20.iso` |
| Member | `MorphOS/C/Quote`, ISO block 178060, 10,186 bytes; block size 2,048 |
| Member SHA256 | `610f226d951dd91194388ea18a967e5cbb32b991562bf28d6ac8b9eb89ddf449` |
| Embedded version | `Quote 1.4 (28.08.2025)`, Harry Sintonen / MorphOS Team |
| Inspection limit | `7f4d4f53` packed native executable; readable version does not establish an undecoded template. |
| Workbench 3.1 | Not in the inspected original Workbench/Install `C:` inventories; not part of the exact `wb31` runtime profile. CC00 owns final media closure. |
| Pure/resident | **Required pure by original installer design**: ISO `hdinstall.fixc`, line 54, adds P to Quote. Installed flags and resident execution have not yet been observed. |

The verified complete ISO SHA256 is
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.
`hdinstall.fixc` has SHA256
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`
(2,478 bytes, block 6348). Adding P does not load a resident, and POSIX mode
bits in the ISO are not AmigaDOS protection. Bind packaging and qualification
to the CC00 [inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json).

The [official 3.20 release notes](https://morphos-team.net/releasenotes/3.20)
state that READITEM now quotes a string containing `=`. This is a required
3.20 regression case, even if an older implementation appears correct for
spaces and quotes. The official partial
[command source archive](https://www.morphos-team.net/files/src/3.20/c.tar.bz2)
contains no Quote source; do not assume a source license.

## Arguments and rule grammar

The [MorphOS Library entry](https://library.morph.zone/Shell_Commands/Quote)
supplies the following candidate template. This community page is not proof
of an exact 3.20 `?` response:

```text
RULE/A,FILE/K,VAR/K,STR,NOLINE/S,NOQUOTES/S,FIRSTLINE/S,REVERSE=UNQUOTE/S
```

Use public DOS `ReadArgs`, with eight initialized slots. `RULE` is a required
string; `STR` is an optional positional string, not `/F`. `FILE` and `VAR`
require their keywords. `REVERSE` and `UNQUOTE` address one switch slot.
Preserve DOS quoting and `?` continuation rather than adding a second
command-line parser.

The rule value has its own grammar: the documentation describes a sequence
of space-separated rule names, applied in order. This rule interpreter is
separate from argument parsing. Probe case sensitivity, tabs, repeated spaces,
empty rules, duplicate rules, unknown names and partial names. Never apply an
encoding to the original raw command tail before DOS parsing has produced the
actual input value.

| Option | Documented meaning | Unresolved interactions and failure fixtures |
| --- | --- | --- |
| `RULE` | Ordered encoding/quoting stages | Unknown/empty stage, case and whitespace, stage failure, multiple stages with REVERSE |
| `FILE` | Read a file as input | Missing/unreadable file, empty and binary file, source precedence, read/close errors |
| `VAR` | Read a variable as input | Missing vs empty, local vs global/environment lookup, source precedence, binary/long value |
| `STR` | Use a supplied input string | Absent vs explicitly empty, data equal to keywords, interaction with FILE/VAR |
| `NOLINE` | Suppress the final line advance | Empty input, existing newline, FIRSTLINE, binary decoded output |
| `NOQUOTES` | Omit quotation marks added after the final stage | Rule-specific wrappers, reverse mode, intermediate-stage quoting |
| `FIRSTLINE` | Restrict printed output to the first line | Whether truncation occurs before or after encoding; LF, CRLF, CR and empty first line |
| `REVERSE=UNQUOTE` | Attempt inverse processing | Both spellings, supported rules, malformed encoded data, order of inverse stages |

Source precedence when FILE/VAR/STR coexist, stdin/default behavior when all
are absent, and unsupported reverse combinations are **not established**.
Do not silently select a preferred order or reject combinations without proof.

## Required encoding coverage

| Rule | Required success and failure corpus; exact reference bytes remain to capture |
| --- | --- |
| `READITEM` | Empty, plain ASCII, spaces, tabs, quotes, stars, `=`, semicolons, newline, escape byte, high bytes; round-trip through DOS ReadItem/ReadArgs as one data item; malformed reverse input |
| `MATCHPATTERN` | Literal pattern operators and quoting characters; compiled pattern must match the original string without broadening it; malformed reverse input and unsupported reverse behavior |
| `URI` | Reserved delimiters, `%`, plus, ASCII safe characters, bytes above 127, NUL via FILE; malformed percent sequences and reverse case rules |
| `HEX` | Empty and odd/even input lengths, byte values 0..255, output case, malformed/odd reverse text, whitespace acceptance |
| `BASE64` | Input lengths modulo three, padding and line wrapping, high/NUL bytes, malformed alphabet/padding and whitespace in reverse |
| `AREXX` | Both quote characters, controls, empty and multiline strings; supported reverse forms and malformed delimiters |
| `SH` | Single/double quotes, dollars, backticks, backslashes, separators, controls; exact shell quoting style and reverse support |
| `JS` | Quotes, backslashes, control/high bytes and multiline input; exact escape style, Unicode assumptions and malformed reverse text |
| `C` | Quotes, backslashes, controls and high bytes, ambiguous escape-following characters; empty input and malformed reverse text |

The 3.20 equality case should feed input bytes `A=B` to READITEM and confirm
the result is quoted as a single literal DOS argument. Do not run the generated
text as a command; use a controlled argument receiver. Native capture must
establish the precise bytes, including the final LF and interaction with
NOQUOTES. Test at least two differently ordered rule pipelines with distinct
results so that stage order is observable.

No blanket assumption about UTF-8, Unicode normalization, NUL termination,
locale conversion, standard-library escape spelling, or printable character
classification is acceptable. FILE and VAR may expose different limits from
STR; reference observations must decide this.

## APIs, ownership, status and cancellation

Prefer `ReadArgs`/`FreeArgs`, `Open`/`Read`/`Close`, `GetVar`, `Output` and
`Write`, with Exec allocation for invocation-owned intermediate buffers.
`GetVar` flags and local/global fallback must follow captured Quote behavior.
There is no Kickstart 3.1 general-purpose Quote API; rule transformation is
command functionality, while Shell tokenization and pattern matching remain
owned by DOS. The original NDK DOS autodoc is at
`D:/TestData/AmigaDeveloperCD.iso`, member `NDK_3.1/DOCS/DOC/DOS.DOC`,
SHA256 `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2`.

Keep rule/input pointers valid until their last use, free each intermediate
allocation exactly once, close only opened files and never the borrowed output
handle. Do not store parser state, library bases, rule cursors, buffers, or
error values in mutable shared resident data. Check all growth arithmetic.

No exact return level, diagnostic prefix, error stream, IoErr preservation,
Ctrl-C timing, or short-write policy has been verified for Quote 1.4. Capture
them; do not substitute a generic success/fail convention or claim that the
documented flags define their errors.

## Implemented READITEM stage

`QuoteReadItemFormatter` now provides the bounded, caller-owned output stage
for one `READITEM` value. It preserves a plain token when safe and otherwise
uses DOS-style double quotes, escaping `*`, `"`, LF, and ESC inside quoted
values. Its required MorphOS 3.20 regression case is `A=B` becoming `"A=B"`;
the formatter also rejects overlap and insufficient capacity before writing.
Eight focused host cases cover those byte sequences and the rejection paths.

This is neither a Quote command entry nor evidence of the exact complete Quote
rule behavior. Source selection, the rule pipeline, reverse operation,
NOQUOTES/NOLINE/FIRSTLINE, other rule families, output I/O, results, IoErr,
purity, and MorphOS runtime comparison remain open.

`QuoteHexFormatter` adds the bounded forward `HEX` stage. It writes lowercase
hexadecimal with no implicit terminator or line feed, and its focused tests
include the MorphOS Library example `Work:Pic #1.jpg` producing
`576f726b3a5069632023312e6a7067`. The Library page is a secondary source, so
this confirms an implementation candidate rather than frozen 3.20 runtime
behavior; reverse HEX, pipeline wrapping, and error outcomes remain open.

`QuoteUriFormatter` adds the bounded forward `URI` component stage. It copies
RFC 3986 unreserved ASCII bytes and percent-encodes every other raw byte using
uppercase digits. Its focused tests include the published `Work:Pic #1.jpg`
example producing `Work%3APic%20%231.jpg` and `%00%FF` for raw control/high
bytes. This is likewise secondary-source candidate behavior; URI reverse,
source text encoding, wrapping, and command status remain open.

`QuoteBase64Formatter` adds the bounded forward `BASE64` stage. It uses the
standard padded alphabet with no implicit line feed; focused tests cover the
one- and two-byte padding boundaries plus the published `Work:Pic #1.jpg`
example producing `V29yazpQaWMgIzEuanBn`. This remains a forward-only,
secondary-source candidate until the MorphOS binary establishes reverse
acceptance, whitespace/wrapping, output, and failure behavior.

The formatter now also has a strict canonical `BASE64` reverse stage. It
accepts the documented padded forms and rejects malformed alphabet, padding,
and non-canonical unused bits before writing output. Those validation choices
are CopperOS safety behavior, not a claim about every string accepted by the
MorphOS 3.20 parser; runtime capture must decide whitespace and permissive
reverse cases.

`QuoteForwardPipeline` composes the currently implemented forward stages from
an invocation-owned byte rule list. It applies `READITEM`, `HEX`, `URI`, and
`BASE64` in declared order, alternating between two non-overlapping
caller-owned guest buffers. It rejects invalid spans, unknown rules, overlap,
or a stage capacity failure without retaining state. This is the bounded core
for forward composition only: DOS rule-token parsing, source selection,
reverse ordering, remaining rule families, output options, I/O, results, and
MorphOS byte-for-byte behavior remain open.

`QuoteHexFormatter` and `QuoteUriFormatter` now also provide strict bounded
reverse candidates. Hex accepts upper- and lowercase ASCII pairs and rejects
odd or malformed input. URI decodes only valid percent pairs, retains literal
`+`, and rejects truncated or malformed escapes. `QuoteReversePipeline` applies
the currently reversible `HEX`, `URI`, and `BASE64` stages in reverse declared
order; it rejects READITEM and the other unimplemented families before writing
scratch buffers. These acceptance rules are safety candidates, not evidence of
the MorphOS 3.20 reverse parser.

`QuoteRuleParser` supplies the candidate conversion from a bounded `RULE`
string to that external byte list. It recognizes every documented name,
preserves order, accepts ASCII case variants and space/tab separators, and
checks the entire value before writing. Case/whitespace acceptance is a
CopperOS candidate pending capture, while recognized but unimplemented rule
families remain deliberately rejected by the forward pipeline.

## Bounded external command boundary

`QuoteCommand` now writes the documented eight-slot template through the
DOS-owned `ReadArgs` lease and releases that lease on every parsed path. Its
template is emitted with scalar guest writes rather than managed string data,
so the command boundary introduces no string-runtime dependency when it is
later linked into a resident root. Its
current source boundary accepts one `STR` input or one `VAR` input. The latter
uses the existing CLI-local variable owner first, then the global environment
owner; this is a provisional CopperOS selection pending the required MorphOS
precedence captures. It copies source bytes into caller-owned workspace before
applying the candidate forward or reverse pipeline, writes through the
borrowed command output, and applies the documented `NOLINE` final-LF toggle.

`FILE`, `NOQUOTES`, and `FIRSTLINE` are deliberately rejected at this boundary:
there is no general DOS-owned binary file reader yet, and the documented prose
does not settle source precedence or the two presentation transformations. The
command does not quietly choose an order or stripping policy. Three focused
host cases cover `STR` READITEM, `STR` with NOLINE, and local-over-global VAR;
they do not establish original result levels, output bytes, or the unsupported
modes. The separate native entry currently admits only forward `STR` behavior
and `NOLINE`; VAR, reverse operation, and the other host boundary modes remain
outside its compiled profile.

## Native reachability checkpoint

`tests/Commands.QuoteNativeRoot` links the exact production parser and forward
stage sources into a private control-block probe. It has no DOS parser, source
selection, file/variable I/O, output, status mapping, or command entry. The
current qualifier rebuilds the following resident-profile HUNK artifacts, each
with `memory=None`, zero managed allocation sites, zero initialized RAM/BSS,
and a `0x3f3` HUNK header:

| CPU | Artifact | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| 68000 | `quote-forward-native-68000.hunk` | 7,216 | `7c48e1f90ad5a337710f93720443ed49fd8c7475e68f954bd08f460c181c4fcb` |
| 68020 | `quote-forward-native-68020.hunk` | 7,416 | `2f8fc26268b608a0fdf53ae0a267f3356e3d2a717fdeabc7cf9c5af4f7e7bd38` |
| 68040 | `quote-forward-native-68040.hunk` | 7,188 | `4d7aedff8136b5a4560a67dd46cb9bc31a24f336d100d8fee86137b615d2128a` |

`QuoteRuleParser` encodes its fixed documented vocabulary as scalar byte
comparisons, so the current compiler map reports 30 reachable methods with
zero managed-string/runtime features, exception regions, fatal machine-fault
sites, helpers, and external native targets on all three CPUs. The reproducible
[`qualify_quote_forward_native_root.ps1`](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_quote_forward_native_root.ps1)
receipt also executes eight direct-control-block invocations per CPU (24 total)
through the generated HUNK. It checks ordered `READITEM`/`HEX`, `URI`/`HEX`,
and `BASE64` outputs; unknown-rule rejection without scratch publication; and
repeat/instruction-interleaved callers on one protected image. It does **not**
close Quote's pure/resident gate or replace required command-entry lifecycle,
same-SegList, DOS input/output, or original-command checks.

## Bounded native STR entry checkpoint

`NativeMorphOSQuoteCommand` and `NativeMorphOSQuoteEntry` now bind the same
eight-slot template to actual native `ReadArgs`, allocate and release five
invocation-local buffers, and run the bounded forward STR path through DOS
output. FILE, VAR, REVERSE, NOQUOTES and FIRSTLINE remain explicit native
rejections; `NOLINE` is retained. The private root compiles resident HUNKs
without runtime features, exception regions, managed allocations, fatal fault
sites, helpers, or external targets:

| CPU | Bytes | SHA-256 | Reachable methods |
| --- | ---: | --- | ---: |
| 68000 | 10,388 | `b13e67524141baa9f37938bc978529ebd4a7e786c4ceb3319e3fdf2c4fc70232` | 41 |
| 68020 | 10,648 | `1a769151a781df93ffeda0a95734496945f0ba77825f62726e72b96b4991cc49` | 41 |
| 68040 | 10,356 | `b0038f89462fc85f70ea2ba73910ea21b00deced85388f21c3a81e02ca9d0ba4` | 41 |

[`qualify_quote_native_entry.ps1`](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_quote_native_entry.ps1)
also executes 12 supplied-vector calls per CPU (36 total) through those HUNKs.
It verifies `READITEM`, `HEX` with NOLINE, ordered `URI HEX`, invalid-rule and
unsupported-REVERSE errors, a short-write error with immediate IoErr capture,
ReadArgs/allocation failure cleanup, and repeated or instruction-interleaved
calls on one protected image. Its adapters are not a real DOS parser or MorphOS
shell, and it does not establish real DOS I/O, same-SegList lifecycle, installed
purity, or original behavior.

## Fixture and completion gates

- [ ] `QT-PARSE`: every slot, both switch aliases, quoted keywords, repeated
  options, unknown/missing arguments, `?` continuation and EOF.
- [ ] `QT-SOURCE`: all eight FILE/VAR/STR presence combinations, plus missing,
  empty, unreadable, long and binary sources; capture default/precedence.
- [ ] `QT-RULE`: every row in the encoding table, forward/reverse success and
  failure, ordered pipelines, all NOLINE/NOQUOTES/FIRSTLINE combinations.
- [ ] `QT-320-EQUALS`: release-specific READITEM equality regression.
- [ ] `QT-ERROR`: input/read/output/short-write/allocation failure and pending
  Ctrl-C; record immediate return and IoErr before observation changes them.
- [ ] `QT-LIFETIME`: repeated failure/success and overlapping resident runs;
  stable shared code and balanced owned resources.
- [ ] Freeze the exact binary/help template, installed P policy, diagnostic
  bytes, output bytes, statuses and side effects, then run differential tests.

These are required fixtures, not completed tests. No reference binary or
implementation source is copied into the repository by this contract.

The current-source 2026-09-22 entry rerun is recorded at
`artifacts/cc10-quote-native-20260922-v1/qualification.json`. It retains 12
supplied invocations per CPU; current HUNK hashes are 68000
`1a55d28fcff35c30b00b3644d1b78e9248a5ace254297e8f7d62b9891f1b8c0e`, 68020
`d3cf8744643fd317b22b427ea9aa1b9b7058b3c76c15fc473304ed952f51248a`, and
68040 `8dd29ee634dfbbabff186401e69d59610cb48c8e3ca8b329f83af01909e86462`.
This rerun does not close the open reference, packaging, or resident-lifecycle
gates.
