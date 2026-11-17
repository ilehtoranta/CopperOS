"""68000 Kickstart probe for mathtrans overflow and task trap handling."""
from pathlib import Path
import argparse
import importlib.util

source = Path(__file__).resolve().parent / 'make_probe.py'
spec = importlib.util.spec_from_file_location('mathtrans_probe', source)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
Assembler = module.Assembler


def build(version, library_name="mathtrans.library", report_path=None, full=False):
    a = Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    def cmpd(value):
        a.words(0x0c80, value >> 16, value); a.branch(0x6600, 'restore')
    def cmpp(label, value):
        a.address(0, label); a.words(0x0c90, value >> 16, value); a.branch(0x6600, 'restore')

    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'name'); a.d(0, version); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x93c9); call(-294)             # FindTask(NULL)
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'fail_close')
    a.words(0x266c, 50)                     # A3 = previous tc_TrapCode
    a.address(0, 'trap'); a.words(0x2948, 50)
    a.words(0x2c4d)                         # library base
    if full:
        # Reject a too-new version and balance a second open of the same base.
        a.d(2,40); a.words(0x2c78,4); a.address(1,'name'); a.d(0,41); call(-552)
        a.words(0x4a80); a.branch(0x6600,'restore')
        a.d(2,41); a.address(1,'name'); a.d(0,40); call(-552)
        a.words(0x220d,0xb081); a.branch(0x6600,'restore')
        a.words(0x2240); call(-414); a.words(0x2c4d)
        # Every public vector, with exact representable reference values.
        cases = [(-30,0,0,0),(-36,0,0,0),(-42,0,0,0x80000041),
                 (-48,0,0,0),(-54,0,0,0),(-60,0,0,0),(-66,0,0,0x80000041),
                 (-72,0,0,0),(-78,0,0,0x80000041),(-84,0x80000041,0,0),
                 (-90,0x80000041,0x80000041,0x80000041),
                 (-96,0x80000041,0,0x80000041),(-102,0x80000041,0,0x3f800000),
                 (-108,0x3f800000,0,0x80000041),(-114,0,0,0),
                 (-120,0x80000041,0,0),(-126,0x80000041,0,0)]
        for index,(lvo,x,y,expected) in enumerate(cases):
            a.d(2,21+index); a.d(0,x)
            if lvo == -54:
                a.address(0,'cosine'); a.words(0x2208)
            else: a.d(1,y)
            call(lvo); cmpd(expected)
        a.address(2,'samples')
        # Capture raw values/CCR for cross-ROM comparison, including nonzero,
        # negative, singular and large arguments. X/C are unspecified.
        samples=[(-30,0x800000c0,0),(-36,0x80000040,0),(-42,0x800000c0,0),
                 (-48,0x80000040,0),(-54,0x800000c0,0),(-60,0x800000c1,0),
                 (-66,0x80000041,0),(-72,0x800000c1,0),(-78,0x80000041,0),
                 (-84,0x80000040,0),(-90,0xc0000042,0x80000040),
                 (-96,0x80000042,0),(-102,0xffffff7f,0),
                 (-108,0x7f800000,0),(-114,0x80000040,0),
                 (-120,0x800000c0,0),(-126,0xa0000044,0),
                 (-36,0x80000055,0),(-84,0,0),(-96,0x800000c1,0)]
        for lvo,x,y in samples:
            a.d(0,x)
            if lvo == -54:
                a.address(0,'cosine'); a.words(0x2208)
            else: a.d(1,y)
            call(lvo)
            a.words(0x40c7,0x24c0,0x0247,14,0x34c7) # D0.l, defined N/Z/V.w
        # The domain samples above must not introduce divide-by-zero traps.
        a.d(2,38); cmpp('traps',0)
    a.words(0x7401)                         # failure stage 1
    a.d(6, 0x12345678); a.address(0, 'cosine'); a.words(0x2208)
    a.d(0, 0x80000055); call(-54)
    a.branch(0x6b00, 'restore'); a.branch(0x6700, 'restore'); a.branch(0x6900, 'restore')
    cmpd(0x12345678); cmpp('cosine', 0x800000aa)
    a.words(0x7402); a.d(6, 0x80000000); a.address(0, 'cosine'); a.words(0x2208)
    a.d(0, 0x800000d5); call(-54)
    a.branch(0x6a00, 'restore'); a.branch(0x6700, 'restore'); a.branch(0x6900, 'restore')
    cmpd(0x80000000); cmpp('cosine', 0x800000aa)
    a.words(0x7403); a.d(0, 0xb17218c6); call(-60)  # last value before underflow
    a.branch(0x6a00, 'restore'); a.branch(0x6900, 'restore')
    cmpd(0xfffffffe); cmpp('traps', 0)
    a.words(0x7404); a.d(0, 0xb17219c6); call(-60)  # first underflow value
    a.branch(0x6a00, 'restore'); a.branch(0x6900, 'restore')
    cmpd(0xfffffffe); cmpp('traps', 1)
    a.words(0x7405); a.d(0, 0x800000c7); call(-60)  # sinh(-64)
    a.branch(0x6a00, 'restore'); a.branch(0x6900, 'restore')
    cmpd(0xfffffffe); cmpp('traps', 2)
    a.words(0x7406); a.d(0, 0x800000c7); call(-66)  # cosh(-64)
    a.branch(0x6800, 'restore')             # overflow must be set
    cmpd(0xffffff7f); cmpp('traps', 2); cmpp('wrong', 0)
    a.words(0x7400)                          # success
    a.label('restore'); a.words(0x294b, 50)  # restore tc_TrapCode
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    if full:
        # Expunge after all calls have returned, then reload the same staged file.
        a.words(0x4a82); a.branch(0x6600,'after_reload')
        a.words(0x224d); call(-402)
        a.address(1,'name'); a.d(0,40); call(-552)
        a.words(0x4a80); a.branch(0x6600,'reopened')
        a.d(2,42); a.branch(0x6000,'after_reload')
        a.label('reopened'); a.words(0x2240); call(-414)
        a.label('after_reload')
    if report_path:
        a.branch(0x6000, 'report')
    else:
        a.words(0x2002, 0x4cdf, 0x7cfc, 0x4e75)
    a.label('fail_close'); a.words(0x7414); a.branch(0x6000, 'close')
    a.label('fail')
    if report_path:
        a.words(0x7414)
        a.label('report'); a.words(0x2802,0x2c78,4)
        a.address(1,'dos'); a.d(0,0); call(-552)
        a.words(0x2640,0x4a80); a.branch(0x6700,'return_report')
        a.words(0x2c4b); a.address(0,'report_path'); a.words(0x2208)
        a.d(2,1006); call(-30)
        a.words(0x2a00,0x4a80); a.branch(0x6700,'close_dos')
        a.address(0,'status'); a.words(0x2084) # raw stage, zero means all assertions passed
        a.words(0x2205,0x2408); a.d(3,4); call(-48)
        if full:
            a.words(0x2205); a.address(0,'samples'); a.words(0x2408)
            a.d(3,len(samples)*6); call(-48)
        a.words(0x2205); call(-36)
        a.label('close_dos'); a.words(0x224b,0x2c78,4); call(-414)
        a.label('return_report'); a.words(0x2004,0x4cdf,0x7cfc,0x4e75)
    else:
        a.words(0x7014,0x4cdf,0x7cfc,0x4e75)

    # Exec calls tc_TrapCode in supervisor mode with exception number at (SP).
    # Preserve A0 and let the CPU resume at the instruction following DIVU.
    a.label('trap'); a.words(0x2f08, 0x0caf, 0, 5, 4)
    a.branch(0x6600, 'wrong_trap')
    a.address(0, 'traps'); a.words(0x5290); a.branch(0x6000, 'trap_return')
    a.label('wrong_trap'); a.address(0, 'wrong'); a.words(0x5290)
    a.label('trap_return'); a.words(0x205f, 0x588f, 0x4e73)
    a.label('cosine'); a.words(0, 0)
    a.label('traps'); a.words(0, 0)
    a.label('wrong'); a.words(0, 0)
    a.text('name', library_name)
    if full:
        if len(a.code) & 1: a.code.append(0)
        a.label('samples'); a.words(*([0]*len(samples)*3))
    if report_path:
        if len(a.code) & 1: a.code.append(0)
        a.label('status'); a.words(0,0)
        a.text('dos','dos.library'); a.text('report_path',report_path)
    return a.finish()


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--version', type=int, default=0)
    parser.add_argument('--library-name', default='mathtrans.library')
    parser.add_argument('--report-path')
    parser.add_argument('--full', action='store_true')
    args = parser.parse_args()
    args.output.write_bytes(build(args.version,args.library_name,args.report_path,args.full))
