using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record IconPosEntryCase(
    string File = "SYS:Prefs.info",
    bool IconPresent = true,
    bool ImagePresent = false,
    bool Create = false,
    string Type = "",
    int? XPosition = null,
    int? YPosition = null,
    bool FreeX = false,
    bool FreeY = false,
    int? DrawerX = null,
    int? DrawerY = null,
    int? DrawerWidth = null,
    int? DrawerHeight = null,
    int? PutResult = null,
    int ParserError = 0,
    bool IconOpenFailure = false,
    bool MissingDos = false);

internal sealed record IconPosNativeLayout(
    uint Control,
    uint File,
    uint Type,
    uint Image,
    uint XValue,
    uint YValue,
    uint DrawerX,
    uint DrawerY,
    uint DrawerWidth,
    uint DrawerHeight,
    uint Icon,
    uint ImageObject,
    uint DefaultObject,
    uint DrawerData);

internal sealed partial class Invocation
{
    public const uint IconPosBase = 0xe000;
    public uint IconBase => IconPosBase;
    public IconPosNativeLayout? IconPosLayout { get; set; }
    public int IconOpens { get; set; }
    public int IconCloses { get; set; }
    public int IconReadArgsCalls { get; set; }
    public int IconFreeArgsCalls { get; set; }
    public int IconPrintFaultCalls { get; set; }
    public int IconIoErrCalls { get; set; }
    public int IconSetIoErrCalls { get; set; }
    public int IconGetDiskObjectCalls { get; set; }
    public int IconGetDefaultCalls { get; set; }
    public int IconPutDiskObjectCalls { get; set; }
    public int IconFreeDiskObjectCalls { get; set; }
    public uint IconObservedX { get; set; }
    public uint IconObservedY { get; set; }
    public byte IconObservedType { get; set; }
    public ushort IconObservedDrawerX { get; set; }
    public ushort IconObservedDrawerY { get; set; }
    public ushort IconObservedDrawerWidth { get; set; }
    public ushort IconObservedDrawerHeight { get; set; }
    public uint IconObservedDrawerXLong { get; set; }
    public uint IconObservedDrawerYLong { get; set; }
    public uint IconObservedDrawerWidthLong { get; set; }
    public uint IconObservedDrawerHeightLong { get; set; }
    public uint IconObservedRender { get; set; }
    public uint IconObservedSelectRender { get; set; }
    public ushort IconObservedWidth { get; set; }
    public ushort IconObservedHeight { get; set; }
    public uint IconObservedDrawerPointer42 { get; set; }
    public uint IconObservedDrawerPointer46 { get; set; }
    public uint IconObservedDrawerPointer4a { get; set; }
    public List<uint> IconWriteAddresses { get; } = [];
}

internal sealed partial class ProbeFixture
{
    public const string IconPosEntrySuite =
        "workbench31-iconpos-native-entry-vector-fixture";

