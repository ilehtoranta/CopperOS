using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Exe2ArcNativeRoot;

/// <summary>
/// Private component entry for the production one-candidate header predicates.
/// No DOS, parser, scanner, file I/O, archive decoder or original command runs.
/// </summary>
public static class Exe2ArcNativeProbe
{
    // Big-endian LONG fields: format, header, window bytes, file offset, file
    // length, mapped base, mapped bytes, mapping enabled; then returned length,
    // mapping calls, byte-read calls, read-offset bitmap, forbidden calls, done.
    public const int ControlBytes = 56;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;
        APTR control = argumentText.Address;
        uint format = APTR.ReadUInt32(control, 0);
        APTR header = new(APTR.ReadUInt32(control, 4));
        uint headerBytes = APTR.ReadUInt32(control, 8);
        uint fileOffset = APTR.ReadUInt32(control, 12);
        uint fileLength = APTR.ReadUInt32(control, 16);
        BorrowedHeader memory = new(control);
        uint payloadLength;
        bool found;
        if (format == 0)
            found = Exe2ArcHeaderProbe.TryGetRar4PayloadLength(ref memory,
                header, headerBytes, fileOffset, fileLength, out payloadLength);
        else if (format == 1)
            found = Exe2ArcHeaderProbe.TryGetCabinetPayloadLength(ref memory,
                header, headerBytes, fileOffset, fileLength, out payloadLength);
        else
            return 20;
        APTR.WriteUInt32(control, 32, payloadLength);
        APTR.WriteUInt32(control, 52, 0x45584148); // EXAH, only after the call.
        return found ? 0 : 10;
    }

    // One pointer wide for the compiler's shared generic representation. Bounds
    // and counters belong to the invocation. Counters instrument the adapter;
    // they are not writes through the production IAmigaGuestMemory interface.
    private readonly struct BorrowedHeader(APTR control) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize)
        {
            APTR.WriteUInt32(control, 36, APTR.ReadUInt32(control, 36) + 1);
            if (APTR.ReadUInt32(control, 28) == 0) return false;
            uint start = APTR.ReadUInt32(control, 20);
            uint bytes = APTR.ReadUInt32(control, 24);
            return address.Raw >= start && byteSize <= bytes &&
                address.Raw - start <= bytes - byteSize;
        }

        public byte ReadUInt8(APTR address, int offset = 0)
        {
            APTR.WriteUInt32(control, 40, APTR.ReadUInt32(control, 40) + 1);
            if ((uint)offset < 32)
                APTR.WriteUInt32(control, 44,
                    APTR.ReadUInt32(control, 44) | (1u << offset));
            else
                Forbidden();
            return APTR.ReadUInt8(address, offset);
        }

        // These methods are not alternate implementations of the predicates.
        // Any call marks the invocation as failed; unsupported writes never
        // receive permission to modify the borrowed header or another image.
        public ushort ReadUInt16(APTR address, int offset = 0) { Forbidden(); return 0; }
        public uint ReadUInt32(APTR address, int offset = 0) { Forbidden(); return 0; }
        public void WriteUInt8(APTR address, int offset, byte value) => Forbidden();
        public void WriteUInt16(APTR address, int offset, ushort value) => Forbidden();
        public void WriteUInt32(APTR address, int offset, uint value) => Forbidden();
        public void Clear(APTR address, uint byteCount) => Forbidden();
        public void Copy(APTR source, APTR destination, uint byteCount) => Forbidden();
        private void Forbidden() => APTR.WriteUInt32(control, 48,
            APTR.ReadUInt32(control, 48) + 1);
    }
}
