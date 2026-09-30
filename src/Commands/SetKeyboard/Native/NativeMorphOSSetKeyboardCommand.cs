using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 SetKeyboard candidate based on the released IPrefs
/// SetKeyMap source.  The packed C:SetKeyboard member is a 7fMOS payload and
/// does not expose a plaintext ReadArgs template; the shared classic grammar
/// is retained as the only syntax admitted until a guest capture proves a
/// difference.  The body keeps the MorphOS search order and resident-node
/// ownership rules: resource reuse, absolute-path loading, KEYMAPS: then
/// MOSSYS:Devs/Keymaps fallback, resident/extended-node admission, duplicate
/// race recheck, and permanent segment retention after publication.
/// </summary>
public static class NativeMorphOSSetKeyboardCommand
{
    public const string Template = "KEYMAP/A";

    private const uint PathBytes = 256;
    private const uint KeymapLibraryVersion = 36;
    private const uint UtilityLibraryVersion = 36;
    private const uint KeymapResourceListOffset = 14;
    private const uint KeymapNodeKeyMapOffset = 14;
    private const uint ExtendedKeymapSegmentOffset = 48;
    private const uint ExtendedKeymapResidentOffset = 52;
    private const ushort ResidentMatchWord = 0x4afc;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, 1,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return arguments.ReturnLevel;
        }

        APTR path = APTR.Null;
        BPTR segment = BPTR.Null;
        APTR utilityLibrary = APTR.Null;
        APTR keymapLibrary = APTR.Null;
        APTR keymapNode = APTR.Null;
        var keepSegment = false;
        var published = false;
        var result = DOS.RETURN_OK;
        var error = 0;

        if (!arguments.TryGetResult(0, out var keymapName) ||
            keymapName == 0)
        {
            result = DOS.RETURN_ERROR;
            error = (int)DOS.Error.BadTemplate;
            goto Cleanup;
        }

        utilityLibrary = Exec.OpenLibraryRaw(Utility.Name,
            UtilityLibraryVersion);
        if (utilityLibrary.IsNull)
        {
            result = DOS.RETURN_FAIL;
            error = (int)DOS.Error.ObjectNotFound;
            goto Cleanup;
        }
        Utility.UtilityLibraryBase = utilityLibrary;

        var resource = Exec.OpenResource(
            CString.FromLiteral("keymap.resource"));
        if (resource.IsNull)
        {
            result = DOS.RETURN_FAIL;
            error = (int)DOS.Error.InvalidResidentLibrary;
            goto Cleanup;
        }

        var filePart = (APTR)DOS.FilePart(
            CString.FromPointer(keymapName));
        if (filePart.IsNull)
        {
            result = DOS.RETURN_FAIL;
            error = (int)DOS.Error.ObjectNotFound;
            goto Cleanup;
        }

        keymapNode = FindExistingNode(resource, filePart);
        if (keymapNode.IsNull)
        {
            segment = LoadKeymap(keymapName, filePart, ref path);
            if (segment.IsNull)
            {
                result = DOS.RETURN_FAIL;
                error = (int)DOS.IoErr();
                goto Cleanup;
            }

            var resident = FindLibResident(segment.Raw);
            keymapNode = resident.IsNotNull
                ? APTR.FromPointer(APTR.ReadUInt32(resident,
                    ExecLayout.Resident.Init))
                : APTR.FromPointer(segment.Address.Raw + 4);
            if (keymapNode.IsNull)
            {
                result = DOS.RETURN_FAIL;
                error = (int)DOS.Error.ObjectNotFound;
                goto Cleanup;
            }
        }

        // Keep the list locked across the duplicate check and publication.
        // This is the race window covered by the released MorphOS source.
        Exec.Forbid();
        var found = FindExistingNodeNoLock(resource, filePart);
        if (found.IsNotNull && found.Raw != keymapNode.Raw)
        {
            keymapNode = found;
            Exec.Permit();
            if (segment.IsNotNull)
                NativeMorphOSSetKeyboardDosRaw.UnLoadSegRaw(segment);
            segment = BPTR.Null;
        }
        else if (found.IsNull)
        {
            if (APTR.ReadUInt8(keymapNode, ExecLayout.Node.Type) ==
                    (byte)NodeType.Extended &&
                APTR.ReadUInt8(keymapNode, ExecLayout.Node.Priority) ==
                    (byte)'E')
            {
                APTR.WriteUInt32(keymapNode, (int)ExtendedKeymapSegmentOffset,
                    segment.Raw);
                var resident = FindLibResident(segment.Raw);
                APTR.WriteUInt32(keymapNode, (int)ExtendedKeymapResidentOffset,
                    resident.Raw);
            }

            Exec.AddHead(APTR.FromPointer(resource.Raw +
                KeymapResourceListOffset), keymapNode);
            published = true;
            Exec.Permit();
        }
        else
        {
            Exec.Permit();
        }

        keymapLibrary = Exec.OpenLibraryRaw(Keymap.Name,
            KeymapLibraryVersion);
        if (keymapLibrary.IsNull)
        {
            result = DOS.RETURN_FAIL;
            error = (int)DOS.Error.ObjectNotFound;
            goto Cleanup;
        }

        Keymap.KeymapLibraryBase = keymapLibrary;
        Keymap.SetKeyMapDefault(APTR.FromPointer(keymapNode.Raw +
            KeymapNodeKeyMapOffset));
        keepSegment = segment.IsNotNull;
        Exec.CloseLibrary(keymapLibrary);
        keymapLibrary = APTR.Null;
        Keymap.KeymapLibraryBase = APTR.Null;

    Cleanup:
        if (keymapLibrary.IsNotNull)
        {
            Exec.CloseLibrary(keymapLibrary);
            Keymap.KeymapLibraryBase = APTR.Null;
        }
        if (published && !keepSegment && keymapNode.IsNotNull)
            Exec.Remove(keymapNode);
        if (!keepSegment && segment.IsNotNull)
            NativeMorphOSSetKeyboardDosRaw.UnLoadSegRaw(segment);
        arguments.Release();
        if (path.IsNotNull)
            Exec.FreeMem(path, PathBytes);
        if (utilityLibrary.IsNotNull)
        {
            Exec.CloseLibrary(utilityLibrary);
            Utility.UtilityLibraryBase = APTR.Null;
        }
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        if (ioError != 0)
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
        return result;
    }

    private static APTR FindExistingNode(APTR resource, APTR name)
    {
        Exec.Forbid();
        var node = FindExistingNodeNoLock(resource, name);
        Exec.Permit();
        return node;
    }

    private static APTR FindExistingNodeNoLock(APTR resource, APTR name)
    {
        var list = APTR.FromPointer(resource.Raw +
            KeymapResourceListOffset);
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        while (node.IsNotNull)
        {
            var nodeName = APTR.ReadUInt32(node, ExecLayout.Node.Name);
            if (nodeName != 0 && Utility.Stricmp(nodeName,
                    APTR.ToUInt32(name)) == 0)
                return node;
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        return APTR.Null;
    }

    private static BPTR LoadKeymap(uint name, APTR filePart, ref APTR path)
    {
        if (filePart.Raw != name)
            return NativeMorphOSSetKeyboardDosRaw.LoadSegRaw(
                CString.FromPointer(name));

        path = Exec.AllocMem(PathBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (path.IsNull)
        {
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return BPTR.Null;
        }

        CopyCString(path, "KEYMAPS:");
        if (DOS.AddPart(CString.FromPointer(path.Raw),
                CString.FromPointer(name), PathBytes) != 0)
        {
            var segment = NativeMorphOSSetKeyboardDosRaw.LoadSegRaw(
                CString.FromPointer(path.Raw));
            if (segment.IsNotNull)
                return segment;
        }

        CopyCString(path, "MOSSYS:Devs/Keymaps");
        if (DOS.AddPart(CString.FromPointer(path.Raw),
                CString.FromPointer(name), PathBytes) == 0)
            return BPTR.Null;
        return NativeMorphOSSetKeyboardDosRaw.LoadSegRaw(
            CString.FromPointer(path.Raw));
    }

    private static void CopyCString(APTR destination, CString value)
    {
        Exec.CopyMem(APTR.FromPointer(CString.ToUInt32(value)), destination,
            CStringLength(value));
    }

    private static uint CStringLength(CString value)
    {
        var pointer = APTR.FromPointer(CString.ToUInt32(value));
        var length = 0u;
        while (APTR.ReadUInt8(pointer, unchecked((int)length)) != 0)
            length++;
        return length + 1;
    }

    private static APTR FindLibResident(uint segmentRaw)
    {
        var currentSegment = segmentRaw;
        var fastSegment = segmentRaw;
        var cycleCheckEnabled = true;
        while (currentSegment != 0)
        {
            if (currentSegment > (uint.MaxValue >> 2))
                return APTR.Null;
            var memoryRaw = currentSegment << 2;
            if (memoryRaw < 4)
                return APTR.Null;
            var memory = APTR.FromPointer(memoryRaw);
            if (Exec.TypeOfMem(APTR.FromPointer(memoryRaw - 4)) == 0 ||
                Exec.TypeOfMem(memory) == 0)
                return APTR.Null;
            var longWords = APTR.ReadUInt32(memory, -4);
            if (longWords < 2 || longWords > uint.MaxValue / 4)
                return APTR.Null;
            var segmentBytes = longWords * 4;
            if (memoryRaw > uint.MaxValue - segmentBytes)
                return APTR.Null;
            var segmentEnd = memoryRaw + segmentBytes;
            if (segmentEnd <= memoryRaw ||
                Exec.TypeOfMem(APTR.FromPointer(segmentEnd - 1)) == 0)
                return APTR.Null;

            // A Resident match needs a complete six-byte word/tag pair.
            var offset = 4u;
            while (offset <= segmentBytes - 6u)
            {
                var resident = APTR.FromPointer(memory.Raw + offset);
                if (APTR.ReadUInt16(resident, ExecLayout.Resident.MatchWord) ==
                        ResidentMatchWord &&
                    APTR.ReadUInt32(resident,
                        ExecLayout.Resident.MatchTag) == resident.Raw)
                    return resident;
                offset += 2;
            }

            currentSegment = APTR.ReadUInt32(memory, 0);
            if (cycleCheckEnabled && fastSegment != 0)
            {
                if (!TryReadSegmentLink(fastSegment, out var fastNext))
                {
                    cycleCheckEnabled = false;
                }
                else
                {
                    fastSegment = fastNext;
                    if (fastSegment != 0)
                    {
                        if (!TryReadSegmentLink(fastSegment,
                                out fastNext))
                            cycleCheckEnabled = false;
                        else
                            fastSegment = fastNext;
                    }
                }
            }
            if (cycleCheckEnabled && currentSegment != 0 &&
                currentSegment == fastSegment)
                return APTR.Null;
        }
        return APTR.Null;
    }

    private static bool TryReadSegmentLink(uint segmentRaw,
        out uint nextSegment)
    {
        nextSegment = 0;
        if (segmentRaw > (uint.MaxValue >> 2))
            return false;
        var memoryRaw = segmentRaw << 2;
        if (memoryRaw == 0)
            return false;
        var memory = APTR.FromPointer(memoryRaw);
        if (Exec.TypeOfMem(memory) == 0)
            return false;
        nextSegment = APTR.ReadUInt32(memory, 0);
        return true;
    }
}

[AmigaLibrary(DOS.Name)]
internal static class NativeMorphOSSetKeyboardDosRaw
{
    [AmigaLvo(-150)]
    [return: M68kRegister(M68kRegister.D0)]
    public static extern BPTR LoadSegRaw(
        [M68kRegister(M68kRegister.D1)] CString name);

    [AmigaLvo(-156)]
    public static extern void UnLoadSegRaw(
        [M68kRegister(M68kRegister.D1)] BPTR segment);
}
