using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Classic resident command mutations through the public DOS vectors.</summary>
internal static class PublicShellResidentMutation
{
    /// <summary>Checks the published DOS version and vector extent before using V50 tags.</summary>
    public static bool SupportsDeferred(ref CopperSharpNativeDosPlatform memory)
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var supported = memory.ReadUInt16(dos, ExecLayout.Library.Version) >= 50 &&
            memory.ReadUInt16(dos, ExecLayout.Library.NegativeSize) >= -DosLvo.AddSegmentTagList;
        global::Amiga.Exec.CloseLibrary(dos);
        return supported;
    }

    /// <summary>Publishes a deferred ADD through the V50 DOS segment API.</summary>
    public static bool AddDeferred(ref CopperSharpNativeDosPlatform memory,
        in ShellResidentManagementRequest request)
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 50);
        if (dos.IsNull) return false;
        var success = false;
        if (request.Name.IsNull || request.NameLength == 0 ||
            !memory.IsMapped(request.Name, request.NameLength + 1) ||
            memory.ReadUInt8(request.Name, (int)request.NameLength) != 0 ||
            request.File.IsNull || request.FileLength == 0 ||
            !memory.IsMapped(request.File, request.FileLength + 1) ||
            memory.ReadUInt8(request.File, (int)request.FileLength) != 0)
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
        else if (CheckFile(ref memory, dos, request.File, true, out var pure) &&
            AdmitDeferred(dos, request.Output, pure, request.Force != 0))
        {
            const uint bytes = 32;
            var tags = memory.AllocateGuest(bytes);
            if (tags.IsNull) PublicShellFileServices.SetError(dos, DOS.Error.NoFreeStore);
            else
            {
                memory.WriteUInt32(tags, 0, (uint)DosAddSegmentTag.Name);
                memory.WriteUInt32(tags, 4, request.Name.Raw);
                memory.WriteUInt32(tags, 8, (uint)DosAddSegmentTag.FileName);
                memory.WriteUInt32(tags, 12, request.File.Raw);
                memory.WriteUInt32(tags, 16, (uint)DosAddSegmentTag.Type);
                memory.WriteUInt32(tags, 20, request.System != 0 ? uint.MaxValue : 1u);
                memory.WriteUInt32(tags, 24, 0);
                memory.WriteUInt32(tags, 28, 0);
                success = PublicShellResidentVectors.AddSegmentTagList(dos, tags) != 0;
                var error = PublicShellFileServices.GetError(dos);
                memory.FreeGuest(tags, bytes);
                PublicShellFileServices.SetError(dos, (DOS.Error)error);
            }
        }
        var finalError = PublicShellFileServices.GetError(dos);
        if (!success) WriteFailure(ref memory, dos, request.Output, finalError);
        PublicShellFileServices.SetError(dos, (DOS.Error)finalError);
        global::Amiga.Exec.CloseLibrary(dos);
        return success;
    }

    public static bool Manage(ref CopperSharpNativeDosPlatform memory,
        in ShellResidentManagementRequest request)
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var success = false;
        if (request.Name.IsNull || request.NameLength == 0 ||
            !memory.IsMapped(request.Name, request.NameLength + 1) ||
            memory.ReadUInt8(request.Name, (int)request.NameLength) != 0)
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
        else if (request.Remove != 0)
            success = Remove(ref memory, dos, request.Name);
        else if (request.File.IsNull || request.FileLength == 0 ||
            !memory.IsMapped(request.File, request.FileLength + 1) ||
            memory.ReadUInt8(request.File, (int)request.FileLength) != 0)
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
        else if (CheckFile(ref memory, dos, request.File, request.Force != 0, out _))
        {
            var image = NativeForegroundDosVectors.LoadSeg(dos, request.File);
            if (image.IsNotNull)
            {
                success = Publish(ref memory, dos, request.Name, ref image,
                    request.Add != 0, request.RequireExisting != 0, request.System != 0);
                if (!success && image.IsNotNull)
                {
                    var error = PublicShellFileServices.GetError(dos);
                    NativeForegroundDosVectors.UnLoadSeg(dos, image);
                    PublicShellFileServices.SetError(dos, (DOS.Error)error);
                }
            }
        }
        var finalError = PublicShellFileServices.GetError(dos);
        if (!success) WriteFailure(ref memory, dos, request.Output, finalError);
        PublicShellFileServices.SetError(dos, (DOS.Error)finalError);
        global::Amiga.Exec.CloseLibrary(dos);
        return success;
    }

    private static bool CheckFile(ref CopperSharpNativeDosPlatform memory,
        APTR dos, APTR path, bool force, out bool pure)
    {
        pure = false;
        var file = PublicShellFileServices.Lock(dos, path);
        if (file.IsNull) return false;
        var info = memory.AllocateGuest(FileInfoBlock.SizeInBytes);
        if (info.IsNull)
        {
            PublicShellFileServices.UnLock(dos, file);
            PublicShellFileServices.SetError(dos, DOS.Error.NoFreeStore);
            return false;
        }
        var examined = PublicShellPathVectors.Examine(dos, file, info) != 0;
        var error = PublicShellFileServices.GetError(dos);
        pure = examined && (DosFileInfoBlockCodec.ReadProtection(ref memory, info) & (int)FileProtection.Pure) != 0;
        var valid = examined && DosFileInfoBlockCodec.ReadDirEntryType(ref memory, info) < 0 &&
            (force || pure);
        memory.FreeGuest(info, FileInfoBlock.SizeInBytes);
        PublicShellFileServices.UnLock(dos, file);
        if (!valid)
        {
            if (examined || error == 0) error = (int)DOS.Error.ObjectWrongType;
            PublicShellFileServices.SetError(dos, (DOS.Error)error);
        }
        return valid;
    }

    private static bool AdmitDeferred(APTR dos, BPTR output, bool pure, bool force)
    {
        if (pure) return true;
        if (!force)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.InvalidResidentLibrary);
            return false;
        }
        return WriteForceWarning(dos, output);
    }

    private static bool WriteForceWarning(APTR dos, BPTR output)
    {
        if (output.IsNull)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.InvalidLock);
            return false;
        }
        var message = APTR.FromPointer(CString.ToUInt32("Warning: forced resident is unsafe.\n"));
        var length = 0u;
        while (APTR.ReadUInt8(message, (int)length) != 0) length++;
        if (PublicShellFileServices.Write(dos, output, message, length) == (int)length)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.None);
            return true;
        }
        if (PublicShellFileServices.GetError(dos) == 0)
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
        return false;
    }

    private static bool Publish(ref CopperSharpNativeDosPlatform memory, APTR dos,
        APTR name, ref BPTR image, bool add, bool requireExisting, bool system)
    {
        var success = false;
        var error = DOS.Error.None;
        global::Amiga.Exec.Forbid();
        var old = add ? APTR.Null : PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 0);
        // V40 treats a matching system entry as an existing name for explicit
        // REPLACE, but keeps it and publishes a new entry alongside it.
        var existingSystem = !add && old.IsNull
            ? PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 1) : APTR.Null;
        if (old.IsNull && existingSystem.IsNull && !add && requireExisting)
            error = DOS.Error.ObjectNotFound;
        else if (old.IsNotNull && (!DosSegmentCodec.IsMapped(ref memory, old) ||
            DosSegmentCodec.ReadUseCount(ref memory, old) != 1))
            error = DOS.Error.ObjectInUse;
        else if (PublicShellResidentVectors.AddSegment(dos, name, image, system ? -1 : 1) == 0)
            error = (DOS.Error)PublicShellFileServices.GetError(dos);
        else
        {
            // Ownership transferred to AddSegment, including rollback below.
            image = BPTR.Null;
            if (old.IsNull) success = true;
            else
            {
                // Publish first while scheduling is forbidden: an allocation failure
                // leaves the old entry intact and no process can see the temporary pair.
                var added = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 0);
                DosSegmentCodec.WriteUseCount(ref memory, old, 0);
                if (PublicShellResidentVectors.RemSegment(dos, old) != 0)
                    success = true;
                else
                {
                    error = (DOS.Error)PublicShellFileServices.GetError(dos);
                    DosSegmentCodec.WriteUseCount(ref memory, old, 1);
                    // SYSTEM entries are permanent. Their negative use count
                    // must not be changed, even if DOS unexpectedly refuses
                    // to remove an unlocked old entry.
                    if (!system && added.IsNotNull && added != old)
                    {
                        DosSegmentCodec.WriteUseCount(ref memory, added, 0);
                        _ = PublicShellResidentVectors.RemSegment(dos, added);
                    }
                }
            }
        }
        global::Amiga.Exec.Permit();
        if (!success && error == DOS.Error.None) error = DOS.Error.ObjectWrongType;
        PublicShellFileServices.SetError(dos, error);
        return success;
    }

    private static bool Remove(ref CopperSharpNativeDosPlatform memory, APTR dos, APTR name)
    {
        var success = false;
        var error = DOS.Error.None;
        global::Amiga.Exec.Forbid();
        var entry = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 0);
        if (entry.IsNull)
        {
            var systemEntry = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 1);
            error = systemEntry.IsNull ? DOS.Error.ObjectNotFound : DOS.Error.ObjectInUse;
        }
        else if (!DosSegmentCodec.IsMapped(ref memory, entry) || DosSegmentCodec.ReadUseCount(ref memory, entry) != 1)
            error = DOS.Error.ObjectInUse;
        else
        {
            DosSegmentCodec.WriteUseCount(ref memory, entry, 0);
            success = PublicShellResidentVectors.RemSegment(dos, entry) != 0;
            if (!success)
            {
                error = (DOS.Error)PublicShellFileServices.GetError(dos);
                DosSegmentCodec.WriteUseCount(ref memory, entry, 1);
            }
        }
        global::Amiga.Exec.Permit();
        if (!success && error == DOS.Error.None) error = DOS.Error.ObjectWrongType;
        PublicShellFileServices.SetError(dos, error);
        return success;
    }

    private static void WriteFailure(ref CopperSharpNativeDosPlatform memory,
        APTR dos, BPTR output, int error)
    {
        if (output.IsNull || error == 0) return;
        const uint size = 384;
        var buffer = memory.AllocateGuest(size);
        if (buffer.IsNull) return;
        _ = PublicShellFaultVectors.Fault(dos, error, APTR.Null, buffer, (int)size - 1);
        var length = 0u;
        if (memory.ReadUInt8(buffer) is >= (byte)'A' and <= (byte)'Z')
            memory.WriteUInt8(buffer, 0, (byte)(memory.ReadUInt8(buffer) + 32));
        while (length + 1 < size && memory.ReadUInt8(buffer, (int)length) != 0) length++;
        memory.WriteUInt8(buffer, (int)length++, (byte)'\n');
        _ = PublicShellFileServices.Write(dos, output, buffer, length);
        memory.FreeGuest(buffer, size);
    }
}
