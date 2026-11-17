"""Real FFS allocation pressure for Execute's source and caller-tail copies."""

from make_kickstart_shell_probe import module


def build_execute_probe():
    """Run packaged Execute with DOS requesters suppressed, then restore them.

    No DOS vector or filesystem operation is replaced by this test command.
    """
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'execute'); a.words(0x2208); call(-150)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2c78, 4, 0x7000, 0x2240); call(-294)
    a.words(0x2840, 0x2e2c, 184, 0x297c, 0xffff, 0xffff, 184)
    a.words(0x2c4d, 0x2206); a.d(2, 65536)
    a.address(0, 'arguments'); a.words(0x2608); a.d(4, len('S:execfull\n')); call(-504)
    a.words(0x2800); call(-132); a.words(0x2a00, 0x2947, 184)
    a.words(0x2206); call(-156)
    a.words(0x2205); call(-462)
    a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x2004); a.branch(0x6000, 'return')
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('execute', 'C:Execute')
    a.text('arguments', 'S:execfull\n')
    return a.finish()


def file_blocks(size):
    data = (size + 511) // 512
    return 1 + data + ((data - 1) // 72 if data else 0)


def scripts(stage):
    padding = (b'; Execute disk-full copy boundary ' + b'x' * 64 + b'\n') * 256
    if stage == 'source':
        source = padding
        caller = (b'FailAt 21\nExecFull\nCapFull\n'
                  b'Echo >SYS:execute-full-done survived\n'
                  b'Echo >SYS:execute-full-signal done\nEcho SCRIPT DONE\n')
    elif stage == 'tail':
        source = b'; Minimal child script\n'
        # The console observer must not begin its exit allowance while the
        # caller still has buffered lines to consume.
        caller = (b'FailAt 21\nExecFull\nCapFull\n' + padding +
                  b'Echo >SYS:execute-full-done survived\n'
                  b'Echo >SYS:execute-full-signal done\nEcho SCRIPT DONE\n')
    else:
        raise ValueError('Expected source or tail Execute failure stage')
    return source, caller


def fill_volume(disk, caller, reserve=32):
    # The harness enters the caller via Execute too. Leave space for that
    # outer work file; pressure must reach the nested copy under test.
    reserve += file_blocks(len(caller))
    available = disk.blocks - len(disk.used)
    budget = available - reserve
    data = budget - 1
    while data > 0 and file_blocks(data * 512) > budget:
        data -= 1
    if data <= 0:
        raise ValueError('Fixture cannot reserve enough free filesystem blocks')
    disk.file(disk.root, 'execute-fill', bytes(data * 512))
    remaining = disk.blocks - len(disk.used)
    if not reserve <= remaining <= reserve + 1:
        raise ValueError('Unexpected FFS allocation pressure')
    return remaining
