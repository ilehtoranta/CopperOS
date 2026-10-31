"""Bind the development HDF to independent, scoped WinUAE receipts."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(data): return hashlib.sha256(data).hexdigest()
def load(path): return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('image', 'readback', 'keyboard', 'negative', 'usershell', 'newcli', 'output'):
        parser.add_argument('--' + name, required=True, type=Path)
    args = parser.parse_args()
    image = args.image.resolve(strict=True); data = image.read_bytes(); image_hash = sha(data)
    readback_path = args.readback.resolve(strict=True); readback = load(readback_path)
    if (readback['imageSha256'] != image_hash or readback['commandCount'] != 61 or
        readback['versionedBinaries'] != 63 or readback['libraries'] != 0):
        raise ValueError('Readback does not establish the expected image inventory')
    dirs = {name: getattr(args, name).resolve(strict=True) for name in ('keyboard', 'negative', 'usershell', 'newcli')}
    receipts = {name: load(path / 'qualification.json') for name, path in dirs.items()}
    for name, receipt in receipts.items():
        if receipt['inputImageSha256'] != image_hash or receipt['runtimeQualified'] is not False:
            raise ValueError('Mismatched or improperly labeled guest evidence: ' + name)
    keyboard = receipts['keyboard']; negative = receipts['negative']; shell = receipts['usershell']
    if (keyboard['withoutBootShell'] or not keyboard['restartImageSha256'] or
        keyboard['observed']['shellPresent'] != 'PRESENT\n' or keyboard['observed']['eval'] != '3\n' or
        keyboard['observed']['version'] != 'Eval 1.0\n' or keyboard['observed']['status'] != 'OK\n' or
        keyboard['observed']['echo'] != 'hello\nagain\n' or not keyboard['observed']['directory'] or
        not keyboard['observed']['removed'] or keyboard['observed']['finish'] != 'done\n'):
        raise ValueError('Keyboard or restart evidence is incomplete')
    if not negative['withoutBootShell'] or negative['observed']['shellPresent'] is not None:
        raise ValueError('The no-BootShell control did not reject the Shell task')
    if not shell['bootPath'] or shell['asyncPublicMemory'] is None or len(set(shell['asyncPublicMemory'])) != 1:
        raise ValueError('User-shell or repeated asynchronous lifetime evidence is incomplete')
    shell_obs = load(dirs['usershell'] / 'observations.json')
    for key, expected in {'systemResult': 'OK\n', 'systemChild': 'C:\n', 'systemFailure': 'OK\n',
                          'asyncResult': 'OK\n', 'asyncChild': 'async\n', 'systemEval': '3\n',
                          'systemEvalResult': 'OK\n'}.items():
        if shell_obs[key] != expected: raise ValueError('User-shell observation mismatch: ' + key)
    newcli_obs = load(dirs['newcli'] / 'observations.json')
    if newcli_obs['newCliPath'] != 'C:\n' or newcli_obs['newCliOutput'] != 'child\n':
        raise ValueError('NewCLI startup observation mismatch')
    launches = [load(dirs[name] / 'run/launch.json') for name in dirs]
    launches.append(load(dirs['keyboard'] / 'restart/launch.json'))
    for key in ('romSha256', 'winuaeSha256', 'winuaeVersion'):
        if len({launch[key] for launch in launches}) != 1:
            raise ValueError('Guest tool identity differs: ' + key)
    for launch in launches:
        config = launch['configuration']
        if ('cpu_model=68000' not in config or 'nr_floppies=0' not in config or
            sum(line.startswith('hardfile2=') for line in config) != 1 or
            any(line.startswith('filesystem2=') for line in config)):
            raise ValueError('Unexpected guest configuration')
    if sha((dirs['keyboard'] / 'restart/candidate.hdf').read_bytes()) != keyboard['restartImageSha256']:
        raise ValueError('Restart HDF differs from keyboard receipt')
    result = {'kind': 'kickstart31-development-qualification', 'status': 'scoped-checks-passed',
              'shipping': False, 'fullShellAbiQualified': False, 'imageSha256': image_hash,
              'cpu': '68000', 'romSha256': launches[0]['romSha256'],
              'winuaeVersion': launches[0]['winuaeVersion'], 'winuaeSha256': launches[0]['winuaeSha256'],
              'commands': 61, 'versionedBinaries': 63, 'libraryBinaries': 0,
              'asyncPublicMemory': shell['asyncPublicMemory'],
              'evidence': {name: {'path': str(path / 'qualification.json'),
                                  'sha256': sha((path / 'qualification.json').read_bytes())}
                           for name, path in dirs.items()},
              'readback': {'path': str(readback_path), 'sha256': sha(readback_path.read_bytes())},
              'limits': ['Guest probes use modified startup sequences and test-only programs.',
                         'WinUAE is forcibly stopped before the cold restart.',
                         'NewCLI test uses a locally supplied reference binary only in the disposable test image.',
                         'Not all Shell built-ins, script semantics, command contracts or other machines are qualified.']}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(result, stream, indent=2); stream.write('\n')
    print(json.dumps({k: result[k] for k in ('status', 'imageSha256', 'cpu', 'winuaeVersion', 'commands')}))


if __name__ == '__main__': main()
