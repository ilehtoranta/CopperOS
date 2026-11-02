using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeIntuiMessageDeliveryTests
{
    private static readonly APTR Original = APTR.FromPointer(0x1200);
    private static readonly APTR History = APTR.FromPointer(0x1400);

    [Fact]
    public void MouseCallbackUsesOwnedBytesAfterWindowCloseReclaimsOriginalMessage()
    {
        var p = Platform();
        IntuitionDrawingGuestCodec.WriteMessage(ref p, Original, new IntuiMessage
        {
            Class = IDCMPFlags.MouseButtons, Code = 0xE8, MouseX = 160, MouseY = 123,
            IDCMPWindow = APTR.FromPointer(0x1600), Seconds = 12, Micros = 34,
        });
        Assert.True(MuiNativeIntuiMessageDeliveryCore.TryPrepare(ref p, Original, out var delivery));
        Assert.NotEqual(Original, delivery.Message);
        // Simulate ReplyMsg followed by callback-driven CloseWindow freeing it.
        delivery.ReplyPending = 0;
        p.Clear(Original, IntuiMessage.Size);
        var copy = IntuitionDrawingGuestCodec.ReadMessage(ref p, delivery.Message);
        Assert.Equal(IDCMPFlags.MouseButtons, copy.Class);
        Assert.Equal((ushort)0xE8, copy.Code);
        Assert.Equal((short)160, copy.MouseX);
        Assert.Equal((short)123, copy.MouseY);
        Assert.Equal(12u, copy.Seconds);
        Assert.Equal(APTR.FromPointer(0x1600), copy.IDCMPWindow);
        var packet = APTR.FromPointer(0x1800);
        Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(ref p,
            delivery.Message, packet, out var eventClass));
        Assert.Equal((uint)IDCMPFlags.MouseButtons, eventClass);
        var frees = p.FreeCount;
        MuiNativeIntuiMessageDeliveryCore.Release(ref p, ref delivery);
        MuiNativeIntuiMessageDeliveryCore.Release(ref p, ref delivery);
        Assert.Equal(frees+1, p.FreeCount);
    }

    [Fact]
    public void RawKeyDeliveryOwnsTheFourDeadKeyHistoryBytes()
    {
        var p = Platform();
        p.WriteUInt32(History, 0, 0xA1B2C3D4);
        IntuitionDrawingGuestCodec.WriteMessage(ref p, Original, new IntuiMessage
        { Class = IDCMPFlags.RawKey, Code = 0x20, IAddress = History });
        Assert.True(MuiNativeIntuiMessageDeliveryCore.TryPrepare(ref p, Original, out var delivery));
        p.Clear(Original, IntuiMessage.Size); p.WriteUInt32(History, 0, 0);
        var copy = IntuitionDrawingGuestCodec.ReadMessage(ref p, delivery.Message);
        Assert.NotEqual(History, copy.IAddress);
        Assert.Equal(0xA1B2C3D4u, p.ReadUInt32(copy.IAddress, 0));
        Assert.True(MuiNativeApplicationWindowEvents.TryTranslateInputEvent(ref p,
            delivery.Message, out var input));
        Assert.Equal(unchecked((int)0xA1B2C3D4), input.Position);
        MuiNativeIntuiMessageDeliveryCore.Release(ref p, ref delivery);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtendedEnvelopeIsCopiedOnlyWhenItHasNoBorrowedTabletPayload(bool tablet)
    {
        var p = Platform();
        IntuitionDrawingGuestCodec.WriteMessage(ref p, Original, new IntuiMessage
        { Class = IDCMPFlags.MouseButtons, ExecMessage = new Message { Length = (ushort)ExtIntuiMessage.Size } });
        p.WriteUInt32(Original, (int)IntuiMessage.Size, tablet ? History.Raw : 0);
        Assert.True(MuiNativeIntuiMessageDeliveryCore.TryPrepare(ref p, Original, out var delivery));
        Assert.Equal(tablet, delivery.Allocation.IsNull);
        Assert.Equal(tablet, delivery.Message == Original);
        MuiNativeIntuiMessageDeliveryCore.Release(ref p, ref delivery);
    }

    [Fact]
    public void UnmappedRawKeyHistoryIsRejectedWithoutLosingTheOriginalReply()
    {
        var p = Platform();
        IntuitionDrawingGuestCodec.WriteMessage(ref p, Original, new IntuiMessage
        { Class = IDCMPFlags.RawKey, IAddress = APTR.FromPointer(0x90000) });
        Assert.False(MuiNativeIntuiMessageDeliveryCore.TryPrepare(ref p, Original, out var delivery));
        Assert.Equal(Original, delivery.Original);
        Assert.Equal(1u, delivery.ReplyPending);
        Assert.True(delivery.Allocation.IsNull);
    }

    [Theory]
    [InlineData(IDCMPFlags.MouseButtons)]
    [InlineData(IDCMPFlags.MouseMove)]
    [InlineData(IDCMPFlags.CloseWindow)]
    [InlineData(IDCMPFlags.RefreshWindow)]
    [InlineData(IDCMPFlags.ChangeWindow)]
    [InlineData(IDCMPFlags.ActiveWindow)]
    [InlineData(IDCMPFlags.InactiveWindow)]
    [InlineData(IDCMPFlags.NewSize)]
    [InlineData(IDCMPFlags.MenuPick)]
    [InlineData(IDCMPFlags.MenuHelp)]
    [InlineData(IDCMPFlags.DiskInserted)]
    [InlineData(IDCMPFlags.DiskRemoved)]
    [InlineData(IDCMPFlags.IntuiTicks)]
    [InlineData(IDCMPFlags.VanillaKey)]
    public void UndefinedIAddressCannotForceBorrowingOfAMouseOrWindowMessage(IDCMPFlags eventClass)
    {
        var p = Platform();
        var unused = APTR.FromPointer(0xF00000); // Deliberately unmapped; no payload for this class.
        IntuitionDrawingGuestCodec.WriteMessage(ref p, Original, new IntuiMessage
        { Class = eventClass, Code = 0xE8, IAddress = unused });
        Assert.True(MuiNativeIntuiMessageDeliveryCore.TryPrepare(ref p, Original, out var delivery));
        Assert.NotEqual(Original, delivery.Message);
        p.Clear(Original, IntuiMessage.Size);
        var copy = IntuitionDrawingGuestCodec.ReadMessage(ref p, delivery.Message);
        Assert.Equal(eventClass, copy.Class);
        Assert.Equal(unused, copy.IAddress); // Preserve opaque bits without reading them.
        Assert.Equal((ushort)0xE8, copy.Code);
        MuiNativeIntuiMessageDeliveryCore.Release(ref p, ref delivery);
    }

    [Theory]
    [InlineData(IDCMPFlags.GadgetDown)]
    [InlineData(IDCMPFlags.GadgetUp)]
    [InlineData(IDCMPFlags.IDCMPUpdate)]
    public void DefinedUncopiedProviderPayloadRetainsBorrowedDelivery(IDCMPFlags eventClass)
    {
        var p = Platform();
        IntuitionDrawingGuestCodec.WriteMessage(ref p, Original, new IntuiMessage
        { Class = eventClass, IAddress = History });
        Assert.True(MuiNativeIntuiMessageDeliveryCore.TryPrepare(ref p, Original, out var delivery));
        Assert.Equal(Original, delivery.Message);
        Assert.True(delivery.Allocation.IsNull);
        Assert.Equal(1u, delivery.ReplyPending);
        MuiNativeIntuiMessageDeliveryCore.Release(ref p, ref delivery);
    }

    private static MuiHeadlessTestPlatform Platform() => new(0x1000, 0x20000,
        0x4000, APTR.FromPointer(0x1000));
}
