"""Bind a failing old Search image to the corrected buffered-output fixture."""
import argparse
import json
import pathlib
import shutil
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from measure_c import identity
from qualify_sizes import run

repo = pathlib.Path(__file__).resolve().parents[3]
parser = argparse.ArgumentParser(__doc__)
parser.add_argument('stage', type=pathlib.Path)
parser.add_argument('--image', type=pathlib.Path,
                    default=repo / 'artifacts/search-os-qualification-20261012/inputs/Search.candidate')
args = parser.parse_args()
stage = args.stage.resolve()
runner = stage / "vector-runner/CopperOS.Commands.NativeExecution.dll"
source = args.image.resolve()
image = stage / 'controls/Search.buffering-negative-control'
receipt = stage / "controls/old-candidate-buffered-check.json"
manifest = stage / "controls/buffering-control.json"
assert not receipt.exists() and not manifest.exists(), "Preserve previous controls; use a fresh stage."
image.parent.mkdir(parents=True, exist_ok=True)
assert not image.exists()
origin = identity(source)
shutil.copy2(source, image)
assert identity(image)['sha256'] == origin['sha256']
before = [identity(path) for path in (runner, image)]
command = ["dotnet", str(runner), str(image), "68000", str(receipt),
           "wb31-search-native-entry-vector-fixture"]
code = run(command, receipt.with_suffix(".log"))
assert code != 0
result = json.loads(receipt.read_text(encoding="utf-8-sig"))
assert result["status"] == "failed" and "without flushing" in result["failure"]
assert result["imageSha256"] == before[1]["sha256"]
assert before == [identity(path) for path in (runner, image)]
manifest.write_text(json.dumps(dict(command=command, exitCode=code,
    source=origin, runner=before[0], image=before[1], receipt=identity(receipt)), indent=2) + "\n", encoding='utf-8')
print(result["failure"])
