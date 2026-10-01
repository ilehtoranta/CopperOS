using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 RequestFile body for the captured thirteen-slot command
/// boundary.  This is a bounded resident candidate: it uses only the public
/// DOS/Exec/ASL calls available to the Workbench profile and deliberately does
/// not include the MorphOS-only INITIALVOLUMES option.
/// </summary>
public static class NativeWorkbench31RequestFileCommand
{
    public const string Template =
        "DRAWER,FILE/K,PATTERN/K,TITLE/K,POSITIVE/K,NEGATIVE/K," +
        "ACCEPTPATTERN/K,REJECTPATTERN/K,SAVEMODE/S,MULTISELECT/S," +
        "DRAWERSONLY/S,NOICONS/S,PUBSCREEN/K";
    public const uint ResultCount = 13;

    private const uint BufferBytes = 512;
    private const uint FileOffset = 4;
    private const uint DrawerOffset = 8;
    private const uint NumberOfArgumentsOffset = 28;
    private const uint ArgumentListOffset = 32;

    private const uint AslTb = 0x8008_0000u;
    private const uint TagDone = 0;
    private const uint TagIgnore = 1;
    private const uint AslFrTitleText = AslTb + 1;
    private const uint AslFrInitialFile = AslTb + 8;
    private const uint AslFrInitialDrawer = AslTb + 9;
    private const uint AslFrInitialPattern = AslTb + 10;
    private const uint AslFrPositiveText = AslTb + 18;
    private const uint AslFrNegativeText = AslTb + 19;
    private const uint AslFrDoSaveMode = AslTb + 44;
    private const uint AslFrDoMultiSelect = AslTb + 45;
    private const uint AslFrDoPatterns = AslTb + 46;
    private const uint AslFrDrawersOnly = AslTb + 47;
    private const uint AslFrPubScreenName = AslTb + 41;
    private const uint AslFrRejectIcons = AslTb + 60;
    private const uint AslFrRejectPattern = AslTb + 61;
    private const uint AslFrAcceptPattern = AslTb + 62;

    private struct FileTags
    {
        public TagItem InitialDrawer;
        public TagItem InitialFile;
        public TagItem InitialPattern;
        public TagItem TitleText;
        public TagItem PositiveText;
        public TagItem NegativeText;
        public TagItem AcceptPattern;
        public TagItem RejectPattern;
        public TagItem DoSaveMode;
        public TagItem DoMultiSelect;
        public TagItem DrawersOnly;
        public TagItem RejectIcons;
        public TagItem PubScreenName;
        public TagItem DoPatterns;
        public TagItem Done;

        public static APTR AddressOf(ref FileTags tags) =>
            throw new System.NotSupportedException(
                "Workbench31RequestFile.FileTags.AddressOf is lowered by CopperSharp.");
    }

    private struct PrintFields
    {
        public uint Value;

