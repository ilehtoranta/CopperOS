"""Test-only HUNKs that inject a command through the guest input device."""
import importlib.util
from pathlib import Path
import struct

path = Path(__file__).resolve().parents[1] / 'Locale.Workbench' / 'make_probe.py'
spec = importlib.util.spec_from_file_location('locale_probe', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def parent(child_path='C:KeyChild'):
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'child'); a.words(0x2208); call(-150)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.address(4, 'tags'); a.words(0x2946, 4, 0x220c); call(-498)
    a.words(0x4a80); a.branch(0x6600, 'close')
    a.words(0x2206); call(-156)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('child', child_path)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags')
    a.code += struct.pack('>8I', 0x800003e9, 0, 0x800003ea, 1,
                          0x800003f3, 8192, 0, 0)
    return a.finish()


def exit_observer(final_delay_ticks=950):
    """Independently observe Shell exit, even if the injector does not return."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c47); a.address(0, 'started'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x220d); a.address(0, 'started_text'); a.words(0x2408); a.d(3, 6); call(-48)
    a.words(0x220d); call(-36)
    # Record an intermediate wakeup as well as the final observation, so a
    # stopped observer is distinguishable from a Shell that is still alive.
    a.d(1, 300); call(-198)
    a.address(0, 'tick'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x220d); a.address(0, 'tick_text'); a.words(0x2408); a.d(3, 5); call(-48)
    a.words(0x220d); call(-36)
    # Start before BootShell and allow the other process time to inject EndCLI.
    a.d(1, final_delay_ticks); call(-198)
    a.words(0x2c78, 4); a.address(1, 'shell_task'); call(-294)
    a.words(0x4a80); a.branch(0x6700, 'gone')
    a.address(4, 'alive_text'); a.branch(0x6000, 'write_result')
    a.label('gone'); a.address(4, 'ended_text')
    a.label('write_result'); a.words(0x2c47); a.address(0, 'result'); a.words(0x2208)
    a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x220d, 0x240c); a.d(3, 6); call(-48)
    a.words(0x220d); call(-36)
    a.label('close'); a.words(0x2247, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('shell_task', 'CopperOS Shell')
    a.text('started', 'SYS:observer-started.txt'); a.text('started_text', 'BEGIN\n')
    a.text('tick', 'SYS:observer-tick.txt'); a.text('tick_text', 'TICK\n')
    a.text('result', 'SYS:observer-result.txt')
    a.text('alive_text', 'ALIVE\n'); a.text('ended_text', 'ENDED\n')
    return a.finish()


def child(commands=None, check_shell_exit=False, record_shell_alive=False, line_delay_ticks=0,
          wait_after_first_line=False, capture_second_alert=False, wait_for_path=None,
          wait_after_line=1):
    # Amiga US raw-key codes and qualifiers for two interactive commands.
    raw = {'a': 0x20, 'b': 0x35, 'c': 0x33, 'd': 0x22, 'e': 0x12,
           'f': 0x23, 'g': 0x24, 'h': 0x25, 'i': 0x17, 'k': 0x27,
           'l': 0x28, 'm': 0x37, 'n': 0x36, 'o': 0x18, 'p': 0x19,
           'q': 0x10, 'r': 0x13, 's': 0x21, 't': 0x14, 'u': 0x16, 'v': 0x34, 'w': 0x11,
           'x': 0x32, 'y': 0x15, '0': 0x0a, '1': 0x01, '2': 0x02,
           '3': 0x03, '4': 0x04, '5': 0x05, '6': 0x06, '7': 0x07, '8': 0x08, '9': 0x09,
           '/': 0x3a, ' ': 0x40, '.': 0x39, '-': 0x0b, '\n': 0x44}
    keys = bytearray()
    if commands is None:
        commands = ('echo >interactive.txt hello\nmakedir interactiveprobe\n'
                    'eval 1 + 2 >eval.txt\nversion c:eval file >version.txt\n'
                    'dir interactiveprobe >dir.txt\ncopperosnosuchcommand\n'
                    'shellstatus\necho >>interactive.txt again\n'
                    'makedir removeme\ndir >before.txt\ndelete removeme\n'
                    'echo >finish.txt done\n')
    for c in commands:
        keys.extend((0x39, 1) if c == '>' else (0x38, 1) if c == '<' else (0x0c, 1) if c == '+' else
                    (0x29, 1) if c == ':' else (0x2a, 1) if c == '"' else (raw[c], 0))
    keys.extend((0xff, 0))
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'fail'); a.words(0x7c00)
    a.words(0x2c47); a.d(1, 500); call(-198)  # Allow normal BootShell to start.
    a.words(0x2c78, 4); a.address(1, 'shell_task'); call(-294)
    a.words(0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x2c47); a.address(0, 'present'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x220d); a.address(0, 'present_text'); a.words(0x2408); a.d(3, 8); call(-48)
    a.words(0x220d); call(-36)
    # If the first boot's injector receipt exists, record a second boot.
    a.address(0, 'sent'); a.words(0x2208); a.d(2, 1005); call(-30)
    a.words(0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x2200); call(-36)
    a.address(0, 'second'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x220d); a.address(0, 'second_text'); a.words(0x2408); a.d(3, 6); call(-48)
    a.words(0x220d); call(-36)
    if capture_second_alert:
        # Exec retains LastAlert across a warm restart. Capture it before
        # reinjecting any commands, without interpreting its contents here.
        a.words(0x2c78, 4, 0x49ee, 514, 0x2c47)
        a.address(0, 'alert_path'); a.words(0x2208); a.d(2, 1006); call(-30)
        a.words(0x2a40, 0x4a80); a.branch(0x6700, 'create_port')
        a.words(0x220d, 0x240c); a.d(3, 16); call(-48)
        a.words(0x220d); call(-36)
    a.label('create_port'); a.words(0x2c78, 4); call(-666)  # CreateMsgPort
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x204c); a.d(0, 48); call(-654)   # CreateIORequest
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'delete_port')
    a.address(0, 'device'); a.words(0x224d, 0x7000, 0x7200); call(-444)
    a.words(0x4a80); a.branch(0x6600, 'delete_request')
    a.address(2, 'keys'); a.address(3, 'event')
    a.label('next'); a.words(0x7000, 0x101a, 0x0c00, 0x00ff)
    a.branch(0x6700, 'complete')
    a.words(0x7200, 0x121a, 0x2a00, 0x2801); a.branch(0x6100, 'send')
    a.words(0x2005, 0x2204, 0x0040, 0x0080); a.branch(0x6100, 'send')
    if wait_after_first_line or wait_for_path:
        # Wait for a guest marker before sending the follow-up line.
        a.words(0x0c05, 0x0044); a.branch(0x6600, 'after_start_wait')
        a.address(0, 'waited'); a.words(0x5210, 0x0c10, wait_after_line)
        a.branch(0x6600, 'after_start_wait')
        a.d(4, 2000 if wait_for_path else 500)
        a.label('start_retry'); a.words(0x2c47); a.address(0, 'wait_path'); a.words(0x2208)
        a.d(2, 1005); call(-30); a.words(0x4a80); a.branch(0x6600, 'start_found')
        a.words(0x7201); call(-198); a.words(0x5384); a.branch(0x6600, 'start_retry')
        a.branch(0x6000, 'start_done')
        a.label('start_found'); a.words(0x2200); call(-36)
        a.label('start_done')
        a.label('after_start_wait')
    a.words(0x2c47, 0x7201)                  # One tick between keys.
    if line_delay_ticks:
        # D5 still holds the raw key: pause after Return before typing the
        # next command, allowing a prior script to finish its foreground work.
        a.words(0x0c05, 0x0044); a.branch(0x6600, 'key_wait')
        a.d(1, line_delay_ticks)
        a.label('key_wait')
    call(-198)
    a.words(0x2c78, 4); a.branch(0x6000, 'next')
    a.label('send')
    a.words(0x4293, 0x377c, 0x0100, 4, 0x3740, 6, 0x3741, 8,
            0x42ab, 10, 0x42ab, 14, 0x42ab, 18)
    a.words(0x3b7c, 11, 28, 0x2b7c, 0, 22, 36, 0x2b4b, 40, 0x224d)
    call(-456); a.words(0x4e75)
    a.label('complete'); a.words(0x7c01)
    a.label('close_device'); a.words(0x224d); call(-450)
    a.label('delete_request'); a.words(0x204d); call(-660)
    a.label('delete_port'); a.words(0x204c); call(-672)
    a.words(0x4a86); a.branch(0x6700, 'close_dos')
    a.words(0x2c47); a.address(0, 'sent'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close_dos_restore')
    a.words(0x220d); a.address(0, 'sent_text'); a.words(0x2408); a.d(3, 5); call(-48)
    a.words(0x220d); call(-36)
    if check_shell_exit:
        # After the final key, give EndCLI time to retire the process. The
        # separate marker is only written when the Shell task is actually gone.
        if record_shell_alive:
            a.address(0, 'before_wait'); a.words(0x2208); a.d(2, 1006); call(-30)
            a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close_dos_restore')
            a.words(0x220d); a.address(0, 'wait_text'); a.words(0x2408); a.d(3, 5); call(-48)
            a.words(0x220d); call(-36)
        a.d(1, 300); call(-198)
        if record_shell_alive:
            a.address(0, 'after_wait'); a.words(0x2208); a.d(2, 1006); call(-30)
            a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close_dos_restore')
            a.words(0x220d); a.address(0, 'wait_text'); a.words(0x2408); a.d(3, 5); call(-48)
            a.words(0x220d); call(-36)
        a.words(0x2c78, 4); a.address(1, 'shell_task'); call(-294)
        a.words(0x4a80)
        if record_shell_alive:
            a.branch(0x6700, 'shell_gone')
            a.words(0x2c47); a.address(0, 'still'); a.words(0x2208); a.d(2, 1006); call(-30)
            a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close_dos_restore')
            a.words(0x220d); a.address(0, 'still_text'); a.words(0x2408); a.d(3, 6); call(-48)
            a.words(0x220d); call(-36); a.branch(0x6000, 'close_dos_restore')
            a.label('shell_gone')
        else:
            a.branch(0x6600, 'close_dos_restore')
        a.words(0x2c47); a.address(0, 'ended'); a.words(0x2208); a.d(2, 1006); call(-30)
        a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close_dos_restore')
        a.words(0x220d); a.address(0, 'ended_text'); a.words(0x2408); a.d(3, 6); call(-48)
        a.words(0x220d); call(-36)
    a.label('close_dos_restore'); a.words(0x2c78, 4)
    a.label('close_dos'); a.words(0x2247); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('device', 'input.device')
    a.text('sent', 'SYS:input-sent.txt'); a.text('sent_text', 'SENT\n')
    a.text('shell_task', 'CopperOS Shell'); a.text('present', 'SYS:shell-present.txt')
    a.text('present_text', 'PRESENT\n')
    if check_shell_exit:
        a.text('ended', 'SYS:shell-ended.txt'); a.text('ended_text', 'ENDED\n')
        if record_shell_alive:
            a.text('still', 'SYS:shell-still.txt'); a.text('still_text', 'ALIVE\n')
            a.text('before_wait', 'SYS:before-wait.txt'); a.text('after_wait', 'SYS:after-wait.txt')
            a.text('wait_text', 'WAIT\n')
    a.text('second', 'SYS:second-boot.txt'); a.text('second_text', 'AGAIN\n')
    if capture_second_alert:
        a.text('alert_path', 'SYS:second-alert.bin')
    if wait_after_first_line or wait_for_path:
        a.text('wait_path', wait_for_path or 'SYS:delay-started')
        a.label('waited'); a.code.append(0)
    if len(a.code) & 1: a.code.append(0)
    a.label('event'); a.code += bytes(22)
    a.label('keys'); a.code += keys
    return a.finish()
