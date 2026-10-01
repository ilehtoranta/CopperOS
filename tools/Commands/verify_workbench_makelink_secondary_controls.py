"""Replay a real bound MakeLink fixture with adversarial secondary-result traces."""
import argparse
import copy
import json
from pathlib import Path
from tempfile import TemporaryDirectory
from types import SimpleNamespace
import xml.etree.ElementTree as ET

import verify_workbench_makelink_secondary as secondary


def run(baseline, output):
    bound = json.loads(baseline.read_text())
    evidence = bound["evidence"]
    fields = {name: Path(evidence[key]["path"]) for name, key in [
        ("reference", "reference_observations"), ("candidate", "candidate_observations"),
        ("reference_media", "reference_media"), ("candidate_media", "candidate_media"),
        ("reference_trx", "reference_trx"), ("candidate_trx", "candidate_trx"),
        ("test_assembly", "test_assembly")]}
    original = SimpleNamespace(**fields, expected_candidate_sha256=bound["candidateBinarySha256"])
    actual = secondary.verify(original)
    secondary.require(actual["candidate"] == bound["candidate"] and actual["reference"] == bound["reference"],
                      "Baseline is not current verified evidence")
    data = secondary.refresh.read(original.candidate)
    segment = data["copyInvocationOwnership"][0]["Segment"]
    task = data["copyInvocationOwnership"][0]["Task"]

    def first_run(trace):
        return next(e for e in trace["observedDosCalls"] if e["Name"] == "RunCommand"
                    and e["D1"] == segment and e["Task"] == task)

    def wrong_close(trace):
        next(e for e in trace["observedDosCalls"] if e["Name"] == "CopyCloseLibrary")["A1"] = 0

    controls = [
        ("success-error-cleared-to-zero", lambda t: first_run(t).__setitem__("ReturnedIoErr", 0), "Final primary/secondary"),
        ("missing-secondary", lambda t: first_run(t).pop("ReturnedIoErr"), "Missing or invalid"),
        ("boolean-secondary", lambda t: first_run(t).__setitem__("ReturnedIoErr", True), "Missing or invalid"),
        ("foreign-task", lambda t: first_run(t).__setitem__("Task", task+4), "cardinality"),
        ("duplicated-completion", lambda t: t["observedDosCalls"].append(copy.deepcopy(first_run(t))), "cardinality"),
        ("primary-ownership-disagreement", lambda t: first_run(t).__setitem__("ReturnedD0", 20), "ownership identity"),
        ("foreign-library-close", wrong_close, "library lifetime"),
        ("changed-stack-request", lambda t: first_run(t).__setitem__("D2", 8192), "requested RunCommand stack"),
    ]
    results = []
    with TemporaryDirectory(prefix="makelink-secondary-controls-", dir=output.parent) as temporary:
        temp = Path(temporary)
        for name, change, reason in controls:
            trace = copy.deepcopy(data)
            change(trace)
            changed = temp / (name + ".json")
            changed.write_text(json.dumps(trace))
            # Keep TRX and observation mutually consistent. The new comparator
            # must reject the semantic corruption, not merely a stale trace hash.
            tree = ET.parse(original.candidate_trx)
            prefix = "DOS passive boot readiness: "
            for node in tree.iter():
                if node.tag.endswith("}StdOut"):
                    node.text = "\n".join(prefix + json.dumps(trace) if line.startswith(prefix) else line
                                          for line in (node.text or "").splitlines())
            trx = temp / (name + ".trx")
            tree.write(trx, encoding="utf-8", xml_declaration=True)
            args = SimpleNamespace(**{**vars(original), "candidate": changed, "candidate_trx": trx})
            try:
                secondary.verify(args)
            except (AssertionError, ValueError) as error:
                secondary.require(reason in str(error), name + " failed for an unrelated reason: " + str(error))
                results.append({"name": name, "rejected": True, "reason": str(error)})
            else:
                raise AssertionError("Control incorrectly accepted: " + name)
    result = {"status": "passed", "positiveFixtureVerified": True,
              "baselineSha256": secondary.refresh.digest(baseline),
              "verifierSha256": secondary.refresh.digest(Path(secondary.__file__)),
              "controlRunnerSha256": secondary.refresh.digest(Path(__file__)), "controls": results}
    secondary.require(not output.exists(), "Refusing to overwrite control evidence")
    output.write_text(json.dumps(result, indent=2) + "\n")
    print("PASS: eight semantic controls rejected with mutually consistent TRX/observation pairs")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("baseline", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    run(args.baseline, args.output)
