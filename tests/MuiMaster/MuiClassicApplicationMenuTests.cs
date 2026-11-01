using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiClassicApplicationMenuTests
{
    private static APTR P(uint value) => APTR.FromPointer(value);
    private static void Tag(ref Platform p, uint address, uint tag, uint data = 0)
    { p.WriteUInt32(P(address), 0, tag); p.WriteUInt32(P(address), 4, data); }

    [Fact]
    public void FlattensControlsAndConvertsMenuWithoutChangingCallerTags()
    {
        var p = new Platform();
        Tag(ref p, 0x1100, 3, 1); // Skip the invalid menu pointer.
        Tag(ref p, 0x1108, MuiClassicApplicationMenu.Menu, 0xffffffff);
        Tag(ref p, 0x1110, 2, 0x1200);
        Tag(ref p, 0x1200, 1);
        Tag(ref p, 0x1208, 0x80420001, 7);
        Tag(ref p, 0x1210, MuiClassicApplicationMenu.Menu, 0x1500);
        Tag(ref p, 0x1218, 0);
        Assert.True(MuiClassicApplicationMenu.Prepare(ref p, P(0x1100), out var copy, out var bytes, out var generated));
        Assert.Equal(24u, bytes);
        Assert.Equal(0x80420001u, p.ReadUInt32(copy));
        Assert.Equal(7u, p.ReadUInt32(copy, 4));
        Assert.Equal(MuiClassicApplicationMenu.Menustrip, p.ReadUInt32(copy, 8));
        Assert.Equal(0x1800u, p.ReadUInt32(copy, 12));
        Assert.Equal(0u, p.ReadUInt32(copy, 16));
        Assert.Equal(0x1500u, p.LastMenu.Raw);
        Assert.Equal(MuiClassicApplicationMenu.Menu, p.ReadUInt32(P(0x1210)));
        Assert.Equal(0x1500u, p.ReadUInt32(P(0x1210), 4));
        MuiClassicApplicationMenu.Finish(ref p, copy, bytes, generated, true);
        Assert.Equal(1u, p.Memory.FreeCount);
        Assert.Equal(0, p.Disposals);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FactoryFailureAndLaterConstructorFailureReleaseOnlyOwnedResources(bool factoryFailure)
    {
        var p = new Platform { FailFactory = factoryFailure };
        Tag(ref p, 0x1100, MuiClassicApplicationMenu.Menu, 0x1500);
        Tag(ref p, 0x1108, 0);
        var ok = MuiClassicApplicationMenu.Prepare(ref p, P(0x1100), out var copy, out var bytes, out var generated);
        Assert.Equal(!factoryFailure, ok);
        if (ok) MuiClassicApplicationMenu.Finish(ref p, copy, bytes, generated, false);
        Assert.Equal(1, p.Creations);
        Assert.Equal(factoryFailure ? 0 : 1, p.Disposals);
        Assert.Equal(1u, p.Memory.FreeCount);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0x1800u)]
    public void NullAndBorrowedMenusDoNotCreateOrDisposeObjects(uint value)
    {
        var p = new Platform();
        Tag(ref p, 0x1100, MuiClassicApplicationMenu.Menu, value); Tag(ref p, 0x1108, 0);
        Assert.True(MuiClassicApplicationMenu.Prepare(ref p, P(0x1100), out var copy, out var bytes, out var generated));
        Assert.Equal(value == 0 ? 1u : MuiClassicApplicationMenu.Menu, p.ReadUInt32(copy));
        Assert.True(generated.IsNull);
        MuiClassicApplicationMenu.Finish(ref p, copy, bytes, generated, false);
        Assert.Equal(0, p.Creations); Assert.Equal(0, p.Disposals);
    }

    [Theory]
    [InlineData(2u, 0xffff0000u)]
    [InlineData(3u, 0xffffffffu)]
    public void MalformedControlTagsFailBeforeAllocation(uint tag, uint data)
    {
        var p = new Platform(); Tag(ref p, 0x1100, tag, data);
        Assert.False(MuiClassicApplicationMenu.Prepare(ref p, P(0x1100), out _, out _, out _));
        Assert.Equal(0u, p.Memory.AllocationCount); Assert.Equal(0, p.Creations);
    }

    [Fact]
    public void AllocatorReentryCannotIntroduceAnotherUntrackedGeneratedMenu()
    {
        var p = new Platform { AddDuplicateDuringAllocation = true };
        Tag(ref p, 0x1100, MuiClassicApplicationMenu.Menu, 0x1500);
        Tag(ref p, 0x1108, 0x80420001, 0x1500); Tag(ref p, 0x1110, 0);
        Assert.False(MuiClassicApplicationMenu.Prepare(ref p, P(0x1100), out _, out _, out _));
        Assert.Equal(0, p.Creations); Assert.Equal(1u, p.Memory.FreeCount);
    }

    [Fact]
    public void OrdinaryTagListIsBorrowedWithoutAllocation()
    {
        var p = new Platform(); Tag(ref p, 0x1100, 0x80420001, 7); Tag(ref p, 0x1108, 0);
        Assert.True(MuiClassicApplicationMenu.Prepare(ref p, P(0x1100), out var copy, out var bytes, out _));
        Assert.Equal(P(0x1100), copy); Assert.Equal(0u, bytes); Assert.Equal(0u, p.Memory.AllocationCount);
    }

    private struct Platform : IMuiClassicMenuPlatform
    {
        public MuiHeadlessTestPlatform Memory;
        public int Creations, Disposals;
        public bool FailFactory, AddDuplicateDuringAllocation;
        public APTR LastMenu;
        public Platform() { Memory = new(0x1000, 0x10000, 0x2000, P(0x1000)); }
        public bool IsMenustripObject(APTR value) => value.Raw == 0x1800;
        public APTR CreateClassicMenustrip(APTR menu) { Creations++; LastMenu = menu; return FailFactory ? APTR.Null : P(0x1800); }
        public void DisposeClassicMenustrip(APTR strip) { Assert.Equal(P(0x1800), strip); Disposals++; }
        public APTR Allocate(uint n, uint flags) { if (AddDuplicateDuringAllocation) Memory.WriteUInt32(P(0x1108), 0, MuiClassicApplicationMenu.Menu); return Memory.Allocate(n, flags); }
        public void Free(APTR a, uint n) => Memory.Free(a, n);
        public bool IsMapped(APTR a, uint n) => Memory.IsMapped(a, n);
        public byte ReadUInt8(APTR a, int o = 0) => Memory.ReadUInt8(a, o);
        public ushort ReadUInt16(APTR a, int o = 0) => Memory.ReadUInt16(a, o);
        public uint ReadUInt32(APTR a, int o = 0) => Memory.ReadUInt32(a, o);
        public void WriteUInt8(APTR a, int o, byte v) => Memory.WriteUInt8(a, o, v);
        public void WriteUInt16(APTR a, int o, ushort v) => Memory.WriteUInt16(a, o, v);
        public void WriteUInt32(APTR a, int o, uint v) => Memory.WriteUInt32(a, o, v);
        public void Clear(APTR a, uint n) => Memory.Clear(a, n);
        public void Copy(APTR s, APTR d, uint n) => Memory.Copy(s, d, n);
    }
}
