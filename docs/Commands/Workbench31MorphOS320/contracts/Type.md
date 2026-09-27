# Type contract

Profiles: `wb31` and `morphos320`. Goal steps: CC01 and CC11. Recorded:
2026-09-27.

Status: **classic identity/syntax-candidate evidence plus partial MorphOS
release-source behavior and bounded native entry execution; no binary-parity claim**. `Type` is an
external C: command in both required profiles. The two original binaries are
different releases and must keep separate capture records even if a later
CopperOS implementation shares an owner.

The current-source CC11 resident entries were requalified on 2026-09-27.
MorphOS Type passes 16 supplied parser/wildcard/provider-version and stream
failure vectors per CPU in
`artifacts/cc11-type-morphos-native-20260927-io-failures-v1/qualification.json`;
the Workbench five-slot syntax candidate passes 17 per CPU in
`artifacts/cc11-type-wb31-native-20260927-io-failures-v1/qualification.json`.
Both cover DOS Read failure and Write failure; the Write case first accepts a
positive short write, then returns an error and checks the selected IoErr and
file cleanup. The Workbench outcomes are candidate-modeled because its original
runtime behavior is not captured. These remain static resident and supplied-
vector receipts only. Exact output and diagnostics, packed correspondence,
original guest parity, PURE/resident admission, packaging, and differential
gates remain open.

## Reference identity

| Profile | Evidence |
| --- | --- |
| Workbench 3.1 | `C/Type` on original Workbench v3.1 rev 40.42 M10 Workbench disk: 1,496-byte Amiga HUNK, SHA-256 `b3d27f65aea039d11b995ebb9e09458f5d68b271018762bf16c4984cfb9221de`; embedded tag `type 37.2 (21.1.91)` at byte 891. File header block 435 has protection `0x00000000`, with P clear. That file metadata does not prove a non-pure runtime design. |
| MorphOS 3.20 | `MorphOS/C/Type` on the official ISO: logical block 178680, 3,985-byte `morphos-packed-native`, SHA-256 `817c75b64fb09a2928e67df5f52a94a096caf9daca3a28b5e76d01c3c463943d`; unexecuted tag `Type 50.6 (15.02.2022)` at byte 3949. ISO `hdinstall.fixc` line 81 adds P to Type, so the original installer design **requires pure** replacement behavior. It neither proves installed AmigaDOS metadata nor resident registration. |

The Workbench media is the admitted Workbench image with ADF SHA-256
`a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`.
The complete MorphOS ISO SHA-256 is
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.
The primary media locations, hashes, and protection-evidence limits are in the
[command inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json),
[media evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json),
and [authorities](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/authorities.md).
No original executable or command output is committed by this contract.

