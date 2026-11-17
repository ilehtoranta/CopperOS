"""Shared guest corpus for built-ins not covered by the process/path matrices."""
import struct

from make_kickstart_shell_probe import build_cli_result_probe, module


RESULTS = ('bi-setup-result', 'bi-alias-result', 'bi-why-result', 'bi-unsetenv-result',
           'bi-quit-result', 'bi-why-zero-result', 'bi-endskip-result', 'bi-standalone-endskip-result',
           'bi-back-result', 'bi-emptylab-result', 'bi-missing-result', 'bi-unlabelled-result',
           'bi-emptyback-result', 'bi-lab-empty-result', 'bi-lab-text-result')
GENERATED = ('t/builtin-env', 't/builtin-envarc', 'c/bienv', 's/biquit', 's/binested',
             's/biback', 's/bibare', 's/bimissing', 's/binolab', 's/bibackempty', 's/bilab',
             *(f'c/bicap{number}' for number in range(len(RESULTS))))


def assign_environment():
    """Give the disposable guest real ENV: and ENVARC: directories via DOS."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d)
    for number in range(3):
        a.address(0, f'directory{number}'); a.words(0x2208); a.d(2, -2); call(-84)
        a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_fail')
        a.address(0, f'name{number}'); a.words(0x2208, 0x2406); call(-612)
        a.words(0x4a80); a.branch(0x6700, 'unlock_fail')
    a.d(1, 0); call(-462)
    a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('unlock_fail'); a.words(0x2206); call(-90)
    a.label('close_fail'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for label, value in (('dos', 'dos.library'), ('directory0', 'SYS:T/builtin-env'),
                         ('directory1', 'SYS:T/builtin-envarc'), ('directory2', 'SYS:T'),
                         ('name0', 'ENV'), ('name1', 'ENVARC'), ('name2', 'T')):
        a.text(label, value)
    return a.finish()


def install(disk, directories):
    for name in ('builtin-env', 'builtin-envarc'):
        directory = disk.directory(directories['t'], name)
        disk.file(directory, 'T/' + name + '/.keep', b'')
    disk.file(directories['c'], 'C/BIEnv', assign_environment())
    for number, name in enumerate(RESULTS):
        disk.file(directories['c'], f'C/BICap{number}', build_cli_result_probe('SYS:' + name))
    disk.file(directories['s'], 'S/biquit',
              b'Echo >SYS:bi-quit-enter yes\nQuit 7\nEcho >SYS:bi-quit-wrong wrong\n')
    disk.file(directories['s'], 'S/binested',
              b'Execute S:biquit\nEcho >SYS:bi-quit-tail wrong\n')
    def without_completion(value):
        return value.replace(b'Echo >SYS:bi-done done\nEcho SCRIPT DONE\n', b'')
    disk.file(directories['s'], 'S/biback', without_completion(control_script('back-named')))
    disk.file(directories['s'], 'S/bibare', without_completion(control_script('lab-empty')).replace(b'C:BICap6', b'C:BICap9'))
    disk.file(directories['s'], 'S/bimissing', control_script('missing').replace(b'bi-skip', b'bi-missing'))
    disk.file(directories['s'], 'S/binolab', without_completion(control_script('lab'))
              .replace(b'C:BICap6\n', b'').replace(b'bi-skip', b'bi-unlabelled'))
    disk.file(directories['s'], 'S/bibackempty', without_completion(control_script('back'))
              .replace(b'bi-back', b'bi-emptyback'))
    disk.file(directories['s'], 'S/bilab',
              b'FailAt 10\nLab\nC:BICap13\nLab target\nLab target\n'
              b'Lab one two\nLab "two words"\nC:BICap14\nFailAt 21\n')


def followup(complete_corpus=False):
    # Execute appends a script caller's tail. QUIT therefore discards that tail;
    # the completion marker must be a subsequent interactive command.
    cases = ('execute s:bilab\nexecute s:biback\nc:bicap8\nexecute s:bibare\n'
             'execute s:bimissing\nc:bicap10\nexecute s:binolab\nc:bicap11\n'
             'execute s:bibackempty\nc:bicap12\n') if complete_corpus else ''
    return cases + 'execute s:binested\nc:bicap4\necho >sys:bi-after-quit done'


def script(include_extensions=True):
    value = (b'FailAt 21\nC:BIEnv\nC:BICap0\nEcho >NIL: baseline\nWhy >SYS:bi-why-zero\nC:BICap5\n'
            b'Echo NOLINE "a *"quote*" ** b" >SYS:bi-quote\n'
            b'Echo FIRST 2 LEN 3 abcdef >SYS:bi-slice\n'
            b'Echo NOLINE alpha >SYS:bi-append\nEcho beta >>SYS:bi-append\n'
            b'Stack 32768\nStack >SYS:bi-stack\nFailAt >SYS:bi-failat\n'
            b'Fault 205 >SYS:bi-fault\nCls >SYS:bi-cls\nCls RESET >SYS:bi-cls-reset\n'
            b'Set BILocal "two words"\nEcho "$BILocal" >SYS:bi-local\nUnset BILocal\n'
            b'If "$BILocal" EQ "two words"\nEcho >SYS:bi-local-wrong wrong\n'
            b'Else\nEcho >SYS:bi-local-unset yes\nEndIf\n'
            b'Alias BIMark Echo alias\nBIMark >SYS:bi-alias\nUnalias BIMark\n'
            b'BIMark >SYS:bi-alias-removed\nC:BICap1\n'
            b'CD SYS:__builtin_missing_directory >NIL:\nWhy >SYS:bi-why\nC:BICap2\n'
            b'Setenv BIVar first\nGetenv BIVar >SYS:bi-env\n'
            b'Setenv BIVar SAVE second\nGetenv BIVar >SYS:bi-env-saved\n'
            b'Unsetenv BIVar\nC:BICap3\n'
            b'Setenv BIRemove SAVE third\nUnsetenv BIRemove SAVE\nEndSkip\nC:BICap7\n'
            b'Skip\nEcho >SYS:bi-skip-wrong wrong\nEndSkip\nC:BICap6\n'
            b'Echo >SYS:bi-before-lab yes\nLab builtin-end\nEcho >SYS:bi-skip yes\n'
            b'Echo >SYS:bi-done done\nEcho SCRIPT DONE\n')
    if include_extensions:
        # Check a real console reset as well as its redirected byte stream:
        # the final marker must return to normal rendition after inverse text.
        value = value.replace(b'Echo >SYS:bi-done done\n',
                              b'Echo RESET SENTINEL\nEcho NOLINE "*E[7m"\nCls RESET\nEcho >SYS:bi-done done\n')
    if not include_extensions:
        # Cls and the SAVE switches are retained Shell extensions. The ROM
        # control exercises the common commands without changing their data.
        value = value.replace(b'Cls >SYS:bi-cls\n', b'')
        value = value.replace(b'Cls RESET >SYS:bi-cls-reset\n', b'')
        value = value.replace(b'Setenv BIVar SAVE second', b'Setenv BIVar second')
        value = value.replace(b'Setenv BIRemove SAVE third', b'Setenv BIRemove third')
        value = value.replace(b'Unsetenv BIRemove SAVE', b'Unsetenv BIRemove')
    return value


def expected():
    """Candidate contracts; original-ROM observations establish shared behavior."""
    values = {'bi-quote': b'a "quote" * b', 'bi-slice': b'bcd\n', 'bi-append': b'alphabeta\n',
              'bi-stack': b'Current stack size is 32768 bytes\n', 'bi-failat': b'Fail limit: 21\n',
              'bi-fault': b'Fault 205: object not found\n', 'bi-cls': b'\x0c', 'bi-cls-reset': b'\x1bc',
              'bi-local': b'two words\n', 'bi-local-wrong': None, 'bi-local-unset': b'yes\n',
              'bi-alias': b'alias\n', 'bi-alias-removed': None,
              'bi-why': b'Last command failed because : object not found\n',
              'bi-env': b'first\n', 'bi-env-saved': b'second\n',
              't/builtin-env/bivar': None, 't/builtin-envarc/bivar': b'second',
              't/builtin-env/biremove': None, 't/builtin-envarc/biremove': None,
              'bi-skip-wrong': None, 'bi-before-lab': b'yes\n', 'bi-skip': b'yes\n',
              'bi-quit-enter': b'yes\n', 'bi-quit-wrong': None, 'bi-quit-tail': None,
              'bi-after-quit': b'done\n',
              'bi-done': b'done\n'}
    for name, value in zip(RESULTS, ((0, 0), (10, 205), (5, 205), (0, 0), (7, 0))):
        values[name] = struct.pack('>ii', *value)
    values['bi-standalone-endskip-result'] = struct.pack('>ii', 0, 0)
    values.update({'bi-why-zero': b'The last command did not set a return code\n',
                   'bi-why-zero-result': struct.pack('>ii', 5, 0),
                   'bi-endskip-result': struct.pack('>ii', 5, 0),
                   'bi-back-first': b'yes\n', 'bi-back-enter': b'yes\n', 'bi-back-fallthrough': None,
                   'bi-skip-diagnostic': b'',
                   'bi-missing-diagnostic': b'object not found\n', 'bi-missing-wrong': None,
                   'bi-unlabelled-diagnostic': b'object not found\n', 'bi-unlabelled-wrong': None,
                   'bi-emptyback-start': b'yes\n', 'bi-emptyback-gate': b'yes\n',
                   'bi-emptyback-enter': b'yes\n', 'bi-emptyback-first': None,
                   'bi-emptyback-wrong': None, 'bi-emptyback-bof': None,
                   'bi-emptyback-origin': None, 'bi-emptyback-fallthrough': None})
    for name, result in zip(RESULTS[8:], ((0, 0), (0, 0), (10, 205), (10, 205), (10, 205), (0, 0), (0, 0))):
        values[name] = struct.pack('>ii', *result)
    return values


def control_script(skip_label=''):
    if skip_label == 'lab-command':
        return (b'FailAt 21\nLab >SYS:bi-lab-output\nC:BICap6\n'
                b'Lab target\nC:BICap0\nLab target\nC:BICap1\n'
                b'Lab "two words"\nC:BICap2\nLab one two\nC:BICap3\n'
                b'Echo >SYS:bi-done done\nEcho SCRIPT DONE\n')
    if skip_label == 'lab-empty':
        return control_script('lab').replace(b'Lab first\n', b'Lab\n')
    if skip_label == 'lab':
        return (b'FailAt 21\nSkip >SYS:bi-skip-diagnostic\nEcho >SYS:bi-skip-wrong wrong\n'
                b'Lab first\nC:BICap6\nEcho >SYS:bi-done done\nEcho SCRIPT DONE\n')
    if skip_label in ('missing', 'eof'):
        command = b'Skip missing' if skip_label == 'missing' else b'Skip'
        return b'FailAt 21\n' + command + b' >SYS:bi-skip-diagnostic\nEcho >SYS:bi-skip-wrong wrong\n'
    if skip_label == 'back':
        return (b'FailAt 21\nEcho >SYS:bi-back-start yes\nIf "$BIBackGate" EQ "seen"\nEcho >SYS:bi-back-bof yes\nSkip finish\nEndIf\n'
                b'Set BIBackGate seen\nEcho >SYS:bi-back-gate yes\nSkip origin\nLab first\nEcho >SYS:bi-back-first yes\n'
                b'Skip finish\nLab middle\nEcho >SYS:bi-back-wrong wrong\nSkip finish\n'
                b'Lab origin\nIf "$BIBackOrigin" EQ "seen"\nEcho >SYS:bi-back-origin yes\n'
                b'Skip finish\nEndIf\nSet BIBackOrigin seen\nEcho >SYS:bi-back-enter yes\nSkip BACK\n'
                b'Echo >SYS:bi-back-fallthrough wrong\nLab finish\n'
                b'Echo >SYS:bi-done done\nEcho SCRIPT DONE\n')
    if skip_label == 'back-named':
        return (b'FailAt 10\nLab target\nIf "$BIBackSeen" EQ "yes"\n'
                b'Echo >SYS:bi-back-first yes\nSkip finish\nEndIf\nSet BIBackSeen yes\n'
                b'Lab middle\nEcho >SYS:bi-back-enter yes\nSkip target BACK\n'
                b'Echo >SYS:bi-back-fallthrough wrong\nLab finish\n'
                b'Echo >SYS:bi-done done\nEcho SCRIPT DONE\n')
    value = (b'FailAt 21\nEcho >NIL: baseline\nWhy >SYS:bi-why-zero\nC:BICap5\n'
            b'CD SYS:__builtin_missing_directory >NIL:\nWhy >SYS:bi-why\nC:BICap2\nEndSkip\nC:BICap7\n'
            b'Skip\nEcho >SYS:bi-skip-wrong wrong\nEndSkip\nC:BICap6\n'
            b'Echo >SYS:bi-done done\nEcho SCRIPT DONE\n')
    if skip_label:
        value = value.replace(b'Skip\n', b'Skip ' + skip_label.encode('ascii') + b'\n')
        value = value.replace(b'Echo >SYS:bi-done', b'Lab ' + skip_label.encode('ascii') + b'\nEcho >SYS:bi-done')
    return value


def control_expected(skip_label=''):
    if skip_label == 'lab-command':
        value = control_expected('lab-empty')
        del value['bi-skip-wrong']
        del value['bi-skip-diagnostic']
        value['bi-lab-output'] = b''
        for result in RESULTS[:4]:
            value[result] = struct.pack('>ii', 0, 0)
        return value
    if skip_label == 'lab-empty':
        value = control_expected('lab')
        value['bi-endskip-result'] = struct.pack('>ii', 0, 0)
        value['bi-skip-diagnostic'] = b''
        return value
    if skip_label == 'lab':
        return {'bi-skip-wrong': None, 'bi-skip-diagnostic': b'object not found\n',
                'bi-endskip-result': struct.pack('>ii', 10, 205),
                'bi-quit-result': struct.pack('>ii', 7, 0), 'bi-quit-enter': b'yes\n',
                'bi-quit-wrong': None, 'bi-quit-tail': None,
                'bi-after-quit': b'done\n', 'bi-done': b'done\n'}
    if skip_label in ('missing', 'eof'):
        return {'bi-skip-wrong': None, 'bi-skip-diagnostic': b'object not found\n',
                'bi-endskip-result': struct.pack('>ii', 10, 205),
                'bi-quit-result': struct.pack('>ii', 7, 0), 'bi-quit-enter': b'yes\n',
                'bi-quit-wrong': None, 'bi-quit-tail': None,
                'bi-after-quit': b'done\n', 'bi-done': b'done\n'}
    if skip_label in ('back', 'back-named'):
        return {'bi-back-first': b'yes\n' if skip_label == 'back-named' else None, 'bi-back-wrong': None,
                'bi-back-bof': None, 'bi-back-origin': None, 'bi-back-fallthrough': None,
                'bi-back-enter': b'yes\n',
                **({'bi-back-start': b'yes\n', 'bi-back-gate': b'yes\n'} if skip_label == 'back' else {}),
                **({'bi-endskip-result': struct.pack('>ii', 10, 205)} if skip_label == 'back' else {}),
                'bi-quit-result': struct.pack('>ii', 7, 0), 'bi-quit-enter': b'yes\n',
                'bi-quit-wrong': None, 'bi-quit-tail': None,
                'bi-after-quit': b'done\n', 'bi-done': b'done\n'}
    return {'bi-why-zero': b'The last command did not set a return code\n',
            'bi-why': b'Last command failed because : object not found\n',
            'bi-why-result': struct.pack('>ii', 5, 205),
            'bi-why-zero-result': struct.pack('>ii', 5, 0),
            'bi-endskip-result': struct.pack('>ii', 5, 0),
            'bi-standalone-endskip-result': struct.pack('>ii', 0, 0),
            'bi-quit-result': struct.pack('>ii', 7, 0), 'bi-quit-enter': b'yes\n',
            'bi-quit-wrong': None, 'bi-quit-tail': None,
            'bi-after-quit': b'done\n', 'bi-done': b'done\n'}
