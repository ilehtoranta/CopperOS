"""Compare nine real ReadArgs/MakeLink scenarios against original Workbench."""
import argparse
import json
from pathlib import Path

import verify_workbench_makelink_error_refresh as binding
from verify_copy_boot_transfer import one, require


STARTUP_SHA256 = "6a3296dee7ac9f3bbf80f01a85189f11bb8fb6c561e751f19ac093734e01febe"
CASES = [
    ("positional-default", "RAM:positional", 0, 205),
    ("reordered-keywords-hard", "RAM:reordered", 0, 205),
    ("equals-keywords-hard", "RAM:equals", 0, 205),
    ("force-regular-file", "RAM:forced", 0, 205),
    ("repeated-hard-switch", "RAM:repeated", 0, 205),
    ("duplicate-from-keyword", None, 20, 118),
    ("unknown-argument", None, 20, 118),
    ("missing-required-to", None, 20, 116),
    ("escaped-quote-in-name", 'RAM:quote"name', 0, 205),
]


def inspect(report, candidate):
    require(report.get("commandUnderTest") == "MakeLink" and report.get("rootInfoReady") and
            report.get("diskBytesUnchanged") and not any(report.get(k) for k in
            ("failure", "boundedStop", "dosObservationOverflow", "returnObservationCollision")), "Incomplete option trace")
    rows = report["copyInvocationOwnership"]
    require(len(rows) == len(CASES), "Expected nine command invocations")
    segment, task = rows[0]["Segment"], rows[0]["Task"]
    require(all(r["Segment"] == segment and r["Task"] == task and r["ImageUnchanged"] and
                r["Allocations"] == r["Frees"] == (1 if candidate else 0) for r in rows), "Resident image/owned allocation lifetime")
    require(report["copyActiveInvocations"] == 0 and report["copyCpuImageWrites"] == [], "Live invocation or changed resident image")
    events = report["observedDosCalls"]
    starts = [n for n,e in enumerate(events) if e["Name"] == "RunCommand" and e["D1"] == segment and e["Task"] == task]
    require(len(starts) == len(CASES), "RunCommand cardinality")
    signatures = []
    last_close = 0
    for n, (name, source, primary, secondary) in enumerate(CASES):
        run = events[starts[n]]
        stop = starts[n+1] if n+1 < len(starts) else len(events)
        calls = [e for e in events[starts[n]+1:stop] if e["Task"] == task]
        opened = one([e for e in calls if e["Name"] == "CopyOpenLibrary"], "DOS open")
        closed = one([e for e in calls if e["Name"] == "CopyCloseLibrary"], "DOS close")
        require(opened.get("ReturnedD0", 0) != 0 and closed["A1"] == opened["ReturnedD0"] and
                "ReturnedD0" in closed and calls.index(opened) < calls.index(closed), "Completed DOS lifetime")
        calls = calls[:calls.index(closed)+1]
        last_close = events.index(closed)
        require(run.get("ReturnedD0") == rows[n]["ReturnCode"] == primary and
                run.get("ReturnedIoErr") == secondary, name + ": final primary/secondary result")
        parser = one([e for e in calls if e["Name"] == "ReadArgs"], "One real ReadArgs")
        require(parser.get("Text") == "FROM/A,TO/A,HARD/S,FORCE/S" and
                (parser.get("ReturnedD0", 0) != 0) == (source is not None), name + ": parser result/template")
        links = [e for e in calls if e["Name"] == "MakeLink"]
        locks = [e for e in calls if e["Name"] == "Lock"]
        faults = [e for e in calls if e["Name"] == "PrintFault"]
        frees = [e for e in calls if e["Name"] == "FreeArgs"]
        if source is None:
            fault = one(faults, "Parser diagnostic")
            require(not links and not locks and not frees and parser["ReturnedIoErr"] == secondary and
                    fault["D1"] == secondary and fault["D2"] == 0 and "ReturnedD0" in fault,
                    name + ": parser failure isolation")
        else:
            lock, link, freed = one(locks, "Target lock"), one(links, "Hard link"), one(frees, "Parser release")
            require(lock["Text"] == "RAM:link source" and lock["D2"] == 0xfffffffe and lock.get("ReturnedD0", 0) != 0,
                    name + ": quoted target binding")
            require(link["Text"] == source and link["D2"] == lock["ReturnedD0"] and link["D3"] == 0 and
                    link.get("ReturnedD0", 0) != 0 and freed["D1"] == parser["ReturnedD0"] and
                    "ReturnedD0" in freed and not faults, name + ": source/switch binding and mutation")
            if candidate:
                unlock = one([e for e in calls if e["Name"] == "UnLock"], "Candidate lock release")
                fib = one([e for e in calls if e["Name"] == "AllocDosObject"], "Candidate FIB")
                release = one([e for e in calls if e["Name"] == "FreeDosObject"], "Candidate FIB release")
                require(unlock["D1"] == lock["ReturnedD0"] and "ReturnedD0" in unlock and
                        fib["D1"] == release["D1"] == 2 and fib.get("ReturnedD0", 0) != 0 and
                        release["D2"] == fib["ReturnedD0"] and "ReturnedD0" in release, "Candidate lock/FIB ownership")
        signatures.append({"case": name, "primary": primary, "secondary": secondary,
                           "parsed": source is not None, "linkSource": source,
                           "linkTarget": "RAM:link source" if source is not None else None,
                           "linkMode": "hard" if source is not None else None})
    removal = one([e for e in events if e["Name"] == "RemSegment" and e.get("RemovedSegmentList") == segment], "Resident removal")
    require(removal.get("ReturnedD0", 0) != 0 and events.index(removal) > last_close, "Removal before completion")
    tail = events[events.index(removal)+1:]
    readbacks = []
    # Repeated HARD has successful MakeLink/target evidence above; no independent
    # readback is claimed for that sixth alias in this fixed fixture.
    for path in ["RAM:positional", "RAM:reordered", "RAM:equals", "RAM:forced", 'RAM:quote"name']:
        matched = one([e for e in tail if e["Name"] == "MatchFirst" and e.get("Text") == path], "Readback path")
        opened = one([e for e in tail if e["Name"] == "Open" and e.get("Text") == path[4:]], "Readback open")
        require(matched.get("ReturnedD0") == 0 and matched.get("FileSize") == 15 and opened.get("ReturnedD0", 0) != 0 and
                events.index(matched) < events.index(opened), "Readback size/ordering")
        index, handle = events.index(opened), opened["ReturnedD0"]
        close = next((e for e in events[index+1:] if e["Name"] == "Close" and e["D1"] == handle and e["Task"] == opened["Task"]), None)
        require(close is not None and close.get("ReturnedD0", 0) != 0, "Readback close")
        payload = [e.get("ReturnedD0") for e in events[index+1:events.index(close)] if e["Name"] == "FGetC" and
                   e["D1"] == handle and e["Task"] == opened["Task"]]
        require(payload == list(b"option-payload\n"), "Independent option alias bytes")
        readbacks.append({"path": path, "bytes": 15, "payloadHex": bytes(payload).hex()})
    return {"cases": signatures, "readbacks": readbacks}


