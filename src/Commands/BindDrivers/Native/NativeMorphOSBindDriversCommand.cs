using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 BindDrivers scanner.  The command has no ReadArgs grammar:
/// it scans the guest SYS:Expansion directory, resolves PRODUCT tool types to
/// expansion ConfigDev chains, and initializes the first resident found in
/// each loadable driver segment.  The Workbench binary is kept as a separate
/// profile until its correspondence and diagnostics are captured.
/// </summary>
public static class NativeMorphOSBindDriversCommand
{
    private const uint DosVersion = 37;
    private const uint IconVersion = 37;
    private const uint ExpansionVersion = 37;
    private const uint PathBytes = 1024;
    private const uint PathOffset = ((uint)DosLayout.AnchorPath.Size + 3u) & ~3u;
    private const uint BindingOffset = PathOffset + PathBytes;
    private const uint AnchorBytes = BindingOffset + (uint)CurrentBinding.Size;
    private const int DiskObjectToolTypesOffset = 0x38;
    private const int AChainLockOffset = 8;
    private const int ConfigDevNextOffset = 48;
    private const ushort ResidentMatchWord = 0x4AFC;
    private const int ProductIdMissing = -1;

    /// <summary>Guest workspace required by the source-bound scanner.</summary>
    public const uint WorkspaceBytes = AnchorBytes;

    public static int Run(out int ioError)
    {
        ioError = 0;
        var dos = Exec.OpenLibraryRaw(DOS.Name, DosVersion);
        if (dos.IsNull) return DOS.RETURN_FAIL;
        DOS.DOSLibraryBase = dos;

        var icon = Exec.OpenLibraryRaw(Icon.Name, IconVersion);
        if (icon.IsNull)
        {
            Exec.CloseLibrary(dos);
            DOS.DOSLibraryBase = APTR.Null;
            return DOS.RETURN_FAIL;
        }
        Icon.IconLibraryBase = icon;

        var expansion = Exec.OpenLibraryRaw(Expansion.Name,
            ExpansionVersion);
        if (expansion.IsNull)
        {
            Exec.CloseLibrary(icon);
            Exec.CloseLibrary(dos);
            Icon.IconLibraryBase = APTR.Null;
            DOS.DOSLibraryBase = APTR.Null;
            return DOS.RETURN_FAIL;
        }
        Expansion.ExpansionLibraryBase = expansion;

        var workspace = Exec.AllocMem(AnchorBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (workspace.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            Exec.CloseLibrary(expansion);
            Exec.CloseLibrary(icon);
            Exec.CloseLibrary(dos);
            Expansion.ExpansionLibraryBase = APTR.Null;
            Icon.IconLibraryBase = APTR.Null;
            DOS.DOSLibraryBase = APTR.Null;
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return DOS.RETURN_FAIL;
        }

        var anchor = workspace;
        var path = APTR.FromPointer(workspace.Raw +
            PathOffset);
        var binding = APTR.FromPointer(workspace.Raw +
            BindingOffset);
        InitializeAnchor(anchor);

        var result = DOS.RETURN_OK;
        var error = 0;
        Expansion.ObtainConfigBinding();
        var match = DOS.MatchFirst(CString.FromLiteral(
            "SYS:Expansion/#?.info"), anchor);
        while (match == 0)
        {
            if (!ProcessMatch(anchor, path, binding, out error))
            {
                result = DOS.RETURN_WARN;
                break;
            }
            match = DOS.MatchNext(anchor);
        }

        // MatchEnd is owned after every MatchFirst attempt, including an
        // immediate no-match result.
        DOS.MatchEnd(anchor);
        Expansion.ReleaseConfigBinding();
        if (error != 0)
        {
            ioError = error;
            DOS.SetIoErr((DOS.Error)error);
        }
        Exec.FreeMem(workspace, AnchorBytes);
        Exec.CloseLibrary(expansion);
        Exec.CloseLibrary(icon);
        Exec.CloseLibrary(dos);
        Expansion.ExpansionLibraryBase = APTR.Null;
        Icon.IconLibraryBase = APTR.Null;
        DOS.DOSLibraryBase = APTR.Null;
        return result;
    }

    private static bool ProcessMatch(APTR anchor, APTR path, APTR binding,
        out int error)
    {
        error = 0;
        var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
            DosLayout.AnchorPath.Current));
        if (current.IsNull)
            return true;

