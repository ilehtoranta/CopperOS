using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 SetFont candidate.  The ACTION_DISK_INFO/InfoData window
/// lookup follows the original v39.1 packet boundary; guest execution must
/// still confirm its target-handler behavior.
/// </summary>
public static class NativeWorkbench31SetFontCommand
{
    public const string Template =
        "NAME/A,SIZE/N/A,SCALE/S,PROP/S,ITALIC/S,BOLD/S,UNDERLINE/S";

    private const uint NameBytes = 80;
    private const uint TextAttrBytes = 12;
    private const uint InfoDataBytes = InfoData.Size;
    private const uint TextFontStyleOffset = 22;
    private const uint TextFontFlagsOffset = 23;
    private const uint ConsoleWindowRastPortOffset = 0x32;
    private const uint ConsoleWindowFontOffset = 0x80;
    private const int ConsoleDiskInfoAction = 25;
    private const byte StyleUnderline = 1;
    private const byte StyleBold = 2;
    private const byte StyleItalic = 4;
    private const byte FlagsDesigned = 0x40;
    private const byte FlagsProportional = 0x20;

    public static int Run(out int ioError)
    {
        ioError = 0;
        NativeCommandArguments arguments = default;
        APTR graphicsLibrary = APTR.Null;
        APTR diskfontLibrary = APTR.Null;
        APTR utilityLibrary = APTR.Null;
        APTR name = APTR.Null;
        APTR textAttr = APTR.Null;
        APTR infoData = APTR.Null;
        APTR font = APTR.Null;
        var result = DOS.RETURN_FAIL;
        var error = 122;

        graphicsLibrary = Exec.OpenLibraryRaw(Graphics.Name, 37);
        diskfontLibrary = Exec.OpenLibraryRaw(Diskfont.Name, 37);
        utilityLibrary = Exec.OpenLibraryRaw(Utility.Name, 37);
        if (graphicsLibrary.IsNull || diskfontLibrary.IsNull ||
            utilityLibrary.IsNull)
            goto Cleanup;

        Graphics.GraphicsLibraryBase = graphicsLibrary;
        Diskfont.DiskfontLibraryBase = diskfontLibrary;
        Utility.UtilityLibraryBase = utilityLibrary;

        if (!NativeCommandArguments.TryRead(Template, 7,
                out arguments))
        {
            result = DOS.RETURN_FAIL;
            error = arguments.IoError;
            goto Cleanup;
        }

        if (!arguments.TryGetResult(0, out var nameRaw) || nameRaw == 0 ||
            !arguments.TryGetResult(1, out var sizeRaw) || sizeRaw == 0)
        {
            error = (int)DOS.Error.BadTemplate;
            goto Cleanup;
        }

        var size = APTR.ReadUInt32(APTR.FromPointer(sizeRaw), 0);
        // The Workbench 3.1 HUNK copies SIZE into TextAttr.ta_YSize with a
        // word store before comparing it with four using CMP.W/BLS. Preserve
        // that low-word behavior for values outside the ordinary range.
        var textSize = size & 0xFFFFu;
        if (textSize <= 4u)
        {
            error = (int)DOS.Error.BadNumber;
            goto Cleanup;
        }

        name = Exec.AllocMem(NameBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        textAttr = Exec.AllocMem(TextAttrBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        infoData = Exec.AllocMem(InfoDataBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (name.IsNull || textAttr.IsNull || infoData.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto Cleanup;
        }

        if (!CopyFontName(name, APTR.FromPointer(nameRaw)))
        {
            error = (int)DOS.Error.LineTooLong;
            goto Cleanup;
        }

        APTR.WriteUInt32(textAttr, 0, name.Raw);
        APTR.WriteUInt16(textAttr, 4, (ushort)textSize);
        APTR.WriteUInt8(textAttr, 6, ComposeStyle(ref arguments));
        var flags = (byte)(0x03 | FlagsDesigned);
        if (HasSwitch(ref arguments, 3)) flags |= FlagsProportional;
        if (HasSwitch(ref arguments, 2)) flags &= unchecked((byte)~FlagsDesigned);
        APTR.WriteUInt8(textAttr, 7, flags);

        font = APTR.FromPointer(Diskfont.OpenDiskFont(textAttr.Raw));
        if (font.IsNull)
        {
            error = (int)DOS.Error.ObjectNotFound;
            goto Cleanup;
        }
        if (!HasSwitch(ref arguments, 3) &&
            (APTR.ReadUInt8(font, (int)TextFontFlagsOffset) &
                FlagsProportional) != 0)
        {
            Graphics.CloseFont(font.Raw);
            font = APTR.Null;
            error = (int)DOS.Error.ObjectWrongType;
            goto Cleanup;
        }

        var process = Exec.FindTask(CString.FromPointer(0));
        if (process.IsNull)
        {
            error = (int)DOS.Error.ObjectWrongType;
            goto Cleanup;
        }
        var consoleTask = APTR.FromPointer(APTR.ReadUInt32(process,
            DosLayout.Process.ConsoleTask));
        if (consoleTask.IsNull)
        {
            error = (int)DOS.Error.ObjectNotFound;
            goto Cleanup;
        }

        // ACTION_DISK_INFO (25) fills InfoData.  The original reads the
        // console Window pointer from id_VolumeNode, then uses Window.RastPort
        // and Window.Font.  Its code treats the returned field as an APTR.
        var infoDataBptr = unchecked((int)(infoData.Raw >> 2));
        _ = DOS.DoPkt(consoleTask, ConsoleDiskInfoAction, infoDataBptr,
            0, 0, 0, 0);
        var consoleWindow = APTR.FromPointer(APTR.ReadUInt32(infoData,
            DosLayout.InfoData.VolumeNode));
        if (consoleWindow.IsNull)
        {
            // The original command succeeds without applying a font when the
            // console handler supplies no intuition window (for example a
            // serial console).
            result = DOS.RETURN_OK;
            error = 0;
            goto Cleanup;
        }

        var rastPort = APTR.FromPointer(APTR.ReadUInt32(consoleWindow,
            (int)ConsoleWindowRastPortOffset));
        if (rastPort.IsNull)
        {
            error = (int)DOS.Error.ObjectWrongType;
            goto Cleanup;
        }

        // Keep the console's RastPort and current-font update atomic with
        // respect to other tasks, matching the Workbench 3.1 command's
        // Forbid/Permit boundary.
        Exec.Forbid();
        Graphics.SetFont(rastPort.Raw, font.Raw);
        var oldFont = APTR.FromPointer(APTR.ReadUInt32(consoleWindow,
            (int)ConsoleWindowFontOffset));
        if (oldFont.IsNotNull)
            Graphics.CloseFont(oldFont.Raw);
        APTR.WriteUInt32(consoleWindow, (int)ConsoleWindowFontOffset,
            font.Raw);
        Exec.Permit();

        _ = DOS.PutStr(CString.FromLiteral("\u001bc"));
        FlushOutput();
        var openedFontStyle = APTR.ReadUInt8(font,
            (int)TextFontStyleOffset);
        if (HasSwitch(ref arguments, 4) &&
            (openedFontStyle & StyleItalic) == 0)
            _ = DOS.PutStr(CString.FromLiteral("\u001b[3m"));
        if (HasSwitch(ref arguments, 5) &&
            (openedFontStyle & StyleBold) == 0)
            _ = DOS.PutStr(CString.FromLiteral("\u001b[1m"));
        if (HasSwitch(ref arguments, 6) &&
            (openedFontStyle & StyleUnderline) == 0)
            _ = DOS.PutStr(CString.FromLiteral("\u001b[4m"));
        FlushOutput();

        result = DOS.RETURN_OK;
        error = 0;

    Cleanup:
        arguments.Release();
        if (infoData.IsNotNull)
            Exec.FreeMem(infoData, InfoDataBytes);
        if (textAttr.IsNotNull)
            Exec.FreeMem(textAttr, TextAttrBytes);
        if (name.IsNotNull)
            Exec.FreeMem(name, NameBytes);
        // The original HUNK's successful no-window path retains the opened
        // font without calling CloseFont. Preserve that branch. On failure,
        // release an untransferred font to avoid leaking it.
        if (font.IsNotNull && result != DOS.RETURN_OK)
            Graphics.CloseFont(font.Raw);
        if (font.IsNull && error != 0)
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
        if (utilityLibrary.IsNotNull)
        {
            Exec.CloseLibrary(utilityLibrary);
            Utility.UtilityLibraryBase = APTR.Null;
        }
        if (diskfontLibrary.IsNotNull)
        {
            Exec.CloseLibrary(diskfontLibrary);
            Diskfont.DiskfontLibraryBase = APTR.Null;
        }
        if (graphicsLibrary.IsNotNull)
        {
            Exec.CloseLibrary(graphicsLibrary);
            Graphics.GraphicsLibraryBase = APTR.Null;
        }
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static byte ComposeStyle(ref NativeCommandArguments arguments)
    {
        var style = (byte)0;
        if (HasSwitch(ref arguments, 4)) style |= StyleItalic;
        if (HasSwitch(ref arguments, 5)) style |= StyleBold;
        if (HasSwitch(ref arguments, 6)) style |= StyleUnderline;
        return style;
    }

    private static bool HasSwitch(ref NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) &&
        value != 0;

    private static bool CopyFontName(APTR destination, APTR source)
    {
        uint length = 0;
        while (length + 1 < NameBytes)
        {
            var value = APTR.ReadUInt8(source, (int)length);
            APTR.WriteUInt8(destination, (int)length, value);
            if (value == 0) break;
            length++;
        }
        if (length == 0 || length + 1 >= NameBytes)
            return false;

        var suffix = length >= 5 &&
            Fold(APTR.ReadUInt8(destination, (int)length - 5)) == (byte)'.' &&
            Fold(APTR.ReadUInt8(destination, (int)length - 4)) == (byte)'f' &&
            Fold(APTR.ReadUInt8(destination, (int)length - 3)) == (byte)'o' &&
            Fold(APTR.ReadUInt8(destination, (int)length - 2)) == (byte)'n' &&
            Fold(APTR.ReadUInt8(destination, (int)length - 1)) == (byte)'t';
        if (suffix) return true;

        if (length + 5 >= NameBytes) return false;
        APTR.WriteUInt8(destination, (int)length, (byte)'.');
        APTR.WriteUInt8(destination, (int)length + 1, (byte)'f');
        APTR.WriteUInt8(destination, (int)length + 2, (byte)'o');
        APTR.WriteUInt8(destination, (int)length + 3, (byte)'n');
        APTR.WriteUInt8(destination, (int)length + 4, (byte)'t');
        APTR.WriteUInt8(destination, (int)length + 5, 0);
        return true;
    }

    private static byte Fold(byte value) => value is >= (byte)'A' and <= (byte)'Z'
        ? (byte)(value + 32) : value;

    private static void FlushOutput()
    {
        var output = DOS.Output();
        if (output.IsNotNull)
            _ = DOS.Flush(output);
    }
}
