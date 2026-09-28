# Command distribution image builder

`build_command_image.py` is the production packaging owner for new command
distribution volumes. It selects only `qualified_shipping_artifacts` from the
command build manifest and requires every selected record to pass the real
[release preflight](../../docs/Commands/Workbench31MorphOS320/release-preflight.md).
There is no development fallback, PURE override or candidate installation mode.
The current shipping manifest is empty; the production command therefore rejects
the request without writing an image or receipt. This is expected until a command
passes all required release checks.

## Output and ownership

The builder creates a fresh 880 KiB ADF or 1..512 MiB raw HDF containing a DOS1 FFS
filesystem with 512-byte blocks. These are nonbootable command distribution
volumes: no boot code, reference operating-system files, RDB partitions or complete
SYS installation is added. Profile, CPU and normal-runtime/installation-media
selection remain explicit. Exact classic ASCII path spelling, command bytes and
raw Amiga protection bytes come from admitted records; host file attributes are
never used. All file, directory and root dates are the Amiga epoch.

The external host dependency is pinned to `amitools==0.8.1` in
`requirements-host-tools.txt`. Upstream identifies it as GPLv2 and provides
`xdftool` for ADF/HDF creation. It is invoked in a separate host process; no
third-party implementation is vendored into this repository or emitted in the
guest command payloads. See [upstream](https://github.com/cnvogelg/amitools) and
the [xdftool command documentation](https://amitools.readthedocs.io/en/latest/tools/xdftool.html).

Each invocation snapshots the installed package's hashed Python sources into a
new private temporary module tree. The child uses `-I -S -B`, excludes preexisting
bytecode and site initialization, and receives arguments as JSON data on stdin.
This binds the external package sources and avoids shell interpolation and the
Windows command-line length limit. It does not make the host Python executable,
standard library and operating system a hermetic build environment.

`verify_image.py` reads the resulting raw image independently through the existing
read-only inventory parser, without importing the writer's filesystem objects.
It verifies exact tree membership, hashes, protection bytes, all declared dates,
directory hash buckets, header checksums, file extension blocks, disjoint block
ownership and exact allocation bitmaps, including bitmap extension blocks. It
does not certify free-space bytes, unused metadata padding, OS execution or PURE
safety. Generic fixtures containing a P bit demonstrate metadata preservation
only; they never qualify executable code as pure.

`canonicalize_bitmap.py` clears only nonexistent-block padding in the final
allocation bitmap and recomputes that bitmap's checksum before independent
readback. This explicit canonical output policy follows the treatment in
[Linux AFFS bitmap initialization](https://github.com/torvalds/linux/blob/master/fs/affs/bitmap.c).
It does not assert that every image with nonzero padding is unmountable. The
helper verifies that valid-block allocation bits, pointers and every other byte
are unchanged, and is used only on the builder's new private image.

## Running

Install the pinned host dependency into the Python environment used for the
builder. From the repository root, the following PowerShell invocation attempts
an exact Workbench selection and is currently expected to reject the empty
shipping manifest:

```powershell
python -m pip install -r tools/DiskBuilder/requirements-host-tools.txt
$commandInventory = Resolve-Path docs/Commands/Workbench31MorphOS320/command-inventory.json
$commandInventoryHash = (Get-FileHash -Algorithm SHA256 $commandInventory).Hash.ToLowerInvariant()
python tools/DiskBuilder/build_command_image.py --manifest docs/Commands/Workbench31MorphOS320/build-manifest.json --inventory $commandInventory --inventory-sha256 $commandInventoryHash --profile wb31 --cpu 68000 --distribution normal-runtime --output artifacts/command-release/wb31-68000.adf --receipt artifacts/command-release/wb31-68000.json
```

For a raw HDF, choose a `.hdf` output and add `--hdf-mib 52`. Existing output and
receipt paths are rejected, never overwritten. Generation uses private payload
snapshots; admission, source hashes and host-tool identity are rechecked before
publication. The bytes about to be published must match the staged independent
readback. The final output is independently read again, and the receipt is written
last. An incomplete image without a successful matching receipt is not an admitted
distribution. This tool does not modify the live `filesystem/` tree.

## Verification and current scope

```powershell
python -m unittest discover -s tools/DiskBuilder/tests -v
```

On 2026-09-08 all 26 tests pass: six writer/release-boundary tests, fifteen
independent-reader tests and five bitmap-normalizer tests. Writer cases exercise
real external serialization, byte-identical ADFs from reversed input order, a
52 MiB HDF with a bitmap extension, empty and extended files, seven protection
patterns, rejection of invalid paths/duplicates/existing images, and rejection
of unqualified shipping/PURE claims. One test explicitly stubs admission to
isolate rejection of a post-readback image change; this is not a production
override or positive command qualification.

The successful test log is
`artifacts/diskbuilder-tests-complete-20260908.log`; earlier failed integration
logs remain historical evidence. Actual owned-data images and readback reports
are recorded in `artifacts/diskbuilder-qualified-20260908/verified.json`.
No original operating-system binary or command candidate is used in these
positive filesystem fixtures.

CC08 remains open. A real admitted command record must still complete the
production packaging path, and packaged commands must execute with correct
metadata in their target systems. Complete system-image composition and boot,
all CPU/profile release evidence, original resident lifecycle and PURE admission
are separate unfinished requirements.