        var lock_ = BPTR.FromRaw(APTR.ReadUInt32(current, AChainLockOffset));
        if (DOS.NameFromLock(lock_, path, (int)PathBytes) == 0)
        {
            error = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)error,
                CString.FromLiteral("Error on NameFromLock"));
            return false;
        }

        var fileName = APTR.FromPointer(anchor.Raw +
            (uint)DosLayout.AnchorPath.Info +
            (uint)FileInfoBlock.FileNameOffset);
        if (DOS.AddPart(CString.FromPointer(path.Raw),
                CString.FromPointer(fileName.Raw), PathBytes) == 0)
        {
            error = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)error,
                CString.FromLiteral("Error on AddPart"));
            return false;
        }

        if (FileInfoBlock.GetDirEntryType(anchor.Raw +
                (uint)DosLayout.AnchorPath.Info) > 0)
        {
            var flags = APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags);
            APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                (byte)(flags & ~(byte)AnchorPathFlags.DidDirectory));
            return true;
        }

        var diskObject = APTR.FromPointer(Icon.GetDiskObjectNew(
            CString.FromPointer(path.Raw)));
        if (diskObject.IsNull)
            return true;

        var toolTypes = APTR.FromPointer(APTR.ReadUInt32(diskObject,
            DiskObjectToolTypesOffset));
        var product = APTR.FromPointer(Icon.FindToolType(toolTypes.Raw,
            CString.FromLiteral("PRODUCT")));
        if (product.IsNotNull)
        {
            var configDev = GetConfigDev(product);
            if (configDev.IsNotNull && StripInfoSuffix(path))
            {
                var segmentRaw = BindDriversDosRaw.LoadSegRaw(
                    CString.FromPointer(path.Raw));
                var segment = BPTR.FromRaw(segmentRaw);
                if (segment.IsNotNull)
                {
                    var resident = FindLibResident(segment.Raw);
                    if (resident.IsNotNull)
                    {
                        // configvars.h orders cb_ProductString before
                        // cb_ToolTypes. Keep both guest pointers valid until
                        // InitResident returns.
                        APTR.WriteUInt32(binding, 0, configDev.Raw);
                        APTR.WriteUInt32(binding, 4, path.Raw);
                        APTR.WriteUInt32(binding, 8, product.Raw);
                        APTR.WriteUInt32(binding, 12, toolTypes.Raw);
                        Expansion.SetCurrentBinding(binding,
                            (uint)CurrentBinding.Size);
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
        return true;
    }

    private static void InitializeAnchor(APTR anchor)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            unchecked((ushort)PathBytes));
    }

    private static bool StripInfoSuffix(APTR path)
    {
        var length = CStringLength(path);
        if (length < 5 || APTR.ReadUInt8(path, unchecked((int)length - 5)) !=
                (byte)'.' || APTR.ReadUInt8(path,
                    unchecked((int)length - 4)) != (byte)'i' ||
            APTR.ReadUInt8(path, unchecked((int)length - 3)) != (byte)'n' ||
            APTR.ReadUInt8(path, unchecked((int)length - 2)) != (byte)'f' ||
            APTR.ReadUInt8(path, unchecked((int)length - 1)) != (byte)'o')
            return false;
        APTR.WriteUInt8(path, unchecked((int)length - 5), 0);
        return true;
    }

    internal static APTR GetConfigDev(APTR product)
    {
        var cursor = product;
        // The MorphOS source applies strchr(PRODUCT, "=") rather than
        // checking only the first byte, then passes each selected substring
        // to atoi. Preserve that order for existing and malformed tool types.
        var equals = FindCharacter(cursor, (byte)'=');
        if (equals.IsNotNull)
            cursor = APTR.FromPointer(equals.Raw + 1);

        var result = APTR.Null;
        while (cursor.IsNotNull && APTR.ReadUInt8(cursor, 0) != 0)
        {
            var manufacturer = ReadDecimal(cursor);
            var productId = ProductIdMissing;
            var slash = FindCharacter(cursor, (byte)'/');
            var listStart = cursor;
            if (slash.IsNotNull)
            {
                listStart = APTR.FromPointer(slash.Raw + 1);
                productId = ReadDecimal(listStart);
            }

            var separator = FindCharacter(listStart, (byte)'|');
            cursor = separator.IsNull ? APTR.Null :
                APTR.FromPointer(separator.Raw + 1);

            var previous = APTR.Null;
            while (true)
            {
                var found = APTR.FromPointer(Expansion.FindConfigDev(
                    previous, manufacturer, productId));
                if (found.IsNull) break;
                APTR.WriteUInt32(found, ConfigDevNextOffset, result.Raw);
                result = found;
                previous = found;
            }
        }
        return result;
    }

    private static APTR FindCharacter(APTR value, byte character)
    {
        var cursor = value;
        while (APTR.ReadUInt8(cursor, 0) != 0)
        {
            if (APTR.ReadUInt8(cursor, 0) == character)
                return cursor;
            cursor = APTR.FromPointer(cursor.Raw + 1);
        }
        return APTR.Null;
    }

    private static int ReadDecimal(APTR value)
    {
        var cursor = value;
        while (IsAtoiWhitespace(APTR.ReadUInt8(cursor, 0)))
            cursor = APTR.FromPointer(cursor.Raw + 1);

        var negative = false;
        var sign = APTR.ReadUInt8(cursor, 0);
        if (sign is (byte)'+' or (byte)'-')
        {
            negative = sign == (byte)'-';
            cursor = APTR.FromPointer(cursor.Raw + 1);
        }

        var number = 0;
        while (APTR.ReadUInt8(cursor, 0) >= (byte)'0' &&
            APTR.ReadUInt8(cursor, 0) <= (byte)'9')
        {
            number = unchecked(number * 10 +
                APTR.ReadUInt8(cursor, 0) - (byte)'0');
            cursor = APTR.FromPointer(cursor.Raw + 1);
        }
        return negative ? unchecked(-number) : number;
    }

    private static bool IsAtoiWhitespace(byte value) =>
        value is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\v' or
            (byte)'\f' or (byte)'\r';

    private static uint CStringLength(APTR value)
    {
        var length = 0u;
        while (APTR.ReadUInt8(value, unchecked((int)length)) != 0)
            length++;
        return length;
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
            var offset = 4u;
            while (offset <= segmentBytes - 6u)
            {
                var resident = APTR.FromPointer(memory.Raw + offset);
                if (APTR.ReadUInt16(resident, 0) == ResidentMatchWord &&
                    APTR.ReadUInt32(resident, 2) == resident.Raw)
                    return resident;
                offset += 2;
            }
            var nextSegment = APTR.ReadUInt32(memory, 0);
            currentSegment = nextSegment;

            // Keep the source's unbounded segment-list behavior for valid
            // drivers while detecting malformed cyclic lists without a
            // managed visited-set or an arbitrary segment-count limit.
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
        var memory = APTR.FromPointer(memoryRaw);
        if (memoryRaw == 0 || Exec.TypeOfMem(memory) == 0)
            return false;
        nextSegment = APTR.ReadUInt32(memory, 0);
        return true;
    }
}

/// <summary>Allocation-free DOS LoadSeg binding used by BindDrivers.</summary>
[AmigaLibrary(DOS.Name)]
internal static class BindDriversDosRaw
{
    [AmigaLvo(-150)]
    [return: M68kRegister(M68kRegister.D0)]
    public static extern uint LoadSegRaw(
        [M68kRegister(M68kRegister.D1)] CString name);
}
