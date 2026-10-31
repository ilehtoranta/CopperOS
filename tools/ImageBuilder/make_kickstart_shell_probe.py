"""Build a test-only 68000 command that runs Shell-Seg with file streams."""
import importlib.util
import struct
from pathlib import Path

path = Path(__file__).resolve().parents[1] / 'Locale.Workbench' / 'make_probe.py'
spec = importlib.util.spec_from_file_location('locale_probe', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def build(boot_path=False):
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    def open_file(label, mode):
        a.address(0, label); a.words(0x2208); a.d(2, mode); call(-30)

    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d)
    open_file('input', 1005)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    open_file('output', 1006)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_input')
    a.address(0, 'shell'); a.words(0x2208); call(-150)
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'close_output')
    a.words(0x2206); call(-294); a.words(0x2440)
    a.words(0x2207); call(-300); a.words(0x2640)
    if boot_path:
        # Give BootShell a file as its CLI's standard input. Its normal child
        # creation path then supplies that handle to the real Shell process.
        a.words(0x2c78, 4, 0x7000, 0x2240); call(-294)
        a.words(0x2040, 0x2028, 172, 0xe588, 0x2040, 0x2146, 28, 0x2c4d)
    else:
        # Direct RunCommand is not a DOS-created Shell and has no startup
        # packet. Mark that temporary entry and restore the caller's userdata.
        a.words(0x2c78, 4, 0x7000, 0x2240); call(-294)
        a.words(0x2040, 0x2a28, 88, 0x217c, 0x434f, 0x5348, 88, 0x2c4d)
    a.words(0x220c); a.d(2, 65536); a.address(0, 'arguments')
    a.words(0x2608, 0x7801); call(-504)
    if not boot_path:
        a.words(0x2c78, 4, 0x7000, 0x2240); call(-294)
        a.words(0x2040, 0x2145, 88, 0x2c4d)
    a.words(0x220a); call(-294)
    a.words(0x220b); call(-300)
    a.words(0x220c); call(-156)
    a.label('close_output'); a.words(0x2207); call(-36)
    a.label('close_input'); a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('input', 'SYS:S/shell-probe-input'),
                       ('output', 'SYS:shell-probe-output'), ('shell', 'C:BootShell' if boot_path else 'L:Shell-Seg'),
                       ('arguments', '\n')]:
        a.text(name, text)
    return a.finish()


