using Amiga;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 LoadResource's worker-owned list of opened resources.
/// This list is distinct from the semaphore-protected, one-shot LoadSeg cache.
/// The worker serializes access and retains the Utility/Graphics/Locale leases.
/// </summary>
public static class NativeWorkbench31LoadResourceRegistry
{
    public const int ListBytes = 12;
    public const int TypeOffset = 8;
    public const int NameOffset = 10;
    public const int HandleOffset = 14;
    public const int NameStorageOffset = 18;

    public static void Initialize(APTR list)
    {
        APTR.WriteUInt32(list, 0, list.Raw + 4);
        APTR.WriteUInt32(list, 4, 0);
        APTR.WriteUInt32(list, 8, list.Raw);
    }

    public static bool IsEmpty(APTR list) =>
        APTR.ReadUInt32(list, 0) == list.Raw + 4;

    /// <summary>Returns the first case-insensitive name match, or null.</summary>
    public static APTR Find(APTR list, CString name)
    {
        var record = APTR.FromPointer(APTR.ReadUInt32(list, 0));
        while (APTR.ReadUInt32(record, 0) != 0)
        {
            // The original utility Stricmp call is followed by TST.W D0.
            if (unchecked((short)Utility.Stricmp(CString.ToUInt32(name),
                APTR.ReadUInt32(record, NameOffset))) == 0)
                return record;
            record = APTR.FromPointer(APTR.ReadUInt32(record, 0));
        }
        return APTR.Null;
    }

    /// <summary>
    /// Copies the name and appends a record. On success the registry owns the
    /// handle; on allocation failure the caller retains it and IoErr is not set.
    /// Duplicate detection belongs to the resource action before opening it.
    /// </summary>
    public static bool TryAdd(APTR list, CString name, byte type, APTR handle)
    {
        var source = APTR.FromPointer(CString.ToUInt32(name));
        var length = 0;
        while (APTR.ReadUInt8(source, length) != 0)
            length++;

        var record = Exec.AllocVec(unchecked((uint)(length + 19)), 0);
        if (record.IsNull)
            return false;

        APTR.WriteUInt8(record, TypeOffset, type);
        APTR.WriteUInt32(record, NameOffset, record.Raw + NameStorageOffset);
        APTR.WriteUInt32(record, HandleOffset, handle.Raw);
        var index = 0;
        while (index <= length)
        {
            APTR.WriteUInt8(record, NameStorageOffset + index,
                APTR.ReadUInt8(source, index));
            index++;
        }
        Exec.AddTail(list, record);
        return true;
    }

    /// <summary>Unlinks and frees a record without closing its handle.</summary>
    public static void Remove(APTR record)
    {
        Exec.Remove(record);
        Exec.FreeVec(record);
    }

    /// <summary>Closes supported opened-resource types, then removes the record.</summary>
    public static void CloseAndRemove(APTR record)
    {
        var type = APTR.ReadUInt8(record, TypeOffset);
        var handle = APTR.ReadUInt32(record, HandleOffset);
        if (type == 0)
            Exec.CloseLibrary(APTR.FromPointer(handle));
        else if (type == 2)
            Graphics.CloseFont(handle);
        else if (type == 3)
            Locale.CloseCatalog(handle);
        // Type 1 and unknown types have no close call in the original switch.
        Remove(record);
    }
}
