using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 AddDataTypes profile entry and shared-list bootstrap. The
/// option template, library floors, catalog, refresh path, and raw named-object
/// tag list are bound to the selected 40.42 Workbench HUNK. Descriptor parsing
/// and registration currently share the bounded common engine; guest parity
/// and some classic list-lifetime details remain open.
/// </summary>
public static class NativeWorkbench31AddDataTypesCommand
{
    public const string Template = "FILES/M,QUIET/S,REFRESH/S";
    public const uint ResultCount = 3;
    public const uint NamedListUserSpaceBytes = 140;
    // The original Workbench 3.1 HUNK passes 4096 to InternalLoadSeg for
    // embedded DTCD code; this differs from the MorphOS AROS_STACKSIZE path.
    public const uint EmbeddedCodeStackSize = 4096;

    private const uint NamedListTagNameSpace = 0x0fa0;
    private const uint NamedListTagUserSpace = 0x0fa1;
    private const uint NamedListTagFlags = 0x0fa3;
    private const int NamedObjectUserData = 0;
    private const int DataTypesListSorted = 46;
    private const int DataTypesListBinary = 60;
    private const int DataTypesListAscii = 74;
    private const int DataTypesListIff = 88;
    private const int DataTypesListMisc = 102;

    public struct LibraryLeases
    {
        public APTR PreviousDosBase;
        public APTR Dos;
        public APTR Utility;
        public APTR Intuition;
        public APTR IffParse;
        public APTR Locale;
        public APTR Catalog;
    }

    private struct NamedListTags
    {
        public TagItem NameSpace;
        public TagItem UserSpace;
        public TagItem Flags;
        public TagItem Done;

