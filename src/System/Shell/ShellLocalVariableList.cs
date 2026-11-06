using Amiga;

namespace CopperOS.Shell;

/// <summary>Public process, memory and output operations for local-variable listings.</summary>
public interface IShellLocalVariableListPlatform : IShellRunnerMemory
{
    APTR CurrentVariableProcess();
    int Write(BPTR handle, APTR source, uint length);
    void SetVariableListError(DOS.Error error);
}

/// <summary>Lists the current Process's public pr_LocalVars list.</summary>
public static class ShellLocalVariableList
{
    private const uint MaximumEntries = 4096;
    private const uint MaximumNameLength = 255;
    private const uint ScratchSize = 19;

    public static bool Write<TPlatform>(ref TPlatform platform, APTR cli, BPTR output, bool aliases)
        where TPlatform : struct, IShellLocalVariableListPlatform
    {
        var process = platform.CurrentVariableProcess();
        if (output.IsNull) return Fail(ref platform, DOS.Error.InvalidLock);
        if (process.IsNull || cli.IsNull || !DosProcessCodec.IsMapped(ref platform, process) ||
            !DosCommandLineInterfaceCodec.IsMapped(ref platform, cli) ||
            DosProcessCodec.ReadCommandLineInterface(ref platform, process).Address != cli)
            return Fail(ref platform, DOS.Error.ObjectWrongType);
        var list = DosProcessCodec.LocalVariablesAddress(process);
        if (list.Raw > uint.MaxValue - 12 || !platform.IsMapped(list, 12))
            return Fail(ref platform, DOS.Error.ObjectWrongType);
        var current = APTR.FromPointer(platform.ReadUInt32(list, 0));
        var tail = platform.ReadUInt32(list, 4);
        var tailPrevious = platform.ReadUInt32(list, 8);
        if (current.IsNull && tail == 0 && tailPrevious == 0)
        { platform.SetVariableListError(DOS.Error.None); return true; }
        if (current.IsNull || tail != 0 || tailPrevious == 0)
            return Fail(ref platform, DOS.Error.ObjectWrongType);
        var scratch = platform.AllocateRunnerMemory(ScratchSize);
        if (scratch.IsNull || scratch.Raw > uint.MaxValue - ScratchSize || !platform.IsMapped(scratch, ScratchSize))
        {
            if (scratch.IsNotNull) platform.FreeRunnerMemory(scratch, ScratchSize);
            return Fail(ref platform, DOS.Error.NoFreeStore);
        }
        for (var i = 0; i < 18; i++) platform.WriteUInt8(scratch, i, (byte)' ');
        platform.WriteUInt8(scratch, 18, (byte)'\n');
        var sentinel = list.Raw + 4;
        var previous = list.Raw;
        var valid = true;
        var ioFailed = false;
        var count = 0u;
        while (current.Raw != sentinel && count++ < MaximumEntries)
        {
            if (!DosLocalVarCodec.IsMapped(ref platform, current)) { valid = false; break; }
            var variable = DosLocalVarCodec.Read(ref platform, current);
            if (variable.Node.Predecessor.Raw != previous || variable.Node.Successor.IsNull)
            { valid = false; break; }
            if (variable.Node.Type == (aliases ? (byte)LocalVariableType.Alias : (byte)LocalVariableType.Variable))
            {
                var name = variable.Node.Name.Address;
                var nameLength = 0u;
                if (name.IsNull) { valid = false; break; }
                while (nameLength <= MaximumNameLength)
                {
                    if (name.Raw > uint.MaxValue - nameLength ||
                        !platform.IsMapped(APTR.FromPointer(name.Raw + nameLength), 1)) { valid = false; break; }
                    if (platform.ReadUInt8(name, (int)nameLength) == 0) break;
                    nameLength++;
                }
                if (!valid || nameLength == 0 || nameLength > MaximumNameLength || variable.Length > int.MaxValue ||
                    (variable.Length != 0 && (variable.Value.IsNull || variable.Value.Raw > uint.MaxValue - variable.Length ||
                     !platform.IsMapped(variable.Value, variable.Length)))) { valid = false; break; }
                var padding = nameLength < 18 ? 18 - nameLength : 1;
                if (platform.Write(output, name, nameLength) != (int)nameLength ||
                    platform.Write(output, scratch, padding) != (int)padding ||
                    (variable.Length != 0 && platform.Write(output, variable.Value, variable.Length) != (int)variable.Length) ||
                    platform.Write(output, APTR.FromPointer(scratch.Raw + 18), 1) != 1)
                { ioFailed = true; break; }
            }
            previous = current.Raw;
            current = variable.Node.Successor;
        }
        if (current.Raw != sentinel || tailPrevious != previous) valid = false;
        platform.FreeRunnerMemory(scratch, ScratchSize);
        if (ioFailed) return false;
        if (!valid) return Fail(ref platform, DOS.Error.ObjectWrongType);
        platform.SetVariableListError(DOS.Error.None);
        return true;
    }

    private static bool Fail<TPlatform>(ref TPlatform platform, DOS.Error error)
        where TPlatform : struct, IShellLocalVariableListPlatform
    { platform.SetVariableListError(error); return false; }
}
