using Amiga;
using CopperOS.Commands;
using CopperSharp.Compiler;

namespace CopperOS.Commands.SearchNativeRoot;

/// <summary>
/// Private reachability root for Search's bounded literal line formatter. It
/// owns no DOS parser, locale, file handle, traversal state, or resident state.
/// </summary>
public static class SearchLineProbe
{
    // Control LONGs: source address/length, pattern address/length, CASE,
    // NONUM, QUIET, LINES, destination address/capacity, mapped start/length,
    // found/written results, then a completion marker.
    public const int ControlBytes = 60;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;

        var control = argumentText.Address;
        var source = new APTR(APTR.ReadUInt32(control, 0));
        var sourceLength = APTR.ReadUInt32(control, 4);
        var pattern = new APTR(APTR.ReadUInt32(control, 8));
        var patternLength = APTR.ReadUInt32(control, 12);
        BorrowedMemory memory = new(control);
        if (!SearchLineFormatter.TryFormat(ref memory, source, sourceLength,
                pattern, patternLength, APTR.ReadUInt32(control, 16) != 0,
                APTR.ReadUInt32(control, 20) != 0,
                APTR.ReadUInt32(control, 24) != 0,
                APTR.ReadUInt32(control, 28),
                new APTR(APTR.ReadUInt32(control, 32)),
                APTR.ReadUInt32(control, 36), out var found, out var written))
            return 10;

        APTR.WriteUInt32(control, 48, found ? 1u : 0u);
        APTR.WriteUInt32(control, 52, written);
        APTR.WriteUInt32(control, 56, 0x534C4646); // SLFF, only after success.
        return 0;
    }

    private readonly struct BorrowedMemory(APTR control) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize)
        {
            var start = APTR.ReadUInt32(control, 40);
            var length = APTR.ReadUInt32(control, 44);
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
