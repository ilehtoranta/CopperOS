"""Create a 68000 HUNK command that writes to Output() and pr_CES."""
import importlib.util
from pathlib import Path
import sys

source = Path(__file__).resolve().parents[2] / 'Locale.Workbench' / 'make_probe.py'
spec = importlib.util.spec_from_file_location('assembler', source)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def build():
    a = module.Assembler()
    def call(offset): a.words(0x4eae, offset)

    a.words(0x48e7, 0x3f3e, 0x2c78, 4)  # save D2-D7/A2-A6; ExecBase
    a.words(0x93c9); call(-294)             # FindTask(NULL)
    a.words(0x2a40)                         # A5 = Process
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'failed')
    a.words(0x2c4c); call(-60)              # Output()
    a.words(0x2200); a.address(0, 'out'); a.words(0x2408); a.d(3, 4); call(-48)
    a.words(0x222d, 224); a.address(0, 'err'); a.words(0x2408); a.d(3, 4); call(-48)
    a.words(0x2c78, 4, 0x224c); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('failed'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('out', 'OUT\n'); a.text('err', 'ERR\n')
    return a.finish()


if __name__ == '__main__':
    Path(sys.argv[1]).write_bytes(build())
