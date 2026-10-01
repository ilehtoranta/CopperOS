using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    private List<object> RunMorphOSSetKeyboardEntryCases()
    {
        ProbeCase[] cases
        = [
            MorphOSSetKeyboardCase("loaded-keymaps", "usa\n", new()),
            MorphOSSetKeyboardCase("existing-keymap", "usa\n",
                new(ExistingKeymap: true)),
            MorphOSSetKeyboardCase("fallback-to-mossys", "usa\n",
                new(KeymapsLoadSuccess: false)),
            MorphOSSetKeyboardCase("extended-keymap-node", "usa\n",
                new(ExtendedNode: true)),
            MorphOSSetKeyboardCase("undersized-segment", "usa\n",
                new(MorphOSSegmentShape: MorphOSKeymapSegmentShape.Undersized)),
            MorphOSSetKeyboardCase("overflowing-segment-size", "usa\n",
                new(MorphOSSegmentShape: MorphOSKeymapSegmentShape.Overflowing)),
            MorphOSSetKeyboardCase("unmapped-segment-end", "usa\n",
                new(MorphOSSegmentShape: MorphOSKeymapSegmentShape.UnmappedEnd)),
            MorphOSSetKeyboardCase("cyclic-segment-list", "usa\n",
                new(MorphOSSegmentShape: MorphOSKeymapSegmentShape.Cyclic)),
            MorphOSSetKeyboardCase("absolute-path", "SYS:Devs/Keymaps/usa\n",
                new()),
            MorphOSSetKeyboardCase("load-failure", "usa\n",
                new(KeymapsLoadSuccess: false, MossysLoadSuccess: false),
                DOS.RETURN_FAIL, 205),
            MorphOSSetKeyboardCase("resource-failure", "usa\n",
                new(ResourceAvailable: false), DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary),
            MorphOSSetKeyboardCase("keymap-library-failure", "usa\n",
                new(KeymapAvailable: false), DOS.RETURN_FAIL, 205),
            MorphOSSetKeyboardCase("utility-library-failure", "usa\n",
                new(UtilityAvailable: false), DOS.RETURN_FAIL, 205),
            MorphOSSetKeyboardCase("parser-failure", "usa\n",
                new(ParserFailure: true), DOS.RETURN_ERROR, 116),
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
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase MorphOSSetKeyboardCase(string name,
        string arguments, SetKeyboardEntryCase definition,
        int result = DOS.RETURN_OK, int error = 0) =>
        new(name, arguments, result, error, "") { SetKeyboard = definition };

    private void PrepareMorphOSSetKeyboardEntry(Invocation invocation)
    {
        var value = invocation.Definition.Arguments.Trim();
        var name = invocation.Arguments + 0x300;
        WriteCString(name, value);
        invocation.SetKeyboardName = name;
        Bus.Memory.AsSpan((int)SetKeyboardResourceBase, 128).Clear();

        var filePartOffset = FilePartOffset(value);
        var filePart = name + (uint)filePartOffset;
        // Model the segment returned by LoadSeg as invocation-owned memory.
        // The BPTR points four bytes into the allocation because BADDR(seg)-4
        // is the HUNK link pointer inspected by FindLibResident.
        invocation.SetKeyboardSegmentAllocation = Bus.Allocate(invocation, 128,
            "SetKeyboardSegment", true);
        invocation.SetKeyboardSegmentAddress = invocation.SetKeyboardSegmentAllocation + 4;
        invocation.SetKeyboardSegmentRaw = invocation.SetKeyboardSegmentAddress >> 2;
        invocation.SetKeyboardSegmentAllocated = true;
        var segmentAddress = invocation.SetKeyboardSegmentAddress;
        var shape = invocation.Definition.SetKeyboard!.MorphOSSegmentShape;
        var segmentWords = shape switch
        {
            MorphOSKeymapSegmentShape.Undersized => 1u,
            MorphOSKeymapSegmentShape.Overflowing => 0x40000000u,
            MorphOSKeymapSegmentShape.UnmappedEnd => 0x00100000u,
            _ => 16u
        };
        var nextSegment = shape == MorphOSKeymapSegmentShape.Cyclic
            ? invocation.SetKeyboardSegmentRaw : 0u;
        Bus.Long(segmentAddress - 4, segmentWords);
        Bus.Long(segmentAddress, nextSegment);
        var segmentNode = segmentAddress + 4;
        Bus.Long(segmentNode + (uint)ExecLayout.Node.Successor, 0);
        Bus.Long(segmentNode + (uint)ExecLayout.Node.Name, filePart);
        Bus.Memory[segmentNode + (uint)ExecLayout.Node.Type] =
            invocation.Definition.SetKeyboard!.ExtendedNode
                ? (byte)NodeType.Extended : (byte)0;
        Bus.Memory[segmentNode + (uint)ExecLayout.Node.Priority] =
            invocation.Definition.SetKeyboard!.ExtendedNode ? (byte)'E' : (byte)0;

        if (invocation.Definition.SetKeyboard!.ExistingKeymap)
        {
            var list = SetKeyboardResourceBase + 14;
            Bus.Long(list, SetKeyboardNode);
            Bus.Long(SetKeyboardNode + (uint)ExecLayout.Node.Successor, 0);
            Bus.Long(SetKeyboardNode + (uint)ExecLayout.Node.Name, filePart);
            Bus.Long(SetKeyboardNode + 14, SetKeyboardNode + 14 + 32);
        }
    }

    private static int FilePartOffset(string value)
    {
        var slash = value.LastIndexOf('/');
        var colon = value.LastIndexOf(':');
        return Math.Max(slash, colon) + 1;
    }

    private void RegisterMorphOSSetKeyboardEntryExec()
    {
        Register(ExecBase, ExecLvo.TypeOfMem, "MorphOS SetKeyboard TypeOfMem",
            (state, invocation) =>
            {
                Require(state.A[6] == ExecBase,
                    "MorphOS SetKeyboard TypeOfMem base ABI differs.");
                invocation.SetKeyboardTypeOfMemCalls++;
                var allocation = invocation.SetKeyboardSegmentAllocation;
                var address = state.A[1];
                return address >= allocation && address - allocation < 128
                    ? 1u : 0u;
            });
        Register(ExecBase, ExecLvo.CopyMem, "CopyMem",
            (state, invocation) =>
            {
                Require(state.D[0] is 9u or 20u && state.A[0] != 0 &&
                    state.A[1] != 0, "MorphOS SetKeyboard CopyMem ABI differs.");
                var destination = Bus.OwnedAllocation(invocation, state.A[1],
                    "SetKeyboardPath");
                var text = Bus.CString(state.A[0]);
                var bytes = System.Text.Encoding.Latin1.GetBytes(text + "\0");
                Require(bytes.Length == state.D[0],
                    "MorphOS SetKeyboard path prefix length differs.");
                bytes.CopyTo(Bus.Memory.AsSpan((int)destination.Address,
                    bytes.Length));
                return 0;
            });
        Register(ExecBase, ExecLvo.OpenResource, "OpenResource",
            (state, invocation) =>
            {
                Require(Bus.CString(state.A[1]) == "keymap.resource",
                    "MorphOS SetKeyboard resource name differs.");
                invocation.SetKeyboardOpenResourceCalls++;
                return invocation.Definition.SetKeyboard!.ResourceAvailable
                    ? SetKeyboardResourceBase : 0;
            });
        Register(ExecBase, ExecLvo.AddHead, "AddHead",
            (state, invocation) =>
            {
                Require(state.A[0] == SetKeyboardResourceBase + 14 &&
                    state.A[1] == invocation.SetKeyboardSegmentAddress + 4,
                    "MorphOS SetKeyboard AddHead ABI differs.");
                Bus.Long(state.A[0], state.A[1]);
                Bus.Long(state.A[1] + (uint)ExecLayout.Node.Predecessor, 0);
                return 0;
            });
        Register(ExecBase, ExecLvo.Remove, "Remove",
            (state, invocation) =>
            {
                Require(state.A[1] == invocation.SetKeyboardSegmentAddress + 4,
                    "MorphOS SetKeyboard removed the wrong node.");
                Bus.Long(SetKeyboardResourceBase + 14, 0);
                return 0;
            });
        Register(SetKeyboardUtilityBase, UtilityLvo.Stricmp, "Stricmp",
            (state, invocation) =>
            {
                Require(state.A[0] != 0 && state.A[1] != 0 &&
                    Bus.CString(state.A[0]).Equals(Bus.CString(state.A[1]),
                        StringComparison.OrdinalIgnoreCase),
                    "MorphOS SetKeyboard utility Stricmp ABI differs.");
                invocation.SetKeyboardStricmpCalls++;
                return invocation.Definition.SetKeyboard!.ExistingKeymap ? 0u : 1u;
            });
        Register(SetKeyboardKeymapBase, -30, "SetKeyMapDefault",
            (state, invocation) =>
            {
                Require(state.A[0] != 0,
                    "MorphOS SetKeyboard passed a null keymap.");
                invocation.SetKeyboardSetDefaultCalls++;
                return 0;
            });
    }

    private void RegisterMorphOSSetKeyboardEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetKeyboard!;
            Require(Bus.CString(state.D[1]) ==
                NativeMorphOSSetKeyboardCommand.Template && state.D[3] == 0 &&
                state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 4 &&
                Bus.Long(state.D[2]) == 0,
                "MorphOS SetKeyboard ReadArgs ABI differs.");
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
        Register(baseAddress, DosLvo.FilePart, "FilePart", (state,
            invocation) =>
        {
            var text = Bus.CString(state.D[1]);
            var offset = FilePartOffset(text);
            return state.D[1] + (uint)offset;
        });
        Register(baseAddress, DosLvo.AddPart, "AddPart", (state,
            invocation) =>
        {
            var path = Bus.CString(state.D[1]);
            Require(state.D[2] == invocation.SetKeyboardName &&
                state.D[3] == 256 &&
                (path == "KEYMAPS:" || path == "MOSSYS:Devs/Keymaps"),
                "MorphOS SetKeyboard AddPart ABI differs.");
            invocation.SetKeyboardAddPartCalls++;
            WriteCString(state.D[1], path.EndsWith(":", StringComparison.Ordinal)
                ? path + "usa" : path + "/usa");
            return 1;
        });
        Register(baseAddress, -150, "LoadSeg", (state, invocation) =>
        {
            var path = Bus.CString(state.D[1]);
            Require(path == "KEYMAPS:usa" || path == "MOSSYS:Devs/Keymapsusa" ||
                path == "MOSSYS:Devs/Keymaps/usa" ||
                path == "SYS:Devs/Keymaps/usa",
                $"MorphOS SetKeyboard LoadSeg path differs: '{path}'.");
            invocation.SetKeyboardLoadSegCalls++;
            var definition = invocation.Definition.SetKeyboard!;
            var success = path.StartsWith("KEYMAPS:",
                    StringComparison.OrdinalIgnoreCase)
                ? definition.KeymapsLoadSuccess
                : definition.MossysLoadSuccess;
            if (!success)
            {
                invocation.IoError = 205;
                return 0;
            }
            return invocation.SetKeyboardSegmentRaw;
        });
        Register(baseAddress, -156, "UnLoadSeg", (state, invocation) =>
        {
            Require(state.D[1] == invocation.SetKeyboardSegmentRaw,
                "MorphOS SetKeyboard unloaded the wrong segment.");
            invocation.SetKeyboardUnLoadSegCalls++;
            if (invocation.SetKeyboardSegmentAllocated)
            {
                Bus.Release(invocation, invocation.SetKeyboardSegmentAllocation,
                    "SetKeyboardSegment");
                invocation.SetKeyboardSegmentAllocated = false;
            }
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

    private void ReclaimMorphOSSetKeyboardSegment(Invocation invocation)
    {
        if (!invocation.SetKeyboardSegmentAllocated)
            return;
        Bus.Release(invocation, invocation.SetKeyboardSegmentAllocation,
            "SetKeyboardSegment");
        invocation.SetKeyboardSegmentAllocated = false;
    }

    private void VerifyMorphOSSetKeyboardEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetKeyboard!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.SetKeyboardReadArgsCalls == 0 &&
                invocation.SetKeyboardOpenResourceCalls == 0,
                "MorphOS SetKeyboard crossed a startup boundary.");
            return;
        }

        var parser = definition.ParserFailure;
        Require(invocation.SetKeyboardReadArgsCalls == 1 &&
            invocation.SetKeyboardFreeArgsCalls == (parser ? 0 : 1),
            "MorphOS SetKeyboard parser ownership differs.");
        if (parser)
        {
            Require(invocation.Allocations == 1 && invocation.FreeMem == 1,
                "MorphOS SetKeyboard parser allocation ownership differs.");
            return;
        }

        var utilityFailure = !definition.UtilityAvailable;
        var resourceFailure = !definition.ResourceAvailable;
        var existing = definition.ExistingKeymap;
        var absolute = invocation.Definition.Arguments.Trim().Contains(':');
        var loaded = !utilityFailure && !resourceFailure && !existing;
        var fallback = loaded && !absolute && !definition.KeymapsLoadSuccess;
        var failedLoad = fallback && !definition.MossysLoadSuccess ||
            loaded && absolute && !definition.LoadSuccess;
        var segmentLoaded = loaded && !failedLoad;
        Require(segmentLoaded
                ? invocation.SetKeyboardTypeOfMemCalls > 0
                : invocation.SetKeyboardTypeOfMemCalls == 0,
            "MorphOS SetKeyboard resident scan skipped TypeOfMem bounds checks.");
        Require(invocation.SetKeyboardOpenResourceCalls ==
            (utilityFailure ? 0 : 1) &&
            invocation.SetKeyboardKeymapOpens ==
                (utilityFailure || resourceFailure || failedLoad ? 0 : 1) &&
            invocation.SetKeyboardKeymapCloses ==
                (utilityFailure || resourceFailure || failedLoad ||
                 !definition.KeymapAvailable ? 0 : 1),
            "MorphOS SetKeyboard library ownership differs.");
        Require(invocation.SetKeyboardUnLoadSegCalls ==
            (loaded && !failedLoad && !definition.KeymapAvailable ? 1 : 0),
            $"MorphOS SetKeyboard segment retention differs for {invocation.Definition.Name}: " +
            $"loaded={loaded}, failedLoad={failedLoad}, keymap={definition.KeymapAvailable}, " +
            $"unload={invocation.SetKeyboardUnLoadSegCalls}.");
        Require(invocation.SetKeyboardSetDefaultCalls ==
            (loaded && !failedLoad && definition.KeymapAvailable || existing &&
             !utilityFailure && !resourceFailure && definition.KeymapAvailable
                ? 1 : 0),
            "MorphOS SetKeyboard default selection differs.");
    }
}
