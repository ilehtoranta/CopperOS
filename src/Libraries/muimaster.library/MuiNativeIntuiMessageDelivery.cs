/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal struct MuiNativeIntuiMessageDelivery
{
    internal APTR Original;
    internal APTR Message;
    internal APTR Allocation;
    internal uint ReplyPending;
}

// CloseWindow deallocates its IDCMP messages. Normal input callbacks may close
// their own window, so deliver owned bytes after replying to Intuition. Raw-key
// IAddress points at four bytes of dead-key history and must be copied as well.
// Opaque provider payloads retain the existing borrowed-message path; this
// bounded classic delivery does not reinterpret arbitrary IDCMPUPDATE tags or
// extended tablet messages.
internal static class MuiNativeIntuiMessageDeliveryCore
{
    private const uint AllocationSize = ExtIntuiMessage.Size + 4;

    internal static bool TryPrepare<T>(ref T platform, APTR original,
        out MuiNativeIntuiMessageDelivery delivery)
        where T : struct, IMuiAllocationPlatform
    {
        delivery = default;
        delivery.Original = original;
        delivery.Message = original;
        delivery.ReplyPending = 1;
        if (original.IsNull || !platform.IsMapped(original, IntuiMessage.Size))
            return false;
        var message = IntuitionDrawingGuestCodec.ReadMessage(ref platform, original);
        var rawKey = (uint)message.Class == MuiIntuiMessageCodec.RawKeyClass;
        if (message.ExecMessage.Length > IntuiMessage.Size &&
            (message.ExecMessage.Length != ExtIntuiMessage.Size ||
             !platform.IsMapped(original, ExtIntuiMessage.Size) ||
             platform.ReadUInt32(original, (int)IntuiMessage.Size) != 0))
            return true;
        // Classic IAddress has meaning only for gadget, RAWKEY and UPDATE
        // messages. Intuition may leave unrelated values in mouse/window
        // messages; those values must not turn safe envelope delivery into
        // a borrowed message that a CloseWindow callback can invalidate.
        // https://wiki.amigaos.net/wiki/Intuition_Input_and_Output_Methods
        if (!rawKey && message.IAddress.IsNotNull &&
            !HasNoClassicAddressPayload(message.Class)) return true;
        if (rawKey && message.IAddress.IsNotNull &&
            !platform.IsMapped(message.IAddress, 4)) return false;
        var copy = platform.Allocate(AllocationSize, MuiHeadlessLayout.AllocationFlags);
        if (copy.IsNull || !platform.IsMapped(copy, AllocationSize))
        {
            if (copy.IsNotNull) platform.Free(copy, AllocationSize);
            return false;
        }
        delivery.Allocation = copy;
        delivery.Message = copy;
        if (rawKey && message.IAddress.IsNotNull)
        {
            var history = platform.ReadUInt32(message.IAddress, 0);
            message.IAddress = APTR.FromPointer(copy.Raw + ExtIntuiMessage.Size);
            platform.WriteUInt32(message.IAddress, 0, history);
        }
        IntuitionDrawingGuestCodec.WriteMessage(ref platform, copy, message);
        return true;
    }

    private static bool HasNoClassicAddressPayload(IDCMPFlags eventClass) =>
        eventClass is IDCMPFlags.NewSize or IDCMPFlags.RefreshWindow or
        IDCMPFlags.MouseButtons or IDCMPFlags.MouseMove or
        IDCMPFlags.CloseWindow or IDCMPFlags.ActiveWindow or
        IDCMPFlags.InactiveWindow or IDCMPFlags.ChangeWindow or
        IDCMPFlags.MenuPick or IDCMPFlags.MenuHelp or
        IDCMPFlags.DiskInserted or IDCMPFlags.DiskRemoved or
        IDCMPFlags.IntuiTicks or IDCMPFlags.VanillaKey;

    internal static void Release<T>(ref T platform,
        ref MuiNativeIntuiMessageDelivery delivery)
        where T : struct, IMuiAllocationPlatform
    {
        if (delivery.Allocation.IsNotNull)
            platform.Free(delivery.Allocation, AllocationSize);
        delivery = default;
    }
}
