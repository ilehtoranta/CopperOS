using Amiga;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell.Dos;

/// <summary>Reserves a task name across both sides of a Shell child lifetime.</summary>
internal static class PublicShellTaskIdentity
{
    // The owner address is retained while the parent can poll. The MemList
    // address remains reserved until the child is removed by Exec. Including
    // both prevents a recycled address from identifying a different child
    // whether the parent detaches first or the child exits first.
    private const string Prefix = "CopperOS-cont-";
    private const uint TemplateBytes = 24;
    public const uint NameBytes = 32;

    /// <summary>
    /// Reserves a name in caller-owned storage and returns an unattached
    /// MemList. The caller must keep <paramref name="owner"/> allocated until
    /// it stops polling, and must either attach or release the returned list.
    /// </summary>
    public static bool TryReserve(APTR owner, APTR name, uint capacity, out APTR reservation)
    {
        reservation = APTR.Null;
        if (owner.IsNull || name.IsNull || capacity < NameBytes ||
            name.Raw > uint.MaxValue - NameBytes) return false;
        var template = APTR.FromPointer(global::Amiga.Exec.AllocMem(TemplateBytes,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (template.IsNull) return false;
        APTR.WriteUInt16(template, ExecLayout.MemList.NumberOfEntries, 1);
        APTR.WriteUInt32(template, ExecLayout.MemList.FirstEntry, (uint)global::Amiga.Exec.MemoryFlags.Public);
        APTR.WriteUInt32(template, ExecLayout.MemList.FirstEntry + 4, 4);
        var list = global::Amiga.Exec.AllocEntry(template);
        global::Amiga.Exec.FreeMem(template.Raw, TemplateBytes);
        // AllocEntry reports failure with bit 31 set, not only a null pointer.
        if (list.IsNull || (list.Raw & 0x80000000u) != 0) return false;
        var prefix = APTR.FromPointer(CString.ToUInt32(Prefix));
        var position = 0;
        while (APTR.ReadUInt8(prefix, position) != 0)
        {
            APTR.WriteUInt8(name, position, APTR.ReadUInt8(prefix, position));
            position++;
        }
        WriteHex(name, ref position, owner.Raw);
        APTR.WriteUInt8(name, position++, (byte)'-');
        WriteHex(name, ref position, list.Raw);
        APTR.WriteUInt8(name, position, 0);
        reservation = list;
        return true;
    }

    /// <summary>
    /// Transfers an unattached reservation to a child that is still blocked
    /// awaiting its startup message. Exec frees it when that task is removed.
    /// </summary>
    public static void Attach(APTR child, APTR reservation)
    {
        global::Amiga.Exec.Forbid();
        global::Amiga.Exec.AddHead(ExecTaskCodec.MemoryEntriesAddress(child), reservation);
        global::Amiga.Exec.Permit();
    }

    /// <summary>Releases an unattached reservation after a failed launch.</summary>
    public static void Release(APTR reservation)
    {
        if (reservation.IsNotNull) global::Amiga.Exec.FreeEntry(reservation);
    }

    /// <summary>Recognizes the complete reserved-name format on a Shell child.</summary>
    public static bool IsReservedName(APTR name)
    {
        if (name.IsNull) return false;
        var prefix = APTR.FromPointer(CString.ToUInt32(Prefix));
        var position = 0;
        while (APTR.ReadUInt8(prefix, position) != 0)
        {
            if (APTR.ReadUInt8(name, position) != APTR.ReadUInt8(prefix, position)) return false;
            position++;
        }
        for (var index = 0; index < 17; index++)
        {
            var value = APTR.ReadUInt8(name, position++);
            if (index == 8) { if (value != (byte)'-') return false; }
            else if (!((value >= (byte)'0' && value <= (byte)'9') ||
                       (value >= (byte)'A' && value <= (byte)'F'))) return false;
        }
        return APTR.ReadUInt8(name, position) == 0;
    }

    private static void WriteHex(APTR text, ref int position, uint value)
    {
        for (var shift = 28; shift >= 0; shift -= 4)
        {
            var digit = (value >> shift) & 15;
            APTR.WriteUInt8(text, position++, (byte)(digit < 10 ? '0' + digit : 'A' + digit - 10));
        }
    }
}
