using Amiga;
using CopperOS.Commands;
using CopperSharp.Compiler;

namespace CopperOS.Commands.TypeNativeRoot;

/// <summary>
/// Private native reachability root for the bounded MorphOS Type text formatter.
/// It is not a Type command entry and performs no DOS parsing, traversal, or I/O.
/// </summary>
public static class TypeTextProbe
{
    // Control LONGs: source address/length; NUMBER/NOLINE flags; destination
    // address/capacity; returned written count; mapped region address/length;
    // then completion marker.
    public const int ControlBytes = 44;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;

        var control = argumentText.Address;
        var source = new APTR(APTR.ReadUInt32(control, 0));
        var sourceLength = APTR.ReadUInt32(control, 4);
        var number = APTR.ReadUInt32(control, 8) != 0;
        var noLine = APTR.ReadUInt32(control, 12) != 0;
        var destination = new APTR(APTR.ReadUInt32(control, 16));
        var capacity = APTR.ReadUInt32(control, 20);
        BorrowedMemory memory = new(control);

        if (!TypeTextFormatter.TryFormat(ref memory, source, sourceLength,
                number, noLine, destination, capacity, out var written))
            return 10;

        APTR.WriteUInt32(control, 24, written);
        APTR.WriteUInt32(control, 40, 0x54545046); // TTPF, only after success.
        return 0;
    }

    private readonly struct BorrowedMemory(APTR control) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize)
        {
            var start = APTR.ReadUInt32(control, 28);
            var length = APTR.ReadUInt32(control, 32);
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
            for (var index = 0u; index < byteCount; index++)
                APTR.WriteUInt8(address, (int)index, 0);
        }
        public void Copy(APTR source, APTR destination, uint byteCount)
        {
            for (var index = 0u; index < byteCount; index++)
                APTR.WriteUInt8(destination, (int)index,
                    APTR.ReadUInt8(source, (int)index));
        }
    }
}

