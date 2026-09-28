using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 AddDataTypes command body and shared-list transaction. This is
/// a minimal 68k interop view assembled from public Exec/Datatypes layouts and
/// field ordering observed in the released command and packed reference; it
/// intentionally does not import the datatypes.library private header.
/// </summary>
public static class NativeMorphOSAddDataTypesCommand
{
    public const string Template = "FILES/M,QUIET/S,REFRESH/S,LIST/S";
    public const uint CliResultCount = 4;
    private const uint ControlC = 1u << 12;

    public struct CliOptions
    {
        public APTR Files;
        public bool Quiet;
        public bool Refresh;
        public bool List;
    }

    public struct LibraryLeases
    {
        public APTR Utility;
        public APTR IffParse;
        public APTR Locale;
        public APTR DataTypes;
    }

    private struct ListCells
    {
        public uint BaseName;
        public uint Name;

        public static APTR AddressOf(ref ListCells cells) =>
            throw new System.NotSupportedException(
                "ListCells.AddressOf is lowered by CopperSharp.");
    }

    private struct PropertyPairs
    {
        public uint TypeHeaderType;
        public uint TypeHeaderId;
        public uint CodeType;
        public uint CodeId;

        public static APTR AddressOf(ref PropertyPairs pairs) =>
            throw new System.NotSupportedException(
                "PropertyPairs.AddressOf is lowered by CopperSharp.");
    }

    private struct ToolCollectionPair
    {
        public uint Type;
        public uint Id;

        public static APTR AddressOf(ref ToolCollectionPair pair) =>
            throw new System.NotSupportedException(
                "ToolCollectionPair.AddressOf is lowered by CopperSharp.");
    }

    private struct LoaderFunctionArray
    {
        public uint Read;
        public uint Allocate;
        public uint Free;

        public static APTR AddressOf(ref LoaderFunctionArray functions) =>
            throw new System.NotSupportedException(
                "LoaderFunctionArray.AddressOf is lowered by CopperSharp.");
    }

    private struct LoaderStackSize
    {
        public uint Value;

        public static APTR AddressOf(ref LoaderStackSize stack) =>
            throw new System.NotSupportedException(
                "LoaderStackSize.AddressOf is lowered by CopperSharp.");
    }

    // SignalSemaphore, List, DataType, and DataTypeHeader are public 68k
    // layouts.  The private tail fields below follow the source's declared
    // field order and remain profile-specific until guest comparison closes.
    public const uint SignalSemaphoreSize = 46;
    public const uint ListSize = 14;
    public const uint DataTypeSize = 58;
    public const uint DataTypeHeaderSize = 32;

    public const int DataTypesListLock = 0;
    public const int DataTypesListSorted = 46;
    public const int DataTypesListBinary = 60;
    public const int DataTypesListAscii = 74;
    public const int DataTypesListIff = 88;
    public const int DataTypesListMisc = 102;
    public const int DataTypesListLongestMask = 116;
    public const int DataTypesListDateStamp = 120;
    public const uint DataTypesListSize = 132;

    public const int CompoundDataTypeNode1 = 0;
    public const int CompoundDataTypeNode2 = 14;
    public const int CompoundDataTypeHeaderPointer = 28;
    public const int CompoundDataTypeToolList = 32;
    public const int CompoundDataTypeFunctionName = 46;
    public const int CompoundDataTypeAttributeList = 50;
    public const int CompoundDataTypeLength = 54;
    public const int CompoundDataTypeFlagLong = 58;
    public const int CompoundDataTypeParsePatternSize = 62;
    public const int CompoundDataTypeParsePatternMemory = 66;
    public const int CompoundDataTypeCodeChunk = 70;
    public const int CompoundDataTypeCodeChunkSize = 74;
    public const int CompoundDataTypeSegment = 78;
    public const int CompoundDataTypeFunction = 82;
    public const int CompoundDataTypeOpenCount = 86;
    public const int CompoundDataTypeInlineHeader = 90;
    public const uint CompoundDataTypeSize = 122;
    private const uint LoaderStateSize = 12;
    // The released MorphOS source initializes the embedded-DTCD loader stack
    // from AROS_STACKSIZE. The selected ppc-morphos target header defines it
    // as 32 KiB; keep the 2004 header-to-binary correspondence open in the
    // profile audit until an exact historical SDK header is captured.
    public const uint MorphOSEmbeddedCodeStackSize = 32768;

