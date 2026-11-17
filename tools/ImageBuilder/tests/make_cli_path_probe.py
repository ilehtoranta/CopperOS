"""Emit a test command that prepends a DH0: lock to cli_CommandDir."""
import importlib.util
from pathlib import Path
import sys

source = Path(__file__).resolve().parents[2] / 'Locale.Workbench' / 'make_probe.py'
spec = importlib.util.spec_from_file_location('assembler', source)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
a = module.Assembler()
def call(offset): a.words(0x4eae, offset)

a.words(0x48e7, 0x3f3e, 0x2c78, 4)
a.address(1, 'dos'); a.d(0, 37); call(-552)
a.words(0x2840, 0x4a80); a.branch(0x6700, 'fail')  # A4 = DOSBase
a.words(0x2c4c); a.address(0, 'path'); a.words(0x2208); a.d(2, -2); call(-84)
a.words(0x2a00, 0x4a80); a.branch(0x6700, 'close_fail')  # D5 = lock
a.words(0x2c78, 4); a.d(0, 8); a.d(1, 0x10001); call(-684)
a.words(0x2440, 0x4a80); a.branch(0x6700, 'unlock_fail')  # A2 = node
a.words(0x2545, 4)                       # node->lock = D5
a.words(0x7000, 0x2240); call(-294)     # FindTask(NULL)
a.words(0x2040, 0x2028, 172, 0xe588, 0x2040)  # CLI address
a.words(0x24a8, 8)                       # node->next = cli_CommandDir
a.words(0x200a, 0xe488, 0x2140, 8)      # cli_CommandDir = BPTR(node)
a.words(0x2c78, 4, 0x224c); call(-414)
a.words(0x7000); a.branch(0x6000, 'done')
a.label('unlock_fail'); a.words(0x2c4c, 0x2205); call(-90)
a.label('close_fail'); a.words(0x2c78, 4, 0x224c); call(-414)
a.label('fail'); a.words(0x7014)
a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
a.text('dos', 'dos.library'); a.text('path', 'DH0:')
Path(sys.argv[1]).write_bytes(a.finish())
