"""Write the finite native DOS corpus; expectations come from the command contract."""
import json
import pathlib
import sys

root = pathlib.Path(sys.argv[1])
definitions = [
    ("literal", "RAM:SearchQ/one.txt needle", 0, 0, "     2> Needle\n"),
    ("nonum", "RAM:SearchQ/one.txt needle NONUM", 0, 0, "Needle\n"),
    # Native NameFromLock expands the RAM: assign to its volume name.
    ("quiet", "RAM:SearchQ/one.txt needle QUIET", 0, 0, "Ram Disk:SearchQ/one.txt\n"),
    ("quick", "RAM:SearchQ/one.txt needle QUICK", 0, 0, "     2> Needle\n"),
    ("miss", "RAM:SearchQ/one.txt absent", 5, 0, ""),
    ("empty", "RAM:SearchQ/empty.txt needle", 5, 0, ""),
    ("pattern", "RAM:SearchQ/one.txt eed PATTERN", 0, 0, "     2> Needle\n"),
    ("file-pattern", "RAM:SearchQ/one.txt #?.txt FILE", 0, 0, "Ram Disk:SearchQ/one.txt\n"),
    ("wildcard-from", "RAM:SearchQ/#?.txt needle QUIET", 0, 0, None),
    ("multiple-from", "RAM:SearchQ/one.txt RAM:SearchQ/sub/two.txt needle NONUM", 0, 0, None),
    ("all-directory", "RAM:SearchQ needle ALL", 0, 0, None),
    ("missing-file", "RAM:SearchQ/missing needle", 20, 205, "object not found\n"),
    ("required-missing", "", 10, 116, "required argument missing\n"),
    ("unterminated-quote", '"RAM:SearchQ/one.txt needle', 10, 120, None),
    # FROM/M consumes additional positional strings; this is a missing-path
    # operation, not an unknown-keyword parser failure.
    ("extra-argument", "RAM:SearchQ/one.txt needle UNKNOWN", 20, 205, None),
    ("ctrl-c", "RAM:SearchQ/one.txt needle", 20, 304, None),
    ("long-line", "RAM:SearchQ/long.txt Needle", 0, 0, "     1> prefix " + "x" * 8179 + " Needle\n"),
    ("quoted-file", '"RAM:SearchQ/spaced name.txt" needle NONUM', 0, 0, "Needle\n"),
]
rows = []

def append(name, arguments, image, stack, rc, error, output, phase):
    rows.append(dict(Name=name, Arguments=arguments, Image=image, Stack=stack,
                     CtrlC=name == "ctrl-c", expectedReturn=rc, expectedIoErr=error,
                     expectedOutput=output, phase=phase))

# Warm native DOS parsing/file buffers before judging resource stability.
for _ in range(2):
    for image in range(2):
        append(*definitions[0][:2], image, 4096, *definitions[0][2:], "warmup")
for definition in definitions:
    for image in range(2):
        append(*definition[:2], image, 4096, *definition[2:], "corpus")
# Same LoadSeg instances, alternating images/options/errors and both stack sizes.
for stack in (4096, 8192):
    for definition in (definitions[0], definitions[1], definitions[4], definitions[12], definitions[5]):
        for image in (1, 0):
            append(*definition[:2], image, stack, *definition[2:], "repeat")
assert len(rows) == 60
(root / "cases.json").write_text(json.dumps(rows, indent=2) + "\n", encoding="utf-8")
