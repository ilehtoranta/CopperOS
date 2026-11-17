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


def build_nonresident_boot_probe():
    """Start the proof Shell from a DOS-owned, nonresident segment list."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'shell'); a.words(0x2208); call(-150)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    # The startup script's current Input is the script file; inherit the
    # parent CLI's standard console input, as the production bootstrap does.
    a.words(0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2040, 0x2028, 172, 0xe588, 0x2040, 0x2828, 28, 0x2c4d)
    a.words(0x4a84); a.branch(0x6600, 'have_input')
    call(-54); a.words(0x2800)
    a.label('have_input')
    call(-60); a.words(0x2e00)  # Output -> D7
    a.address(4, 'tags')
    a.words(0x2946, 4, 0x2944, 36, 0x2947, 44)
    a.address(0, 'name'); a.words(0x2948, 68)
    # Publish the direct-start marker and interactive CLI before scheduling.
    a.words(0x2c78, 4); call(-132)
    a.words(0x2c4d, 0x220c); call(-498)
    a.words(0x2440, 0x4a80); a.branch(0x6700, 'failed_create')
    a.words(0x202a, 172, 0xe588, 0x2040)
    a.words(0x217c, 0xffff, 0xffff, 40, 0x42a8, 44)
    a.words(0x257c, 0x434f, 0x5348, 88)
    a.words(0x2c78, 4); call(-138)
    a.words(0x2c4d, 0x2207); a.address(0, 'banner'); a.words(0x2408)
    a.d(3, len('COPPEROS NONRESIDENT SHELL\n')); call(-48)
    a.words(0x2c78, 4)
    # Keep the boot process and its borrowed console handles alive.
    a.label('wait'); a.words(0x7000); call(-318); a.branch(0x6000, 'wait')
    a.label('failed_create'); a.words(0x2c78, 4); call(-138)
    a.words(0x2c4d, 0x2206); call(-156)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.label('fail'); a.words(0x7014, 0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('shell', 'L:Shell-Seg'); a.text('name', 'CopperOS Shell')
    a.text('banner', 'COPPEROS NONRESIDENT SHELL\n')
    if len(a.code) & 1: a.code.append(0)
    a.label('tags')
    a.code += struct.pack('>20I', 0x800003e9, 0, 0x800003ea, 1,
                          0x800003fa, 1, 0x800003f3, 65536,
                          0x800003ec, 0, 0x800003ed, 0,
                          0x800003ee, 0, 0x800003ef, 0,
                          0x800003f4, 0, 0, 0)
    return a.finish()


def build_prompt_probe():
    """Write the current CLI prompt to a fixed test result file."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c47); a.address(0, 'buffer'); a.words(0x2208); a.d(2, 256); call(-588)
    a.words(0x4a80); a.branch(0x6700, 'close')
    a.address(0, 'output'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x220d); a.address(0, 'buffer'); a.words(0x2408); a.d(3, 7); call(-48)
    a.words(0x220d); call(-36)
    a.label('close'); a.words(0x2247, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('output', 'SYS:newprompt')
    if len(a.code) & 1: a.code.append(0)
    a.label('buffer'); a.code += bytes(256)
    return a.finish()


def build_temp_assign_probe(create_assign=False):
    """Record whether T: can be locked, with requesters suppressed."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    if create_assign:
        a.words(0x2c4d); a.address(0, 'directory'); a.words(0x2208); a.d(2, -2); call(-84)
        a.words(0x4a80); a.branch(0x6700, 'close_dos')
        a.words(0x2c00, 0x2400); a.address(0, 'assign_name'); a.words(0x2208); call(-612)
        a.words(0x4a80); a.branch(0x6600, 'assigned')
        a.words(0x2206); call(-90); a.branch(0x6000, 'close_dos')
        a.label('assigned'); a.words(0x2c78, 4)
    a.words(0x7000, 0x2240); call(-294)  # Exec FindTask(NULL)
    a.words(0x2840, 0x282c, 184, 0x297c, 0xffff, 0xffff, 184)
    a.words(0x2c4d); a.address(0, 'assign'); a.words(0x2208); a.d(2, -2); call(-84)
    a.words(0x2a00, 0x4a80); a.branch(0x6700, 'restore')
    a.words(0x2200); call(-90)
    a.label('restore'); a.words(0x2944, 184)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x2206, 0x4a85); a.branch(0x6700, 'missing')
    a.address(0, 'present'); a.branch(0x6000, 'write')
    a.label('missing'); a.address(0, 'absent')
    a.label('write'); a.words(0x2408); a.d(3, 8); call(-48)
    a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('assign', 'T:'), ('assign_name', 'T'),
                       ('directory', 'SYS:T'), ('path', 'SYS:tassign'),
                       ('present', 'PRESENT\n'), ('absent', 'MISSING\n')]:
        a.text(name, text)
    return a.finish()


def build_append_file_copy_probe():
    """Copy the first output directly through DOS without Shell redirection."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'source'); a.words(0x2208); a.d(2, 1005); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.address(0, 'dest'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_source')
    a.words(0x2206); a.address(0, 'buffer'); a.words(0x2408); a.d(3, 32); call(-42)
    a.words(0x2800, 0x4a80); a.branch(0x6b00, 'close_dest')
    a.words(0x2207); a.address(0, 'buffer'); a.words(0x2408, 0x2604); call(-48)
    a.label('close_dest'); a.words(0x2207); call(-36)
    a.label('close_source'); a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('source', 'SYS:appendfile'); a.text('dest', 'SYS:firstcopy')
    if len(a.code) & 1: a.code.append(0)
    a.label('buffer'); a.code += bytes(32)
    return a.finish()


def build_task_userdata_probe():
    """Capture the current task's user data without changing Shell streams."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2040, 0x2828, 88)
    a.address(3, 'value'); a.words(0x2684)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x240b); a.d(3, 4); call(-48)
    a.words(0x2206); call(-36)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('path', 'SYS:appendtrace')
    if len(a.code) & 1: a.code.append(0)
    a.label('value'); a.code += bytes(4)
    return a.finish()


def build_cli_result_probe(output_path='SYS:rdiagnostics.bin'):
    """Write the previous command's CLI return code and Result2 as longwords."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2040, 0x2028, 172, 0xe588, 0x2040, 0x2828, 12, 0x2a10)
    a.address(3, 'metrics'); a.words(0x2684, 0x2745, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x240b); a.d(3, 8); call(-48)
    a.words(0x2206); call(-36)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('path', output_path)
    if len(a.code) & 1: a.code.append(0)
    a.label('metrics'); a.code += bytes(8)
    return a.finish()


def build_resident_order_probe():
    """Run output on one handle across buffered Echo and the resident listing."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.address(4, 'tags'); a.words(0x2946, 12, 0x240c)
    a.address(0, 'command'); a.words(0x2208); call(-606)
    a.words(0x2800, 0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x2004); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('path', 'SYS:rorder'),
                       ('command', 'Echo BEFORE\nResident\nEcho AFTER\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags'); a.code += struct.pack('>6I', 0x80000024, 1, 0x80000022, 0, 0, 0)
    return a.finish()


def build_path_signal_probe(kind):
    """Reserve, send, or wait for a test-only signal on the proof Shell task."""
    if kind not in ('setup', 'send', 'wait'):
        raise ValueError('Unknown path signal probe')
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    if kind == 'send':
        a.address(1, 'shell'); call(-294)
        a.words(0x4a80); a.branch(0x6700, 'fail')
        a.words(0x2240); a.d(0, 0x10000); call(-324)
        a.words(0x7000); a.branch(0x6000, 'done')
    else:
        if kind == 'setup':
            a.d(0, 16); call(-330)
            # AllocSignal returns a signed byte; the upper register bits are
            # not part of its public result.
            a.words(0x0c00, 16); a.branch(0x6600, 'fail')
        else:
            a.d(0, 0x10000); call(-318)
            a.d(0, 16); call(-336)
        a.address(1, 'dos'); a.d(0, 37); call(-552)
        a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
        a.words(0x2c4d); a.address(0, 'receipt'); a.words(0x2208); a.d(2, 1006); call(-30)
        a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_failure')
        a.words(0x2206); a.address(0, 'text'); a.words(0x2408)
        a.d(3, 6 if kind == 'setup' else 7); call(-48)
        a.words(0x2206); call(-36)
        a.words(0x224d, 0x2c78, 4); call(-414)
        a.words(0x7000); a.branch(0x6000, 'done')
        a.label('close_failure'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    if kind == 'send':
        a.text('shell', 'CopperOS Shell')
    else:
        a.text('dos', 'dos.library')
        a.text('receipt', 'SYS:path-setup-ok' if kind == 'setup' else 'SYS:path-wait-ok')
        a.text('text', 'READY\n' if kind == 'setup' else 'WAITED\n')
    return a.finish()


def build_deferred_segment_probe(path):
    """Record -1 if absent, 0 if deferred, or 1 if a resident is loaded."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.d(4, -1); a.words(0x2c78, 4); call(-132)
    a.words(0x2c4d); a.address(0, 'name'); a.words(0x2208, 0x7400, 0x7600); call(-780)
    a.words(0x4a80); a.branch(0x6700, 'permit')
    a.words(0x2040, 0x2828, 8, 0x4a84); a.branch(0x6700, 'permit')
    a.words(0x7801)
    a.label('permit'); a.words(0x2c78, 4); call(-138)
    a.address(3, 'result'); a.words(0x2684, 0x2c4d)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x240b); a.d(3, 4); call(-48)
    a.words(0x2206); call(-36)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('name', 'Dprobe'); a.text('path', path)
    if len(a.code) & 1: a.code.append(0)
    a.label('result'); a.code += bytes(4)
    return a.finish()


