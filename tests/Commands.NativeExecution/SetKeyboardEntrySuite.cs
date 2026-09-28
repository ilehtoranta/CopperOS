using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record SetKeyboardEntryCase(
    bool ParserFailure = false,
    bool UtilityAvailable = true,
    bool KeymapAvailable = true,
    bool LoadSuccess = true,
    bool ExistingKeymap = false,
    bool ResourceAvailable = true,
    bool KeymapsLoadSuccess = true,
    bool MossysLoadSuccess = true,
    bool ExtendedNode = false,
    MorphOSKeymapSegmentShape MorphOSSegmentShape =
        MorphOSKeymapSegmentShape.Standard);

internal enum MorphOSKeymapSegmentShape
{
    Standard,
    Undersized,
    Overflowing,
    UnmappedEnd,
    Cyclic
}

internal sealed partial class ProbeFixture
{
    public const string Workbench31SetKeyboardEntrySuite =
        "workbench31-setkeyboard-native-entry-vector-fixture";
    public const string MorphOSSetKeyboardEntrySuite =
        "morphos320-setkeyboard-native-entry-vector-fixture";

    private const uint SetKeyboardUtilityBase = 0xa000;
    private const uint SetKeyboardKeymapBase = 0xb000;
    private const uint SetKeyboardResourceBase = 0xc000;
    private const uint SetKeyboardSegment = 0x14000;
    private const uint SetKeyboardNode = 0x14020;

