using System.Text.RegularExpressions;
using Copper68k;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    private uint compilerContextBytes;
    private uint compilerContextWrapperEnd;

    private void ReadCompilerContextMetadata(string? imagePath)
    {
        if (imagePath is null || !File.Exists(imagePath + ".map")) return;
        var map = File.ReadAllText(imagePath + ".map");
        var context = Regex.Match(map, @"^RESIDENT-CONTEXT bytes=(\d+) placement=heap\r?$", RegexOptions.Multiline);
        if (!context.Success) return;
        Require(map.Contains("PROFILE Resident", StringComparison.Ordinal), "Heap context needs a resident image.");
        compilerContextBytes = uint.Parse(context.Groups[1].Value);
        Require(compilerContextBytes > 0, "Empty heap context.");
        var methods = Regex.Matches(map, @"^([0-9A-F]{8})\s+\d+\s+.+::.+$", RegexOptions.Multiline);
        Require(methods.Count > 0, "Context map lacks method bounds.");
        compilerContextWrapperEnd = methods.Select(m => Convert.ToUInt32(m.Groups[1].Value, 16)).Min();
        Require(compilerContextWrapperEnd > 0 && compilerContextWrapperEnd < Image.Code.Length,
            "Context wrapper bounds are invalid.");
    }

    private bool InCompilerContextWrapper(M68kCpuState state)
    {
        var returnPc = Bus.Long(state.A[7]);
        return returnPc >= LoadAddress + 4 && returnPc < LoadAddress + compilerContextWrapperEnd &&
            Bus.Word(returnPc - 4) == 0x4eae;
    }

    private bool TryAllocateCompilerContext(M68kCpuState state, Invocation invocation, out uint address)
    {
        address = 0;
        if (compilerContextBytes == 0 || !InCompilerContextWrapper(state)) return false;
        Require(invocation.CompilerContextAllocations == 0 && invocation.Allocations == 0 &&
            invocation.Opens == 0 && invocation.Reads == 0 &&
            state.D[0] == compilerContextBytes && state.D[1] == 0 &&
            Bus.Word(Bus.Long(state.A[7]) - 2) == unchecked((ushort)ExecLvo.AllocMem),
            "Compiler context allocation size, flags, position or lifetime differs.");
        address = Bus.Allocate(invocation, compilerContextBytes, "CompilerContext", false);
        invocation.CompilerContextAddress = address;
        invocation.CompilerContextAllocations++;
        invocation.Events.RemoveAt(invocation.Events.Count - 1);
        return true;
    }

    private bool TryFreeCompilerContext(M68kCpuState state, Invocation invocation)
    {
        if (compilerContextBytes == 0 || state.A[1] != invocation.CompilerContextAddress ||
            invocation.CompilerContextAllocations == 0) return false;
        Require(invocation.CompilerContextFrees == 0 && InCompilerContextWrapper(state) &&
            state.D[0] == compilerContextBytes && state.A[5] == invocation.CompilerContextAddress &&
            Bus.Word(Bus.Long(state.A[7]) - 2) == unchecked((ushort)ExecLvo.FreeMem),
            "Compiler context free address, size or lifetime differs.");
        Bus.AssertReleased(invocation, "CompilerContext");
        Bus.Release(invocation, state.A[1], "CompilerContext", state.D[0]);
        invocation.CompilerContextFrees++;
        invocation.Events.RemoveAt(invocation.Events.Count - 1);
        return true;
    }
}
