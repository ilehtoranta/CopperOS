using Amiga;

namespace CopperOS.Shell;

/// <summary>Shell-owned input for one command supplied as process arguments.</summary>
public static class ShellCommandSource
{
    private const uint Magic = 0x53434d44; // SCMD
    private const uint HeaderSize = 8;
    private const uint MaximumText = 65_535;

    /// <summary>Copies the command and appends the line terminator owned by the Shell.</summary>
    public static BPTR Create<TMemory>(ref TMemory memory, APTR text, uint length)
        where TMemory : struct, IShellRunnerMemory
    {
        if (text.IsNull || length == 0 || length > MaximumText ||
            text.Raw > uint.MaxValue - length || !memory.IsMapped(text, length)) return BPTR.Null;
        var size = HeaderSize + length + 1;
        var source = memory.AllocateRunnerMemory(size);
        if (source.IsNull || (source.Raw & 3) != 0 || source.Raw > uint.MaxValue - size ||
            !memory.IsMapped(source, size))
        {
            if (source.IsNotNull) memory.FreeRunnerMemory(source, size);
            return BPTR.Null;
        }
        memory.WriteUInt32(source, 0, Magic);
        memory.WriteUInt32(source, 4, length + 1);
        for (var i = 0u; i < length; i++)
            memory.WriteUInt8(source, (int)(HeaderSize + i), memory.ReadUInt8(text, (int)i));
        memory.WriteUInt8(source, (int)(HeaderSize + length), (byte)'\n');
        return BPTR.FromAddress(source);
    }

    public static bool IsValid<TMemory>(ref TMemory memory, BPTR source)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (source.IsNull || source.Raw > (uint.MaxValue >> 2)) return false;
        var address = source.Address;
        if (address.Raw > uint.MaxValue - HeaderSize || !memory.IsMapped(address, HeaderSize) ||
            memory.ReadUInt32(address, 0) != Magic) return false;
        var length = memory.ReadUInt32(address, 4);
        return length != 0 && length <= MaximumText + 1 &&
            address.Raw <= uint.MaxValue - HeaderSize - length &&
            memory.IsMapped(address, HeaderSize + length);
    }

    /// <summary>Reads with FGets-style line and cursor behavior.</summary>
    public static bool TryRead<TMemory>(ref TMemory memory, BPTR source, uint offset,
        APTR destination, uint capacity, out uint length, out uint nextOffset, out uint eof)
        where TMemory : struct, IAmigaGuestMemory
    {
        length = 0; nextOffset = offset; eof = 0;
        if (!IsValid(ref memory, source) || destination.IsNull || capacity < 2 ||
            destination.Raw > uint.MaxValue - capacity || !memory.IsMapped(destination, capacity)) return false;
        var owner = source.Address;
        var available = memory.ReadUInt32(owner, 4);
        if (offset >= available) { eof = 1; return true; }
        var terminated = false;
        while (nextOffset < available && length + 1 < capacity)
        {
            var value = memory.ReadUInt8(owner, (int)(HeaderSize + nextOffset++));
            if (value == (byte)'\n') { terminated = true; break; }
            memory.WriteUInt8(destination, (int)length++, value);
        }
        if (terminated && length != 0 && memory.ReadUInt8(destination, (int)length - 1) == (byte)'\r') length--;
        memory.WriteUInt8(destination, (int)length, 0);
        return true;
    }

    public static bool Release<TMemory>(ref TMemory memory, BPTR source)
        where TMemory : struct, IShellRunnerMemory
    {
        if (!IsValid(ref memory, source)) return false;
        var address = source.Address;
        var size = HeaderSize + memory.ReadUInt32(address, 4);
        memory.WriteUInt32(address, 0, 0);
        memory.FreeRunnerMemory(address, size);
        return true;
    }
}