        public static APTR AddressOf(ref PrintFields fields) =>
            throw new System.NotSupportedException(
                "Workbench31RequestFile.PrintFields.AddressOf is lowered by CopperSharp.");
    }

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.SetIoErr((DOS.Error)ioError);
            DOS.PrintFault((DOS.Error)ioError, "RequestFile");
            return arguments.ReturnLevel;
        }

        var buffer = Exec.AllocVec(BufferBytes,
            (uint)(Exec.MemoryFlags.Any | Exec.MemoryFlags.Clear));
        if (buffer.IsNull)
        {
            ioError = (int)DOS.IoErr();
            if (ioError == 0) ioError = (int)DOS.Error.NoFreeStore;
            arguments.Release();
            DOS.SetIoErr((DOS.Error)ioError);
            DOS.PrintFault((DOS.Error)ioError, "RequestFile");
            return DOS.RETURN_FAIL;
        }

        var result = RunParsed(buffer, ref arguments, out ioError);
        arguments.Release();
        Exec.FreeVec(buffer);
        return result;
    }

    private static int RunParsed(APTR buffer,
        ref NativeCommandArguments arguments,
        out int ioError)
    {
        ioError = 0;
        var multiSelect = ReadResult(ref arguments, 9);
        var requester = BuildRequester(ref arguments);
        var result = DOS.RETURN_OK;
        do
        {
            if (requester.IsNull)
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError, "RequestFile");
                result = DOS.RETURN_ERROR;
                break;
            }
            if (ASL.AslRequest(requester, APTR.Null) != 0)
            {
                var count = unchecked((int)APTR.ReadUInt32(requester,
                    (int)NumberOfArgumentsOffset));
                if (multiSelect == 0 || count == 0)
                {
                    var drawerPointer = APTR.FromPointer(APTR.ReadUInt32(
                        requester, (int)DrawerOffset));
                    var filePointer = APTR.FromPointer(APTR.ReadUInt32(
                        requester, (int)FileOffset));
                    if (CopyString(drawerPointer, buffer) &&
                        DOS.AddPart(CString.FromPointer(buffer.Raw),
                            CString.FromPointer(filePointer.Raw), BufferBytes) != 0)
                        PrintPath(buffer);
                }
                else
                {
                    var drawerPointer = APTR.FromPointer(APTR.ReadUInt32(
                        requester, (int)DrawerOffset));
                    var args = APTR.FromPointer(APTR.ReadUInt32(requester,
                        (int)ArgumentListOffset));
                    for (var index = 0; index < count; index++)
                    {
                        var item = APTR.FromPointer(args.Raw +
                            unchecked((uint)index * WBArg.Size));
                        var name = APTR.FromPointer(APTR.ReadUInt32(item, 4));
                        if (CopyString(drawerPointer, buffer) &&
                            DOS.AddPart(CString.FromPointer(buffer.Raw),
                                CString.FromPointer(name.Raw), BufferBytes) != 0)
                            PrintPathWithSpace(buffer);
                    }
                    DOS.PutStr("\n");
                }
            }
            else
            {
                ioError = (int)DOS.IoErr();
                if (ioError == 0)
                {
                    result = DOS.RETURN_WARN;
                    ioError = (int)DOS.Error.Break;
                    DOS.SetIoErr((DOS.Error)ioError);
                }
                else
                {
                    DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
                    result = DOS.RETURN_FAIL;
                }
            }
        }
        while (false);

        if (requester.IsNotNull) ASL.FreeAslRequest(requester);
        if (ioError == 0) DOS.SetIoErr((DOS.Error)0);
        return result;
    }

    private static APTR BuildRequester(ref NativeCommandArguments arguments)
    {
        var drawer = ReadResult(ref arguments, 0);
        var file = ReadResult(ref arguments, 1);
        var pattern = ReadResult(ref arguments, 2);
        var title = ReadResult(ref arguments, 3);
        var positive = ReadResult(ref arguments, 4);
        var negative = ReadResult(ref arguments, 5);
        var acceptPattern = ReadResult(ref arguments, 6);
        var rejectPattern = ReadResult(ref arguments, 7);
        var saveMode = ReadResult(ref arguments, 8);
        var multiSelect = ReadResult(ref arguments, 9);
        var drawersOnly = ReadResult(ref arguments, 10);
        var noIcons = ReadResult(ref arguments, 11);
        var publicScreen = ReadResult(ref arguments, 12);
        var tags = default(FileTags);
        tags.InitialDrawer = new TagItem { Tag = AslFrInitialDrawer, Data = drawer };
        tags.InitialFile = new TagItem { Tag = AslFrInitialFile, Data = file };
        tags.InitialPattern = new TagItem { Tag = AslFrInitialPattern, Data = pattern };
        tags.TitleText = new TagItem { Tag = AslFrTitleText, Data = title };
        tags.PositiveText = new TagItem { Tag = AslFrPositiveText, Data = positive };
        tags.NegativeText = new TagItem { Tag = AslFrNegativeText, Data = negative };
        tags.AcceptPattern = new TagItem { Tag = AslFrAcceptPattern, Data = acceptPattern };
        tags.RejectPattern = new TagItem { Tag = AslFrRejectPattern, Data = rejectPattern };
        tags.DoSaveMode = new TagItem { Tag = AslFrDoSaveMode, Data = saveMode != 0 ? 1u : 0u };
        tags.DoMultiSelect = new TagItem { Tag = AslFrDoMultiSelect, Data = multiSelect != 0 ? 1u : 0u };
        tags.DrawersOnly = new TagItem { Tag = AslFrDrawersOnly, Data = drawersOnly != 0 ? 1u : 0u };
        tags.RejectIcons = new TagItem { Tag = AslFrRejectIcons, Data = noIcons != 0 ? 1u : 0u };
        tags.PubScreenName = new TagItem { Tag = AslFrPubScreenName, Data = publicScreen };
        tags.DoPatterns = new TagItem { Tag = AslFrDoPatterns, Data = pattern != 0 ? 1u : 0u };
        tags.Done = new TagItem { Tag = TagDone, Data = 0 };
        return ASL.AllocAslRequest(0, FileTags.AddressOf(ref tags));
    }

    private static uint ReadResult(ref NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) ? value : 0;

    private static bool CopyString(APTR source, APTR destination)
    {
        if (source.IsNull) return false;
        for (var index = 0u; index + 1 < BufferBytes; index++)
        {
            var value = APTR.ReadUInt8(source, (int)index);
            APTR.WriteUInt8(destination, (int)index, value);
            if (value == 0) return true;
        }
        APTR.WriteUInt8(destination, (int)(BufferBytes - 1), 0);
        return false;
    }

    private static void PrintPath(APTR buffer)
    {
        var fields = new PrintFields { Value = buffer.Raw };
        DOS.VPrintf("\"%s\"\n", PrintFields.AddressOf(ref fields));
    }

    private static void PrintPathWithSpace(APTR buffer)
    {
        var fields = new PrintFields { Value = buffer.Raw };
        DOS.VPrintf("\"%s\" ", PrintFields.AddressOf(ref fields));
    }
}