The official partial [3.20 command source archive](https://www.morphos-team.net/files/src/3.20/c.tar.bz2)
is 214,038 bytes with SHA-256
`db099324cbf4b07de70bcafc626af0b168903a714911f840652e1a364578b5ba`.
It contains `c/type/type.c` (9,895 bytes, SHA-256
`ca3b47ab97824e7e50e999e18bd61c4fa7373b0de4f47f27a24525041ee3f1dd`)
and `c/type/type.notes` (416 bytes, SHA-256
`118b77d47e28dce22b68b7b5ac082c87f7cebf93aad4957bfcf0b87ebec46a2c`).
The source `$Id` is dated 2022-02-15 and the notes identify 50.6, matching the
shipped member's visible version/date. This is strong release-source evidence,
but not a source-to-packed-binary identity proof. Its header contains copyright
notices for AROS and Harry Sintonen and no reusable license grant was established;
it is inspection evidence only, not implementation material.

## Grammar boundary

The classic binary contains one template-looking byte string:

```text
FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S
```

It is an inventory **syntax-only candidate**, not source, help, or reference
execution evidence. It does not establish accepted aliases, case handling,
keyword precedence, default values, errors, or even that the shipping code
passes this string to `ReadArgs`. The packed MorphOS 3.20 member has no
plaintext template candidate. Do not transfer the classic candidate to 3.20 or
derive either profile's syntax from later manuals.

The inspected 3.20 source supplies a separate, source-observed template:

```text
FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S,NOLINE/S
```

Its `main` passes that string to `ReadArgs`, frees RDArgs on every visible
post-parse path, maps `OPT` characters `H`/`h` to HEX and `N`/`n` to NUMBER,
prints an ignored-option line for other characters, and returns `RETURN_WARN`
when HEX and NUMBER are both set. It opens `TO` with `MODE_NEWFILE` or borrows
`Output()`, walks every FROM item with `MatchFirst`/`MatchNext` and an
`AnchorPath`, opens matched files with `MODE_OLDFILE`, and writes via buffered
`Read`/`Write`. `NUMBER` enables line numbers; `NOLINE` suppresses a final added
newline when the final byte is not newline. The notes say NOLINE was added in
50.5 and 50.6 fixed write-error memory corruption. The code also sets
`APEF_LiteralSLinks`, which is a MorphOS-specific enumeration detail.

The official MorphOS SDK's [`dos/dosasl.h`](https://morphos-team.net/sdk/includes/dos/dosasl.html)
defines `ap_Extended` as the `ap_Reserved` byte, `APSF_EXTENDED` as bit 15 of
`ap_Strlen`, and `APEF_LiteralSLinks` as bit 1 of that byte; the header marks
these extension flags as available from dos.library 50.67. The MorphOS
resident candidate now checks the opened DOS library version, sets both fields
only when 50.67 or newer is present, and returns `ERROR_NOT_IMPLEMENTED` with
`PrintFault` on older providers. The Workbench profile continues to initialize
only the classic AnchorPath fields. The fixture verifies both the supported
layout and the older-provider rejection; it does not prove the packed command's
runtime behavior.

CopperOS already publishes the Kickstart-compatible `MatchFirst`, `MatchNext`,
`MatchEnd`, `AnchorPath`, `Open`, `Read`, `Write`, `Close`, `Output`,
`ReadArgs`, and `FreeArgs` calls needed by this command. The extension is
accessed through its documented MorphOS overlay and version gate; the
Kickstart-compatible Workbench entry never writes those extension fields.

These observations do not establish the packed executable, diagnostic bytes,
full parser behavior, 37.2 behavior, or runtime effects. They authorize neither
copying source nor presenting the source behavior as a completed differential
result.

Once a profile's exact outer grammar is captured, parse it through DOS
`ReadArgs` and release its RDArgs with `FreeArgs`. A command-private tokenizer
would change DOS quoting, star escapes, `?` continuation, repeated arguments,
and error behavior. Until capture completes, do not expose an implementation
that accepts unverified operands or switches.

| Candidate slot (classic only) | Unverified possible role | Capture obligation |
| --- | --- | --- |
| `FROM/A/M` | Required list of input operands | Files, patterns, directories, quoted values, empty/repeated inputs, and input ordering |
| `TO/K` | Destination or output handle/name | Whether it is a path, stream, append/replace mode, and close/error ownership |
| `OPT/K` | Selection or presentation string | Accepted values, case/abbreviation rules, duplicate precedence, and invalid-value diagnostics |
| `HEX/S` | Presentation switch | Interaction with `NUMBER`, text layout, and no-input/default cases |
| `NUMBER/S` | Presentation switch | Numbering format, line basis, interaction with `HEX`, and no-input/default cases |

The table is a fixture map, not an implementation contract. In particular, no
semantics are inferred from the familiar command name or the candidate labels.

## Implementation and ownership boundary

The likely operations involve ordinary public DOS stream and filesystem calls,
but their exact sequence and effects remain unobserved. A later implementation
must use the normal standalone DOS command startup and public Kickstart 3.1
APIs, including `ReadArgs`/`FreeArgs` for a verified outer grammar. It must not
use host filesystem rules, host text encodings, a second shell parser, or an
assumed incoming library base.

All RDArgs, result slots, file handles, file-info buffers, input buffers,
rendering state, and `TO` ownership must belong to one invocation until they
are released or handed off through the verified DOS owner. Shared resident data
may contain only immutable code/constants. The MorphOS command needs
same-SegList repeat and interleaving evidence because P is required by the
observed installer design; the Workbench profile remains `audit/open` until its
original classification is established.

## Bounded text-rendering checkpoint

`TypeTextFormatter` independently implements the source-observed MorphOS text
rendering slice without allocating or touching DOS state. It copies one supplied
byte stream, prepends the six-byte `NUMBER` prefix (five right-aligned decimal
columns plus a space) before the first and each following logical line, and
adds one final LF unless `NOLINE` is set or the input already ends in LF. It
performs a complete capacity/mapping/overlap preflight before writing, so a
short or invalid destination remains unchanged. The focused host suite covers
empty, terminated and unterminated streams, NOLINE, multi-line numbering, and
atomic failures.

This is not a Type command entry: it has no ReadArgs, input traversal, HEX,
TO, stream I/O, break, IoErr, or native qualification. The observable source
slice still needs packed-50.6 and reference-runtime comparison.

The private `Commands.TypeNativeRoot` compiles this formatter through an
explicit caller-owned guest-memory probe on all required CPUs. The resident
HUNKs have ten reachable methods and no managed allocations, runtime features,
helpers, external native targets, exception regions, or fatal machine-fault
sites:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 2,076 | `d1a8b3573dfd8ba7085e36a021bc63f55a40c118834ee52e4e344c353a793c5e` |
| 68020 | 1,944 | `584e3dff71b0c1de564ef2f5ea115b6476bb8971cc0f393e6131125eda15623c` |
| 68040 | 1,876 | `315fc4b4d38e0ec663a1ad10f568c03f4a33c80f27fdee5ca293127d5d1aa362` |

This is a direct native component-execution checkpoint, not a command
qualification. The same private HUNKs execute ten control-block cases on each
CPU under Copper68k: empty and unterminated streams, NOLINE, numbered lines,
an atomic capacity rejection, and repeated/interleaved callers. The fixture
requires no DOS or Exec calls, no resident allocation, no leaked guest storage,
and no shared-image writes. It does not exercise a DOS stream, same-SegList
command lifecycle, or either original Type binary.

The reproducible [native qualification script](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_type_text_native.ps1)
emits hash-bound compile and execution receipts for this exact scope.

`NativeMorphOSTypeTextIo` is the next invocation-owned integration component.
It accepts already-opened DOS streams and caller-supplied buffers, carries the
NUMBER line counter across arbitrary `Read` chunks, completes positive short
`Write` results, flushes logical lines, and clears/observes only Ctrl-C while
leaving other signals intact. It therefore does not impose a whole-file host or guest buffer
limit. Its private 68000/020/040 resident roots execute six supplied-DOS-vector
invocations each: one-byte Read chunk numbering, positive short writes,
NOLINE, empty NUMBER output, Ctrl-C after 255 copied bytes, and repeat/
interleaved callers. The exact HUNK receipts are 2,528 bytes
`d1902dbef54aef8f00bdd2848769f47ef3e9346c77f0e57db63cab8ddc264f26`,
2,476 bytes `af1af7c61c8caf8861ea0815d00a7cfff2af1e1de367afd971746827b84b9898`,
and 2,408 bytes
`d0bc8483f88ced36a9d36cf618a9847168375f5c425ecb56e94e9730036de88f`.
Each uses one shared image with no leaks or image writes. The reproducible
[I/O qualification script](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_type_text_io_native.ps1)
binds those receipts. This is deliberately not an external command yet:
`ReadArgs`, FROM pattern iteration, HEX, TO open/close ownership, diagnostics,
and original comparison remain separate gates.

`NativeMorphOSTypeCommand` now joins the source-observed MorphOS outer
template to public DOS ownership for the text-mode slice: DOS `ReadArgs`/
`FreeArgs`, every `/M` FROM vector member, `MatchFirst`/`MatchNext`/`MatchEnd`,
old-file `Open`/`Close`, optional new-file `TO`, and invocation-owned anchor,
input, and output buffers. The MorphOS entry now uses the documented 50.67+
literal-soft-link AnchorPath extension and rejects older DOS providers before
opening streams; the Workbench entry keeps classic fields. The frontend
separately renders the source-observed HEX
layout: uppercase hexadecimal, grouped 16-byte rows, printable-byte column,
and the partial-row blank line. Its private external entry now
compiles as three pure/resident HUNKs with no managed runtime feature, helper,
external target, exception region, or fatal fault site: 68000 is 8,820 bytes
`669da83da52c21da0f915cce3746afb25cde562527801a54b0a4ffcd7e8ad4a9`,
68020 is 8,876 bytes
`1f021239552f90561b9f6de14aad0f103dbcb0aaa73fca937f3fa2092502b5c7`,
and 68040 is 8,684 bytes
`f21e7cf8dfb46d39cfb63b3cac0fc35de1b0fec4b88ad2a1e7e9f01a56ac9bac`.
The [entry qualification script](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_type_native_entry.ps1)
rebuilds the compiler CLI so its raw BPTR `OpenRaw` binding is current, then
emits the three hash-bound static and execution reports. Its supplied-vector
fixture executes sixteen calls per CPU: plain and NUMBER text, a two-member
`/M` FROM vector, NOLINE with TO, no-match cleanup, partial-row HEX with
short writes, source-observed ignored-OPT, HEX+NUMBER, no-match, and
input- and TO-open-failure diagnostics, a Ctrl-C-interrupted HEX stream, and
repeat/interleaved callers, and a pre-50.67 provider rejection. It checks the
exact `ReadArgs` template/result slots, literal-soft-link `AnchorPath` fields,
single-name matcher lifecycle, input/
output open/close ownership, 17,176-byte workspace, argument lease, selected
IoErr publication, source-observed 16-row interrupt polling, and no leaks or
shared-image writes. Two added vectors inject a DOS Read failure and a
short-positive-Write followed by an error; they verify the source-shaped
diagnostic, return level, selected IoErr, and handle/matcher cleanup. The DOS
parser, wildcard, and I/O results are supplied by the fixture; it makes no real
filesystem, shipping/package, or reference-equivalence claim.

`NativeWorkbench31TypeCommand` now provides the separate classic five-slot
profile, omitting MorphOS's `NOLINE` result. Its DOS 36 resident entry uses the
observed candidate `FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S` and reuses the same
public-DOS text/HEX worker. Its refreshed supplied fixture runs seventeen cases
per CPU (51 total), adding explicit Workbench-startup, missing-DOS, and
candidate-modeled Read/Write failure
boundaries while retaining Workbench-compatible result storage, confirming the
missing `NOLINE` slot does not alter ownership, and verifying that the MorphOS
extension byte remains zero and `ap_Strlen` stays classic. Receipt:
`artifacts/cc11-type-wb31-native-20260927-io-failures-v1/qualification.json`.
Its resident HUNKs have 27 reachable methods and no managed runtime
features/helpers, external targets, exception regions or fatal machine-fault
sites:

| CPU | HUNK bytes | SHA-256 | Invocations |
| --- | ---: | --- | ---: |
| 68000 | 8896 | `f4bf0aa90370bb36a84519df5652d58afd886b2f417c9cac8a10a6ac1517f06e` | 17 |
| 68020 | 8952 | `a8b62809ab0a1409eb2084ca57a8f19647f4b949ff3b24dd87a7737693d2c9f7` | 17 |
| 68040 | 8760 | `edba6d583cba19baf42df16be0d487fec53cebbdd0490ba401f961b6420a0a17` | 17 |

This remains bounded candidate evidence only: exact Workbench output,
diagnostics, original parity, PURE/resident lifecycle, packaging and
differential gates remain open.

## Required captures and fixtures

These are capture requirements, **not completed tests or original oracles**.
Each run records exact input bytes, command/profile hash, stdout and error bytes,
return level, IoErr, filesystem/stream side effects, break signals, and resource
deltas.

| IDs | Cases and oracle obligation |
| --- | --- |
| `TY-PARSE-WB-01..` | Validate the classic template candidate: every slot alone and in combinations; space and equals forms; case variants; abbreviations/aliases; unknown keys; missing values; quoted empty/spaced/star-escaped values; duplicates; `?` continuation and EOF. Capture the actual `ReadArgs`/help diagnostic boundary. |
| `TY-PARSE-MOS-01..` | Confirm the release-source `FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S,NOLINE/S` template, `OPT` H/N compatibility mapping, and HEX+NUMBER warning against the packed 50.6 command. Record the same parser/error cases after syntax is known. |
| `TY-FROM-01..` | Empty, regular, binary, long-line, final-no-newline, unreadable, missing, directory, device, assign, wildcard/pattern, and multiple input cases. Capture ordering, traversal, labels, and status. |
| `TY-MODES-01..` | Every observed `OPT`, `HEX`, and `NUMBER` value/presence combination, including conflicts and defaults. Compare raw output bytes, numbering base, whitespace, line endings, and diagnostic stream. |
| `TY-TO-01..` | Existing/new/protected destination; stdout default; open/write/close/short-write failure; aliasing an input; cleanup/partial-output policy; return and IoErr. |
| `TY-BREAK-01..` | Break before open, during input, during output, and during close; establish diagnostics, result/IoErr precedence, and cleanup. |
| `TY-LIFE-01..` | Repeated successful/failing calls and interleaved callers sharing one loaded segment. Check that RDArgs, buffers, handles, and presentation state are invocation-owned and that required-pure MorphOS runs do not write the image. |
| `TY-LINK-MOS-01..` | Capture literal soft-link versus followed-link behavior on original 50.6, including the documented 50.67+ `APEF_LiteralSLinks` layout, real provider version boundary, and the public AnchorPath compatibility behavior. The supplied fixtures verify only the field layout and explicit old-provider rejection. |

## Completion gates

- [ ] Bind the exact grammar, help behavior, aliases, defaults, and errors for
  each profile to original captures; prove or reject the partial 50.6 source
  correspondence separately.
- [ ] Capture file/pattern traversal, text/binary rendering, `OPT`, `HEX`,
  `NUMBER`, output ownership, statuses, IoErr, and break behavior separately
  for 37.2 and 50.6.
- [ ] Implement the captured subset through public DOS/Exec APIs with
  invocation-owned resources and no host-path fallback.
- [ ] Qualify actual 68000/020/040 HUNK entry/resource paths, MorphOS P purity,
  packaging, and reference differential behavior.

## 2026-09-12 native-entry rerun

The current `qualify_type_native_entry.ps1` receipt at
`artifacts/type-morphos-native-20260912-qualified/qualification.json` rebuilds the same
entry and passes 13 supplied-DOS invocations on each CPU. The current HUNK
hashes are 68000 `7478aee3a8859a9bbd64f7d4b3843b9e81a95b1ac49cefa0d225f214cbca073e`,
68020 `5ecb3c666fe8336387859a95c5e259e59e22520118c69fc68fee2c3845f05224`, and
68040 `3b9abe6109131392a3256667256a0398a634038d9b718d057e308318fe6ebb62`.
This receipt supersedes earlier generated-byte hashes for that checkpoint;
the scope remains supplied vectors only and does not close real-DOS,
Workbench-parity, packaging, or production PURE/resident gates.
