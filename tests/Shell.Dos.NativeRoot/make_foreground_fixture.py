"""Create a tiny 68000 HUNK that checks RunCommand's argument registers."""
from pathlib import Path
import struct
import sys

# On entry D0 must be 4 and A0 must point to b'ABC\n'. Return 7 if both
# match, otherwise 9. The Shell supplies the newline in its command tail.
code = bytes.fromhex('0c8000000004660c0c904142430a660470074e7570094e75')
words = [0x3F3, 0, 1, 0, 0, len(code) // 4, 0x3E9, len(code) // 4]
Path(sys.argv[1]).write_bytes(struct.pack('>8I', *words) + code + struct.pack('>I', 0x3F2))
