# Bounded raw Write and FPutC checked-byte failures

Recorded 2026-08-31. This CC02.API17 checkpoint covers generated DOS raw
`Write` and unbuffered `FPutC` paths when a checked byte-provider operation
fails. It makes the first fresh operation error observable, preserves the
completed prefix, and prevents the caller from publishing an unwritten byte as
complete. It is not an original packet-result differential or a shipping command
qualification.

## Bound behavior

The two existing void byte-write call sites in `DosCore.cs` now consume the
checked provider result. `FPutC` fails before it publishes position/length for a
failed byte. The `Write` transfer loop stops at the first fresh error, publishes
only the completed prefix, then returns `-1` with that fresh I/O error. Existing
success, zero-length, console, read and Close behavior remains outside this
change.

The audited live [DosCore.cs](D:/Koodit/GIT/CopperStart/src/CopperStart.Dos/DosCore.cs)
is SHA-256 `120a9c7d6254e1fc822c4264555b23e07d200cf943eaac0587c947a55aede4fd`.
The captured source delta is
`82150d4eacde954a9d6bec34373b8efa20573ec429cbc11fae2b8d83143d0dc7`.
No provider, compiler, SDK, CPU, Shell, command frontend, pure/resident or
task-retirement source is changed by this slice.

## Reproduced evidence

The fresh source-bound audit is
[direct-write-qualification.json](D:/TestData/CopperOSCommands/Q/directWrite195d72df/direct-write-qualification.json),
SHA-256 `d17ee19cb6f029673cce6d7107152a9435da04e19ddc18c9cd4763164a171633`.
It revalidated 1,349 consumed files, including captured sources, binaries,
native maps, runtime identities, PDB source checksums and all retained receipts.
Five PE/PDB pairs cover 250 matching source documents.

The portable red baseline has 22 failures and four controls among 26 cases.
After the change, all 26 focused cases and all 349 inclusive checks pass. The
frozen host regression assembly passes all 64 checks after rebinding only its
DOS dependency. Those receipts are respectively
`1ff0f6dd3271be49f343b6a7c5c4030e7fde4451d4afd9be69b1b79a10161adb`,
`d76f3afec0c8b8c3a7e816350cdc26dc03797734111e1493082697f6975a8db4`, and
`7fdd13f0e5ccd254e3d0029a7a870f22c576f6fb9cf1d4f31b71bbf31ea9ad9f`.

Each native CPU uses the same 15 scenario names. The retained before runs
record 12 controls and 33 expected failures across 45 attempts. The after runs
pass all 45 attempts across 68000, 68020 and 68040. They make 51 write/FPutC
operation entries and returns, 45 cleanup returns, and execute 17,572,890 guest
instructions in total. Every after case retains exactly one End packet, its
expected prefix bytes and position, balanced return records, stream ownership,
stack guards, and no code write. The supplied-vector reports explicitly record
zero original-write, original-FPutC, Shell-command and distribution-command
invocations.

## Limits

This does not establish original positive-short, zero, or negative packet
results for raw `Write`/`FPutC`; the byte-provider mappings are not evidence for
one original DOS packet. `PrepareFileWrite` capacity/length reservation is not
rolled back, so provider metadata rollback is still unqualified. Other buffered
callers retain their existing failure mappings. No HUNK is admitted to shipping,
and there is no claim about a full filesystem, resident lifecycle, native task
retirement, library expunge, or any Workbench/MorphOS command.
