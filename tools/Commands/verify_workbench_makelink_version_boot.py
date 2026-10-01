"""Verify a versioned MakeLink boot and original Version's complete RAM output.

This is new original-DOS evidence for the versioned 68000 candidate, not a
transfer of old-hash reports or full resident/shipping qualification.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct

from append_hunk_version import parse_hunk
from verify_copy_boot_transfer import one, require
from verify_workbench_makelink_error_refresh import bind, digest, verify_trx
from verify_workbench_makelink_output import SCRIPT as MAKE_SCRIPT, inspect_output, readback
from Inventory.prepare_execute_fixture_adf import inventory, load_image


CANDIDATE_SHA256 = "12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45"
VERSION_SHA256 = "dc2f55cd48b37bd1efecdbf07d38757463129dd9076ea6bf1b5d967f2189f224"
VERSION_TAG = b"$VER: MakeLink 0.1 (8.9.2026) CopperOS wb31"
VERSION_TEMPLATE = "NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S"
VERSION_OUTPUT = b"MakeLink 0.1\nCopperOS wb31\n"
SCRIPT = MAKE_SCRIPT.replace(b"Wait 300\n", b"C:Version >RAM:out-version C:Ed FILE FULL\nC:Type >NIL: RAM:out-version\nWait 300\n")
EXPECTED_OUTPUTS = (b"", b"required argument missing\n", b"Can't find RAM:absent object not found\n",
                    b"object already exists\n", b"")


def verify_media(trace, media, startup):
    require(startup.read_bytes() == SCRIPT and b"\r" not in SCRIPT, "Wrong full-readback startup")
    require(media["probe_bytes"] == len(SCRIPT) and media["probe_sha256"] == hashlib.sha256(SCRIPT).hexdigest(), "Script receipt binding")
    output = Path(media["output_path"])
    require(digest(output) == media["output_adf_sha256"], "Derivative disk changed")
    disk = inventory.Adf(output.read_bytes())
    tree = disk.walk()
    archive = Path(media["reference_archive"])
    member, original_bytes = load_image(archive)
    require(digest(archive) == trace["archiveSha256"].lower() and member == media["archive_member"] and
            hashlib.sha256(original_bytes).hexdigest() == trace["imageSha256"].lower(), "Original disk binding")
    original = inventory.Adf(original_bytes)
    original_tree = original.walk()
    require(set(tree) == set(original_tree), "Derivative directory membership changed")
    require(disk.read_file(tree["s/startup-sequence"]["block"]) == SCRIPT, "Actual disk startup changed")
    candidate = disk.read_file(tree["c/ed"]["block"])
    require(len(candidate) == 3232 and hashlib.sha256(candidate).hexdigest() == CANDIDATE_SHA256 and
            b"\0" + VERSION_TAG + b"\0" in candidate, "Versioned MakeLink bytes/tag absent")
    version = disk.read_file(tree["c/version"]["block"])
    require(len(version) == 4764 and hashlib.sha256(version).hexdigest() == VERSION_SHA256 and
            VERSION_TEMPLATE.encode("ascii") + b"\0" in version and
            b"$VER: version 40.1 (9.2.93)\0" in version, "Original Version command identity/template")
    require(struct.unpack(">8I", version[:32]) == (0x3F3, 0, 1, 0, 0, 1182, 0x3E9, 1182), "Version loaded CODE extent")
    # Check every other original regular file. The fixture is not allowed to
    # replace Version, Type, Echo, Shell or another unlisted helper.
    unchanged = []
    for name, entry in tree.items():
        require(entry["secondary_type"] == original_tree[name]["secondary_type"], "Disk entry type changed: " + name)
        if name not in ("c/ed", "s/startup-sequence") and entry["secondary_type"] == -3:
            require(disk.read_file(entry["block"]) == original.read_file(original_tree[name]["block"]), "Unlisted disk file modified: " + name)
            unchanged.append(name)
    require("c/version" in unchanged and "c/type" in unchanged, "Original reader/helper verification absent")
    require(trace.get("referenceMediaUnmodified") is True and trace.get("diskWriteProtected") is True, "Media protection")
    return candidate, unchanged


def inspect(trace):
    events = trace["observedDosCalls"]
    version_load = one([v for v in events if v["Name"] == "LoadSeg" and v.get("Text") == "C:Version"], "Original Version load")
    version_start = events.index(version_load)
    # The five-case checker intentionally owns the first command section only.
    # Version subsequently LoadSeg/UnLoadSegs C:Ed to inspect its metadata. That
    # second load is checked below and must not masquerade as a sixth invocation.
    command_section = dict(trace, observedDosCalls=events[:version_start])
    cases = inspect_output(command_section, True)
    require([item["final_ioerr"] for item in cases] == [205, 116, 205, 203, 205], "MakeLink final errors")
    require([bytes.fromhex(item["rendered_hex"]) for item in cases] == list(EXPECTED_OUTPUTS), "MakeLink exact diagnostics")
    require(len(events) < 2048 and not trace.get("dosObservationOverflow"), "Full trace is truncated")
    segment = trace["copyInvocationOwnership"][0]["Segment"]
    task = trace["copyInvocationOwnership"][0]["Task"]
    removal = one([v for v in events if v["Name"] == "RemSegment" and v.get("RemovedSegmentList") == segment], "Original resident removal")
    require(events.index(removal) < version_start and removal.get("ReturnedD0") == 0xFFFFFFFF, "Version ran before resident removal")
    require(version_load["Task"] == task and version_load.get("ReturnedD0", 0) != 0, "Version loader success/task")
    version_segment = version_load["ReturnedD0"]
    version_code = version_segment * 4 + 4
    version_unload = one([v for v in events[version_start + 1:] if v["Name"] == "UnLoadSeg" and
                          v["D1"] == version_segment and v["Task"] == task], "Version unload")
    version_end = events.index(version_unload)
    version_events = events[version_start + 1:version_end]
    run = one([v for v in version_events if v["Name"] == "RunCommand" and v["D1"] == version_segment], "Original Version RunCommand")
    require(run["Task"] == task and run["D2"] == 4096 and run.get("ReturnedD0") == 0 and run.get("ReturnedIoErr") == 0 and
            version_unload.get("ReturnedD0") == 0xFFFFFFFF, "Original Version completion")
    parser = one([v for v in version_events if v["Name"] == "ReadArgs"], "Version real ReadArgs")
    require(parser.get("Text") == VERSION_TEMPLATE and parser.get("ReturnedD0", 0) != 0, "Version parser/template")
    parser_free = one([v for v in version_events if v["Name"] == "FreeArgs"], "Version parser cleanup")
    require(parser_free["D1"] == parser["ReturnedD0"] and "ReturnedD0" in parser_free, "Version parser ownership")
    output_open = one([v for v in version_events if v["Name"] == "Open" and v.get("Text") == "RAM:out-version"], "Version output creation")
    require(output_open["Task"] == task and output_open["D2"] == 1006 and output_open.get("ReturnedD0", 0) != 0 and
            events.index(output_open) < events.index(run) < events.index(parser), "Version redirection ordering")
    output_close = one([v for v in version_events if v["Name"] == "Close" and v["D1"] == output_open["ReturnedD0"] and v["Task"] == task], "Version output close")
    require(output_close.get("ReturnedD0") == 0xFFFFFFFF and events.index(parser_free) < events.index(output_close), "Version output completion")
    probe_open = one([v for v in version_events if v["Name"] == "Open" and v.get("Text") == "C:Ed"], "Version inspected selected file")
    require(probe_open["Task"] == task and probe_open["D2"] == 1005 and probe_open.get("ReturnedD0", 0) != 0, "Version file open")
    inspection_load = one([v for v in version_events if v["Name"] == "LoadSeg" and v.get("Text") == "C:Ed"], "Version metadata LoadSeg")
    inspection_segment = inspection_load.get("ReturnedD0", 0)
    inspection_unload = one([v for v in version_events if v["Name"] == "UnLoadSeg" and v["D1"] == inspection_segment], "Version metadata unload")
    require(inspection_segment != 0 and inspection_unload.get("ReturnedD0") == 0xFFFFFFFF and
            events.index(probe_open) < events.index(inspection_load) < events.index(inspection_unload) < events.index(parser_free), "Version metadata load lifetime")
    require(not any(v["Name"] == "RunCommand" and v["D1"] == inspection_segment for v in version_events), "Version executed candidate while inspecting it")
    for call in (parser, parser_free, probe_open, inspection_load, inspection_unload):
        require(call["Task"] == task and version_code <= call["CallerPc"] < version_code + 4728,
                "Version public call not from original loaded Version CODE")
    candidate_loads = [v for v in events if v["Name"] == "LoadSeg" and v.get("Text") == "C:Ed"]
    require(len(candidate_loads) == 2 and candidate_loads[0]["ReturnedD0"] == segment and candidate_loads[1] is inspection_load,
            "Unexpected candidate load chronology")
    payload = readback(events, events.index(removal), "RAM:out-version", VERSION_OUTPUT)
    output_read = one([v for v in events if v["Name"] == "Open" and v.get("Text") == "out-version"], "Version output reader")
    require(events.index(output_read) > version_end, "Version readback before Version completed")
    nil_open = one([v for v in events[version_end + 1:events.index(output_read)] if v["Name"] == "Open" and v.get("Text") == "NIL:"], "Complete readback requires NIL redirection")
    require(nil_open["Task"] == task and nil_open["D2"] == 1006 and nil_open.get("ReturnedD0", 0) != 0, "NIL redirection identity")
    nil_close = one([v for v in events[events.index(output_read) + 1:] if v["Name"] == "Close" and v["D1"] == nil_open["ReturnedD0"] and v["Task"] == task], "NIL reader output close")
    require(nil_close.get("ReturnedD0") == 0xFFFFFFFF, "NIL reader output cleanup")
    return {"makelink_cases": cases,
            "version": {"return": run["ReturnedD0"], "final_ioerr": run["ReturnedIoErr"],
                        "rendered_bytes": len(payload), "rendered_hex": payload.hex(), "rendered_latin1": payload.decode("latin1"),
                        "embedded_version_id": VERSION_TAG.decode("ascii"), "original_version": "40.1 (9.2.93)",
                        "metadata_load_segment": inspection_segment,
                        "note": "Original Version FULL prints name/version and CopperOS profile in this fixture; its rendered output does not include the embedded date."},
            "section_event_ranges": {"makelink_and_readbacks": [0, version_start], "version_and_readback": [version_start, len(events)]},
            "original_resident_segment": segment, "event_count": len(events)}


def verify(args):
    trace, media = bind(args.observations, args.media, "candidate", CANDIDATE_SHA256)
    verify_trx(args.trx, trace)
    candidate, unchanged = verify_media(trace, media, args.startup)
    require(digest(args.test_assembly) == trace["testAssemblySha256"].lower(), "Observer identity changed")
    emulator = args.test_assembly.with_name("CopperMod.Amiga.Emulator.dll")
    require(digest(emulator) == trace["emulatorAssemblySha256"].lower() and digest(args.rom) == trace["romSha256"].lower(), "Emulator/ROM identity changed")
    result = inspect(trace)
    # The unchanged observer stores the most recently loaded C:Ed image. Here
    # that is Version's second, non-executed metadata load, not the resident load.
    parsed = parse_hunk(candidate)
    loaded = bytearray(parsed.code)
    base = result["version"]["metadata_load_segment"] * 4 + 4
    for offset in parsed.relocations:
        struct.pack_into(">I", loaded, offset, (struct.unpack_from(">I", loaded, offset)[0] + base) & 0xFFFFFFFF)
    require(trace["loadedCopyCodeBytes"] == len(loaded) and hashlib.sha256(loaded).hexdigest() == trace["loadedCopyImageSha256"].lower(),
            "Version metadata load bytes do not match relocated candidate CODE")
    paths = {name: getattr(args, name) for name in ("observations", "media", "trx", "startup", "test_assembly", "rom")}
    paths.update(emulator=emulator, candidate=Path(media["replacements"][0]["local_file"]), verifier=Path(__file__),
                 output_verifier=Path(__file__).with_name("verify_workbench_makelink_output.py"),
                 behavior_verifier=Path(__file__).with_name("compare_workbench_makelink_errors.py"),
                 binding_verifier=Path(__file__).with_name("verify_workbench_makelink_error_refresh.py"),
                 hunk_parser=Path(__file__).with_name("append_hunk_version.py"),
                 media_reader=Path(__file__).parent / "Inventory/inventory.py")
    return {"schema_version": 1, "status": "versioned-workbench-makelink-original-dos-and-version-passed",
            "candidate_binary_sha256": CANDIDATE_SHA256, "candidate_binary_bytes": len(candidate),
            "original_version_command_sha256": VERSION_SHA256, "unchanged_original_files": unchanged,
            "machine": trace["machine"], "provider": trace["provider"], "test_status": "Passed", **result,
            "shipping_qualified": False, "pure_admitted": False, "full_profile_parity": False,
            "minimum_stack_qualified": False,
            "scope": "Fresh 68000 versioned HUNK: five original-DOS MakeLink runs, exact redirected diagnostics/final errors, balanced direct allocations/locks/FIBs and independent post-removal alias contents; original C:Version discovers tag through file/load inspection and complete 27-byte RAM output is independently read. Unchanged observer, ROM/media and 32x250000 bound. Not full startup/profile, cross-CPU real-OS, stack minimum or shipping/resident admission.",
            "evidence": {name: {"path": str(path.resolve()), "sha256": digest(path)} for name, path in paths.items()}}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("observations", "media", "trx", "startup", "test_assembly", "rom"):
        parser.add_argument(name, type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Refusing to overwrite evidence")
    result = verify(args)
    with args.output.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
