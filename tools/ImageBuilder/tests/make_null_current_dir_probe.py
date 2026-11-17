"""Create a 68000 command that sets the current-directory lock to zero."""
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
a.words(0x2840, 0x4a80); a.branch(0x6700, 'failed')
a.words(0x2c4c, 0x7200); call(-126)  # CurrentDir(0)
a.words(0x2200, 0x4a80); a.branch(0x6700, 'close')
call(-90)  # UnLock(previous)
a.label('close'); a.words(0x2c78, 4, 0x224c); call(-414)
a.words(0x7000); a.branch(0x6000, 'done')
a.label('failed'); a.words(0x7014)
a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
a.text('dos', 'dos.library')
Path(sys.argv[1]).write_bytes(a.finish())
