using Amiga;

namespace CopperOS.Desktop;

internal static class DesktopMemory
{
    internal static uint L(uint p, int o) => APTR.ReadUInt32(APTR.FromPointer(p), o);
    internal static ushort H(uint p, int o) => APTR.ReadUInt16(APTR.FromPointer(p), o);
    internal static byte B(uint p, int o) => APTR.ReadUInt8(APTR.FromPointer(p), o);
    internal static void L(uint p, int o, uint v) => APTR.WriteUInt32(APTR.FromPointer(p), o, v);
    internal static void H(uint p, int o, ushort v) => APTR.WriteUInt16(APTR.FromPointer(p), o, v);
    internal static void B(uint p, int o, byte v) => APTR.WriteUInt8(APTR.FromPointer(p), o, v);
    internal static uint Allocate(uint bytes) => Exec.AllocMem(bytes, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
    internal static uint ExecBase() => L(4, 0);

    // Native compiled roots do not promise the Amiga callee-save convention.
    internal static void CallWrapper(uint p)
    {
        H(p, 0, 0x48E7); H(p, 2, 0x3F3E); H(p, 4, 0x4E93);
        H(p, 6, 0x4CDF); H(p, 8, 0x7CFC); H(p, 10, 0x4E75);
    }

    internal static void CallbackWrapper(uint p, uint code)
    {
        L(p, 0, 0x48E73F3E); H(p, 4, 0x4EB9); L(p, 6, code);
        L(p, 10, 0x4CDF7CFC); H(p, 14, 0x4E75);
    }

    internal static uint ResidentModule(uint dos, uint name, uint path)
    {
        Exec.Forbid();
        var resident = DesktopDos.Find(dos, name, 0, -1);
        var segment = resident == 0 ? 0 : L(resident, DosLayout.Segment.SegmentList);
        Exec.Permit();
        if (segment != 0) return segment;
        var loaded = DesktopDos.Load(dos, path);
        if (loaded == 0) return 0;
        Exec.Forbid();
        resident = DesktopDos.Find(dos, name, 0, -1);
        segment = resident == 0 ? 0 : L(resident, DosLayout.Segment.SegmentList);
        if (segment == 0 && DesktopDos.Add(dos, name, loaded, -1) != 0) segment = loaded;
        Exec.Permit();
        if (segment != loaded) DesktopDos.Unload(dos, loaded);
        return segment;
    }

    internal static void Error(uint dos)
    {
        DesktopDos.Write(dos, DesktopDos.Output(dos), CString.ToUInt32("Desktop: startup failed; console retained.\n"), 43);
    }
}