def build_resident_system_probe():
    """Record the public DOS segment use count for the Sysprobe resident."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    # FindSegment is protected by Forbid; retain only the scalar use count.
    a.d(4, -999999); a.words(0x2c78, 4); call(-132)
    a.words(0x2c4d); a.address(0, 'name'); a.words(0x2208, 0x7400, 0x7601); call(-780)
    a.words(0x4a80); a.branch(0x6700, 'permit')
    a.words(0x2040, 0x2828, 4)
    a.label('permit'); a.words(0x2c78, 4); call(-138)
    a.address(3, 'result'); a.words(0x2684, 0x2c4d)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x240b); a.d(3, 4); call(-48)
    a.words(0x2206); call(-36)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('name', 'Sysprobe'); a.text('path', 'SYS:segcount.bin')
    if len(a.code) & 1: a.code.append(0)
    a.label('result'); a.code += bytes(4)
    return a.finish()


def build_run_notice_probe():
    """Capture a synchronous Shell's output around an empty background Run."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x7814, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.address(4, 'tags'); a.words(0x2946, 12, 0x240c)
    a.address(0, 'command'); a.words(0x2208); call(-606)
    a.words(0x2800, 0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x2004); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('path', 'SYS:runnotice'),
                       ('command', 'Run\nEcho AFTER\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags'); a.code += struct.pack('>6I', 0x80000024, 1, 0x80000022, 0, 0, 0)
    return a.finish()


