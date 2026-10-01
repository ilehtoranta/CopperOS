using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 BindDrivers profile. Its original HUNK scans SYS:Expansion
/// with Lock/Examine/CurrentDir/ExNext and has a stricter PRODUCT parser than
/// the MorphOS 3.20 profile.
/// </summary>
public static class NativeWorkbench31BindDriversCommand
{
    private const uint DosVersion = 37;
    private const uint ExpansionVersion = 37;
    private const uint IconVersion = 37;
    private const int DiskObjectToolTypesOffset = 0x38;
    private const int ConfigDevNextOffset = 48;
    private const ushort ResidentMatchWord = 0x4afc;

    public struct BindingCells
    {
        public uint ConfigDev;
        public uint FileName;
        public uint ProductString;
        public uint ToolTypes;

        public static APTR AddressOf(ref BindingCells cells) =>
            throw new System.NotSupportedException(
                "BindingCells.AddressOf is lowered by CopperSharp.");
    }

    public struct NumberCell
    {
        public uint Value;

        public static APTR AddressOf(ref NumberCell cell) =>
            throw new System.NotSupportedException(
                "NumberCell.AddressOf is lowered by CopperSharp.");
    }

    public struct LibraryLeases
    {
        public APTR PreviousDos;
        public APTR Dos;
        public APTR Expansion;
        public APTR Icon;
    }

    /// <summary>
    /// Runs the no-argument command body. CLI text is intentionally not
    /// parsed: the Workbench HUNK has no ReadArgs call or command options.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!TryOpenLibraries(out var libraries))
        {
            if (libraries.Dos.IsNotNull)
            {
                ioError = (int)DOS.IoErr();
                CloseLibraries(ref libraries, ioError);
            }
            else if (libraries.PreviousDos.IsNotNull)
            {
                ioError = (int)DOS.IoErr();
            }
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_WARN;
        var directory = DOS.LockRaw(CString.FromLiteral("SYS:Expansion"),
            DOS.LockMode.Read);
        var previousDirectory = BPTR.Null;
        var changedDirectory = false;
        var obtainedBinding = false;
        var directoryInfo = default(FileInfoBlock);
        var binding = default(BindingCells);
        var number = default(NumberCell);

        if (directory.IsNotNull &&
            DOS.Examine(directory, FileInfoBlock.AddressOf(ref directoryInfo)) != 0 &&
            FileInfoBlock.GetDirEntryType(
                FileInfoBlock.AddressOf(ref directoryInfo).Raw) > 0)
        {
            Expansion.ObtainConfigBinding();
            obtainedBinding = true;
            result = DOS.RETURN_OK;

            // The reference ignores a null previous lock and still scans.
            previousDirectory = DOS.CurrentDirRaw(directory);
            changedDirectory = true;
            while (DOS.ExNext(directory,
                       FileInfoBlock.AddressOf(ref directoryInfo)) != 0)
                ProcessEntry(ref directoryInfo, ref binding, ref number);
        }

        if (changedDirectory)
            DOS.CurrentDirRaw(previousDirectory);
        if (obtainedBinding)
            Expansion.ReleaseConfigBinding();
        if (directory.IsNotNull)
            DOS.UnLock(directory);

        ioError = (int)DOS.IoErr();
        CloseLibraries(ref libraries, ioError);
        return result;
    }

    private static bool TryOpenLibraries(out LibraryLeases libraries)
    {
        libraries = default;
        libraries.PreviousDos = DOS.DOSLibraryBase;

        libraries.Dos = Exec.OpenLibraryRaw(DOS.Name, DosVersion);
        if (libraries.Dos.IsNull)
            return false;
        DOS.DOSLibraryBase = libraries.Dos;

        libraries.Expansion = Exec.OpenLibraryRaw(Expansion.Name,
            ExpansionVersion);
        if (libraries.Expansion.IsNull)
            return false;
        Expansion.ExpansionLibraryBase = libraries.Expansion;

        libraries.Icon = Exec.OpenLibraryRaw(Icon.Name, IconVersion);
        if (libraries.Icon.IsNull)
            return false;
        Icon.IconLibraryBase = libraries.Icon;
        return true;
    }

