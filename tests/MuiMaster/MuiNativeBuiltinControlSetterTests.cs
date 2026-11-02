using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeBuiltinControlSetterTests
{
    private static readonly APTR State = APTR.FromPointer(0x1000);

    [Theory]
    [InlineData(BOOPSI.OM_SET, 720000, "720000")]
    [InlineData(BOOPSI.OM_UPDATE, 200000, "200000")]
    [InlineData(BOOPSI.OM_SET, 0, "0")]
    [InlineData(BOOPSI.OM_UPDATE, -123, "-123")]
    [InlineData(MuiNotifyCore.SetMethod, 37, "37")]
    [InlineData(MuiNotifyCore.NoNotifySetMethod, -17, "-17")]
    public void ClassicIntegerTagsMaterializeOwnedStringContents(uint method,
        int number, string expected)
    {
        var p = Create(out var obj);
        var packet = Packet(ref p, method, MuiCommonControlCore.StringInteger,
            unchecked((uint)number), false);
        Assert.Equal(1u, MuiNativeBuiltinControlSetter.Dispatch(ref p, State,
            obj, packet, method));
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, obj,
            MuiCommonControlCore.StringContents, out var contents));
        Assert.Equal(expected, Text(ref p, APTR.FromPointer(contents)));
        Assert.True(MuiNativeBuiltinControlGetter.PreserveClassResult(
            MuiCommonControlCore.StringContents, 1));
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, obj,
            MuiCommonControlCore.StringInteger, out var value));
        Assert.Equal(unchecked((uint)number), value);
        Assert.True(MuiNativeBuiltinControlGetter.PreserveClassResult(
            MuiCommonControlCore.StringInteger, 1));
    }

    [Theory]
    [InlineData("j")]
    [InlineData("out")]
    public void CallerMayClearDestinationBeforeGettingOwnedString(string suffix)
    {
        var p = Create(out var obj);
        var source = APTR.FromPointer(0x1400);
        p.WriteCString(source, suffix);
        var packet = Packet(ref p, BOOPSI.OM_SET,
            MuiCommonControlCore.StringContents, source.Raw, false);
        Assert.Equal(1u, MuiNativeBuiltinControlSetter.Dispatch(ref p, State,
            obj, packet, BOOPSI.OM_SET));
        // MUIJoin clears its destination just before GetAttr, then copies the
        // returned text into it. Returning the retained raw tag loses suffixes.
        p.WriteCString(source, "");
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, obj,
            MuiCommonControlCore.StringContents, out var contents));
        Assert.True(MuiNativeBuiltinControlGetter.PreserveClassResult(
            MuiCommonControlCore.StringContents, 1));
        Assert.NotEqual(source.Raw, contents);
        Assert.Equal(suffix, Text(ref p, APTR.FromPointer(contents)));
    }

    [Theory]
    [InlineData(MuiCommonControlCore.StringContents)]
    [InlineData(MuiCommonControlCore.StringInteger)]
    [InlineData(MuiWindowPublicCore.Window)]
    [InlineData(0xdeadbeefu)]
    public void FailedOrFacadeGettersRetainProjection(uint attribute)
    {
        Assert.False(MuiNativeBuiltinControlGetter.PreserveClassResult(attribute, 0));
        if (attribute != MuiCommonControlCore.StringContents &&
            attribute != MuiCommonControlCore.StringInteger)
            Assert.False(MuiNativeBuiltinControlGetter.PreserveClassResult(attribute, 1));
    }

    [Fact]
    public void ClassicContentsTagsCopyCallerStorageAndNoNotifyIsOperationLocal()
    {
        var p = Create(out var obj);
        var follow = APTR.FromPointer(0x1300);
        p.WriteUInt32(follow, 0, 0x90000001);
        Assert.True(MuiNotifyCore.Add(ref p, State, obj,
            MuiCommonControlCore.StringContents, 1233727793, obj, 1, follow));
        var source = APTR.FromPointer(0x1400);
        p.WriteCString(source, "4096");
        var packet = Packet(ref p, BOOPSI.OM_SET,
            MuiCommonControlCore.StringContents, source.Raw, true);
        Assert.Equal(1u, MuiNativeBuiltinControlSetter.Dispatch(ref p, State,
            obj, packet, BOOPSI.OM_SET));
        Assert.Equal(0u, p.DispatchCount);
        p.WriteCString(source, "changed");
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, obj,
            MuiCommonControlCore.StringContents, out var copy));
        Assert.NotEqual(source.Raw, copy);
        Assert.Equal("4096", Text(ref p, APTR.FromPointer(copy)));
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, obj,
            MuiCommonControlCore.StringInteger, out var number));
        Assert.Equal(4096u, number);
        p.WriteCString(source, "8192");
        packet = Packet(ref p, BOOPSI.OM_SET,
            MuiCommonControlCore.StringContents, source.Raw, false);
        Assert.Equal(1u, MuiNativeBuiltinControlSetter.Dispatch(ref p, State,
            obj, packet, BOOPSI.OM_SET));
        Assert.Equal(1u, p.DispatchCount);
    }

    private static MuiHeadlessTestPlatform Create(out APTR obj)
    {
        var p = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, State);
        p.WriteCString(APTR.FromPointer(0x1100), "String.mui");
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, State));
        var cls = MuiHeadlessObjectCore.RegisterClass(ref p, State,
            APTR.FromPointer(0x1100), APTR.Null, 0, APTR.FromPointer(1), false);
        obj = MuiCommonControlCore.CreateControl(ref p, State, cls, APTR.Null);
        Assert.NotEqual(APTR.Null, obj);
        return p;
    }

    private static APTR Packet(ref MuiHeadlessTestPlatform p, uint method,
        uint attribute, uint value, bool noNotify)
    {
        var tags = APTR.FromPointer(0x1600);
        p.WriteUInt32(tags, 0, 0x804237F9);
        p.WriteUInt32(tags, 4, noNotify ? 1u : 0u);
        p.WriteUInt32(tags, 8, attribute);
        p.WriteUInt32(tags, 12, value);
        p.WriteUInt32(tags, 16, 0);
        p.WriteUInt32(tags, 20, 0);
        var packet = APTR.FromPointer(0x1500);
        p.WriteUInt32(packet, 0, method);
        if (method == MuiNotifyCore.SetMethod || method == MuiNotifyCore.NoNotifySetMethod)
        {
            p.WriteUInt32(packet, 4, attribute);
            p.WriteUInt32(packet, 8, value);
            return packet;
        }
        p.WriteUInt32(packet, 4, tags.Raw);
        p.WriteUInt32(packet, 8, 0);
        p.WriteUInt32(packet, 12, 0);
        return packet;
    }

    private static string Text(ref MuiHeadlessTestPlatform p, APTR address)
    {
        var text = "";
        for (var i = 0; i < 128; i++)
        {
            var c = p.ReadUInt8(address, i);
            if (c == 0) return text;
            text += (char)c;
        }
        throw new InvalidDataException("Unterminated test string");
    }
}
