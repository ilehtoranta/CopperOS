"""Bind fresh read-only boots to the exact disks exported by a qualified relabel."""
import argparse
import json
from pathlib import Path
from verify_relabel_boot import digest, read_trace, require
from verify_relabel_persistent_boot import observe


def qualified_source(source):
    source = Path(source).resolve()
    comparison_path = source / "qualified/comparison.json"
    comparison = json.loads(comparison_path.read_text(encoding="utf-8"))
    require(comparison["status"] == "passed" and comparison["suite"] == "Relabel-persistent-FFS-original-DOS-boot" and
            comparison["verifierSha256"] == digest(Path(__file__).with_name("verify_relabel_persistent_boot.py")), "Qualified persistence source")
    results, reports = [], {}
    for role in ["reference", "candidate"]:
        observed = source / "qualified" / (role + "-observations.json")
        media = source / "media" / (role + "-media.json")
        trx = source / (role + ".trx")
        bound = comparison["inputs"][role]
        require(digest(observed) == bound["observationsSha256"] and digest(media) == bound["mediaSha256"] and
                digest(trx) == bound["trxSha256"], "Persistence capture bindings")
        report = json.loads(observed.read_text(encoding="utf-8"))
        require(report == read_trace(trx), "Persistence observation matches actual passed test")
        results.append(observe(report, json.loads(media.read_text(encoding="utf-8")), role == "candidate"))
        reports[role] = report
    require(results[0] == results[1] == comparison["observations"], "Persistence effects still qualify")
    return comparison, reports


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--directory", type=Path, required=True)
    args = parser.parse_args()
    require(not args.directory.exists(), "Fresh remount data directory")
    comparison, reports = qualified_source(args.source)
    folder = args.directory.resolve()
    folder.mkdir(parents=True)
    for role, report in reports.items():
        receipt = {"scope": "Cold read-only mount; no Relabel invocation or copied guest state.",
                   "sourceDirectory": str(args.source.resolve()), "sourceRole": role,
                   "sourceComparisonSha256": digest(args.source / "qualified/comparison.json"),
                   "sourceObservationsSha256": comparison["inputs"][role]["observationsSha256"],
                   "inputPath": report["dataDiskOutputPath"], "inputSha256": report["dataDiskFinalSha256"].lower(),
                   "outputPath": str(folder / (role + "-after.adf")),
                   "producerSha256": digest(__file__)}
        (folder / (role + "-data.json")).write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(folder)


if __name__ == "__main__":
    main()
