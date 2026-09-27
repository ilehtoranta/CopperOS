# Trashcan contract

Profile: morphos320. Goal steps: CC01 and CC13. Recorded: 2026-09-04.

Status: identity and dependency evidence only. No grammar, behavior, body,
packed-binary correspondence beyond identity, runtime capture, or parity claim
is made.

The observed MorphOS 3.20 `MorphOS/C/Trashcan` is an 18,092-byte packed native
member with SHA-256
`c4fe2e61256d87c2d38cea491d3b3d25a6178db8cf826cf08616d81d9e69bad2` and
version tag `Trashcan 51.2 (25.4.2017)` at member offset 18,021. The observed
ISO extent is 178,668. Its POSIX-mode metadata is not AmigaDOS protection
evidence.

Trashcan does not appear in the selected MorphOS installer P-addition list.
That is unresolved purity status, not proof that it is non-pure. The inspected
hash-bound 3.20 C source archive has no Trashcan member and exposes no supported
replacement specification.

The command depends on the real trash/desktop owner named by CC13. It must not
be reduced to Delete or implemented with host filesystem moves. Before a body is
admitted, establish through admitted documentation, safe binary inspection, and
controlled disposable-reference captures: syntax/options; trash location and
selection policy; metadata and icon handling; cross-volume and missing-trash
behavior; directories, links, cycles, collisions, undo/restore if applicable;
requesters and QUIET-like modes; partial/cancelled operations; diagnostics;
result/IoErr; exact pure/resident lifecycle; package placement; and reference
comparisons.
