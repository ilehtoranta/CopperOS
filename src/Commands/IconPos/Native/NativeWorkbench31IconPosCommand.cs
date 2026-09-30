using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 IconPos implementation.  The original command edits
/// a Workbench DiskObject in place and uses icon.library for acquisition and
/// storage.  This body keeps the parser and every icon lease invocation-local;
/// the resident entry owns only DOS startup and the Workbench reply.
/// </summary>
public static class NativeWorkbench31IconPosCommand
{
    public const string Template =
        "FILE/A,XPOS/N,YPOS/N,TYPE/K,DXPOS/N,DYPOS/N,DWIDTH/N,DHEIGHT/N," +
        "CREATE/S,FREEX/S,FREEY/S,IMAGE/K";
    public const uint ResultCount = 12;

    private const int TypeOffset = 0x30;
    private const int CurrentXOffset = 0x3a;
    private const int CurrentYOffset = 0x3e;
    private const int DrawerDataOffset = 0x42;
    private const int GadgetWidthOffset = 0x0c;
    private const int GadgetHeightOffset = 0x0e;
    private const int GadgetRenderOffset = 0x16;
    private const int SelectRenderOffset = 0x1a;
    private const uint NoIconPosition = 0x8000_0000u;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            DOS.SetIoErr((DOS.Error)ioError);
            return arguments.ReturnLevel;
        }

        APTR iconLibrary = APTR.Null;
        uint diskObject = 0;
        uint imageObject = 0;
        var result = DOS.RETURN_ERROR;
        var error = 0;

        do
        {
            iconLibrary = Exec.OpenLibraryRaw(Icon.Name, 0);
            if (iconLibrary.IsNull)
            {
                error = (int)DOS.IoErr();
                if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                break;
            }
            Icon.IconLibraryBase = iconLibrary;

            if (!arguments.TryGetResult(0, out var file) || file == 0)
            {
                error = (int)DOS.Error.RequiredArgumentMissing;
                break;
            }

            arguments.TryGetResult(1, out var xPosition);
            arguments.TryGetResult(2, out var yPosition);
            arguments.TryGetResult(3, out var typeName);
            arguments.TryGetResult(4, out var drawerX);
            arguments.TryGetResult(5, out var drawerY);
            arguments.TryGetResult(6, out var drawerWidth);
            arguments.TryGetResult(7, out var drawerHeight);
            arguments.TryGetResult(8, out var create);
            arguments.TryGetResult(9, out var freeX);
            arguments.TryGetResult(10, out var freeY);
            arguments.TryGetResult(11, out var image);

            var iconType = MapType(APTR.FromPointer(typeName));
            if (typeName != 0 && iconType == 0)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            diskObject = Icon.GetDiskObject(CString.FromPointer(file));
            if (image != 0)
            {
                imageObject = Icon.GetDiskObject(
                    CString.FromPointer(image));
                if (imageObject == 0)
                {
                    error = (int)DOS.IoErr();
                    if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                    break;
                }
            }

            if (diskObject == 0)
            {
                if (create == 0)
                {
                    error = (int)DOS.IoErr();
                    if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                    break;
                }
                if (iconType == 0)
                {
                    error = (int)DOS.Error.RequiredArgumentMissing;
                    break;
                }
                diskObject = Icon.GetDefDiskObject(iconType);
                if (diskObject == 0)
                {
                    error = (int)DOS.IoErr();
                    if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                    break;
                }
            }

            var icon = APTR.FromPointer(diskObject);
            if (iconType != 0)
                APTR.WriteUInt8(icon, TypeOffset, unchecked((byte)iconType));
            if (xPosition != 0)
                APTR.WriteUInt32(icon, CurrentXOffset,
                    APTR.ReadUInt32(APTR.FromPointer(xPosition), 0));
            if (yPosition != 0)
                APTR.WriteUInt32(icon, CurrentYOffset,
                    APTR.ReadUInt32(APTR.FromPointer(yPosition), 0));
            if (freeX != 0)
                APTR.WriteUInt32(icon, CurrentXOffset, NoIconPosition);
            if (freeY != 0)
                APTR.WriteUInt32(icon, CurrentYOffset, NoIconPosition);

            var drawerData = APTR.FromPointer(APTR.ReadUInt32(icon,
                DrawerDataOffset));
            if (drawerData.IsNotNull)
            {
                if (drawerX != 0)
                    APTR.WriteUInt16(drawerData, 0,
                        APTR.ReadUInt16(APTR.FromPointer(drawerX), 0));
                if (drawerY != 0)
                    APTR.WriteUInt16(drawerData, 2,
                        APTR.ReadUInt16(APTR.FromPointer(drawerY), 0));
                if (drawerWidth != 0)
                    APTR.WriteUInt16(drawerData, 4,
                        APTR.ReadUInt16(APTR.FromPointer(drawerWidth), 0));
                if (drawerHeight != 0)
                    APTR.WriteUInt16(drawerData, 6,
                        APTR.ReadUInt16(APTR.FromPointer(drawerHeight), 0));
            }
            else if (drawerX != 0 || drawerY != 0 || drawerWidth != 0 ||
                drawerHeight != 0)
            {
                error = (int)DOS.Error.ObjectWrongType;
                break;
            }

            if (imageObject != 0)
            {
                var source = APTR.FromPointer(imageObject);
                APTR.WriteUInt32(icon, GadgetRenderOffset,
                    APTR.ReadUInt32(source, GadgetRenderOffset));
                APTR.WriteUInt32(icon, SelectRenderOffset,
                    APTR.ReadUInt32(source, SelectRenderOffset));
                APTR.WriteUInt16(icon, GadgetWidthOffset,
                    APTR.ReadUInt16(source, GadgetWidthOffset));
                APTR.WriteUInt16(icon, GadgetHeightOffset,
                    APTR.ReadUInt16(source, GadgetHeightOffset));
            }

            if (Icon.PutDiskObject(CString.FromPointer(file), diskObject) == 0)
            {
                error = (int)DOS.IoErr();
                if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                break;
            }
            result = DOS.RETURN_OK;
        }
        while (false);

        if (imageObject != 0)
            Icon.FreeDiskObject(imageObject);
        if (diskObject != 0)
            Icon.FreeDiskObject(diskObject);
        Icon.IconLibraryBase = APTR.Null;
        if (iconLibrary.IsNotNull)
            Exec.CloseLibrary(iconLibrary);

        arguments.Release();
        ioError = error;
        if (error != 0)
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
        DOS.SetIoErr((DOS.Error)error);
        return result;
    }

    private static int MapType(APTR value)
    {
        if (value.IsNull) return 0;
        if (Match(value, 4, (byte)'d', (byte)'i', (byte)'s', (byte)'k')) return 1;
        if (Match(value, 6, (byte)'d', (byte)'r', (byte)'a', (byte)'w',
                (byte)'e', (byte)'r')) return 2;
        if (Match(value, 4, (byte)'t', (byte)'o', (byte)'o', (byte)'l')) return 3;
        if (Match(value, 7, (byte)'p', (byte)'r', (byte)'o', (byte)'j',
                (byte)'e', (byte)'c', (byte)'t')) return 4;
        if (Match(value, 7, (byte)'g', (byte)'a', (byte)'r', (byte)'b',
                (byte)'a', (byte)'g', (byte)'e')) return 5;
        return 0;
    }

    private static bool Match(APTR value, uint length, byte first, byte second,
        byte third, byte fourth, byte fifth = 0, byte sixth = 0,
        byte seventh = 0)
    {
        if (CStringLength(value) != length) return false;
        for (var index = 0u; index < length; index++)
        {
            var left = APTR.ReadUInt8(value, unchecked((int)index));
            var right = index switch
            {
                0 => first,
                1 => second,
                2 => third,
                3 => fourth,
                4 => fifth,
                5 => sixth,
                _ => seventh
            };
            if (left >= (byte)'a' && left <= (byte)'z') left -= 32;
            if (right >= (byte)'a' && right <= (byte)'z') right -= 32;
            if (left != right) return false;
        }
        return true;
    }

    private static uint CStringLength(APTR value)
    {
        var length = 0u;
        while (APTR.ReadUInt8(value, unchecked((int)length)) != 0)
            length++;
        return length;
    }
}