def build_control_script(padding_lines=0):
    """Guest script exercising arguments, control flow, state and streams."""
    if not 0 <= padding_lines <= 1024:
        raise ValueError('Control-script padding must be between 0 and 1024 lines')
    header = b'.key name/A,option/K\n.def option fallback\n.bra {\n.ket }\n'
    padding = (b'; Execute buffered-copy boundary ' + b'x' * 64 + b'\n') * padding_lines
    return (header + padding + b'Echo >SYS:ctl-arguments "{name} {option}"\n'
            b'If "{name}" EQ "world"\nEcho >SYS:ctl-true yes\n'
            b'Else\nEcho >SYS:ctl-wrong wrong\nEndIf\n'
            b'If "x" EQ "y"\nEcho >SYS:ctl-wrong wrong\n'
            b'Else\nEcho >SYS:ctl-else yes\nEndIf\n'
            b'Skip target\nEcho >SYS:ctl-skipped wrong\nLab target\n'
            b'Set Local "two words"\nGet Local >SYS:ctl-variable\n'
            b'Alias Mark Echo alias\nMark >SYS:ctl-alias\n'
            b'Echo alpha >SYS:ctl-redirection\nEcho beta >>SYS:ctl-redirection\n'
            b'Echo >SYS:ctl-childdone done\n')