        public static APTR AddressOf(ref NamedListTags tags) =>
            throw new System.NotSupportedException(
                "NamedListTags.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Acquires the versioned classic dependencies in original open order.
    /// Catalog loading is optional, as the HUNK continues when it fails.
    /// </summary>
    public static bool TryOpenLibraries(out LibraryLeases libraries)
    {
        libraries = default;
        libraries.PreviousDosBase = DOS.DOSLibraryBase;

        libraries.Dos = Exec.OpenLibraryRaw(DOS.Name, 39);
        if (libraries.Dos.IsNull)
            return false;
        DOS.DOSLibraryBase = libraries.Dos;

        libraries.Utility = Exec.OpenLibraryRaw(Utility.Name, 39);
        if (libraries.Utility.IsNull)
        {
            CloseLibraries(ref libraries);
            return false;
        }
        Utility.UtilityLibraryBase = libraries.Utility;

        libraries.Intuition = Exec.OpenLibraryRaw(Intuition.Name, 39);
        if (libraries.Intuition.IsNull)
        {
            CloseLibraries(ref libraries);
            return false;
        }
        Intuition.IntuitionLibraryBase = libraries.Intuition;

        libraries.IffParse = Exec.OpenLibraryRaw(IffParse.Name, 37);
        if (libraries.IffParse.IsNull)
        {
            CloseLibraries(ref libraries);
            return false;
        }
        IffParse.IffParseLibraryBase = libraries.IffParse;

        libraries.Locale = Exec.OpenLibraryRaw(Locale.Name, 38);
        if (libraries.Locale.IsNull)
        {
            CloseLibraries(ref libraries);
            return false;
        }
        Locale.LocaleLibraryBase = libraries.Locale;

        libraries.Catalog = APTR.FromPointer(Locale.OpenCatalogA(0,
            CString.FromLiteral("sys/c.catalog"), 0));
        return true;
    }

    /// <summary>Closes opened dependencies in reverse order and restores IoErr.</summary>
    public static void CloseLibraries(ref LibraryLeases libraries)
    {
        var ioError = libraries.Dos.IsNotNull
            ? (int)DOS.IoErr() : 0;

        if (libraries.Catalog.IsNotNull && libraries.Locale.IsNotNull)
        {
            Locale.CloseCatalog(libraries.Catalog.Raw);
            libraries.Catalog = APTR.Null;
        }
        if (libraries.Locale.IsNotNull)
        {
            Locale.LocaleLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.Locale);
            libraries.Locale = APTR.Null;
        }
        if (libraries.IffParse.IsNotNull)
        {
            IffParse.IffParseLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.IffParse);
            libraries.IffParse = APTR.Null;
        }
        if (libraries.Intuition.IsNotNull)
        {
            Intuition.IntuitionLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.Intuition);
            libraries.Intuition = APTR.Null;
        }
        if (libraries.Utility.IsNotNull)
        {
            Utility.UtilityLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.Utility);
            libraries.Utility = APTR.Null;
        }
        if (libraries.Dos.IsNotNull)
        {
            DOS.DOSLibraryBase = libraries.PreviousDosBase;
            Exec.CloseLibrary(libraries.Dos);
            libraries.Dos = APTR.Null;
        }
        if (libraries.PreviousDosBase.IsNotNull)
            DOS.SetIoErr((DOS.Error)ioError);
        libraries.PreviousDosBase = APTR.Null;
    }

    /// <summary>
    /// Runs classic CLI parsing and command cleanup. Workbench uses its
    /// three-result ReadArgs template and does not implement MorphOS LIST.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!TryOpenLibraries(out var libraries))
        {
            // TryOpenLibraries may already have unwound a partial library
            // stack and restored the outer DOS base. Read its preserved IoErr
            // even when the lease record has been cleared by that unwind.
            ioError = (int)DOS.IoErr();
            CloseLibraries(ref libraries);
            return DOS.RETURN_FAIL;
        }

        var list = AcquireSharedList();
        if (list.IsNull)
        {
            ioError = (int)DOS.IoErr();
            CloseLibraries(ref libraries);
            return DOS.RETURN_FAIL;
        }

        NativeMorphOSAddDataTypesCommand.EnsureBuiltins(list);
        if (!NativeCommandArguments.TryRead(CString.FromLiteral(Template),
                ResultCount, out var arguments))
        {
            ioError = arguments.IoError;
            if (ioError != 0)
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            NativeMorphOSAddDataTypesCommand.ReleaseSharedList(list);
            CloseLibraries(ref libraries);
            return DOS.RETURN_FAIL;
        }

        var files = APTR.FromPointer(ReadResult(ref arguments, 0));
        var quiet = ReadResult(ref arguments, 1) != 0;
        var refresh = ReadResult(ref arguments, 2) != 0;
        if (refresh)
            NativeMorphOSAddDataTypesCommand.RefreshClassicDirectory(list,
                quiet, EmbeddedCodeStackSize);
        else if (files.IsNotNull)
            NativeMorphOSAddDataTypesCommand.ScanFiles(files, list, quiet,
                EmbeddedCodeStackSize);

        arguments.Release();
        NativeMorphOSAddDataTypesCommand.ReleaseSharedList(list);
        ioError = (int)DOS.IoErr();
        CloseLibraries(ref libraries);
        return DOS.RETURN_OK;
    }

    /// <summary>Runs Workbench startup argument processing without ReadArgs.</summary>
    public static int RunWorkbenchStartup(APTR startup, out int ioError)
    {
        ioError = 0;
        if (!TryOpenLibraries(out var libraries))
        {
            ioError = (int)DOS.IoErr();
            CloseLibraries(ref libraries);
            return DOS.RETURN_FAIL;
        }

        var list = AcquireSharedList();
        if (list.IsNull)
        {
            ioError = (int)DOS.IoErr();
            CloseLibraries(ref libraries);
            return DOS.RETURN_FAIL;
        }

        NativeMorphOSAddDataTypesCommand.EnsureBuiltins(list);
        var result = NativeMorphOSAddDataTypesCommand.ProcessWorkbenchStartup(
            startup, list, EmbeddedCodeStackSize)
            ? DOS.RETURN_OK : DOS.RETURN_FAIL;
        ioError = (int)DOS.IoErr();
        NativeMorphOSAddDataTypesCommand.ReleaseSharedList(list);
        CloseLibraries(ref libraries);
        return result;
    }

    /// <summary>
    /// Finds the existing named list or creates the classic 140-byte
    /// user-space object, initializes its semaphore and list heads, creates
    /// the four built-ins, and publishes it in the global namespace.
    /// </summary>
    public static APTR AcquireSharedList()
    {
        var name = CString.FromLiteral("DataTypesList");
        var namedObject = APTR.FromPointer(Utility.FindNamedObject(
            APTR.Null, name, APTR.Null));
        var created = namedObject.IsNull;
        if (created)
        {
            var tags = new NamedListTags
            {
                NameSpace = TagItem.Create(NamedListTagNameSpace, 1),
                UserSpace = TagItem.Create(NamedListTagUserSpace,
                    NamedListUserSpaceBytes),
                Flags = TagItem.Create(NamedListTagFlags, 3),
                Done = TagItem.Done
            };
            namedObject = APTR.FromPointer(Utility.AllocNamedObjectA(name,
                APTR.ToUInt32(NamedListTags.AddressOf(ref tags))));
            if (namedObject.IsNull)
                return APTR.Null;
        }

        var list = APTR.FromPointer(APTR.ReadUInt32(namedObject,
            NamedObjectUserData));
        if (list.IsNull)
        {
            Utility.ReleaseNamedObject(APTR.ToUInt32(namedObject));
            return APTR.Null;
        }

        if (created)
        {
            Exec.InitSemaphore(list);
            NativeMorphOSAddDataTypesCommand.InitializeList(
                APTR.FromPointer(list.Raw + DataTypesListSorted));
            NativeMorphOSAddDataTypesCommand.InitializeList(
                APTR.FromPointer(list.Raw + DataTypesListBinary));
            NativeMorphOSAddDataTypesCommand.InitializeList(
                APTR.FromPointer(list.Raw + DataTypesListAscii));
            NativeMorphOSAddDataTypesCommand.InitializeList(
                APTR.FromPointer(list.Raw + DataTypesListIff));
            NativeMorphOSAddDataTypesCommand.InitializeList(
                APTR.FromPointer(list.Raw + DataTypesListMisc));
            NativeMorphOSAddDataTypesCommand.EnsureBuiltins(list);
            if (Utility.AddNamedObject(APTR.Null,
                    APTR.ToUInt32(namedObject)) == 0)
            {
                Utility.FreeNamedObject(APTR.ToUInt32(namedObject));
                return APTR.Null;
            }
        }

        Utility.ReleaseNamedObject(APTR.ToUInt32(namedObject));
        Exec.ObtainSemaphore(APTR.FromPointer(list.Raw));
        return list;
    }

    private static uint ReadResult(ref NativeCommandArguments arguments,
        uint index)
    {
        return arguments.TryGetResult(index, out var value) ? value : 0;
    }
}
