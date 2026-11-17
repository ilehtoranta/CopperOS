"""Smoke-test native probe generation used by the Shell lifetime checks."""
import sys
import struct
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from make_kickstart_shell_probe import (build_deferred_segment_probe, build_path_signal_probe, build_nonresident_boot_probe, build_run_lifetime_probe,
                                       build_nonresident_memory_probe, build_nonresident_swap_probe, build_run_delayed_probe, build_segment_layout_probe,
                                       build_system_lifetime_probe, build_system_input_probe,
                                       build_system_async_probe, build_control_script, build_many_path_script,
                                       build_run_stream_probe, build_system_run_output_probe,
                                       build_run_parent_state_probe, build_run_shared_error_probe)
from qualify_copperstart_run import requires_run_notice
from make_kickstart_keyboard_probe import child
from compare_path_many31 import compare
from compare_run_stream31 import compare as compare_run_stream
from compare_ask31 import compare as compare_ask
from inspect_exit_lifecycle import decode as decode_exit_lifecycle
from execute_disk_full_fixture import file_blocks, scripts as disk_full_scripts, fill_volume, build_execute_probe
from build_system_image import Disk, verify


class ShellProbeBuilderTests(unittest.TestCase):
    def test_execute_pressure_accounts_for_file_headers_extensions_and_outer_script(self):
        for size in (0, 1, 512, 513, 72 * 512, 72 * 512 + 1, 144 * 512 + 1):
            disk = Disk()
            before = len(disk.used)
            disk.file(disk.root, 'sized', bytes(size))
            self.assertEqual(file_blocks(size), len(disk.used) - before)
        for stage in ('source', 'tail'):
            source, caller = disk_full_scripts(stage)
            self.assertGreater(len(source if stage == 'source' else caller), 16384)
            self.assertTrue(all(len(line) < 512 for line in (source + caller).splitlines()))
            self.assertTrue(caller.endswith(b'Echo SCRIPT DONE\n'))
            disk = Disk()
            directory = disk.directory(disk.root, 'S')
            disk.file(directory, 'S/execfull', source)
            disk.file(directory, 'S/regress', caller)
            free = fill_volume(disk, caller)
            self.assertIn(free, (32 + file_blocks(len(caller)), 33 + file_blocks(len(caller))))
            checked = verify(disk.finish(), disk.files)['filesystemCheckOnPrivateBootNormalizedCopy']
            self.assertTrue(checked['bitmap']['exact_reachable_allocation'])
            self.assertEqual(free, checked['bitmap']['free_blocks'])
        probe = build_execute_probe()
        self.assertEqual(b'\x00\x00\x03\xf3', probe[:4])
        self.assertIn(b'C:Execute\0', probe)
        with self.assertRaises(ValueError):
            disk_full_scripts('other')

    def test_large_control_script_crosses_copy_buffers_and_retains_late_assertions(self):
        script = build_control_script(128)
        self.assertGreater(len(script), 8192)
        self.assertTrue(script.startswith(b'.key name/A,option/K\n'))
        self.assertGreater(script.index(b'Echo >SYS:ctl-arguments'), 8192)
        self.assertTrue(script.endswith(b'Echo >SYS:ctl-childdone done\n'))
        self.assertTrue(all(len(line) < 512 for line in script.splitlines()))
        for count in (-1, 1025):
            with self.assertRaises(ValueError):
                build_control_script(count)

    def test_ask_comparison_checks_results_prompt_and_rom_identity(self):
        reference = {'originalShell': True, 'romSha256': 'rom', 'winuaeSha256': 'uae',
                     'input-sent.txt': 'SENT\n', 'askdone': 'done\n',
                     'ask-yes-result': [5, 0], 'ask-no-result': [0, 0],
                     'ask-retry-result': [5, 118], 'ask-yes-text': 'Continue? ',
                     'ask-no-text': 'Continue? ', 'ask-retry-text': 'Continue? Continue? '}
        candidate = dict(reference, originalShell=False)
        self.assertTrue(compare_ask(reference, candidate)['matched'])
        with self.assertRaises(ValueError):
            compare_ask(reference, dict(candidate, **{'ask-retry-result': [5, 0]}))
        with self.assertRaises(ValueError):
            compare_ask(reference, dict(candidate, **{'ask-yes-text': 'Continue?'}))
        with self.assertRaises(ValueError):
            compare_ask(reference, dict(candidate, romSha256='different'))

    def test_exit_lifecycle_observation_checks_retirement_and_result(self):
        observation = struct.pack('>9I', 15, 3, 0, 0, 0, 7, 0x80000000, 0x1234, 0)
        self.assertTrue(decode_exit_lifecycle(observation)['cliVisibleDuringCallback'])
        no_cli_during_callback = struct.pack('>9I', 13, 3, 0, 0, 0, 7, 1, 0x1234, 0)
        self.assertFalse(decode_exit_lifecycle(no_cli_during_callback)['cliVisibleDuringCallback'])
        for invalid in (None, observation[:-1],
                        struct.pack('>9I', 7, 3, 0, 0, 0, 7, 1, 0x1234, 0),
                        struct.pack('>9I', 15, 3, 0, 0, 0, 8, 1, 0x1234, 0),
                        struct.pack('>9I', 15, 3, 0, 0, 0, 7, 3, 0x1234, 0)):
            with self.assertRaises(ValueError):
                decode_exit_lifecycle(invalid)

    def test_run_stream_comparison_rejects_output_and_rom_mismatches(self):
        reference = {'originalShell': True, 'romSha256': 'rom', 'winuaeSha256': 'uae',
                     'systemOutput': True, 'input-sent.txt': 'SENT\n', 'systemdone': 'OK\n',
                     'runshared': 'PARENT\n', 'runprefix': '[CLI 3]\n', 'streamname': '',
                     'streamreport': [6, 0, 0, 123, 0, 0, 0]}
        candidate = dict(reference, originalShell=False, streamreport=[6, 0, 0, 456, 0, 0, 0])
        self.assertTrue(compare_run_stream(reference, candidate)['matched'])
        with self.assertRaises(ValueError):
            compare_run_stream(reference, dict(candidate, runshared='CHILD\nPARENT\n'))
        with self.assertRaises(ValueError):
            compare_run_stream(reference, dict(candidate, romSha256='other'))

    def test_run_notice_is_only_required_for_background_cases(self):
        self.assertFalse(requires_run_notice('control-flow'))
        self.assertFalse(requires_run_notice('system-api'))
        self.assertFalse(requires_run_notice('longpath'))
        self.assertFalse(requires_run_notice('path-many'))
        self.assertFalse(requires_run_notice('path-isolation'))
        self.assertFalse(requires_run_notice('path'))
        self.assertFalse(requires_run_notice('cd'))
        self.assertFalse(requires_run_notice('resident-defer'))
        self.assertFalse(requires_run_notice('ask'))
        self.assertFalse(requires_run_notice('console-clear'))
        self.assertFalse(requires_run_notice('console-reset'))
        self.assertTrue(requires_run_notice('broad'))

    def test_injector_can_wait_for_a_guest_marker(self):
        injector = child('runnerprobe\nendcli\n', wait_for_path='SYS:runner-ok')
        self.assertEqual(b'\x00\x00\x03\xf3', injector[:4])
        self.assertIn(b'SYS:runner-ok\0', injector)

    def test_many_path_script_stays_within_the_workspace(self):
        line = build_many_path_script().splitlines()[2]
        self.assertEqual(65, line.split().count(b'SYS:S'))
        self.assertLess(65 * (len(b'SYS:S') + 1), 512)

    def test_path_comparison_checks_identity_and_results(self):
        reference = {'originalShell': True, 'romSha256': 'rom', 'winuaeSha256': 'uae',
                     'input-sent.txt': 'SENT\n', 'pathmany': 'one\n', 'pathmanydone': 'done\n'}
        candidate = dict(reference, **{'shell-ended.txt': 'ENDED\n'})
        launch = {'romSha256': 'rom', 'winuaeSha256': 'uae'}
        compare(reference, candidate, launch)
        with self.assertRaises(ValueError):
            compare(reference, dict(candidate, pathmany='different\n'), launch)
        with self.assertRaises(ValueError):
            compare(reference, candidate, dict(launch, romSha256='other'))

    def test_control_script_uses_literal_echo_arguments_for_append(self):
        script = build_control_script().splitlines()
        first = script.index(b'Echo alpha >SYS:ctl-redirection')
        self.assertEqual(b'Echo beta >>SYS:ctl-redirection', script[first + 1])
        # FIRST and LEN are Echo options, so they cannot serve as bare output
        # text in this fixture without changing the command's meaning.
        self.assertNotIn(b'Echo first >SYS:ctl-redirection', script)

    def test_lifetime_probes_build_loadable_hunks(self):
        for build in (build_system_lifetime_probe, build_system_input_probe,
                      build_run_stream_probe, build_system_run_output_probe, build_run_parent_state_probe,
                      build_run_shared_error_probe,
                      lambda: build_deferred_segment_probe('SYS:defer-before'),
                      lambda: build_path_signal_probe('setup'),
                      lambda: build_path_signal_probe('wait'),
                      lambda: build_system_async_probe(null_command=True), build_run_lifetime_probe,
                      lambda: build_run_lifetime_probe(new_shell=True), build_nonresident_boot_probe,
                      lambda: build_nonresident_memory_probe('SYS:memory1'),
                      build_nonresident_swap_probe, lambda: build_nonresident_swap_probe(True),
                      lambda: build_nonresident_swap_probe(missing=True),
                      lambda: build_nonresident_swap_probe(True, missing=True),
                      build_segment_layout_probe,
                      lambda: build_run_delayed_probe(500, verify_parent_retired=True)):
            with self.subTest(build=build):
                image = build()
                self.assertEqual(b'\x00\x00\x03\xf3', image[:4])
                self.assertIn(b'dos.library\0', image)

    def test_path_signal_sender_targets_the_proof_shell(self):
        image = build_path_signal_probe('send')
        self.assertEqual(b'\x00\x00\x03\xf3', image[:4])
        self.assertIn(b'CopperOS Shell\0', image)


if __name__ == '__main__':
    unittest.main()
