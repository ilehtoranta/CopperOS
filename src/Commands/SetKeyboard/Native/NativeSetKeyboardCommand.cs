using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Shared classic/MorphOS SetKeyboard body. Keymap files are load segments
/// whose first data word is a KeyMapNode; once selected as the system default
/// the segment must remain resident for the lifetime of the machine.
/// </summary>
public static class NativeSetKeyboardCommand
{
    public const string Template = "KEYMAP/A";
    private const uint PathBytes = 256;
    private const uint KeymapLibraryVersion = 36;
    private const uint UtilityLibraryVersion = 36;
    private const uint KeymapBasePathBytes = 13; // "DEVS:Keymaps" plus NUL
    private const uint KeymapResourceListOffset = 14;
    private const uint KeymapNodeKeyMapOffset = 14;

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

        var path = APTR.Null;
        BPTR segment = BPTR.Null;
        var keepSegment = false;
        var utilityLibrary = APTR.Null;
        var keymapLibrary = APTR.Null;
        var result = DOS.RETURN_OK;
        var error = 0;

        if (!arguments.TryGetResult(0, out var keymap) || keymap == 0)
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

        // Keymaps already registered by the system must be reused. Their
        // nodes are permanently resident and must not be loaded a second
        // time, otherwise SetKeyMapDefault would retain an avoidable leak.
        var resource = Exec.OpenResource(CString.FromLiteral("keymap.resource"));
        var existing = resource.IsNull
            ? APTR.Null
            : FindExistingKeyMap(resource, keymap);

        APTR keyMapAddress;
        if (existing.IsNotNull)
        {
            keyMapAddress = existing;
        }
        else
        {
            path = Exec.AllocMem(PathBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (path.IsNull)
            {
                result = DOS.RETURN_FAIL;
                error = (int)DOS.Error.NoFreeStore;
                goto Cleanup;
            }

            // AddPart is the DOS path join operation used by the original
            // command; it preserves assigns and handler-specific path rules.
            Exec.CopyMem(
                APTR.FromPointer(CString.ToUInt32(
                    CString.FromLiteral("DEVS:Keymaps"))),
                path, KeymapBasePathBytes);
            if (DOS.AddPart(CString.FromPointer(path.Raw),
                    CString.FromPointer(keymap), PathBytes) == 0)
            {
                result = DOS.RETURN_FAIL;
                error = (int)DOS.IoErr();
                goto Cleanup;
            }

            var loaded = SetKeyboardDosRaw.LoadSegRaw(
                CString.FromPointer(path.Raw));
            if (loaded == 0)
            {
                result = DOS.RETURN_FAIL;
                error = (int)DOS.IoErr();
                goto Cleanup;
            }
            segment = BPTR.FromRaw(loaded);
            keyMapAddress = APTR.FromPointer(segment.Address.Raw +
                KeymapNodeKeyMapOffset);
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
        Keymap.SetKeyMapDefault(keyMapAddress);
        // A newly loaded segment is now owned by the keymap resource/default;
        // an existing resource node has no private segment to release.
        keepSegment = segment.IsNotNull;
        Exec.CloseLibrary(keymapLibrary);
        Keymap.KeymapLibraryBase = APTR.Null;

    Cleanup:
        if (!keepSegment && segment.IsNotNull)
            SetKeyboardDosRaw.UnLoadSegRaw(segment);
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

    private static APTR FindExistingKeyMap(APTR resource, uint name)
    {
        var list = APTR.FromPointer(resource.Raw + KeymapResourceListOffset);
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        while (node.IsNotNull)
        {
            var nodeName = APTR.ReadUInt32(node, ExecLayout.Node.Name);
            if (nodeName != 0 && Utility.Stricmp(nodeName, name) == 0)
                return APTR.FromPointer(node.Raw + KeymapNodeKeyMapOffset);
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        return APTR.Null;
    }
}

/// <summary>Raw DOS bindings used for the classic LoadSeg ABI.</summary>
[AmigaLibrary(DOS.Name)]
internal static class SetKeyboardDosRaw
{
    [AmigaLvo(-150)]
    [return: M68kRegister(M68kRegister.D0)]
    public static extern uint LoadSegRaw(
        [M68kRegister(M68kRegister.D1)] CString name);

    [AmigaLvo(-156)]
    public static extern void UnLoadSegRaw(
        [M68kRegister(M68kRegister.D1)] BPTR segment);
}