    private List<object> RunIconPosEntryCases()
    {
        ProbeCase[] cases =
        [
            IconPosCase("edit-position-and-type", new(
                XPosition: 321, YPosition: 654, Type: "drawer",
                DrawerX: 4, DrawerY: 5, DrawerWidth: 640, DrawerHeight: 480),
                result: DOS.RETURN_OK),
            IconPosCase("free-positions", new(FreeX: true, FreeY: true),
                result: DOS.RETURN_OK),
            IconPosCase("copy-image", new(ImagePresent: true),
                result: DOS.RETURN_OK),
            IconPosCase("create-default", new(IconPresent: false,
                Create: true, Type: "project"), result: DOS.RETURN_OK),
            IconPosCase("missing-without-create", new(IconPresent: false),
                result: DOS.RETURN_ERROR,
                error: (int)DOS.Error.ObjectNotFound),
            IconPosCase("create-without-type", new(IconPresent: false,
                Create: true), result: DOS.RETURN_ERROR,
                error: (int)DOS.Error.RequiredArgumentMissing),
            IconPosCase("unknown-type", new(Type: "unknown"),
                result: DOS.RETURN_ERROR, error: (int)DOS.Error.BadTemplate),
            IconPosCase("put-failure", new(PutResult: 0),
                result: DOS.RETURN_ERROR,
                error: (int)DOS.Error.ObjectNotFound),
            IconPosCase("parser-failure", new(ParserError: 116),
                result: DOS.RETURN_ERROR, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { IconPos = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { IconPos = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { IconPos = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { IconPos = new(MissingDos: true), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            IconPosCase("interleaved-edit", new(XPosition: 17)),
            IconPosCase("interleaved-image", new(ImagePresent: true))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase IconPosCase(string name,
        IconPosEntryCase definition, int result = DOS.RETURN_OK, int error = 0) =>
        new(name, definition.File, result, error, "") { IconPos = definition };

    private void PrepareIconPosEntry(Invocation invocation)
    {
        var definition = invocation.Definition.IconPos ?? new();
        var control = invocation.Arguments + 0x300;
        var file = invocation.Arguments + 0x400;
        var type = invocation.Arguments + 0x480;
        var image = invocation.Arguments + 0x500;
        var xValue = invocation.Arguments + 0x580;
        var yValue = invocation.Arguments + 0x584;
        var drawerX = invocation.Arguments + 0x588;
        var drawerY = invocation.Arguments + 0x58c;
        var drawerWidth = invocation.Arguments + 0x590;
        var drawerHeight = invocation.Arguments + 0x594;
        WriteCString(file, definition.File);
        if (definition.Type.Length != 0) WriteCString(type, definition.Type);
        if (definition.ImagePresent) WriteCString(image, "SYS:Image.info");
        WriteNumber(xValue, definition.XPosition);
        WriteNumber(yValue, definition.YPosition);
        WriteDrawerNumber(drawerX, definition.DrawerX);
        WriteDrawerNumber(drawerY, definition.DrawerY);
        WriteDrawerNumber(drawerWidth, definition.DrawerWidth);
        WriteDrawerNumber(drawerHeight, definition.DrawerHeight);

        uint icon = 0;
        uint imageObject = 0;
        uint defaultObject = 0;
        uint drawerData = 0;
        var validType = IsKnownType(definition.Type);
        var validStartup = !invocation.Definition.Workbench &&
            !invocation.Definition.MissingDos &&
            invocation.Definition.EntryLength is not < 0 &&
            !invocation.Definition.NullArgumentPointer;
        if (validStartup && definition.IconPresent &&
            definition.ParserError == 0 && validType)
        {
            icon = Bus.Allocate(invocation, 128, "IconDiskObject", true);
            if (definition.DrawerX is not null || definition.DrawerY is not null ||
                definition.DrawerWidth is not null || definition.DrawerHeight is not null)
            {
                drawerData = Bus.Allocate(invocation, 16, "IconDrawerData", true);
                Bus.Long(icon + 0x42, drawerData);
            }
            Bus.Memory[icon + 0x30] = definition.Type.Length == 0
                ? (byte)1 : TypeValue(definition.Type);
            Bus.Long(icon + 0x3a, 10);
            Bus.Long(icon + 0x3e, 20);
        }
        if (definition.ImagePresent)
        {
            imageObject = Bus.Allocate(invocation, 128, "IconImageObject", true);
            Bus.Long(imageObject + 0x16, 0x12345678);
            Bus.Long(imageObject + 0x1a, 0x87654321);
            Bus.Word(imageObject + 0x0c, 64);
            Bus.Word(imageObject + 0x0e, 32);
        }
        Bus.Long(control, 0);
        invocation.IconPosLayout = new(control, file, type, image,
            xValue, yValue, drawerX, drawerY, drawerWidth, drawerHeight,
            definition.IconPresent ? icon : 0, imageObject, defaultObject,
            drawerData);
    }

    private void RegisterIconPosEntryIcon()
    {
        Register(Invocation.IconPosBase, -78, "GetDiskObject", (state, invocation) =>
        {
            invocation.IconGetDiskObjectCalls++;
            var definition = invocation.Definition.IconPos!;
            var image = invocation.IconPosLayout!.Image;
            var isImage = state.A[0] == image;
            Require(state.A[0] == invocation.IconPosLayout.File || isImage,
                "IconPos GetDiskObject path differs.");
            if ((!isImage && !definition.IconPresent) ||
                (isImage && !definition.ImagePresent))
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return 0;
            }
            return isImage ? invocation.IconPosLayout.ImageObject :
                invocation.IconPosLayout.Icon;
        });
        Register(Invocation.IconPosBase, -84, "PutDiskObject", (state, invocation) =>
        {
            invocation.IconPutDiskObjectCalls++;
            Require(state.A[0] == invocation.IconPosLayout!.File &&
                (state.A[1] == invocation.IconPosLayout.Icon ||
                 state.A[1] == invocation.IconPosLayout.DefaultObject),
                "IconPos PutDiskObject ABI differs.");
            var result = invocation.Definition.IconPos!.PutResult ?? 1;
            if (result == 0) invocation.IoError = (int)DOS.Error.ObjectNotFound;
            return unchecked((uint)result);
        });
        Register(Invocation.IconPosBase, -90, "FreeDiskObject", (state, invocation) =>
        {
            invocation.IconFreeDiskObjectCalls++;
            var layout = invocation.IconPosLayout!;
            if (state.A[0] == layout.ImageObject)
                Bus.Release(invocation, state.A[0], "IconImageObject", 128);
            else if (state.A[0] == layout.Icon)
            {
                CaptureIcon(invocation, state.A[0]);
                Bus.Release(invocation, state.A[0], "IconDiskObject", 128);
                if (layout.DrawerData != 0)
                    Bus.Release(invocation, layout.DrawerData, "IconDrawerData", 16);
            }
            else if (state.A[0] == layout.DefaultObject)
            {
                CaptureIcon(invocation, state.A[0]);
                Bus.Release(invocation, state.A[0], "IconDefaultObject", 128);
                if (layout.DrawerData != 0)
                    Bus.Release(invocation, layout.DrawerData, "IconDrawerData", 16);
            }
            else throw new InvalidOperationException("IconPos freed an unknown DiskObject.");
            return 0;
        });
        Register(Invocation.IconPosBase, -120, "GetDefDiskObject", (state, invocation) =>
        {
            invocation.IconGetDefaultCalls++;
            Require(state.D[0] >= 1 && state.D[0] <= 5,
                "IconPos default type is outside the classic range.");
            var layout = invocation.IconPosLayout!;
            if (layout.DefaultObject == 0)
            {
                var defaultObject = Bus.Allocate(invocation, 128,
                    "IconDefaultObject", true);
                Bus.Memory[defaultObject + 0x30] = unchecked((byte)state.D[0]);
                Bus.Long(defaultObject + 0x3a, 0x8000_0000);
                Bus.Long(defaultObject + 0x3e, 0x8000_0000);
                invocation.IconPosLayout = layout with
                {
                    DefaultObject = defaultObject
                };
            }
            return invocation.IconPosLayout!.DefaultObject;
        });
    }

    private void RegisterIconPosEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.IconPos!;
            var layout = invocation.IconPosLayout!;
            Require(Bus.CString(state.D[1]) == NativeWorkbench31IconPosCommand.Template &&
                state.D[3] == 0 && state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                    NativeWorkbench31IconPosCommand.ResultCount * 4u,
                "IconPos ReadArgs ABI differs.");
            invocation.IconReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var results = state.D[2];
            Bus.Long(results + 0, layout.File);
            Bus.Long(results + 4, definition.XPosition is null ? 0 : layout.XValue);
            Bus.Long(results + 8, definition.YPosition is null ? 0 : layout.YValue);
            Bus.Long(results + 12, definition.Type.Length == 0 ? 0 : layout.Type);
            Bus.Long(results + 16, definition.DrawerX is null ? 0 : layout.DrawerX);
            Bus.Long(results + 20, definition.DrawerY is null ? 0 : layout.DrawerY);
            Bus.Long(results + 24, definition.DrawerWidth is null ? 0 : layout.DrawerWidth);
            Bus.Long(results + 28, definition.DrawerHeight is null ? 0 : layout.DrawerHeight);
            Bus.Long(results + 32, definition.Create ? uint.MaxValue : 0);
            Bus.Long(results + 36, definition.FreeX ? uint.MaxValue : 0);
            Bus.Long(results + 40, definition.FreeY ? uint.MaxValue : 0);
            Bus.Long(results + 44, definition.ImagePresent ? layout.Image : 0);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.IconFreeArgsCalls++;
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, invocation) =>
        {
            invocation.IconPrintFaultCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
        {
            invocation.IconIoErrCalls++;
            return unchecked((uint)invocation.IoError);
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IconSetIoErrCalls++;
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyIconPosEntry(Invocation invocation)
    {
        var definition = invocation.Definition.IconPos!;
        var boundary = invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos;
        if (boundary)
        {
            Require(invocation.IconReadArgsCalls == 0 &&
                invocation.IconGetDiskObjectCalls == 0 &&
                invocation.IconPutDiskObjectCalls == 0,
                "IconPos crossed an invalid startup boundary.");
            return;
        }

        var parser = definition.ParserError != 0;
        Require(invocation.IconReadArgsCalls == 1 &&
            invocation.IconFreeArgsCalls == (parser ? 0 : 1) &&
            invocation.Allocations >= 1 &&
            invocation.FreeMem == 1,
            $"IconPos parser/result ownership differs (reads={invocation.IconReadArgsCalls}, freeArgs={invocation.IconFreeArgsCalls}, allocations={invocation.Allocations}, freeMem={invocation.FreeMem}, parser={parser}).");
        if (parser)
        {
            Require(invocation.IconGetDiskObjectCalls == 0 &&
                invocation.IconPutDiskObjectCalls == 0 &&
                invocation.IconPrintFaultCalls == 1,
                "IconPos parser failure crossed the icon path.");
            return;
        }

        if (!IsKnownType(definition.Type))
        {
            Require(invocation.IconGetDiskObjectCalls == 0 &&
                invocation.IconPutDiskObjectCalls == 0 &&
                invocation.IconFreeDiskObjectCalls == 0 &&
                invocation.IconPrintFaultCalls == 1,
                "IconPos rejected an unknown type after opening icon.library.");
            return;
        }

        Require(invocation.IconOpens == 1 && invocation.IconCloses == 1 &&
            invocation.IconFreeArgsCalls == 1 &&
            invocation.IconPutDiskObjectCalls ==
                (definition.IconPresent || definition.Create &&
                    definition.Type.Length != 0 ? 1 : 0),
            $"IconPos library or store ownership differs (opens={invocation.IconOpens}, closes={invocation.IconCloses}, freeArgs={invocation.IconFreeArgsCalls}, gets={invocation.IconGetDiskObjectCalls}, defaults={invocation.IconGetDefaultCalls}, puts={invocation.IconPutDiskObjectCalls}, frees={invocation.IconFreeDiskObjectCalls}, present={definition.IconPresent}, create={definition.Create}).");
        if (definition.ImagePresent)
            Require(invocation.IconGetDiskObjectCalls == 2,
                "IconPos image acquisition count differs.");
        else Require(invocation.IconGetDiskObjectCalls == 1,
            "IconPos icon acquisition count differs.");
        var storeSucceeded = definition.PutResult is null or > 0;
        if (storeSucceeded)
        {
            if (definition.XPosition is int x)
                Require(unchecked((int)invocation.IconObservedX) == x,
                    "IconPos X position was not stored.");
            if (definition.YPosition is int y)
                Require(unchecked((int)invocation.IconObservedY) == y,
                    "IconPos Y position was not stored.");
            if (definition.FreeX) Require(invocation.IconObservedX == 0x8000_0000,
                "IconPos FREEX was not stored.");
            if (definition.FreeY) Require(invocation.IconObservedY == 0x8000_0000,
                "IconPos FREEY was not stored.");
            if (definition.Type.Length != 0)
                Require(invocation.IconObservedType == TypeValue(definition.Type),
                    "IconPos type was not stored.");
            if (definition.DrawerX is int drawerX)
                Require(invocation.IconObservedDrawerX == drawerX,
                    $"IconPos drawer X was not stored (actual={invocation.IconObservedDrawerX}, long={invocation.IconObservedDrawerXLong}, expected={drawerX}, p42={invocation.IconObservedDrawerPointer42:X8}, p46={invocation.IconObservedDrawerPointer46:X8}, p4a={invocation.IconObservedDrawerPointer4a:X8}, writes={string.Join(",", invocation.IconWriteAddresses.Select(a => $"{a:X8}"))}).");
            if (definition.DrawerY is int drawerY)
                Require(invocation.IconObservedDrawerY == drawerY,
                    "IconPos drawer Y was not stored.");
            if (definition.DrawerWidth is int drawerWidth)
                Require(invocation.IconObservedDrawerWidth == drawerWidth,
                    "IconPos drawer width was not stored.");
            if (definition.DrawerHeight is int drawerHeight)
                Require(invocation.IconObservedDrawerHeight == drawerHeight,
                    "IconPos drawer height was not stored.");
            if (definition.ImagePresent)
                Require(invocation.IconObservedRender == 0x12345678 &&
                    invocation.IconObservedSelectRender == 0x87654321 &&
                    invocation.IconObservedWidth == 64 &&
                    invocation.IconObservedHeight == 32,
                    "IconPos image fields were not copied.");
        }
    }

    private void CaptureIcon(Invocation invocation, uint icon)
    {
        invocation.IconObservedX = Bus.Long(icon + 0x3a);
        invocation.IconObservedY = Bus.Long(icon + 0x3e);
        invocation.IconObservedType = Bus.Memory[icon + 0x30];
        invocation.IconObservedDrawerPointer42 = Bus.Long(icon + 0x42);
        invocation.IconObservedDrawerPointer46 = Bus.Long(icon + 0x46);
        invocation.IconObservedDrawerPointer4a = Bus.Long(icon + 0x4a);
        var drawer = Bus.Long(icon + 0x42);
        if (drawer != 0)
        {
            invocation.IconObservedDrawerX = Bus.Word(drawer);
            invocation.IconObservedDrawerY = Bus.Word(drawer + 2);
            invocation.IconObservedDrawerWidth = Bus.Word(drawer + 4);
            invocation.IconObservedDrawerHeight = Bus.Word(drawer + 6);
            invocation.IconObservedDrawerXLong = Bus.Long(drawer);
            invocation.IconObservedDrawerYLong = Bus.Long(drawer + 2);
            invocation.IconObservedDrawerWidthLong = Bus.Long(drawer + 4);
            invocation.IconObservedDrawerHeightLong = Bus.Long(drawer + 6);
        }
        invocation.IconObservedRender = Bus.Long(icon + 0x16);
        invocation.IconObservedSelectRender = Bus.Long(icon + 0x1a);
        invocation.IconObservedWidth = Bus.Word(icon + 0x0c);
        invocation.IconObservedHeight = Bus.Word(icon + 0x0e);
    }

    private static bool IsKnownType(string value) => value.Length == 0 ||
        value.Equals("disk", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("drawer", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("tool", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("project", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("garbage", StringComparison.OrdinalIgnoreCase);

    private static byte TypeValue(string value) =>
        value.Equals("disk", StringComparison.OrdinalIgnoreCase) ? (byte)1 :
        value.Equals("drawer", StringComparison.OrdinalIgnoreCase) ? (byte)2 :
        value.Equals("tool", StringComparison.OrdinalIgnoreCase) ? (byte)3 :
        value.Equals("project", StringComparison.OrdinalIgnoreCase) ? (byte)4 :
        value.Equals("garbage", StringComparison.OrdinalIgnoreCase) ? (byte)5 :
        (byte)0;

    private void WriteNumber(uint address, int? value)
    {
        if (value is int number) Bus.Long(address, unchecked((uint)number));
    }

    private void WriteDrawerNumber(uint address, int? value)
    {
        if (value is int number)
        {
            var word = unchecked((uint)(ushort)number);
            Bus.Long(address, word | (word << 16));
        }
    }

}
