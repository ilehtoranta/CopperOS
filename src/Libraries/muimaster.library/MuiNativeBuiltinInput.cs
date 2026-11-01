/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiNativeBuiltinInput
{
    internal static uint HandleWindow(ref MuiNativeBuiltinPlatform p, APTR window, APTR message)
    {
        if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref p, message, out var input)) return 0;
        APTR changed; bool notify;
        if (input.Class == (uint)IDCMPFlags.InactiveWindow)
        {
            var cancelled = MuiBuiltinPointerCore.Cancel(ref p, p.Registry, window, out changed);
            if (changed.IsNotNull) MuiNativeBuiltinPresentation.PublishInputChanges(ref p, changed, false);
            return cancelled ? 1u : 0u;
        }
        if (input.Class != (uint)IDCMPFlags.MouseButtons || (input.Code != 0x68 && input.Code != 0xE8)) return 0;
        var handled = MuiBuiltinPointerCore.Handle(ref p, p.Registry, window,
            input.MouseX, input.MouseY, input.Code == 0x68, out changed, out notify);
        if (changed.IsNotNull) MuiNativeBuiltinPresentation.PublishInputChanges(ref p, changed, notify);
        return handled ? 1u : 0u;
    }
    internal static void CancelWindow(ref MuiNativeBuiltinPlatform p, APTR window)
    {
        MuiBuiltinPointerCore.Cancel(ref p, p.Registry, window, out var changed);
        if (changed.IsNotNull) MuiNativeBuiltinPresentation.PublishInputChanges(ref p, changed, false);
    }
}
