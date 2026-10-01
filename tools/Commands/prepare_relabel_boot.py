"""Prepare paired disposable Relabel boot images; original media stays private."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/Commands/Inventory"))
import inventory

STARTUP = '''FailAt 21
Echo >RAM:relabel-proof "relabel-payload"
Resident C:Ed PURE
Ed RAM: "First Volume"
Type "First Volume:relabel-proof"
Ed NAME="Second Volume" DRIVE=RAM:
Type "Second Volume:relabel-proof"
Assign RLC: RAM:
Ed RLC: AliasVolume
Type AliasVolume:relabel-proof
Ed >RAM:rl-diag1 RAM Trimmed
Ed >RAM:rl-diag2 RAM: "Bad:Name"
Ed >RAM:rl-diag3 DRIVE=RAM:
Ed >RAM:rl-diag4 DRIVE=RAM: NAME=Ignored EXTRA
Ed RAM: FinalVolume
Type FinalVolume:relabel-proof
Resident Ed REMOVE
Type RAM:rl-diag1
Type RAM:rl-diag2
Type RAM:rl-diag3
Type RAM:rl-diag4
Wait 300
'''

ARGUMENT_STARTUP = '''FailAt 21
Echo >RAM:relabel-proof "relabel-payload"
Resident C:Ed PURE
Ed >RAM:ra01 drive=RAM: name=Lower
Type Lower:relabel-proof
Ed >RAM:ra02 DRIVE=RAM: DRIVE=DF0: NAME=DuplicateDrive
Ed >RAM:ra03 DRIVE=RAM: NAME=First NAME=Second
Ed >RAM:ra04 DRIVE=RAM: NAME=Ignored UNKNOWN
Ed >RAM:ra05 RAM: "Quote*"Name"
Type "Quote*"Name:relabel-proof"
Ed >RAM:ra06 RAM: "Star**Name"
Type "Star**Name:relabel-proof"
Ed >RAM:ra07 RAM: ""
Ed >RAM:ra08 RAM: Recovery
Type Recovery:relabel-proof
Resident Ed REMOVE
Type RAM:ra01
Type RAM:ra02
Type RAM:ra03
Type RAM:ra04
Type RAM:ra05
Type RAM:ra06
Type RAM:ra07
Type RAM:ra08
Wait 300
'''

HELP_HANDLER_STARTUP = '''FailAt 21
Echo >RAM:relabel-proof "relabel-payload"
Echo >RAM:rh-input "RAM: HelpVolume"
Resident C:Ed PURE
Ed >RAM:rh01 DF0: MustNotChange
Ed <NIL: >RAM:rh02 ?
Ed <RAM:rh-input >RAM:rh03 ?
Type HelpVolume:relabel-proof
Ed >RAM:rh04 RAM: Recovery
Type Recovery:relabel-proof
Resident Ed REMOVE
Type RAM:rh01
Type RAM:rh02
Type RAM:rh03
Type RAM:rh04
Wait 300
'''

# Keep the protected-disk probe intact. A requester can block that first call;
# help-only isolates independent parser coverage without claiming it completed.
HELP_STARTUP = HELP_HANDLER_STARTUP.replace("Ed >RAM:rh01 DF0: MustNotChange\n", "").replace("Type RAM:rh01\n", "")
CONCURRENT_STARTUP = '''FailAt 21
Echo >RAM:relabel-proof "relabel-payload"
Echo >RAM:rl-worker "FailAt 21*nEd >RAM:rc01 DF0: MustNotChange*nEcho >RAM:rl-done done"
Resident C:Ed PURE
Run Execute RAM:rl-worker
ChangeTaskPri -1
Ed >RAM:rc02 RAM: ConcurrentVolume
Type ConcurrentVolume:relabel-proof
Lab WaitDone
If EXISTS RAM:rl-done
Skip Done
EndIf
Skip BACK WaitDone
Lab Done
Ed >RAM:rc03 RAM: Recovery
Type Recovery:relabel-proof
Resident Ed REMOVE
Type RAM:rc01
Type RAM:rc02
Type RAM:rc03
Wait 300
'''
SCENARIOS = {"baseline": STARTUP, "arguments": ARGUMENT_STARTUP,
    "help-handler": HELP_HANDLER_STARTUP, "help": HELP_STARTUP,
    "concurrent": CONCURRENT_STARTUP}
ACTIVE_REMOVE_STARTUP = CONCURRENT_STARTUP.replace(
    "ChangeTaskPri -1\n", "ChangeTaskPri -1\nResident >RAM:rc-remove Ed REMOVE\n"
).replace("Type RAM:rc01\n", "Type RAM:rc-remove\nType RAM:rc01\n").replace(
    "Type ConcurrentVolume:relabel-proof\n", "Type >NIL: ConcurrentVolume:relabel-proof\n")
SCENARIOS["active-remove"] = ACTIVE_REMOVE_STARTUP
ACTIVE_REPLACE_STARTUP = ACTIVE_REMOVE_STARTUP.replace(
    "Resident >RAM:rc-remove Ed REMOVE\n", "Resident >RAM:rc-replace Ed C:Ed REPLACE PURE\n"
).replace("Type RAM:rc-remove\n", "Type RAM:rc-replace\n")
SCENARIOS["active-replace"] = ACTIVE_REPLACE_STARTUP
IDLE_REPLACE_STARTUP = '''FailAt 21
Echo >RAM:relabel-proof "relabel-payload"
Resident C:Ed PURE
Ed >RAM:rr01 RAM: BeforeReplace
Type >NIL: BeforeReplace:relabel-proof
Resident >RAM:rr-replace Ed C:Ed REPLACE PURE
Ed >RAM:rr02 RAM: AfterReplace
Type >NIL: AfterReplace:relabel-proof
Ed >RAM:rr03 RAM: Recovery
Type >NIL: Recovery:relabel-proof
Resident >RAM:rr-remove Ed REMOVE
Type RAM:rr01
Type RAM:rr02
Type RAM:rr03
Type RAM:rr-replace
Type RAM:rr-remove
Wait 300
'''
SCENARIOS["idle-replace"] = IDLE_REPLACE_STARTUP
FAILED_REPLACE_STARTUPS = {
    "replace-missing": IDLE_REPLACE_STARTUP.replace("Ed C:Ed REPLACE PURE", "Ed C:MissingRelabel REPLACE PURE"),
    "replace-invalid": IDLE_REPLACE_STARTUP.replace("Ed C:Ed REPLACE PURE", "Ed RAM:relabel-proof REPLACE PURE"),
}
SCENARIOS.update(FAILED_REPLACE_STARTUPS)
PERSISTENT_STARTUP = '''FailAt 21
MakeDir RAM:ENV
Assign ENV: RAM:ENV
Echo >RAM:rl-mount "DF1:*nDevice=trackdisk.device*nUnit=1*nSurfaces=2*nBlocksPerTrack=11*nReserved=2*nLowCyl=0*nHighCyl=79*nBuffers=5*nBufMemType=1*nStackSize=4000*nPriority=5*nGlobVec=-1*nDosType=0x444F5301*nActivate=1*n#"
Mount DF1: FROM RAM:rl-mount
Resident C:Ed PURE
Type >NIL: DF1:proof
Type >NIL: FixtureDisk:proof
Ed >RAM:rp01 DF1: FirstDisk
Type >NIL: FirstDisk:proof
Ed >RAM:rp02 FirstDisk: SavedDisk
Type >NIL: SavedDisk:proof
Resident Ed REMOVE
Type RAM:rp01
Type RAM:rp02
Wait 300
'''
SCENARIOS["persistent-volume"] = PERSISTENT_STARTUP
COLD_REMOUNT_STARTUP = PERSISTENT_STARTUP.split("Resident C:Ed PURE\n", 1)[0] + '''Type >NIL: DF1:proof
Type >NIL: SavedDisk:proof
Type >NIL: SavedDisk:nested/guard
Wait 300
'''
SCENARIOS["cold-remount"] = COLD_REMOUNT_STARTUP
NAME_BOUNDARIES = {"name30": "N" * 30, "name31": "N" * 31, "name-empty": "", "name-slash": "Left/Right"}
for boundary, label in NAME_BOUNDARIES.items():
    SCENARIOS[boundary] = PERSISTENT_STARTUP.split("Resident C:Ed PURE\n", 1)[0] + f'''Resident C:Ed PURE
Ed >RAM:rn01 DF1: "{label}"
Type >NIL: DF1:proof
Ed >RAM:rn02 DF1: SavedDisk
Type >NIL: SavedDisk:proof
Resident Ed REMOVE
Type RAM:rn01
Type RAM:rn02
Wait 300
'''


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--original", type=Path, required=True)
    parser.add_argument("--qualification", type=Path, required=True)
    parser.add_argument("--private-directory", type=Path, required=True)
    parser.add_argument("--output-directory", type=Path, required=True)
    parser.add_argument("--scenario", choices=SCENARIOS, default="baseline")
    args = parser.parse_args()
    for directory in [args.private_directory, args.output_directory]:
        if directory.exists():
            raise ValueError("Use fresh directories; do not replace prior media or evidence.")
    if args.private_directory.resolve().is_relative_to(ROOT.resolve()) or args.original.resolve().is_relative_to(ROOT.resolve()):
        raise ValueError("Original and derivative media must remain outside the repository.")
    source = inventory.WB_SOURCES[1]
    if digest(args.archive) != source["zip_sha256"]:
        raise ValueError("Wrong Workbench archive.")
    if digest(args.original) != "163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff":
        raise ValueError("Wrong original Relabel 37.2.")
    qualification = json.loads(args.qualification.read_text(encoding="utf-8-sig"))
    if qualification["status"] != "passed" or qualification["suite"] != "CC12-Relabel-wb31-reference-vectors":
        raise ValueError("Workbench Relabel native comparison receipt required.")
    row = next(a for a in qualification["artifacts"] if a["cpu"] == "68000")
    candidate = args.qualification.resolve().parent / "relabel-wb31-68000.hunk"
    if digest(candidate) != row["sha256"]:
        raise ValueError("Candidate differs from native comparison receipt.")
    args.output_directory.mkdir(parents=True)
    args.private_directory.mkdir(parents=True)
    startup = args.output_directory.resolve() / "startup.txt"
    startup.write_bytes(SCENARIOS[args.scenario].encode("ascii"))
    for role, binary in [("reference", args.original.resolve()), ("candidate", candidate)]:
        receipt = args.output_directory.resolve() / (role + "-media.json")
        command = [sys.executable, str(ROOT / "tools/Commands/Inventory/prepare_execute_fixture_adf.py"),
            "--archive", str(args.archive.resolve()), "--startup", str(startup),
            "--output", str(args.private_directory.resolve() / (role + ".adf")),
            "--receipt", str(receipt), "--replace-file", "C/Ed", str(binary)]
        subprocess.run(command, check=True, stdout=subprocess.DEVNULL)
        data = json.loads(receipt.read_text(encoding="utf-8"))
        data.update(command_under_test="Relabel", binary_role="workbench31-" + role,
            scenario="ram-volume-keywords-assign-errors-recovery" if args.scenario == "baseline" else args.scenario,
            startup_sha256=digest(startup),
            native_qualification_sha256=digest(args.qualification),
            preparation_script_sha256=digest(Path(__file__)))
        receipt.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
        print(receipt)


if __name__ == "__main__":
    main()
