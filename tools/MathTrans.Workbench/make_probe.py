"""Emit a small 68000 command that exercises the disk-loaded mathtrans API."""
from pathlib import Path
import argparse
import importlib.util

source = Path(__file__).resolve().parents[1] / 'Locale.Workbench' / 'make_probe.py'
spec = importlib.util.spec_from_file_location('locale_probe', source)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
Assembler = module.Assembler


def build(version=0, library_name='mathtrans.library', report_path=None):
    a = Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    def compare(value):
        a.words(0x0c80, value >> 16, value)
        a.branch(0x6600, 'fail_close')

    a.words(0x48e7, 0x3f3e)               # preserve D2-D7/A2-A6
    a.words(0x2c78, 4)                    # ExecBase
    a.address(1, 'name'); a.d(0, version); call(-552)
    a.words(0x2a40, 0x4a80)             # A5 = MathTransBase
    a.branch(0x6700, 'fail')
    a.words(0x2c4d)                      # A6 = MathTransBase
    a.words(0x7401); a.d(0, 0); call(-36); compare(0)     # sin(0)
    a.words(0x7402); a.d(0, 0); call(-42); compare(0x80000041)
    a.words(0x7403); a.d(0, 0x80000041); call(-96); compare(0x80000041)
    a.words(0x7404); a.d(0, 0x80000041); call(-102); compare(0x3f800000)
    a.words(0x7405); a.d(0, 0x3f800000); call(-108); compare(0x80000041)
    a.words(0x7406); a.d(0, 0x80000055); call(-36)        # large sin aborts, V set
    a.branch(0x6800, 'fail_close')        # BVC
    compare(0x800000aa)
    a.words(0x7407); a.address(0, 'cosine'); a.words(0x2208)
    a.d(0, 0); call(-54)
    compare(0)
    a.words(0x7408); a.address(0, 'cosine'); a.words(0x0c90, 0x8000, 0x0041)
    a.branch(0x6600, 'fail_close')
    a.words(0x7400); a.branch(0x6000, 'close')  # return success
    a.label('fail_close')                        # return failed stage after CloseLibrary
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    if report_path:
        a.branch(0x6000, 'report')
        a.label('fail'); a.words(0x7414)
        a.label('report'); a.words(0x2802, 0x2c78, 4)  # D4 = result
        a.address(1, 'dos'); a.d(0, 0); call(-552)
        a.words(0x2840, 0x4a80); a.branch(0x6700, 'return_report')
        a.words(0x2c4c); a.address(0, 'report_path'); a.words(0x2208)
        a.d(2, 1006); call(-30)                         # Open NEWFILE
        a.words(0x2a00, 0x4a80); a.branch(0x6700, 'close_dos')
        a.words(0x2205, 0x4a84); a.branch(0x6600, 'report_failure')
        a.address(0, 'pass_text'); a.d(3, 9); a.branch(0x6000, 'write_report')
        a.label('report_failure'); a.address(0, 'fail_text'); a.d(3, 10)
        a.label('write_report'); a.words(0x2408); call(-48)
        a.words(0x2205); call(-36)
        a.label('close_dos'); a.words(0x224c, 0x2c78, 4); call(-414)
        a.label('return_report'); a.words(0x2004, 0x4cdf, 0x7cfc, 0x4e75)
    else:
        a.words(0x2002, 0x4cdf, 0x7cfc, 0x4e75)
        a.label('fail'); a.words(0x7014, 0x4cdf, 0x7cfc, 0x4e75)
    a.label('cosine'); a.words(0, 0)
    a.text('name', library_name)
    if report_path:
        a.text('dos', 'dos.library')
        a.text('report_path', report_path)
        a.text('pass_text', 'RESULT 0\n')
        a.text('fail_text', 'RESULT 20\n')
    return a.finish()


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--version', type=int, default=0)
    parser.add_argument('--library-name', default='mathtrans.library')
    parser.add_argument('--report-path')
    args = parser.parse_args()
    args.output.write_bytes(build(args.version, args.library_name, args.report_path))