def build_many_path_script():
    """Exercise more than 64 Path arguments within the Shell workspace."""
    return (b'FailAt 21\nPath RESET\nPath ' + b'SYS:S ' * 65 +
            b'ADD\nPath >SYS:pathmany\nEcho >SYS:pathmanydone done\n')


def build_run_stream_probe():
    """Record a delayed background output write's result and IoErr."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); call(-60); a.words(0x2e00)  # save Output()
    a.d(1, 250); call(-198)                      # outlive RUN's return
    a.words(0x2207); a.address(0, 'payload'); a.words(0x2408)
    a.d(3, 6); call(-48); a.words(0x2a00)       # D5 = Write result
    call(-132); a.words(0x2c00)                 # D6 = IoErr
    a.address(0, 'record'); a.words(0x2085, 0x2146, 4)
    a.words(0x2207); a.address(0, 'namebuffer'); a.words(0x2408)
    a.d(3, 256); call(-408)                      # NameFromFH(Output())
    a.address(0, 'record'); a.words(0x2140, 8, 0x2147, 12)
    a.words(0x2207); call(-216)                  # IsInteractive(Output())
    a.address(0, 'record'); a.words(0x2140, 16)
    a.words(0x2007, 0xe588, 0x2040, 0x2028, 8)  # Output()->fh_Type
    a.address(0, 'record'); a.words(0x2140, 20)
    a.address(0, 'nil'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2800, 0x4a80); a.branch(0x6700, 'after_nil')
    a.words(0x2004, 0xe588, 0x2040, 0x2028, 8)
    a.address(0, 'record'); a.words(0x2140, 24)
    a.words(0x2204); call(-36)
    a.label('after_nil')
    a.address(0, 'report'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2800, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2204); a.address(0, 'record'); a.words(0x2408)
    a.d(3, 28); call(-48); a.words(0x2204); call(-36)
    a.address(0, 'namepath'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2800, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2204); a.address(0, 'namebuffer'); a.words(0x2408)
    a.d(3, 256); call(-48); a.words(0x2204); call(-36)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('report', 'SYS:streamreport')
    a.text('namepath', 'SYS:streamname'); a.text('nil', 'NIL:')
    a.text('payload', 'CHILD\n')
    if len(a.code) & 1: a.code.append(0)
    a.label('record'); a.code += bytes(28)
    a.label('namebuffer'); a.code += bytes(256)
    return a.finish()


def build_system_run_output_probe(leading=True, parent_state=False, nested=False, console_input=False):
    """Keep SYS_Output open while a System-launched Run child reports."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4, 0x7814)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'nil'); a.words(0x2208); a.d(2, 1005); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.address(0, 'shared'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_input')
    a.address(4, 'tags'); a.words(0x2946, 12, 0x2947, 20, 0x240c)
    a.address(0, 'command'); a.words(0x2208); call(-606); a.words(0x2800)
    a.d(1, 350); call(-198)
    # The caller still owns SYS_Output after System() and the worker finish.
    a.words(0x2207); a.address(0, 'parent'); a.words(0x2408); a.d(3, 7); call(-48)
    a.words(0x0c80, 0, 7); a.branch(0x6700, 'output_ok')
    a.words(0x7814)
    a.label('output_ok')
    a.words(0x2207); call(-36)
    a.label('close_input'); a.words(0x2206); call(-36)
    a.address(0, 'donepath'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x2206, 0x4a84); a.branch(0x6600, 'bad')
    a.address(0, 'ok'); a.d(3, 3); a.branch(0x6000, 'write')
    a.label('bad'); a.address(0, 'badtext'); a.d(3, 4)
    a.label('write'); a.words(0x2408); call(-48); a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, value in [('dos', 'dos.library'), ('nil', '*' if console_input else 'NIL:'),
                        ('shared', 'SYS:runshared'), ('donepath', 'SYS:systemdone'),
                        ('command', ('C:ParentProbe\n' if parent_state else '') +
                         ('Run >SYS:runprefix ' if leading else 'Run ') +
                         ('Run ' if nested else '') + 'C:StreamProbe\n'),
                        ('parent', 'PARENT\n'),
                        ('ok', 'OK\n'), ('badtext', 'BAD\n')]:
        a.text(name, value)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags'); a.code += struct.pack('>8I', 0x80000024, 1, 0x80000021, 0,
                                            0x80000022, 0, 0, 0)
    return a.finish()


