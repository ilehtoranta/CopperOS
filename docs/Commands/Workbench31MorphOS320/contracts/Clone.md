# Clone contract

Profile: morphos320. Goal steps: CC01 and CC13. Recorded: 2026-09-04.

Status: identity and pure-design evidence only. No grammar, behavior, body,
packed-binary correspondence beyond identity, runtime capture, or parity claim
is made.

The observed MorphOS 3.20 `MorphOS/C/Clone` is a 2,556-byte packed native
member with SHA-256
`02089aafa517ec2a9b5c6e210a108bd456ef727606da569e5397a271739aef5f` and
version tag `Clone 50.5 (27.3.2018)` at member offset 2,498. The observed ISO
extent is 175,252. Its POSIX-mode metadata is not AmigaDOS protection evidence.

The selected MorphOS installer script adds P to Clone at `hdinstall.fixc` line
14. This establishes a required-pure replacement design obligation. It does not
establish installed file protection, resident registration/removal lifecycle, or
the behavior of a replacement artifact.

The inspected hash-bound 3.20 C source archive has no Clone member. The command
must therefore be specified from separately admitted original documentation,
safe binary-string inspection, and controlled disposable-reference captures
before implementation. Do not infer that Clone aliases Copy, or transfer Copy's
options, metadata policy, requester behavior, error handling, result levels, or
IoErr to it. A future body must use the owning guest filesystem/metadata
interfaces and keep every buffer, lock, stream, matcher, and error state
invocation-local.

Required next evidence: exact syntax and option matrix; copy/data and metadata
semantics; links, directories, same-object and nested-destination behavior;
short I/O and capacity failures; Ctrl-C; diagnostics; final result/IoErr; pure
resident lifecycle; packaging; and comparisons against the original profile.