    [AmigaLibrary(DOS.Name)]
    [AmigaLvo(DosLvo.InternalLoadSeg)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern BPTR InternalLoadSegRaw(
        [M68kRegister(M68kRegister.D0)] BPTR file,
        [M68kRegister(M68kRegister.A0)] APTR table,
        [M68kRegister(M68kRegister.A1)] APTR functionArray,
        [M68kRegister(M68kRegister.A2)] APTR stack);

    [AmigaLibrary(DOS.Name)]
    [AmigaLvo(DosLvo.DupLock)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern BPTR DupLockRaw(
        [M68kRegister(M68kRegister.D1)] BPTR lock_);

    private const int NamedObjectUserData = 0;
    private const int NodeSuccessor = 0;
    private const int NodeName = 10;
    private const int ListHead = 0;
    private const int FileHeaderName = 0;
    private const int FileHeaderBaseName = 4;
    private const int FileHeaderPattern = 8;
    private const int FileHeaderMask = 12;
    private const int FileHeaderGroup = 16;
    private const int FileHeaderId = 20;
    private const int FileHeaderMaskLength = 24;
    private const int FileHeaderFlags = 28;
    private const int FileHeaderPriority = 30;
    private const int WorkbenchStartupNumArgs = 28;
    private const int WorkbenchStartupArgList = 36;
    private const int WorkbenchArgumentLock = 0;
    private const int WorkbenchArgumentName = 4;
    private const uint WorkbenchArgumentSize = 8;
    private const uint ExclusionPatternBytes = 39;
    private const uint PatternUnusedFlag = 1;
    private const uint PatternWildFlag = 2;
    private const uint TypeId = 0x44545950;
    private const uint HeaderId = 0x44544844;
    private const uint CodeId = 0x44544344;
    private const uint ToolListId = 0x4454544c;
    private const uint FormId = 0x464f524d;

    /// <summary>
    /// Uses DOS ReadArgs with the MorphOS 3.20 template. The returned lease
    /// owns its result array and RDArgs record; the caller must release it
    /// only after all FILES pointers have been consumed.
    /// </summary>
    public static bool TryReadArguments(
        out NativeCommandArguments arguments) =>
        NativeCommandArguments.TryRead(CString.FromLiteral(Template),
            CliResultCount, out arguments);

    /// <summary>
    /// Opens the libraries required by the MorphOS command in source order.
    /// On failure, closes every earlier lease while preserving the failing
    /// OpenLibrary IoErr for the caller.
    /// </summary>
    public static bool TryOpenLibraries(out LibraryLeases libraries)
    {
        libraries = default;
        libraries.Utility = Exec.OpenLibraryRaw(Utility.Name, 37);
        if (libraries.Utility.IsNull)
            return false;
        Utility.UtilityLibraryBase = libraries.Utility;

        libraries.IffParse = Exec.OpenLibraryRaw(IffParse.Name, 37);
        if (libraries.IffParse.IsNull)
        {
            CloseLibraries(ref libraries);
            return false;
        }
        IffParse.IffParseLibraryBase = libraries.IffParse;

        libraries.Locale = Exec.OpenLibraryRaw(Locale.Name, 37);
        if (libraries.Locale.IsNull)
        {
            CloseLibraries(ref libraries);
            return false;
        }
        Locale.LocaleLibraryBase = libraries.Locale;

        libraries.DataTypes = Exec.OpenLibraryRaw(Datatypes.Name, 44);
        if (libraries.DataTypes.IsNull)
        {
            CloseLibraries(ref libraries);
            return false;
        }
        Datatypes.DatatypesLibraryBase = libraries.DataTypes;
        return true;
    }

    /// <summary>Closes all opened MorphOS command libraries in reverse order.</summary>
    public static void CloseLibraries(ref LibraryLeases libraries)
    {
        var ioError = DOS.IoErr();
        if (libraries.DataTypes.IsNotNull)
        {
            Datatypes.DatatypesLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.DataTypes);
            libraries.DataTypes = APTR.Null;
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
        if (libraries.Utility.IsNotNull)
        {
            Utility.UtilityLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.Utility);
            libraries.Utility = APTR.Null;
        }
        DOS.SetIoErr(ioError);
    }

    /// <summary>
    /// Runs the MorphOS 3.20 CLI command from ReadArgs through shared-list
    /// cleanup. Startup message receive/reply and the DOS lease remain the
    /// caller's responsibility, as for the other command bodies.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!TryOpenLibraries(out var libraries))
        {
            ioError = (int)DOS.IoErr();
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        var list = AcquireSharedList();
        if (list.IsNull)
        {
            ioError = (int)DOS.IoErr();
            CloseLibraries(ref libraries);
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        EnsureBuiltins(list);
        if (!TryReadArguments(out var arguments))
        {
            ioError = arguments.IoError;
            if (ioError != 0)
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            ReleaseSharedList(list);
            CloseLibraries(ref libraries);
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        if (!TryGetOptions(ref arguments, out var options))
        {
            arguments.Release();
            ioError = (int)DOS.Error.BadTemplate;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            ReleaseSharedList(list);
            CloseLibraries(ref libraries);
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        if (options.Refresh)
            RefreshDirectories(list, options.Quiet,
                MorphOSEmbeddedCodeStackSize);
        else if (options.Files.IsNotNull)
            ScanFiles(options.Files, list, options.Quiet,
                MorphOSEmbeddedCodeStackSize);
        if (options.List)
            ListTypes(list);

        arguments.Release();
        ReleaseSharedList(list);
        ioError = (int)DOS.IoErr();
        CloseLibraries(ref libraries);
        DOS.SetIoErr((DOS.Error)ioError);
        return DOS.RETURN_OK;
    }

    /// <summary>Runs MorphOS Workbench-startup descriptor processing.</summary>
    public static int RunWorkbenchStartup(APTR startup, out int ioError)
    {
        ioError = 0;
        if (!TryOpenLibraries(out var libraries))
        {
            ioError = (int)DOS.IoErr();
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        var list = AcquireSharedList();
        if (list.IsNull)
        {
            ioError = (int)DOS.IoErr();
            CloseLibraries(ref libraries);
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        EnsureBuiltins(list);
        var result = ProcessWorkbenchStartup(startup, list,
            MorphOSEmbeddedCodeStackSize)
            ? DOS.RETURN_OK : DOS.RETURN_FAIL;
        ioError = (int)DOS.IoErr();
        ReleaseSharedList(list);
        CloseLibraries(ref libraries);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    /// <summary>Copies the four positional ReadArgs slots while the lease lives.</summary>
    public static bool TryGetOptions(
        ref NativeCommandArguments arguments, out CliOptions options)
    {
        options = default;
        if (!arguments.TryGetResult(0, out var files) ||
            !arguments.TryGetResult(1, out var quiet) ||
            !arguments.TryGetResult(2, out var refresh) ||
            !arguments.TryGetResult(3, out var list))
            return false;

        options.Files = APTR.FromPointer(files);
        options.Quiet = quiet != 0;
        options.Refresh = refresh != 0;
        options.List = list != 0;
        return true;
    }

    /// <summary>
    /// Scans each FILES/M pattern with the public DOS matcher. The matcher may
    /// enter only its first directory, matching the MorphOS command's flat
    /// traversal rule. The caller keeps the ReadArgs lease and shared-list
    /// semaphore live until this method returns.
    /// </summary>
    public static void ScanFiles(APTR patterns, APTR list, bool quiet,
        uint embeddedCodeStackSize)
    {
        if (!TryCreateExclusionPattern(quiet, out var exclusion))
            return;

        var pattern = patterns;
        while (pattern.IsNotNull)
        {
            var text = APTR.ReadUInt32(pattern, 0);
            if (text == 0)
                break;
            ScanDirectory(CString.FromPointer(text), list, exclusion, quiet,
                embeddedCodeStackSize);
            pattern = APTR.FromPointer(pattern.Raw + 4);
        }

        Exec.FreeVec(exclusion);
    }

    /// <summary>
    /// Refreshes the two MorphOS descriptor directories in source order. The
    /// system directory is scanned first and the MOSSYS directory second so
    /// newer system descriptors can replace earlier registrations. Directory
    /// aliases are scanned only once, and the process window pointer is
    /// hidden from any DOS requester for the duration of the refresh.
    /// </summary>
    public static void RefreshDirectories(APTR list, bool quiet,
        uint embeddedCodeStackSize)
    {
        if (list.IsNull || !TryCreateExclusionPattern(quiet,
                out var exclusion))
            return;

        var process = Exec.FindTask(CString.FromPointer(0));
        if (process.IsNull)
        {
            Exec.FreeVec(exclusion);
            return;
        }

        var oldWindowPointer = APTR.ReadUInt32(process,
            DosLayout.Process.WindowPointer);
        APTR.WriteUInt32(process, DosLayout.Process.WindowPointer,
            uint.MaxValue);

        var systemLock = DOS.LockRaw(
            CString.FromLiteral("DEVS:DataTypes/"), DOS.LockMode.Read);
        var morphOsLock = DOS.LockRaw(
            CString.FromLiteral("MOSSYS:Devs/DataTypes/"),
            DOS.LockMode.Read);
        if (systemLock.IsNotNull && morphOsLock.IsNotNull &&
            DOS.SameLock(systemLock, morphOsLock) == 0)
        {
            DOS.UnLock(systemLock);
            systemLock = BPTR.Null;
        }

        if (systemLock.IsNotNull)
        {
            if (DateScan(list, CString.FromLiteral("DEVS:DataTypes")))
                ScanDirectory(CString.FromLiteral("DEVS:DataTypes"), list,
                    exclusion, quiet, embeddedCodeStackSize);
            DOS.UnLock(systemLock);
        }

        if (morphOsLock.IsNotNull)
        {
            if (DateScan(list,
                    CString.FromLiteral("MOSSYS:Devs/DataTypes")))
                ScanDirectory(CString.FromLiteral(
                    "MOSSYS:Devs/DataTypes"), list, exclusion, quiet,
                    embeddedCodeStackSize);
            DOS.UnLock(morphOsLock);
        }

        APTR.WriteUInt32(process, DosLayout.Process.WindowPointer,
            oldWindowPointer);
        Exec.FreeVec(exclusion);
    }

    /// <summary>
    /// Runs the Workbench 3.1 REFRESH date check and conditional directory
    /// scan. The selected classic HUNK uses DEVS:DataTypes for its first-entry
    /// stamp and DEVS:DataTypes/#? for the subsequent scan.
    /// </summary>
    public static void RefreshClassicDirectory(APTR list, bool quiet,
        uint embeddedCodeStackSize)
    {
        if (list.IsNull || !TryCreateExclusionPattern(quiet,
                out var exclusion))
            return;

        var process = Exec.FindTask(CString.FromPointer(0));
        if (process.IsNull)
        {
            Exec.FreeVec(exclusion);
            return;
        }

        var oldWindowPointer = APTR.ReadUInt32(process,
            DosLayout.Process.WindowPointer);
        APTR.WriteUInt32(process, DosLayout.Process.WindowPointer,
            uint.MaxValue);

        if (DateScan(list, CString.FromLiteral("DEVS:DataTypes")))
            ScanDirectory(CString.FromLiteral("DEVS:DataTypes/#?"), list,
                exclusion, quiet, embeddedCodeStackSize);

        APTR.WriteUInt32(process, DosLayout.Process.WindowPointer,
            oldWindowPointer);
        Exec.FreeVec(exclusion);
    }

    /// <summary>
    /// Processes MorphOS Workbench startup arguments. The first argument's
    /// lock supplies the startup directory; each later argument is opened
    /// relative to its own lock, then the original process directory is
    /// restored and the duplicated startup lock is released.
    /// </summary>
    public static bool ProcessWorkbenchStartup(APTR startup, APTR list,
        uint embeddedCodeStackSize)
    {
        if (startup.IsNull || list.IsNull)
        {
            DOS.SetIoErr(DOS.Error.RequiredArgumentMissing);
            return false;
        }

        var argumentCount = APTR.ReadUInt32(startup,
            WorkbenchStartupNumArgs);
        var argumentList = APTR.FromPointer(APTR.ReadUInt32(startup,
            WorkbenchStartupArgList));
        if (argumentCount == 0 || argumentList.IsNull ||
            argumentCount > (uint.MaxValue - argumentList.Raw) /
                WorkbenchArgumentSize)
        {
            DOS.SetIoErr(DOS.Error.RequiredArgumentMissing);
            return false;
        }

        var firstArgument = APTR.FromPointer(argumentList.Raw);
        var firstLock = BPTR.FromRaw(APTR.ReadUInt32(firstArgument,
            WorkbenchArgumentLock));
        var duplicateLock = DupLockRaw(firstLock);
        if (duplicateLock.IsNull)
            return false;

        var originalDirectory = DOS.CurrentDirRaw(duplicateLock);
        for (uint index = 1; index < argumentCount; index++)
        {
            var argument = APTR.FromPointer(argumentList.Raw +
                index * WorkbenchArgumentSize);
            var lock_ = BPTR.FromRaw(APTR.ReadUInt32(argument,
                WorkbenchArgumentLock));
            var name = CString.FromPointer(APTR.ReadUInt32(argument,
                WorkbenchArgumentName));
            var previousDirectory = DOS.CurrentDirRaw(lock_);
            LoadDatatype(name, list, embeddedCodeStackSize);
            DOS.CurrentDirRaw(previousDirectory);
        }

        var duplicatedDirectory = DOS.CurrentDirRaw(originalDirectory);
        if (duplicatedDirectory.IsNotNull)
            DOS.UnLock(duplicatedDirectory);
        return true;
    }

    private static bool TryCreateExclusionPattern(bool quiet,
        out APTR exclusion)
    {
        exclusion = Exec.AllocVec(ExclusionPatternBytes,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (exclusion.IsNull)
        {
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            if (!quiet)
                DOS.PrintFault(DOS.Error.NoFreeStore,
                    CString.FromPointer(0));
            return false;
        }

        if (DOS.ParsePatternNoCase(
                CString.FromLiteral("#?.(info|backdrop)"), exclusion,
                (int)ExclusionPatternBytes) >= 0)
            return true;

        Exec.FreeVec(exclusion);
        exclusion = APTR.Null;
        return false;
    }

    private static bool DateScan(APTR list, CString path)
    {
        var lock_ = DOS.LockRaw(path, DOS.LockMode.Read);
        if (lock_.IsNull)
            return true;

        var result = true;
        var fileInfo = DOS.AllocDosObject(
            (uint)DosObjectType.FileInfoBlock, APTR.Null);
        if (fileInfo.IsNotNull)
        {
            if (DOS.Examine(lock_, fileInfo) != 0 &&
                DOS.ExNext(lock_, fileInfo) != 0)
            {
                var directoryStamp = APTR.FromPointer(fileInfo.Raw +
                    (uint)FileInfoBlock.DateDaysOffset);
                var listStamp = APTR.FromPointer(list.Raw +
                    (uint)DataTypesListDateStamp);
                if (DOS.CompareDates(APTR.ToUInt32(directoryStamp),
                        APTR.ToUInt32(listStamp)) == 0)
                    result = false;
                else
                {
                    for (var offset = 0; offset <
                            DosLayout.DateStamp.Size; offset += 4)
                        APTR.WriteUInt32(listStamp, offset,
                            APTR.ReadUInt32(directoryStamp, offset));
                }
            }

            DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock,
                fileInfo);
        }

        DOS.UnLock(lock_);
        return result;
    }

    private static void ScanDirectory(CString pattern, APTR list,
        APTR exclusion, bool quiet, uint embeddedCodeStackSize)
    {
        var anchor = Exec.AllocVec(AnchorPath.Size,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (anchor.IsNull)
            return;

        APTR.WriteUInt32(anchor, 8, ControlC);
        var result = DOS.MatchFirst(pattern, anchor);
        var directoryEntered = false;
        var stoppedForBreak = false;
        while (result == 0)
        {
            if (DOS.CheckSignal(unchecked((int)ControlC)) != 0)
            {
                if (!quiet)
                    DOS.PrintFault(DOS.Error.Break, CString.FromPointer(0));
                stoppedForBreak = true;
                break;
            }

            var fib = APTR.FromPointer(anchor.Raw +
                DosLayout.AnchorPath.Info);
            var entryType = APTR.ReadUInt32(fib,
                FileInfoBlock.DirEntryTypeOffset);
            if (unchecked((int)entryType) > 0)
            {
                if (!directoryEntered)
                {
                    directoryEntered = true;
                    var flags = APTR.ReadUInt8(anchor,
                        DosLayout.AnchorPath.Flags);
                    if ((flags & (byte)AnchorPathFlags.DidDirectory) == 0)
                        flags |= (byte)AnchorPathFlags.DoDirectory;
                    flags &= unchecked((byte)~(byte)AnchorPathFlags.DidDirectory);
                    APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                        flags);
                }
            }
            else
            {
                var name = CString.FromPointer(anchor.Raw +
                    DosLayout.AnchorPath.Info +
                    FileInfoBlock.FileNameOffset);
                if (DOS.MatchPatternNoCase(
                        CString.FromPointer(exclusion.Raw), name) == 0)
                {
                    var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
                        DosLayout.AnchorPath.Current));
                    if (current.IsNull)
                    {
                        result = (int)DOS.Error.ObjectNotFound;
                        break;
                    }
                    var lock_ = BPTR.FromRaw(APTR.ReadUInt32(current,
                        DosLayout.AChain.Lock));
                    if (lock_.Raw != 0)
                    {
                        var oldDirectory = DOS.CurrentDirRaw(lock_);
                        LoadDatatype(name, list, embeddedCodeStackSize);
                        DOS.CurrentDirRaw(oldDirectory);
                    }
                }
            }

            result = DOS.MatchNext(anchor);
        }

        if (!stoppedForBreak && result != (int)DOS.Error.NoMoreEntries &&
            !quiet)
            DOS.PrintFault((DOS.Error)result, CString.FromPointer(0));

        DOS.MatchEnd(anchor);
        Exec.FreeVec(anchor);
    }

    private static void LoadDatatype(CString name, APTR list,
        uint embeddedCodeStackSize)
    {
        var iff = IffParse.AllocIFF();
        if (iff.IsNull)
            return;

        var file = DOS.OpenRaw(name, DOS.FileMode.OldFile);
        if (file.Raw == 0)
        {
            IffParse.FreeIFF(iff);
            return;
        }

        iff.SetStream(file);
        IffParse.InitIFFasDOS(iff);
        var opened = IffParse.OpenIFF(iff, IffParse.IFFF_READ) == 0;
        if (opened)
        {
            var properties = new PropertyPairs
            {
                TypeHeaderType = TypeId,
                TypeHeaderId = HeaderId,
                CodeType = TypeId,
                CodeId = CodeId
            };
            var tools = new ToolCollectionPair
            {
                Type = TypeId,
                Id = ToolListId
            };
            if (IffParse.PropChunks(iff,
                    PropertyPairs.AddressOf(ref properties), 2) == 0 &&
                IffParse.CollectionChunks(iff,
                    ToolCollectionPair.AddressOf(ref tools), 1) == 0 &&
                IffParse.StopOnExit(iff, unchecked((int)TypeId),
                    unchecked((int)FormId)) == 0 &&
                IffParse.ParseIFF(iff, IffParse.IFFPARSE_SCAN) ==
                    (int)IffError.Eoc)
            {
                CreateDatatype(iff, list, embeddedCodeStackSize);
            }
            IffParse.CloseIFF(iff);
        }

        DOS.Close(file);
        IffParse.FreeIFF(iff);
    }

    private static void CreateDatatype(IFFHandle iff, APTR list,
        uint embeddedCodeStackSize)
    {
        var storedHeader = APTR.FromPointer(IffParse.FindProp(iff,
            unchecked((int)TypeId), unchecked((int)HeaderId)));
        if (storedHeader.IsNull)
            return;

        var headerBytes = APTR.ReadUInt32(storedHeader, 0);
        var headerData = APTR.FromPointer(APTR.ReadUInt32(storedHeader, 4));
        if (headerBytes < DataTypeHeaderSize || headerData.IsNull)
            return;

        var storedCode = APTR.FromPointer(IffParse.FindProp(iff,
            unchecked((int)TypeId), unchecked((int)CodeId)));
        RegisterFileHeader(list, headerData, headerBytes, storedCode,
            embeddedCodeStackSize);
    }

    private static void RegisterFileHeader(APTR list, APTR fileHeader,
        uint headerBytes, APTR storedCode, uint embeddedCodeStackSize)
    {
        if (headerBytes < DataTypeHeaderSize ||
            headerBytes > uint.MaxValue -
                (CompoundDataTypeInlineHeader + DataTypeHeaderSize))
            return;

        var nameOffset = APTR.ReadUInt32(fileHeader, FileHeaderName);
        var baseNameOffset = APTR.ReadUInt32(fileHeader,
            FileHeaderBaseName);
        var patternOffset = APTR.ReadUInt32(fileHeader,
            FileHeaderPattern);
        var maskOffset = APTR.ReadUInt32(fileHeader, FileHeaderMask);
        var maskLength = APTR.ReadUInt16(fileHeader,
            FileHeaderMaskLength);
        if (!IsFileString(fileHeader, headerBytes, nameOffset) ||
            !IsFileString(fileHeader, headerBytes, baseNameOffset) ||
            !IsFileString(fileHeader, headerBytes, patternOffset) ||
            maskLength != 0 && (maskOffset < DataTypeHeaderSize ||
                maskOffset > headerBytes ||
                (uint)maskLength * 2 > headerBytes - maskOffset))
            return;

        var flags = APTR.ReadUInt16(fileHeader, FileHeaderFlags);
        var typeListOffset = TypeListOffset(flags);
        if (typeListOffset < 0)
            return;

        var allocationBytes = CompoundDataTypeInlineHeader + headerBytes;
        var compound = Exec.AllocVec(allocationBytes,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (compound.IsNull)
        {
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return;
        }

        var header = APTR.FromPointer(compound.Raw +
            (uint)CompoundDataTypeInlineHeader);
        var payload = APTR.FromPointer(compound.Raw +
            CompoundDataTypeSize);
        for (uint index = DataTypeHeaderSize; index < headerBytes; index++)
            APTR.WriteUInt8(payload, (int)(index - DataTypeHeaderSize),
                APTR.ReadUInt8(fileHeader, (int)index));

        var name = APTR.FromPointer(header.Raw + nameOffset);
        var baseName = APTR.FromPointer(header.Raw + baseNameOffset);
        var pattern = APTR.FromPointer(header.Raw + patternOffset);
        APTR.WriteUInt32(compound, CompoundDataTypeHeaderPointer,
            APTR.ToUInt32(header));
        APTR.WriteUInt32(compound, CompoundDataTypeLength, allocationBytes);
        APTR.WriteUInt32(header, 0, APTR.ToUInt32(name));
        APTR.WriteUInt32(header, 4, APTR.ToUInt32(baseName));
        APTR.WriteUInt32(header, 8, APTR.ToUInt32(pattern));
        APTR.WriteUInt32(header, 12, maskLength == 0 ? 0 :
            APTR.ToUInt32(APTR.FromPointer(header.Raw + maskOffset)));
        APTR.WriteUInt32(header, 16,
            APTR.ReadUInt32(fileHeader, FileHeaderGroup));
        APTR.WriteUInt32(header, 20,
            APTR.ReadUInt32(fileHeader, FileHeaderId));
        APTR.WriteUInt16(header, 24, maskLength);
        APTR.WriteUInt16(header, 26,
            APTR.ReadUInt16(fileHeader, 26));
        APTR.WriteUInt16(header, 28, flags);
        APTR.WriteUInt16(header, 30,
            APTR.ReadUInt16(fileHeader, FileHeaderPriority));

        APTR.WriteUInt32(compound,
            CompoundDataTypeNode1 + NodeName, APTR.ToUInt32(name));
        APTR.WriteUInt32(compound,
            CompoundDataTypeNode2 + NodeName, APTR.ToUInt32(name));
        InitializeList(APTR.FromPointer(compound.Raw +
            (uint)CompoundDataTypeToolList));

        if (!BuildParsePattern(compound, pattern))
        {
            DeleteDatatype(compound);
            return;
        }

        if (storedCode.IsNotNull && !LoadEmbeddedCode(compound, storedCode,
                embeddedCodeStackSize))
        {
            DeleteDatatype(compound);
            return;
        }

        AddDatatype(list, compound, typeListOffset);
    }

    private static bool LoadEmbeddedCode(APTR compound, APTR storedProperty,
        uint embeddedCodeStackSize)
    {
        var size = APTR.ReadUInt32(storedProperty, 0);
        var source = APTR.FromPointer(APTR.ReadUInt32(storedProperty, 4));
        if (size == 0 || source.IsNull)
            return false;

        var code = Exec.AllocVec(size,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (code.IsNull)
            return false;
        Exec.CopyMem(source, code, size);

        var loaderState = Exec.AllocVec(LoaderStateSize,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (loaderState.IsNull)
        {
            Exec.FreeVec(code);
            return false;
        }

        APTR.WriteUInt32(loaderState, 0, code.Raw);
        APTR.WriteUInt32(loaderState, 4, size);
        APTR.WriteUInt32(loaderState, 8, 0);

        var functions = new LoaderFunctionArray
        {
            Read = APTR.ExportAddress("copperos.morphos.adddatatypes.load.read").Raw,
            Allocate = APTR.ExportAddress("copperos.morphos.adddatatypes.load.alloc").Raw,
            Free = APTR.ExportAddress("copperos.morphos.adddatatypes.load.free").Raw
        };
        var stack = new LoaderStackSize { Value = embeddedCodeStackSize };
        var segment = InternalLoadSegRaw(BPTR.FromRaw(loaderState.Raw),
            APTR.Null, LoaderFunctionArray.AddressOf(ref functions),
            LoaderStackSize.AddressOf(ref stack));
        Exec.FreeVec(loaderState);

        if (segment.IsNull)
        {
            Exec.FreeVec(code);
            return false;
        }

        APTR.WriteUInt32(compound, CompoundDataTypeCodeChunk, code.Raw);
        APTR.WriteUInt32(compound, CompoundDataTypeCodeChunkSize, size);
        APTR.WriteUInt32(compound, CompoundDataTypeSegment,
            segment.Raw);
        APTR.WriteUInt32(compound, CompoundDataTypeFunction,
            unchecked((segment.Raw << 2) + 4));
        return true;
    }

    [M68kExport("copperos.morphos.adddatatypes.load.read")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint ReadEmbeddedCode(
        [M68kRegister(M68kRegister.D1)] APTR state,
        [M68kRegister(M68kRegister.A0)] APTR destination,
        [M68kRegister(M68kRegister.D0)] uint requested)
    {
        var size = APTR.ReadUInt32(state, 4);
        var position = APTR.ReadUInt32(state, 8);
        if (position >= size)
            return 0;

        var remaining = size - position;
        var actual = requested < remaining ? requested : remaining;
        var source = APTR.FromPointer(unchecked(
            APTR.ReadUInt32(state, 0) + position));
        Exec.CopyMem(source, destination, actual);
        APTR.WriteUInt32(state, 8, position + actual);
        return actual;
    }

    [M68kExport("copperos.morphos.adddatatypes.load.alloc")]
    [return: M68kRegister(M68kRegister.D0)]
    public static APTR AllocateEmbeddedCodeSegment(
        [M68kRegister(M68kRegister.D0)] uint size,
        [M68kRegister(M68kRegister.D1)] uint flags) =>
        Exec.AllocMem(size, (Exec.MemoryFlags)flags);

    [M68kExport("copperos.morphos.adddatatypes.load.free")]
    public static void FreeEmbeddedCodeSegment(
        [M68kRegister(M68kRegister.A1)] APTR memory,
        [M68kRegister(M68kRegister.D0)] uint size) =>
        Exec.FreeMem(memory, size);

    private static bool IsFileString(APTR fileHeader, uint headerBytes,
        uint offset)
    {
        if (offset < DataTypeHeaderSize || offset >= headerBytes)
            return false;
        for (uint index = offset; index < headerBytes; index++)
            if (APTR.ReadUInt8(fileHeader, (int)index) == 0)
                return true;
        return false;
    }

    private static bool BuildParsePattern(APTR compound, APTR pattern)
    {
        var length = CStringLength(CString.FromPointer(pattern.Raw));
        if (length == 0 || Utility.Stricmp(pattern.Raw,
                CString.ToUInt32(CString.FromLiteral("#?"))) == 0)
        {
            APTR.WriteUInt32(compound, CompoundDataTypeFlagLong,
                PatternUnusedFlag);
            return true;
        }

        if (length > (uint.MaxValue - 2) / 2)
            return false;
        var bytes = 2 * length + 2;
        var parsed = Exec.AllocVec(bytes,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (parsed.IsNull)
        {
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return false;
        }

        var result = DOS.ParsePatternNoCase(CString.FromPointer(pattern.Raw),
            parsed, unchecked((int)bytes));
        if (result < 0)
        {
            Exec.FreeVec(parsed);
            return false;
        }
        if (result == 0)
        {
            Exec.FreeVec(parsed);
            return true;
        }

        APTR.WriteUInt32(compound, CompoundDataTypeFlagLong,
            PatternWildFlag);
        APTR.WriteUInt32(compound, CompoundDataTypeParsePatternSize, bytes);
        APTR.WriteUInt32(compound, CompoundDataTypeParsePatternMemory,
            APTR.ToUInt32(parsed));
        return true;
    }

    private static void AddDatatype(APTR list, APTR compound,
        int typeListOffset)
    {
        var typeList = APTR.FromPointer(list.Raw +
            (uint)typeListOffset);
        var header = APTR.FromPointer(APTR.ReadUInt32(compound,
            CompoundDataTypeHeaderPointer));
        var name = APTR.ReadUInt32(header, 0);
        var existingNode = FindNameNoCase(typeList,
            CString.FromPointer(name));
        if (existingNode.IsNotNull)
        {
            var existing = existingNode;
            if (APTR.ReadUInt32(existing, CompoundDataTypeOpenCount) != 0)
            {
                DeleteDatatype(compound);
                return;
            }

            var oldHeader = APTR.FromPointer(APTR.ReadUInt32(existing,
                CompoundDataTypeHeaderPointer));
            if (SameDescriptor(existing, oldHeader, compound, header))
            {
                APTR.WriteUInt32(oldHeader, 16,
                    APTR.ReadUInt32(header, 16));
                APTR.WriteUInt32(oldHeader, 20,
                    APTR.ReadUInt32(header, 20));
                DeleteDatatype(compound);
                return;
            }
            DeleteDatatype(existing);
        }

        InsertByPriority(typeList, compound);
        var sorted = APTR.FromPointer(list.Raw +
            (uint)DataTypesListSorted);
        AlphaInsert(sorted, APTR.FromPointer(compound.Raw +
            (uint)CompoundDataTypeNode2));
        var maskLength = APTR.ReadUInt16(header, 24);
        var longest = APTR.ReadUInt16(list, DataTypesListLongestMask);
        if (maskLength > longest)
            APTR.WriteUInt16(list, DataTypesListLongestMask, maskLength);
    }

    private static bool SameDescriptor(APTR left, APTR leftHeader,
        APTR right, APTR rightHeader)
    {
        return Utility.Stricmp(APTR.ReadUInt32(leftHeader, 0),
                   APTR.ReadUInt32(rightHeader, 0)) == 0 &&
            Utility.Stricmp(APTR.ReadUInt32(leftHeader, 4),
                   APTR.ReadUInt32(rightHeader, 4)) == 0 &&
            Utility.Stricmp(APTR.ReadUInt32(leftHeader, 8),
                   APTR.ReadUInt32(rightHeader, 8)) == 0 &&
            APTR.ReadUInt16(leftHeader, 28) ==
                APTR.ReadUInt16(rightHeader, 28) &&
            APTR.ReadUInt16(leftHeader, 30) ==
                APTR.ReadUInt16(rightHeader, 30) &&
            APTR.ReadUInt16(leftHeader, 24) ==
                APTR.ReadUInt16(rightHeader, 24);
    }

    private static void InsertByPriority(APTR list, APTR compound)
    {
        var current = APTR.FromPointer(APTR.ReadUInt32(list, ListHead));
        var previous = APTR.Null;
        var candidateHeader = APTR.FromPointer(APTR.ReadUInt32(compound,
            CompoundDataTypeHeaderPointer));
        while (current.IsNotNull && APTR.ReadUInt32(current,
                   NodeSuccessor) != 0)
        {
            var currentCompound = current;
            var currentHeader = APTR.FromPointer(APTR.ReadUInt32(
                currentCompound, CompoundDataTypeHeaderPointer));
            if (ComparePriority(candidateHeader, currentHeader) > 0)
                break;
            previous = current;
            current = APTR.FromPointer(APTR.ReadUInt32(current,
                NodeSuccessor));
        }
        Exec.Insert(list, compound, previous);
    }

    private static int ComparePriority(APTR candidate, APTR current)
    {
        var candidateLength = APTR.ReadUInt16(candidate, 24);
        var currentLength = APTR.ReadUInt16(current, 24);
        var minLength = candidateLength < currentLength
            ? candidateLength : currentLength;
        var candidateMask = APTR.FromPointer(APTR.ReadUInt32(candidate, 12));
        var currentMask = APTR.FromPointer(APTR.ReadUInt32(current, 12));
        for (var index = 0; index < minLength; index++)
        {
            var difference = (int)APTR.ReadUInt16(currentMask, index * 2) -
                APTR.ReadUInt16(candidateMask, index * 2);
            if (difference != 0)
                return difference;
        }
        if (candidateLength != currentLength)
            return candidateLength > currentLength ? 1 : -1;

        var candidateUsesPattern =
            (APTR.ReadUInt32(APTR.FromPointer(candidate.Raw -
                (uint)CompoundDataTypeInlineHeader),
                CompoundDataTypeFlagLong) &
             PatternUnusedFlag) == 0;
        var currentUsesPattern =
            (APTR.ReadUInt32(APTR.FromPointer(current.Raw -
                (uint)CompoundDataTypeInlineHeader),
                CompoundDataTypeFlagLong) &
             PatternUnusedFlag) == 0;
        if (candidateUsesPattern != currentUsesPattern)
            return candidateUsesPattern ? 1 : -1;

        var candidatePriority = APTR.ReadUInt16(candidate, 30);
        var currentPriority = APTR.ReadUInt16(current, 30);
        return candidatePriority == currentPriority ? 0 :
            candidatePriority > currentPriority ? 1 : -1;
    }

    private static void DeleteDatatype(APTR compound)
    {
        if (compound.IsNull)
            return;
        var pattern = APTR.FromPointer(APTR.ReadUInt32(compound,
            CompoundDataTypeParsePatternMemory));
        if (pattern.IsNotNull)
            Exec.FreeVec(pattern);
        var code = APTR.FromPointer(APTR.ReadUInt32(compound,
            CompoundDataTypeCodeChunk));
        if (code.IsNotNull)
            Exec.FreeVec(code);
        var segment = BPTR.FromRaw(APTR.ReadUInt32(compound,
            CompoundDataTypeSegment));
        if (segment.Raw != 0)
            DOS.UnLoadSeg(segment);
        if (APTR.ReadUInt32(compound, CompoundDataTypeNode1) != 0 &&
            APTR.ReadUInt32(compound, CompoundDataTypeNode1 + 4) != 0)
        {
            Exec.Remove(compound);
            Exec.Remove(APTR.FromPointer(compound.Raw +
                (uint)CompoundDataTypeNode2));
            APTR.WriteUInt32(compound, CompoundDataTypeNode1, 0);
            APTR.WriteUInt32(compound, CompoundDataTypeNode1 + 4, 0);
            APTR.WriteUInt32(compound, CompoundDataTypeNode2, 0);
            APTR.WriteUInt32(compound, CompoundDataTypeNode2 + 4, 0);
        }
        Exec.FreeVec(compound);
    }

    private static int TypeListOffset(ushort flags) =>
        (flags & 3) switch
        {
            0 => DataTypesListBinary,
            1 => DataTypesListAscii,
            2 => DataTypesListIff,
            3 => DataTypesListMisc,
            _ => -1
        };

    /// <summary>
    /// Finds the library-owned shared datatype list, releases the named-object
    /// reference, then takes the list semaphore. Returns null if the named
    /// object or its user-space pointer is absent.
    /// </summary>
    public static APTR AcquireSharedList()
    {
        var namedObject = APTR.FromPointer(Utility.FindNamedObject(
            APTR.Null, CString.FromLiteral("DataTypesList"), APTR.Null));
        if (namedObject.IsNull)
            return APTR.Null;

        var list = APTR.FromPointer(APTR.ReadUInt32(namedObject,
            NamedObjectUserData));
        Utility.ReleaseNamedObject(APTR.ToUInt32(namedObject));
        if (list.IsNull)
            return APTR.Null;

        Exec.ObtainSemaphore(APTR.FromPointer(list.Raw +
            (uint)DataTypesListLock));
        return list;
    }

    /// <summary>Releases a list acquired by <see cref="AcquireSharedList"/>.</summary>
    public static void ReleaseSharedList(APTR list)
    {
        if (list.IsNotNull)
            Exec.ReleaseSemaphore(APTR.FromPointer(list.Raw +
                (uint)DataTypesListLock));
    }

    /// <summary>Returns the compound record containing a sorted-list Node2.</summary>
    public static APTR CompoundFromSortedNode(APTR node)
    {
        if (node.IsNull || node.Raw < (uint)CompoundDataTypeNode2)
            return APTR.Null;
        return APTR.FromPointer(node.Raw -
            (uint)CompoundDataTypeNode2);
    }

    /// <summary>
    /// Ensures the four built-in descriptors are present while the caller
    /// holds the shared list semaphore. Allocation failures preserve the
    /// source command's best-effort initialization behavior.
    /// </summary>
    public static void EnsureBuiltins(APTR list)
    {
        if (list.IsNull)
            return;

        EnsureBasicType(list, DataTypesListBinary,
            CString.FromLiteral("binary"), 0,
            MakeId((byte)'b', (byte)'i', (byte)'n', (byte)'a'),
            MakeId((byte)'s', (byte)'y', (byte)'s', (byte)'t'));
        EnsureBasicType(list, DataTypesListAscii,
            CString.FromLiteral("ascii"), 1,
            MakeId((byte)'a', (byte)'s', (byte)'c', (byte)'i'),
            MakeId((byte)'t', (byte)'e', (byte)'x', (byte)'t'));
        EnsureBasicType(list, DataTypesListIff,
            CString.FromLiteral("iff"), 2,
            MakeId((byte)'i', (byte)'f', (byte)'f', 0),
            MakeId((byte)'s', (byte)'y', (byte)'s', (byte)'t'));
        EnsureBasicType(list, DataTypesListMisc,
            CString.FromLiteral("directory"), 3,
            MakeId((byte)'d', (byte)'i', (byte)'r', (byte)'e'),
            MakeId((byte)'s', (byte)'y', (byte)'s', (byte)'t'));
    }

    /// <summary>
    /// Prints the sorted list using the MorphOS source's base/name format.
    /// The caller owns the list semaphore for the duration of this walk.
    /// Ctrl-C flushes output and reports ERROR_BREAK but does not change the
    /// command's RETURN_OK result, matching the source's LIST branch.
    /// </summary>
    public static void ListTypes(APTR list)
    {
        if (list.IsNull)
            return;

        var sorted = APTR.FromPointer(list.Raw +
            (uint)DataTypesListSorted);
        var node = APTR.FromPointer(APTR.ReadUInt32(sorted, ListHead));
        while (node.IsNotNull &&
            APTR.ReadUInt32(node, NodeSuccessor) != 0)
        {
            if (DOS.CheckSignal(unchecked((int)ControlC)) != 0)
            {
                DOS.Flush(DOS.Output());
                DOS.PrintFault(DOS.Error.Break, CString.FromPointer(0));
                break;
            }

            var compound = CompoundFromSortedNode(node);
            var header = APTR.FromPointer(APTR.ReadUInt32(compound,
                CompoundDataTypeHeaderPointer));
            var cells = default(ListCells);
            cells.BaseName = APTR.ReadUInt32(header, 4);
            cells.Name = APTR.ReadUInt32(header, 0);
            DOS.VPrintf("%s, \"%s\"\n", ListCells.AddressOf(ref cells));
            node = APTR.FromPointer(APTR.ReadUInt32(node, NodeSuccessor));
        }
    }

    private static void EnsureBasicType(APTR dataTypesList, int typeListOffset,
        CString name, ushort flags, uint id, uint groupId)
    {
        var typeList = APTR.FromPointer(dataTypesList.Raw +
            (uint)typeListOffset);
        if (FindNameNoCase(typeList, name).IsNotNull)
            return;

        var nameLength = CStringLength(name);
        var allocationBytes = CompoundDataTypeSize + nameLength + 1;
        var compound = Exec.AllocVec(allocationBytes,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (compound.IsNull)
            return;

        var header = APTR.FromPointer(compound.Raw +
            (uint)CompoundDataTypeInlineHeader);
        var storedName = APTR.FromPointer(compound.Raw +
            CompoundDataTypeSize);
        CopyCString(storedName, name);

        APTR.WriteUInt32(compound, CompoundDataTypeHeaderPointer,
            APTR.ToUInt32(header));
        APTR.WriteUInt32(compound, CompoundDataTypeLength,
            allocationBytes);
        APTR.WriteUInt32(header, 0, APTR.ToUInt32(storedName));
        APTR.WriteUInt32(header, 4, APTR.ToUInt32(storedName));
        APTR.WriteUInt32(header, 16, groupId);
        APTR.WriteUInt32(header, 20, id);
        APTR.WriteUInt16(header, 28, flags);
        APTR.WriteUInt32(compound, CompoundDataTypeNode1 + NodeName,
            APTR.ToUInt32(storedName));
        APTR.WriteUInt32(compound, CompoundDataTypeNode2 + NodeName,
            APTR.ToUInt32(storedName));

        InitializeList(APTR.FromPointer(compound.Raw +
            (uint)CompoundDataTypeToolList));
        Exec.AddTail(typeList, compound);
        AlphaInsert(APTR.FromPointer(dataTypesList.Raw +
            (uint)DataTypesListSorted), APTR.FromPointer(compound.Raw +
                (uint)CompoundDataTypeNode2));
    }

    private static APTR FindNameNoCase(APTR list, CString name)
    {
        var node = APTR.FromPointer(APTR.ReadUInt32(list, ListHead));
        while (node.IsNotNull && APTR.ReadUInt32(node, NodeSuccessor) != 0)
        {
            var nodeName = APTR.ReadUInt32(node, NodeName);
            if (nodeName != 0 && Utility.Stricmp(nodeName,
                    CString.ToUInt32(name)) == 0)
                return node;
            node = APTR.FromPointer(APTR.ReadUInt32(node, NodeSuccessor));
        }
        return APTR.Null;
    }

    private static void AlphaInsert(APTR list, APTR node)
    {
        var current = APTR.FromPointer(APTR.ReadUInt32(list, ListHead));
        var previous = APTR.Null;
        var nodeName = APTR.ReadUInt32(node, NodeName);
        while (current.IsNotNull &&
            APTR.ReadUInt32(current, NodeSuccessor) != 0)
        {
            var currentName = APTR.ReadUInt32(current, NodeName);
            if (Utility.Stricmp(currentName, nodeName) > 0)
                break;
            previous = current;
            current = APTR.FromPointer(APTR.ReadUInt32(current,
                NodeSuccessor));
        }
        Exec.Insert(list, node, previous);
    }

    public static void InitializeList(APTR list)
    {
        APTR.WriteUInt32(list, 0, list.Raw + 4);
        APTR.WriteUInt32(list, 4, 0);
        APTR.WriteUInt32(list, 8, list.Raw);
        APTR.WriteUInt8(list, 12, (byte)NodeType.Unknown);
        APTR.WriteUInt8(list, 13, 0);
    }

    private static uint CStringLength(CString text)
    {
        var pointer = APTR.FromPointer(CString.ToUInt32(text));
        uint length = 0;
        while (APTR.ReadUInt8(pointer, (int)length) != 0)
            length++;
        return length;
    }

    private static void CopyCString(APTR destination, CString text)
    {
        var source = APTR.FromPointer(CString.ToUInt32(text));
        for (var i = 0; ; i++)
        {
            var value = APTR.ReadUInt8(source, i);
            APTR.WriteUInt8(destination, i, value);
            if (value == 0)
                return;
        }
    }

    private static uint MakeId(byte a, byte b, byte c, byte d) =>
        ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
}