def build_run_parent_state_probe():
    """Record the launching Shell's public Process and CLI stream fields."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2040)
    a.address(3, 'record')
    for index, offset in enumerate((164, 156, 160)):
        a.words(0x2028, offset, 0x2740, index * 4)
    a.words(0x2028, 172, 0xe588, 0x2040)
    for index, offset in enumerate((28, 32, 40, 44, 48, 56), start=3):
        a.words(0x2028, offset, 0x2740, index * 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x240b); a.d(3, 36); call(-48)
    a.words(0x2206); call(-36)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('path', 'SYS:parentstate')
    if len(a.code) & 1: a.code.append(0)
    a.label('record'); a.code += bytes(36)
    return a.finish()


def build_run_shared_error_probe(pre_only=False):
    """Delay and write to both Output() and the public process error stream."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2a40)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4c); a.address(0, 'entrypath'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'delay')
    a.words(0x2206); a.address(0, 'entrytext'); a.words(0x2408); a.d(3, 6); call(-48)
    a.words(0x2206); call(-36)
    a.label('delay'); a.d(1, 250); call(-198)
    a.address(3, 'record')
    call(-60); a.words(0x2200); a.address(0, 'out'); a.words(0x2408); a.d(3, 4); call(-48)
    # Publish the output result and raw error handle before attempting to use
    # that handle: the original can fail or stop during the error write.
    a.words(0x2680, 0x202d, 224, 0x2740, 4)
    a.address(0, 'prepath'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'write_error')
    a.words(0x2206, 0x240b); a.d(3, 8); call(-48)
    a.words(0x2206); call(-36)
    a.label('write_error')
    if pre_only: a.branch(0x6000, 'close')
    a.words(0x222d, 224); a.address(0, 'err'); a.words(0x2408); a.d(3, 4); call(-48)
    a.words(0x2740, 4)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x240b); a.d(3, 8); call(-48)
    a.words(0x2206); call(-36)
    a.label('close'); a.words(0x224c, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('path', 'SYS:sharedreport'); a.text('prepath', 'SYS:sharedpre')
    a.text('entrypath', 'SYS:sharedentry'); a.text('entrytext', 'ENTER\n')
    a.text('out', 'OUT\n'); a.text('err', 'ERR\n')
    if len(a.code) & 1: a.code.append(0)
    a.label('record'); a.code += bytes(8)
    return a.finish()


def build_run_input_probe():
    """Copy one small unbuffered read from the inherited input to output."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); call(-54); a.words(0x2200)
    a.address(0, 'buffer'); a.words(0x2408); a.d(3, 32); call(-42)
    a.words(0x2800, 0x4a80); a.branch(0x6b00, 'close')
    call(-60); a.words(0x2200); a.address(0, 'buffer'); a.words(0x2408, 0x2604); call(-48)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library')
    if len(a.code) & 1: a.code.append(0)
    a.label('buffer'); a.code += bytes(32)
    return a.finish()


def build_run_delayed_probe(delay_ticks=150, observe_followup=False, publish_sentinel=False,
                            poll_followup=False, verify_parent_retired=False):
    """Verify the inherited standard output remains open after a delay."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d)
    if observe_followup:
        # Signal only after the child is inside RunCommand, then hold that
        # invocation open while the injector types the parent's next line.
        a.address(0, 'started_path'); a.words(0x2208); a.d(2, 1006); call(-30)
        a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
        a.words(0x2206); a.address(0, 'started_text'); a.words(0x2408); a.d(3, 6); call(-48)
        a.words(0x2206); call(-36)
        # Record whether the command and interactive Shell use the same DOS
        # input handle. The Shell is waiting or ready while this child runs.
        call(-54); a.words(0x2800, 0x2c78, 4)
        a.address(1, 'parent_task'); call(-294); a.words(0x4a80); a.branch(0x6700, 'identity_none')
        a.words(0x2040, 0x2028, 156, 0xb084); a.branch(0x6700, 'identity_same')
        a.address(4, 'identity_diff'); a.branch(0x6000, 'identity_report')
        a.label('identity_same'); a.address(4, 'identity_same_text'); a.branch(0x6000, 'identity_report')
        a.label('identity_none'); a.address(4, 'identity_none_text')
        a.label('identity_report'); a.words(0x2c4d); a.address(0, 'identity_path'); a.words(0x2208)
        a.d(2, 1006); call(-30); a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
        a.words(0x2206, 0x240c); a.d(3, 5); call(-48)
        a.words(0x2206); call(-36)
        if publish_sentinel:
            # A test emulator can observe this public guest-memory marker
            # without depending on host filesystem access from the guest.
            a.address(0, 'run_sentinel')
            for value in (0x55c0ffee, 0x0badbeef, 0x12cafe34, 0x5eedface):
                a.words(0x20fc, value >> 16, value)
    if not poll_followup:
        a.d(1, delay_ticks); call(-198)
    if observe_followup:
        if poll_followup:
            a.d(4, 50000)
            a.label('followup_retry'); a.address(0, 'after_path'); a.words(0x2208); a.d(2, 1005); call(-30)
            a.words(0x4a80); a.branch(0x6600, 'followup_found')
            a.words(0x7201); call(-198); a.words(0x5384); a.branch(0x6600, 'followup_retry')
            a.address(4, 'late_text'); a.branch(0x6000, 'followup_report')
            a.label('followup_found'); a.words(0x2200); call(-36); a.address(4, 'early_text')
        else:
            a.address(0, 'after_path'); a.words(0x2208); a.d(2, 1005); call(-30)
            a.words(0x4a80); a.branch(0x6700, 'followup_late')
            a.words(0x2200); call(-36)
            a.address(4, 'early_text'); a.branch(0x6000, 'followup_report')
            a.label('followup_late'); a.address(4, 'late_text')
        a.label('followup_report')
        if publish_sentinel:
            a.address(0, 'run_sentinel'); a.words(0x4298, 0x4298, 0x4298, 0x4298)
        a.address(0, 'overlap_path'); a.words(0x2208); a.d(2, 1006); call(-30)
        a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
        a.words(0x2206, 0x240c); a.d(3, 6); call(-48)
        a.words(0x2206); call(-36)
    if verify_parent_retired:
        a.words(0x2c78, 4); a.address(1, 'parent_task'); call(-294)
        a.words(0x4a80); a.branch(0x6700, 'parent_gone')
        a.address(4, 'parent_alive'); a.branch(0x6000, 'parent_report')
        a.label('parent_gone'); a.address(4, 'parent_ended')
        a.label('parent_report'); a.words(0x2c4d); a.address(0, 'parent_path'); a.words(0x2208)
        a.d(2, 1006); call(-30); a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
        a.words(0x2206, 0x240c); a.d(3, 6); call(-48)
        a.words(0x2206); call(-36)
    a.words(0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2040, 0x2028, 172, 0xe588, 0x2040, 0x2228, 56, 0x2c4d)
    a.address(0, 'alive'); a.words(0x2408); a.d(3, 6); call(-48)
    a.words(0x0c80, 0, 6); a.branch(0x6600, 'invalid_stream')
    call(-60); a.words(0x2200); a.address(0, 'output'); a.words(0x2408); a.d(3, 7); call(-48)
    a.branch(0x6000, 'close')
    a.label('invalid_stream')
    call(-60); a.words(0x2200); a.address(0, 'invalid'); a.words(0x2408); a.d(3, 8); call(-48)
    a.label('close')
    a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('output', 'nested\n'); a.text('invalid', 'INVALID\n')
    a.text('alive', 'ALIVE\n')
    if observe_followup:
        a.text('started_path', 'SYS:delay-started'); a.text('started_text', 'START\n')
        a.text('after_path', 'SYS:afterrun'); a.text('overlap_path', 'SYS:overlap')
        a.text('early_text', 'EARLY\n'); a.text('late_text', 'LATE!\n')
        a.text('identity_path', 'SYS:input-identity')
        a.text('identity_same_text', 'SAME\n'); a.text('identity_diff', 'DIFF\n')
        a.text('identity_none_text', 'NONE\n')
        if publish_sentinel:
            if len(a.code) & 1: a.code.append(0)
            a.label('run_sentinel'); a.code += bytes(16)
    if observe_followup or verify_parent_retired:
        a.text('parent_task', 'CopperOS Shell')
    if verify_parent_retired:
        a.text('parent_path', 'SYS:parent-state'); a.text('parent_alive', 'ALIVE\n')
        a.text('parent_ended', 'ENDED\n')
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


def build_system_async_probe(null_command=False):
    """Start an asynchronous SYS_UserShell with independently owned handles."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    def open_nil(mode):
        a.address(0, 'nil'); a.words(0x2208); a.d(2, mode); call(-30)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d)
    if null_command:
        a.address(0, 'input'); a.words(0x2208); a.d(2, 1005); call(-30)
    else:
        open_nil(1005)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    open_nil(1006); a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_input')
    a.address(4, 'tags'); a.words(0x2946, 20, 0x2947, 28)
    if null_command:
        a.d(1, 0); a.words(0x240c); call(-606)
    else:
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
    for name, text in [('dos', 'dos.library'), ('nil', 'NIL:'),
                       ('input', 'SYS:S/sysnull'),
                       ('path', 'SYS:nullresult.txt' if null_command else 'SYS:asyncresult.txt'),
                       ('command', 'Echo >SYS:asyncchild.txt async\n'), ('ok', 'OK\n'), ('bad_text', 'BAD\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    # System(NULL) must return asynchronously even without SYS_Async.
    a.label('tags'); a.code += struct.pack('>10I', 0x80000024, 1, 0x80000023, 0 if null_command else 1,
                                          0x80000021, 0, 0x80000022, 0, 0, 0)
    return a.finish()


def build_system_input_probe():
    """Require a System command to read, and leave open, its SYS_Input."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d)
    a.address(0, 'input'); a.words(0x2208); a.d(2, 1005); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.address(4, 'tags'); a.words(0x2946, 12)
    a.address(0, 'command'); a.words(0x2208, 0x240c); call(-606)
    a.words(0x4a80); a.branch(0x6600, 'bad')
    # Seek and read after System returns to prove the caller's handle is open.
    a.words(0x2206); a.d(2, 0); a.d(3, -1); call(-66)
    a.words(0x4a80); a.branch(0x6b00, 'bad')
    a.words(0x2206); call(-306); a.words(0x0c80, 0, ord('I'))
    a.branch(0x6600, 'bad')
    a.address(4, 'ok'); a.d(4, 3); a.branch(0x6000, 'report')
    a.label('bad'); a.address(4, 'bad_text'); a.d(4, 4)
    a.label('report'); a.address(0, 'result'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_input')
    a.words(0x2207, 0x240c, 0x2604); call(-48)
    a.words(0x2207); call(-36)
    a.label('close_input'); a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, value in [('dos', 'dos.library'), ('input', 'SYS:S/sysinput'),
                        ('command', 'C:ReadProbe >SYS:sysread\n'),
                        ('result', 'SYS:sysinputresult'), ('ok', 'OK\n'), ('bad_text', 'BAD\n')]:
        a.text(name, value)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags'); a.code += struct.pack('>6I', 0x80000024, 1, 0x80000021, 0, 0, 0)
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


def build_segment_layout_probe():
    """Capture the public Process segment-array words from a live command."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2040, 0x2028, 128, 0xe588, 0x2800, 0x2840, 0x4a84); a.branch(0x6700, 'fail')
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x240c); a.d(3, 20); call(-48)
    a.words(0x2206); call(-36)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('path', 'SYS:segments.bin')
    return a.finish()


def build_nonresident_swap_probe(restore=False, missing=False):
    """Swap a disposable fixture's installed Shell and alternate image."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d)
    moves = ((('saved', 'installed'),) if restore else (('installed', 'saved'),)) if missing else (
        (('installed', 'alternate'), ('saved', 'installed')) if restore else
        (('installed', 'saved'), ('alternate', 'installed')))
    for src, dst in moves:
        a.address(0, src); a.words(0x2208); a.address(0, dst); a.words(0x2408); call(-78)
        a.words(0x4a80); a.branch(0x6700, 'close_fail')
    a.words(0x7800); a.branch(0x6000, 'close')
    a.label('close_fail'); a.words(0x7814)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414); a.words(0x2004)
    a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('installed', 'L:Shell-Seg'),
                       ('saved', 'L:SavedShell'), ('alternate', 'L:AltShell')]:
        a.text(name, text)
    return a.finish()


