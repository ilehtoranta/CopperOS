# RunCommand retirement range owner revalidation

Recorded 2026-08-30. This is a bounded CC02.API14.RET03.2A host inspection
checkpoint. It proves that the range-inspection admission rejects a binding that
changes while `GetExecBase` executes. It does **not** retire a task, free a
range, qualify native scheduling, or complete a command/profile gate.

## Change and safety boundary

`DosServices.RunCommandRetirementRanges.cs` snapshots the expected Exec-memory
owner, DOS context, memory/bus, allocate/free delegates, allocator kind and
`GetExecBase` delegate before asking for the Exec base. The added post-getter
check requires every captured field to still be the same before it exposes any
range facts. It returns `InvalidOwner` on a change and leaves facts at their
default value.

This avoids treating delegates, a bus, an allocator policy, or an Exec-base
getter installed during the callback as though they owned the earlier
inspection. It does not invoke another callback to obtain a new snapshot. The
inspection remains Classic-only, read-only and budgeted. It performs no
allocation, free, callback pop, task transition, `Ready` operation,
`CurrentDosTask` query, CPU mutation, or cleanup admission.

The source delta is from the accepted initial range checkpoint
`c63c83754b2b17c8d0217021338005c69fc1aef779df93c73fb772f3bc0cf4c0` to
`d36c7331f85311668c2cf4a3a2e7f71626a62f79219bc76f9f7067970decb113`.
The latter is the current SHA-256 of
[DosServices.RunCommandRetirementRanges.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.RunCommandRetirementRanges.cs).

## Source-bound before/after evidence

The fresh [before receipt](D:/TestData/CopperOSCommands/Q/ret03rangeowner01/before-receipt.json)
records the intentional red state: five stale-binding failures and 147 controls
passing in a 152-test assembly. Each failure is a changed binding observed by
the new regression, rather than a setup or unrelated control failure. Its
receipt SHA-256 is
`e69959e1d9fbf7fd42ac7b335ece91d800cbdd0b37eadc287486486d755343d7`.

The fresh [after receipt](D:/TestData/CopperOSCommands/Q/ret03rangeowner01/after-receipt.json)
records 152 executed and passing cases, with zero failures, errors, skips,
timeouts or aborted cases. Its SHA-256 is
`c5f2f88161c9cce9dd14bbb46ccfe4ddb9dcac5940babc87dec315d179797ee5`.
The run copied and compiled 126 captured host sources against 188 captured
references, 21 pins and five restore inputs. It rebuilt the private emulator
output, then replaced only `CopperMod.Amiga.Emulator.dll` and its PDB in the
frozen 110-file test runtime. The test DLL remained byte-identical at
`14acbe876e4864bb28468147f82383779788e883d0dc1c0fdcbb3274bbf8e4dd`.
The rebound emulator DLL is
`bd96c2b5f0c644cf3262dbf1f53a51f728ad8f9d8deea938e3454ee5cc5cb9d1`.

The receipt validates the before/after source and output identities, the copied
project, actual compile inputs, references and the accepted parent checkpoint.
The parent [range inspection receipt](D:/TestData/CopperOSCommands/Q/ret03range02/after-receipt.json)
remains a distinct 145-host/337-portable read-only result; it must not be
attributed to this later source revalidation.

## What remains open

This result is admission evidence only. It is not proof that every surviving
foreign callback, I/O root, packet, notification, CPU frame, stack or native
Switch owner has stopped using the range. The four retained retirement-liveness
failures, external-root census, actual RET02 scheduling composition and all
cleanup/release behavior remain open. Future work must establish those owners
before any release capability is enabled.