def verify(args):
    reference, rm = binding.bind(args.reference, args.reference_media, "reference", "c24b0af713e6f3a252d964c4545da29535bf6652394879a7dc4c57fdce7c02de")
    candidate, cm = binding.bind(args.candidate, args.candidate_media, "candidate", args.expected_candidate_sha256)
    require(rm["probe_sha256"] == cm["probe_sha256"] == STARTUP_SHA256 and rm["probe_bytes"] == cm["probe_bytes"] == 579,
            "Changed option script")
    for key in ("archiveSha256", "imageSha256", "romSha256", "emulatorAssemblySha256", "testAssemblySha256"):
        require(reference[key].lower() == candidate[key].lower(), "Changed runtime input: " + key)
    require(candidate["testAssemblySha256"].lower() == binding.digest(args.test_assembly), "Changed observer assembly")
    binding.verify_trx(args.reference_trx, reference)
    binding.verify_trx(args.candidate_trx, candidate)
    expected, actual = inspect(reference, False), inspect(candidate, True)
    require(expected == actual, "Original/candidate option behavior differs")
    paths = {k: getattr(args,k) for k in ("reference", "candidate", "reference_media", "candidate_media", "reference_trx", "candidate_trx", "test_assembly")}
    paths.update(verifier=Path(__file__), bindingVerifier=Path(binding.__file__),
                 commonVerifier=Path(__file__).with_name("verify_copy_boot_transfer.py"))
    return {"status": "workbench-makelink-option-matrix-verified", **actual,
            "scope": "Nine original-DOS ReadArgs scenarios for positional/reordered/equals syntax, quoted spaces/escaped quote, HARD/FORCE on a regular file, repeated switch and rejected duplicate/unknown/missing arguments. Six hard-link calls succeed and five aliases have independent post-removal readback. No interactive help/EOF, Latin-1, empty names, all path forms or complete option/profile admission.",
            "candidateBinarySha256": args.expected_candidate_sha256, "fullOptionsQualified": False,
            "fullProfileParity": False, "shippingQualified": False,
            "evidence": {k: {"path": str(p), "sha256": binding.digest(p)} for k,p in paths.items()}}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("reference", "candidate", "reference_media", "candidate_media", "reference_trx", "candidate_trx", "test_assembly"):
        parser.add_argument(name, type=Path)
    parser.add_argument("--expected-candidate-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Refusing to overwrite option evidence")
    result = verify(args)
    args.output.write_text(json.dumps(result, indent=2) + "\n")
    print("PASS: nine real ReadArgs option scenarios and five post-removal alias readbacks match")
