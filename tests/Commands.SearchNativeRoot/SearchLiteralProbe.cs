using Amiga;
using CopperOS.Commands;
using CopperSharp.Compiler;

namespace CopperOS.Commands.SearchNativeRoot;

/// <summary>
/// Private reachability root for the bounded Search literal matcher. It is not
/// a Search command entry and performs no DOS parsing, traversal, or I/O.
/// </summary>
public static class SearchLiteralProbe
{
    // Control LONGs: text address/length; pattern address/length; CASE flag;
    // mapped region address/length; found result; completion marker.
    public const int ControlBytes = 36;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;

        var control = argumentText.Address;
        var text = new APTR(APTR.ReadUInt32(control, 0));
        var textLength = APTR.ReadUInt32(control, 4);
        var pattern = new APTR(APTR.ReadUInt32(control, 8));
        var patternLength = APTR.ReadUInt32(control, 12);
        var caseSensitive = APTR.ReadUInt32(control, 16) != 0;
        BorrowedMemory memory = new(control);
        if (!SearchLiteralMatcher.TryContains(ref memory, text, textLength,
                pattern, patternLength, caseSensitive, out var found))
            return 10;

        APTR.WriteUInt32(control, 28, found ? 1u : 0u);
        APTR.WriteUInt32(control, 32, 0x534C5046); // SLPF, only after success.
        return 0;
    }

    private readonly struct BorrowedMemory(APTR control) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize)
        {
            var start = APTR.ReadUInt32(control, 20);
            var length = APTR.ReadUInt32(control, 24);
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