def build_status_probe():
    """Report the CLI status left by a missing command, before returning."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4, 0x7000, 0x2240)
    call(-294)                          # Exec FindTask(NULL)
    a.words(0x2040, 0x2028, 172, 0xe588, 0x2040)  # process CLI BPTR to A0
    a.words(0x7800)                    # D4 = success
    a.words(0x0ca8, 0, 10, 12)        # CLI ReturnCode == 10
    a.branch(0x6600, 'bad')
    a.words(0x0c90, 0, 205)           # CLI Result2 == OBJECT_NOT_FOUND
    a.branch(0x6700, 'report')
    a.label('bad'); a.words(0x7801)
    a.label('report'); a.words(0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x2206, 0x4a84); a.branch(0x6600, 'bad_text')
    a.address(0, 'ok'); a.d(3, 3); a.branch(0x6000, 'write')
    a.label('bad_text'); a.address(0, 'bad_value'); a.d(3, 4)
    a.label('write'); a.words(0x2408); call(-48)
    a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return_status')
    a.label('fail'); a.words(0x7014)
    a.label('return_status'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('path', 'SYS:shell-status.txt'),
                       ('ok', 'OK\n'), ('bad_value', 'BAD\n')]:
        a.text(name, text)
    return a.finish()


def build_system_probe(command='Path >SYS:systemchild.txt\n', result_path='SYS:systemresult.txt'):
    """Invoke DOS SystemTagList and record whether its child returned zero."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail_system')
    a.words(0x2c4d); a.address(0, 'system_tags'); a.words(0x2408)
    a.address(0, 'command'); a.words(0x2208); call(-606)
    a.words(0x2800)  # Save SystemTagList result across DOS calls.
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_system_dos')
    a.words(0x2206, 0x4a84); a.branch(0x6600, 'bad_system')
    a.address(0, 'ok_system'); a.d(3, 3); a.branch(0x6000, 'write_system')
    a.label('bad_system'); a.address(0, 'bad_value_system'); a.d(3, 4)
    a.label('write_system'); a.words(0x2408); call(-48)
    a.words(0x2206); call(-36)
    a.label('close_system_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return_system')
    a.label('fail_system'); a.words(0x7014)
    a.label('return_system'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('path', result_path),
                       ('command', command),
                       ('ok_system', 'OK\n'), ('bad_value_system', 'BAD\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    a.label('system_tags'); a.code += struct.pack('>4I', 0x80000024, 1, 0, 0)
    return a.finish()


def build_system_failure_probe():
    """Require SYS_UserShell to return RC 10 and IoErr 205 for a missing command."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'tags'); a.words(0x2408)
    a.address(0, 'command'); a.words(0x2208); call(-606)
    a.words(0x2800); call(-132); a.words(0x2a00)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x0c84, 0, 10); a.branch(0x6600, 'bad')
    a.words(0x0c85, 0, 205); a.branch(0x6600, 'bad')
    a.address(0, 'ok'); a.d(3, 3); a.branch(0x6000, 'write')
    a.label('bad'); a.address(0, 'bad_text'); a.d(3, 4)
    a.label('write'); a.words(0x2206, 0x2408); call(-48)
    a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('path', 'SYS:systemfailure.txt'),
                       ('command', 'CopperOSNoSuchCommand\n'), ('ok', 'OK\n'), ('bad_text', 'BAD\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags'); a.code += struct.pack('>4I', 0x80000024, 1, 0, 0)
    return a.finish()


def build_system_async_probe():
    """Start SYS_UserShell asynchronously with its own NIL: handles."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    def open_nil(mode):
        a.address(0, 'nil'); a.words(0x2208); a.d(2, mode); call(-30)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); open_nil(1005)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    open_nil(1006); a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_input')
    a.address(4, 'tags'); a.words(0x2946, 20, 0x2947, 28)
    a.address(0, 'command'); a.words(0x2208, 0x240c); call(-606)
    a.words(0x2800, 0x4a84); a.branch(0x6700, 'wait_child')
    a.words(0x2207); call(-36); a.words(0x2206); call(-36)
    a.label('wait_child'); a.d(1, 100); call(-198)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x2206, 0x4a84); a.branch(0x6600, 'bad')
    a.address(0, 'ok'); a.d(3, 3); a.branch(0x6000, 'write')
    a.label('bad'); a.address(0, 'bad_text'); a.d(3, 4)
    a.label('write'); a.words(0x2408); call(-48)
    a.words(0x2206); call(-36); a.branch(0x6000, 'close_dos')
    a.label('close_input'); a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('nil', 'NIL:'), ('path', 'SYS:asyncresult.txt'),
                       ('command', 'Echo >SYS:asyncchild.txt async\n'), ('ok', 'OK\n'), ('bad_text', 'BAD\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags'); a.code += struct.pack('>10I', 0x80000024, 1, 0x80000023, 1,
                                          0x80000021, 0, 0x80000022, 0, 0, 0)
    return a.finish()


def build_system_lifetime_probe():
    """Measure public free memory after warmup and two equal async batches."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    def batch(count, loop):
        a.words(0x7800 | count)
        a.label(loop); a.branch(0x6100, 'run_one')
        a.words(0x4a80); a.branch(0x6600, 'close_dos')
        a.words(0x5384); a.branch(0x6600, loop)
    def sample(offset):
        a.words(0x2c78, 4, 0x7201); call(-216)
        a.words(0x2740, offset, 0x2c4d)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(3, 'metrics')
    batch(3, 'warmup'); sample(0)
    batch(10, 'first_batch'); sample(4)
    batch(10, 'second_batch'); sample(8)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x2206, 0x240b); a.d(3, 12); call(-48)
    a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.label('run_one')
    a.address(0, 'nil'); a.words(0x2208); a.d(2, 1005); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'run_failed')
    a.address(0, 'nil'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_failed_input')
    a.address(4, 'tags'); a.words(0x2946, 20, 0x2947, 28)
    a.address(0, 'command'); a.words(0x2208, 0x240c); call(-606)
    a.words(0x4a80); a.branch(0x6700, 'run_wait')
    a.words(0x2207); call(-36)
    a.label('close_failed_input'); a.words(0x2206); call(-36)
    a.label('run_failed'); a.words(0x7001, 0x4e75)
    a.label('run_wait'); a.d(1, 20); call(-198)
    a.words(0x7000, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('nil', 'NIL:'), ('path', 'SYS:asynclife.bin'),
                       ('command', 'Path >NIL:\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    a.label('metrics'); a.code += bytes(12)
    a.label('tags'); a.code += struct.pack('>10I', 0x80000024, 1, 0x80000023, 1,
                                          0x80000021, 0, 0x80000022, 0, 0, 0)
    return a.finish()
