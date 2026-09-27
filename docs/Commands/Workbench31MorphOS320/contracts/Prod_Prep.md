# Prod_Prep contract

Profile: `wb31`. Goal step: CC22. Status: partial media/static contract only;
no implementation, guest, PURE, package, or shipping claim.

The Workbench 3.1 installation image contains `Install3.1:C/Prod_Prep`, a
37,008-byte Amiga HUNK with version `$VER: prod_prep 39.1 (22.12.92)` and
SHA-256 `b2b1278e80433fe87885ccfc1bb062d48b3d06168fb73d8bade6b638ef7e33b3`.
The member is on the selected `Install3.1` disk and has protection word zero;
that metadata does not establish a non-PURE runtime design.

The binary contains a custom interactive command surface rather than a
verified DOS `ReadArgs` template. Its startup usage text identifies these
optional launch words:

```text
Prod_Prep [<filename>|-] [device <name>] [unit <number>] [layout]
         [badfile <file>] [formatonly] [noverify] [verifyonly] [slowdown]
```

The prompt advertises the following operations:

```text
addpart, deletepart, writerdb, format, verify, readfs,
synch, reselect, readrdb
```

`addpart` accepts a name and a size with `M`, `K`, `C`, `%`, or `rest`, then
optional `bootable`, `dostype`, `buffers`, `mask`, `maxtransfer`, `customboot`,
and `nomount` values. `deletepart` accepts a name and optional `noerror`;
`writerdb`, `format`, and `verify` expose `force`/bad-block behavior. These
strings are operation-surface evidence, not a frozen parser grammar or help
transcription.

Static dependency strings identify `dos.library`, `intuition.library`,
`expansion.library`, `FileSystem.resource`, `scsi.device`, and `xt.device`.
The observed failure and status text covers RDB discovery, partition layout,
low-level formatting, verification, SCSI sense/error reporting, filesystem
loading, bad-block replacement, and configuration-file output. The command is
destructive and must be exercised only with disposable guest disks or a fully
simulated device provider.

## Required before implementation admission

- Capture the startup tail, prompt/input grammar, command abbreviations, exact
  result/`IoErr`/diagnostic behavior, Ctrl-C handling, and output stream policy
  on a disposable Workbench 3.1 guest.
- Map every device/resource/library call to the existing guest providers,
  including SCSI/XT request layouts, RDB and partition checksums, bad-block
  mapping, filesystem validation, and `FileSystem.resource` ownership.
- Implement separate invocation-owned parser, disk/request, and output state;
  no host disk access or persistent managed state is allowed.
- Qualify failure-first cases before destructive success: missing device,
  write protection, malformed RDB, unsupported geometry, partial I/O,
  allocation failure, Ctrl-C, and cleanup after failed format/verify.
- Compare the original command and replacement on synthetic disposable images,
  then prove resident lifecycle and package placement. Do not redistribute the
  installation media or extracted disk contents.