def build_nonresident_memory_probe(path):
    """Wait for the background worker to retire, then record public memory."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.d(4, 500)
    a.label('wait'); a.words(0x2c78, 4); a.address(1, 'worker'); call(-294)
    a.words(0x4a80); a.branch(0x6700, 'settle')
    a.words(0x2c4d, 0x7201); call(-198)
    a.words(0x5384); a.branch(0x6600, 'wait')
    a.words(0x7814); a.branch(0x6000, 'close_dos')
    a.label('settle'); a.words(0x2c4d); a.d(1, 50); call(-198)
    a.words(0x2c78, 4, 0x7201); call(-216)
    a.address(3, 'sample'); a.words(0x2680, 0x2c4d)
    a.address(0, 'path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'failed_write')
    a.words(0x2206, 0x240b); a.d(3, 4); call(-48)
    a.words(0x2206); call(-36); a.words(0x7800); a.branch(0x6000, 'close_dos')
    a.label('failed_write'); a.words(0x7814)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x2004); a.branch(0x6000, 'done')
    a.label('fail'); a.words(0x7014)
    a.label('done'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('worker', 'CopperOS background Shell')
    a.text('path', path)
    if len(a.code) & 1: a.code.append(0)
    a.label('sample'); a.code += bytes(4)
    return a.finish()


def build_run_lifetime_probe(owned_output=False, nested=False, console_input=False,
                             new_shell=False):
    """Sample public memory after repeated public Shell children have exited."""
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
    batch(8 if new_shell else 3, 'warmup'); sample(0)
    batch(5, 'first_batch'); sample(4)
    batch(5, 'second_batch'); sample(8)
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
    a.address(0, 'shared'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_failed_input')
    a.address(4, 'tags'); a.words(0x2946, 12, 0x2947, 20, 0x240c)
    a.address(0, 'command'); a.words(0x2208); call(-606)
    a.words(0x2a00)  # Preserve System's result in D5.
    a.address(2, 'attempts'); a.d(0, 250); a.words(0x2480)
    a.label('wait_worker')
    a.words(0x2c78, 4); a.address(1, 'worker'); call(-294)
    a.words(0x4a80); a.branch(0x6700, 'settle')
    a.words(0x2c4d, 0x7201); call(-198)
    a.words(0x5392); a.branch(0x6600, 'wait_worker')
    a.words(0x7a14)  # A worker that did not exit is a failed batch.
    a.label('settle'); a.words(0x2c4d); a.d(1, 50 if new_shell else 5); call(-198)
    a.words(0x2207); call(-36)
    a.words(0x2206); call(-36)
    a.words(0x2005, 0x4e75)
    a.label('close_failed_input'); a.words(0x2206); call(-36)
    a.label('run_failed'); a.words(0x7001, 0x4e75)
    for name, text in [('dos', 'dos.library'), ('nil', '*' if console_input else 'NIL:'), ('shared', 'SYS:runshared'),
                       ('path', 'SYS:runlife.bin'), ('worker', 'CopperOS child Shell' if new_shell else 'CopperOS background Shell'),
                       ('command', 'NewShell FROM S:lifestart\n' if new_shell else
                                   'Run >SYS:runowned Run Echo hi >NIL:\n' if nested else
                                   'Run >SYS:runowned Echo hi\n' if owned_output else 'Run Echo hi\n')]:
        a.text(name, text)
    if len(a.code) & 1: a.code.append(0)
    a.label('metrics'); a.code += bytes(12)
    a.label('attempts'); a.code += bytes(4)
    a.label('tags'); a.code += struct.pack('>8I', 0x80000024, 1,
                                          0x80000021, 0, 0x80000022, 0, 0, 0)
    return a.finish()
