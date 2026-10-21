"""Compare native CopperStart locale behavior with the original Workbench boot.

Native Workbench renders its own desktop. Pixel identity remains a separate
qualification in verify_switch.py; this check records both framebuffer hashes.
"""
import argparse
import hashlib
import json
from pathlib import Path
from verify_switch import framebuffer, verify


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('candidate', 'original', 'output'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--ram-profile', choices=('one-mib', 'fast-2m'))
    p.add_argument('--require-clean-commands', action='store_true')
    a = p.parse_args()
    candidate = json.loads(a.candidate.read_text())
    original = json.loads(a.original.read_text())
    result = verify(candidate, original)
    result['scope'] = 'Native CopperStart Workbench/IPrefs public locale API comparison'
    if candidate.get('replacement') and candidate.get('phase') != 3:
        result['errors'].append('candidate: startup did not reach EndCLI')
    if candidate.get('replacement') and 'nativeDesktopReady' in candidate and not candidate['nativeDesktopReady']:
        result['errors'].append('candidate: native desktop initialization did not finish')
    if candidate.get('startupReturnedToInteractive'):
        result['errors'].append('candidate: startup returned to the interactive prompt')
    for name, receipt in (('candidate', candidate), ('original', original)):
        if receipt.get('lowPcFailure'):
            result['errors'].append(name + ': execution entered the exception-vector page')
        if receipt.get('bootFailure'):
            result['errors'].append(name + ': native boot failed with DOS error ' + str(receipt.get('bootError')))
        if a.ram_profile:
            expected_memory = dict(chipBytes=524288,
                                   slowBytes=524288 if a.ram_profile == 'one-mib' else 0,
                                   fastBytes=0 if a.ram_profile == 'one-mib' else 2097152)
            if receipt.get('memory') != expected_memory:
                result['errors'].append(name + ': RAM profile differs from ' + a.ram_profile)
    result['requiredRamProfile'] = a.ram_profile
    result['cleanCommandsRequired'] = a.require_clean_commands
    result['candidateCommandFailures'] = candidate.get('commandFailures')
    if a.require_clean_commands:
        if 'commandFailures' not in candidate:
            result['errors'].append('candidate: missing startup return-level observations')
        elif candidate['commandFailures']:
            result['errors'].append('candidate: startup reported ' + ', '.join(candidate['commandFailures']))
    expected = framebuffer(a.original.parent / 'screen.png')
    actual = framebuffer(a.candidate.parent / 'screen.png')
    result['desktopPixelsMatchReference'] = actual == expected
    result['candidateDesktopPixelsSha256'] = hashlib.sha256(actual[2]).hexdigest().upper()
    result['originalDesktopPixelsSha256'] = hashlib.sha256(expected[2]).hexdigest().upper()
    result['desktopVisualQualification'] = 'Separate visual review of the native Workbench desktop'
    result['passed'] = not result['errors']
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