    private List<object> RunSetKeyboardEntryCases()
    {
        ProbeCase[] cases =
        [
            SetKeyboardCase("loaded-keymap", new()),
            SetKeyboardCase("existing-keymap", new(ExistingKeymap: true)),
            SetKeyboardCase("load-failure", new(LoadSuccess: false),
                DOS.RETURN_FAIL, 205),
            SetKeyboardCase("keymap-library-failure",
                new(KeymapAvailable: false), DOS.RETURN_FAIL, 205),
            SetKeyboardCase("utility-library-failure",
                new(UtilityAvailable: false), DOS.RETURN_FAIL, 205),
            SetKeyboardCase("parser-failure", new(ParserFailure: true),
                DOS.RETURN_ERROR, 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, SetKeyboard = new() },
            new("missing-dos", "usa\n", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { MissingDos = true, SetKeyboard = new() }
        ];

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            SetKeyboardCase("interleaved-loaded-a", new()),
            SetKeyboardCase("interleaved-existing-b", new(ExistingKeymap: true))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase SetKeyboardCase(string name,
        SetKeyboardEntryCase definition, int result = DOS.RETURN_OK,
        int error = 0) => new(name, "usa\n", result, error, "")
        { SetKeyboard = definition };

    private void PrepareSetKeyboardEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetKeyboard!;
        var name = invocation.Arguments + 0x300;
        WriteCString(name, "usa");
        invocation.SetKeyboardName = name;
        Bus.Memory.AsSpan((int)SetKeyboardResourceBase, 128).Clear();
        if (definition.ExistingKeymap)
        {
            var list = SetKeyboardResourceBase + 14;
            Bus.Long(list, SetKeyboardNode);
            Bus.Long(SetKeyboardNode + (uint)ExecLayout.Node.Successor, 0);
            Bus.Long(SetKeyboardNode + (uint)ExecLayout.Node.Name, name);
            Bus.Long(SetKeyboardNode + 14, SetKeyboardNode + 14 + 32);
        }
    }

    private void RegisterSetKeyboardEntryExec()
    {
        Register(ExecBase, ExecLvo.CopyMem, "CopyMem",
            (state, invocation) =>
            {
                Require(state.D[0] == 13 && state.A[0] != 0 && state.A[1] != 0,
                    "SetKeyboard CopyMem ABI differs.");
                var destination = Bus.OwnedAllocation(invocation, state.A[1],
                    "SetKeyboardPath");
                var source = Bus.Memory.AsSpan((int)state.A[0], 13);
                source.CopyTo(Bus.Memory.AsSpan((int)destination.Address, 13));
                return 0;
            });
        Register(ExecBase, ExecLvo.OpenResource, "OpenResource",
            (state, invocation) =>
            {
                Require(Bus.CString(state.A[1]) == "keymap.resource",
                    "SetKeyboard resource name differs.");
                invocation.SetKeyboardOpenResourceCalls++;
                return invocation.Definition.SetKeyboard!.ExistingKeymap
                    ? SetKeyboardResourceBase : 0;
            });
        Register(SetKeyboardUtilityBase, UtilityLvo.Stricmp, "Stricmp",
            (state, invocation) =>
            {
                Require(state.A[0] != 0 && state.A[1] == invocation.SetKeyboardName,
                    "SetKeyboard utility Stricmp ABI differs.");
                invocation.SetKeyboardStricmpCalls++;
                return 0;
            });
        Register(SetKeyboardKeymapBase, -30, "SetKeyMapDefault",
            (state, invocation) =>
            {
                Require(state.A[0] != 0,
                    "SetKeyboard passed a null keymap.");
                invocation.SetKeyboardSetDefaultCalls++;
                return 0;
            });
    }

    private void RegisterSetKeyboardEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetKeyboard!;
            Require(Bus.CString(state.D[1]) == NativeSetKeyboardCommand.Template &&
                state.D[3] == 0 && state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 4 &&
                Bus.Long(state.D[2]) == 0,
                "SetKeyboard ReadArgs ABI differs.");
            invocation.SetKeyboardReadArgsCalls++;
            if (definition.ParserFailure)
            {
                invocation.IoError = 116;
                return 0;
            }
            Bus.Long(state.D[2], invocation.SetKeyboardName);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.SetKeyboardFreeArgsCalls++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.AddPart, "AddPart", (state,
            invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "DEVS:Keymaps" &&
                state.D[2] == invocation.SetKeyboardName && state.D[3] == 256,
                "SetKeyboard AddPart ABI differs.");
            invocation.SetKeyboardAddPartCalls++;
            WriteCString(state.D[1], "DEVS:Keymaps/usa");
            return 1;
        });
        Register(baseAddress, -150, "LoadSeg", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "DEVS:Keymaps/usa",
                $"SetKeyboard LoadSeg path differs: '{Bus.CString(state.D[1])}'.");
            invocation.SetKeyboardLoadSegCalls++;
            if (!invocation.Definition.SetKeyboard!.LoadSuccess)
            {
                invocation.IoError = 205;
                return 0;
            }
            return SetKeyboardSegment;
        });
        Register(baseAddress, -156, "UnLoadSeg", (state, invocation) =>
        {
            Require(state.D[1] == SetKeyboardSegment,
                "SetKeyboard unloaded the wrong segment.");
            invocation.SetKeyboardUnLoadSegCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.SetKeyboardPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifySetKeyboardEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetKeyboard!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.SetKeyboardReadArgsCalls == 0 &&
                invocation.SetKeyboardOpenResourceCalls == 0,
                "SetKeyboard crossed a startup boundary.");
            return;
        }

        var parser = definition.ParserFailure;
        Require(invocation.SetKeyboardReadArgsCalls == 1 &&
            invocation.SetKeyboardFreeArgsCalls == (parser ? 0 : 1),
            $"SetKeyboard parser/result ownership differs (parser={parser}, freeArgs={invocation.SetKeyboardFreeArgsCalls}).");
        if (parser)
        {
            Require(invocation.Allocations == 1 && invocation.FreeMem == 1,
                $"SetKeyboard parser failure ownership differs (allocations={invocation.Allocations}, free={invocation.FreeMem}).");
            Require(invocation.SetKeyboardOpenResourceCalls == 0 &&
                invocation.SetKeyboardSetDefaultCalls == 0,
                "SetKeyboard parser failure crossed the operation path.");
            return;
        }

        var existing = definition.ExistingKeymap;
        var loadFailure = !definition.LoadSuccess;
        var utilityFailure = !definition.UtilityAvailable;
        var keymapFailure = !definition.KeymapAvailable;
        var pathNeeded = !existing && !utilityFailure;
        var expectedAllocations = 1 + (pathNeeded ? 1 : 0);
        Require(invocation.SetKeyboardOpenResourceCalls == (utilityFailure ? 0 : 1) &&
            invocation.SetKeyboardStricmpCalls == (existing && !utilityFailure ? 1 : 0) &&
            invocation.SetKeyboardAddPartCalls == (existing || utilityFailure ? 0 : 1) &&
            invocation.SetKeyboardLoadSegCalls == (existing || utilityFailure ? 0 : 1) &&
            invocation.SetKeyboardUnLoadSegCalls ==
                (!existing && !utilityFailure && keymapFailure ? 1 : 0) &&
            invocation.SetKeyboardSetDefaultCalls ==
                (!utilityFailure && !loadFailure && !keymapFailure ? 1 : 0),
            $"SetKeyboard operation vector calls differ (existing={existing}, utilityFailure={utilityFailure}, loadFailure={loadFailure}, keymapFailure={keymapFailure}; openResource={invocation.SetKeyboardOpenResourceCalls}, stricmp={invocation.SetKeyboardStricmpCalls}, addPart={invocation.SetKeyboardAddPartCalls}, loadSeg={invocation.SetKeyboardLoadSegCalls}, unload={invocation.SetKeyboardUnLoadSegCalls}, default={invocation.SetKeyboardSetDefaultCalls}).");
        Require(invocation.Allocations == expectedAllocations &&
            invocation.FreeMem == expectedAllocations,
            "SetKeyboard path/result ownership differs.");
        Require(invocation.SetKeyboardUtilityOpens == 1 &&
            invocation.SetKeyboardUtilityCloses == (utilityFailure ? 0 : 1) &&
            invocation.SetKeyboardKeymapOpens == (utilityFailure || loadFailure ? 0 : 1) &&
            invocation.SetKeyboardKeymapCloses ==
                (!utilityFailure && !loadFailure && !keymapFailure ? 1 : 0),
            $"SetKeyboard library ownership differs (utility opens/closes={invocation.SetKeyboardUtilityOpens}/{invocation.SetKeyboardUtilityCloses}, keymap opens/closes={invocation.SetKeyboardKeymapOpens}/{invocation.SetKeyboardKeymapCloses}).");
    }
}
