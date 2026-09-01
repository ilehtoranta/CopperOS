using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands.EvalNativeRoot;

/// <summary>
/// Private component protocol for the real numeric formatter. This is neither
/// an Eval command entry nor an implementation of guest DOS or memory services.
/// </summary>
public static class EvalNumericProbe
{
    // Big-endian LONG fields: format, flags, value high, value low, destination,
    // capacity, borrowed region base, borrowed region length, returned byte count.
    public const int ControlBytes = 36;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;

        APTR control = argumentText.Address;
        uint format = APTR.ReadUInt32(control, 0);
        uint flags = APTR.ReadUInt32(control, 4);
        ulong value = unchecked((ulong)M68kRuntime.CombineInt64(
            APTR.ReadUInt32(control, 8), APTR.ReadUInt32(control, 12)));
        APTR destination = new(APTR.ReadUInt32(control, 16));
        uint capacity = APTR.ReadUInt32(control, 20);
        BorrowedMemory memory = new(control);
        uint count = 0;
        bool success;
        if (format == 0)
            success = EvalNumericFormatter.TryWriteDecimal(ref memory,
                unchecked((long)value), destination, capacity, (flags & 1) != 0,
                out count);
        else if (format == 1)
            success = EvalNumericFormatter.TryWriteHexadecimal(ref memory,
                value, destination, capacity, (flags & 2) != 0, (flags & 1) != 0,
                out count);
        else if (format == 2)
            success = EvalNumericFormatter.TryWriteOctal(ref memory,
                value, destination, capacity, (flags & 1) != 0, out count);
        else
            return 20;

        APTR.WriteUInt32(control, 32, count);
        return success ? 0 : 10;
    }

    // The caller lends exactly one region. Native byte accesses go through SDK
    // intrinsics; the instruction fixture independently guards every access.
    // Keep the generic adapter one pointer wide, as required by CopperSharp's
    // shared generic representation. Its bounds remain in the caller's block.
    private readonly struct BorrowedMemory(APTR control) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize)
        {
            uint start = APTR.ReadUInt32(control, 24);
            uint length = APTR.ReadUInt32(control, 28);
            return address.Raw >= start && byteSize <= length &&
                address.Raw - start <= length - byteSize;
        }

        public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
        public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
        public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
        public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
        public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
        public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
        public void Clear(APTR address, uint byteCount)
        {
            for (uint index = 0; index < byteCount; index++)
                APTR.WriteUInt8(new(address.Raw + index), 0, 0);
        }
        public void Copy(APTR source, APTR destination, uint byteCount)
        {
            for (uint index = 0; index < byteCount; index++)
                APTR.WriteUInt8(new(destination.Raw + index), 0,
                    APTR.ReadUInt8(new(source.Raw + index), 0));
        }
    }
}