    private static void CloseLibraries(ref LibraryLeases libraries,
        int ioError)
    {
        if (libraries.Dos.IsNotNull)
            DOS.SetIoErr((DOS.Error)ioError);

        if (libraries.Icon.IsNotNull)
        {
            Icon.IconLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.Icon);
            libraries.Icon = APTR.Null;
        }
        if (libraries.Expansion.IsNotNull)
        {
            Expansion.ExpansionLibraryBase = APTR.Null;
            Exec.CloseLibrary(libraries.Expansion);
            libraries.Expansion = APTR.Null;
        }
        if (libraries.Dos.IsNotNull)
        {
            DOS.DOSLibraryBase = libraries.PreviousDos;
            Exec.CloseLibrary(libraries.Dos);
            libraries.Dos = APTR.Null;
            if (libraries.PreviousDos.IsNotNull)
                DOS.SetIoErr((DOS.Error)ioError);
        }
        libraries.PreviousDos = APTR.Null;
    }

    private static void ProcessEntry(ref FileInfoBlock fileInfo,
        ref BindingCells binding, ref NumberCell number)
    {
        var name = FileInfoBlock.AddressOf(ref fileInfo);
        var fileName = APTR.FromPointer(name.Raw +
            (uint)FileInfoBlock.FileNameOffset);
        if (!StripInfoSuffix(fileName))
            return;

        var diskObject = APTR.FromPointer(Icon.GetDiskObject(
            CString.FromPointer(fileName.Raw)));
        if (diskObject.IsNull)
            return;

        var toolTypes = APTR.FromPointer(APTR.ReadUInt32(diskObject,
            DiskObjectToolTypesOffset));
        var product = APTR.FromPointer(Icon.FindToolType(toolTypes.Raw,
            CString.FromLiteral("PRODUCT")));
        if (product.IsNotNull)
        {
            var configDev = GetConfigDev(product, NumberCell.AddressOf(ref number));
            if (configDev.IsNotNull)
            {
                var segmentRaw = Workbench31BindDriversDosRaw.LoadSegRaw(
                    CString.FromPointer(fileName.Raw));
                var segment = BPTR.FromRaw(segmentRaw);
                if (segment.IsNotNull)
                {
                    var resident = FindFirstHunkResident(segment.Raw);
                    if (resident.IsNotNull)
                    {
                        var bindingAddress = BindingCells.AddressOf(ref binding);
                        APTR.WriteUInt32(bindingAddress, 0, configDev.Raw);
                        APTR.WriteUInt32(bindingAddress, 4, fileName.Raw);
                        APTR.WriteUInt32(bindingAddress, 8, product.Raw);
                        APTR.WriteUInt32(bindingAddress, 12, toolTypes.Raw);
                        Expansion.SetCurrentBinding(bindingAddress,
                            CurrentBinding.Size);
                        if (Exec.InitResident(resident, segment).IsNull)
                            DOS.UnLoadSeg(segment);
                    }
                    else
                    {
                        DOS.UnLoadSeg(segment);
                    }
                }
            }
        }

        Icon.FreeDiskObject(diskObject.Raw);
    }

    private static bool StripInfoSuffix(APTR fileName)
    {
        var length = CStringLength(fileName);
        if (length < 5)
            return false;

        var suffix = APTR.FromPointer(fileName.Raw + length - 5u);
        if (FoldAscii(APTR.ReadUInt8(suffix, 0)) != (byte)'.' ||
            FoldAscii(APTR.ReadUInt8(suffix, 1)) != (byte)'I' ||
            FoldAscii(APTR.ReadUInt8(suffix, 2)) != (byte)'N' ||
            FoldAscii(APTR.ReadUInt8(suffix, 3)) != (byte)'F' ||
            FoldAscii(APTR.ReadUInt8(suffix, 4)) != (byte)'O')
            return false;

        APTR.WriteUInt8(fileName, unchecked((int)(length - 5u)), 0);
        return true;
    }

    private static APTR GetConfigDev(APTR product, APTR numberStorage)
    {
        var result = APTR.Null;
        var pair = product;
        while (APTR.ReadUInt8(pair, 0) != 0)
        {
            var separator = FindCharacter(pair, (byte)'|');
            var pairLength = separator.IsNull
                ? CStringLength(pair)
                : separator.Raw - pair.Raw;
            var slash = FindWithin(pair, (byte)'/', pairLength);
            var manufacturerLength = slash.IsNull
                ? pairLength : slash.Raw - pair.Raw;

            if (TryParseDigits(pair, manufacturerLength, numberStorage))
            {
                var manufacturer = unchecked((int)
                    APTR.ReadUInt32(numberStorage, 0));
                var productId = -1;
                var validProduct = true;
                if (slash.IsNotNull)
                {
                    var productStart = APTR.FromPointer(slash.Raw + 1u);
                    var productLength = pairLength - manufacturerLength - 1u;
                    validProduct = TryParseDigits(productStart,
                        productLength, numberStorage);
                    if (validProduct)
                        productId = unchecked((int)
                            APTR.ReadUInt32(numberStorage, 0));
                }

                if (validProduct)
                    AddMatchingConfigDevs(ref result, manufacturer,
                        productId);
            }

            if (separator.IsNull)
                break;
            pair = APTR.FromPointer(separator.Raw + 1u);
        }

        return result;
    }

    private static bool TryParseDigits(APTR text, uint length,
        APTR numberStorage)
    {
        if (length == 0)
            return false;
        for (var index = 0u; index < length; index++)
        {
            var value = APTR.ReadUInt8(text, unchecked((int)index));
            if (value < (byte)'0' || value > (byte)'9')
                return false;
        }

        return DOS.StrToLong(CString.FromPointer(text), numberStorage) > 0;
    }

    private static void AddMatchingConfigDevs(ref APTR result,
        int manufacturer, int product)
    {
        var previous = APTR.Null;
        while (true)
        {
            var found = APTR.FromPointer(Expansion.FindConfigDev(previous,
                manufacturer, product));
            if (found.IsNull)
                return;
            previous = found;
            if (ContainsConfigDev(result, found))
                continue;

            APTR.WriteUInt32(found, ConfigDevNextOffset, result.Raw);
            result = found;
        }
    }

    private static bool ContainsConfigDev(APTR list, APTR node)
    {
        var cursor = list;
        while (cursor.IsNotNull)
        {
            if (cursor.Raw == node.Raw)
                return true;
            cursor = APTR.FromPointer(APTR.ReadUInt32(cursor,
                ConfigDevNextOffset));
        }
        return false;
    }

    private static APTR FindFirstHunkResident(uint segmentRaw)
    {
        if (segmentRaw > (uint.MaxValue >> 2))
            return APTR.Null;
        var segment = APTR.FromPointer(segmentRaw << 2);
        if (segment.Raw < 4 || Exec.TypeOfMem(
                APTR.FromPointer(segment.Raw - 4u)) == 0 ||
            Exec.TypeOfMem(segment) == 0)
            return APTR.Null;

        // The Workbench helper reads a byte extent immediately before this
        // segment and scans this HUNK only. Do not follow a second HUNK link:
        // that is the captured profile behavior.
        var extent = APTR.ReadUInt32(segment, -4);
        if (extent < 34u || segment.Raw > uint.MaxValue - extent)
            return APTR.Null;
        var last = segment.Raw + extent - 1u;
        if (Exec.TypeOfMem(APTR.FromPointer(last)) == 0)
            return APTR.Null;

        var remaining = extent - 8u;
        var offset = 4u;
        while (remaining > 26u)
        {
            var resident = APTR.FromPointer(segment.Raw + offset);
            if (APTR.ReadUInt16(resident, 0) == ResidentMatchWord &&
                APTR.ReadUInt32(resident, 2) == resident.Raw)
                return resident;
            offset += 2u;
            remaining -= 2u;
        }
        return APTR.Null;
    }

    private static APTR FindCharacter(APTR text, byte character)
    {
        var cursor = text;
        while (APTR.ReadUInt8(cursor, 0) != 0)
        {
            if (APTR.ReadUInt8(cursor, 0) == character)
                return cursor;
            cursor = APTR.FromPointer(cursor.Raw + 1u);
        }
        return APTR.Null;
    }

    private static APTR FindWithin(APTR text, byte character, uint length)
    {
        for (var index = 0u; index < length; index++)
        {
            var cursor = APTR.FromPointer(text.Raw + index);
            if (APTR.ReadUInt8(cursor, 0) == character)
                return cursor;
        }
        return APTR.Null;
    }

    private static uint CStringLength(APTR text)
    {
        var length = 0u;
        while (APTR.ReadUInt8(text, unchecked((int)length)) != 0)
            length++;
        return length;
    }

    private static byte FoldAscii(byte value) =>
        value is >= (byte)'a' and <= (byte)'z'
            ? (byte)(value - (byte)'a' + (byte)'A')
            : value;
}

[AmigaLibrary(DOS.Name)]
internal static class Workbench31BindDriversDosRaw
{
    [AmigaLvo(-150)]
    [return: M68kRegister(M68kRegister.D0)]
    public static extern uint LoadSegRaw(
        [M68kRegister(M68kRegister.D1)] CString name);
}
