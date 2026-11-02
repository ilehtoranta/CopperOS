/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiNativeBuiltinInput
{
    internal static uint HandleWindow(ref MuiNativeBuiltinPlatform p, APTR window, APTR message, int muiKey)
    {
        if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref p, message, out var input)) return 0;
        APTR changed; bool notify;
        if (input.Class == (uint)IDCMPFlags.InactiveWindow)
        {
            var cancelled = MuiBuiltinPointerCore.Cancel(ref p, p.Registry, window, out changed);
            if (changed.IsNotNull) MuiNativeBuiltinPresentation.PublishInputChanges(ref p, changed, false);
            return cancelled ? 1u : 0u;
        }
        if (input.Class == MuiIntuiMessageCodec.RawKeyClass)
        {
            var result = MuiBuiltinKeyboardCore.Handle(ref p, p.Registry, window, message, muiKey,
                out changed, out var contentsChanged, out var acknowledged);
            if (changed.IsNotNull)
            {
                MuiNativeBuiltinPresentation.PublishInputChanges(ref p, changed, true, contentsChanged);
                if (acknowledged) MuiNativeBuiltinPresentation.PublishStringAcknowledge(ref p, changed);
                MuiNativeBuiltinPresentation.DrawObject(ref p, changed, 0);
            }
            return result;
        }
        if (input.Class != (uint)IDCMPFlags.MouseButtons || (input.Code != 0x68 && input.Code != 0xE8)) return 0;
        var handled = MuiBuiltinPointerCore.Handle(ref p, p.Registry, window,
            input.MouseX, input.MouseY, input.Code == 0x68, out changed, out notify);
        if (input.Code == 0x68 && changed.IsNotNull &&
            MuiCommonControlCore.Classify(ref p, p.Registry, changed) == MuiControlClass.String)
        {
            var native = p.Native;
            if (MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref p, p.Context, out var objects))
                MuiNativePublicObjectCore.SetAttribute(ref native, objects, native.OwnerRoot,
                    window, MuiWindowPublicCore.ActiveObject, changed.Raw, true);
            MuiNativeBuiltinPresentation.DrawObject(ref p, changed, 0);
        }
        if (changed.IsNotNull) MuiNativeBuiltinPresentation.PublishInputChanges(ref p, changed, notify);
        if (changed.IsNotNull && notify && input.Code == 0xE8)
            MuiNativeBuiltinPopasl.ReleasedButton(ref p, changed);
        return handled ? 1u : 0u;
    }
    internal static void CancelWindow(ref MuiNativeBuiltinPlatform p, APTR window)
    {
        MuiBuiltinPointerCore.Cancel(ref p, p.Registry, window, out var changed);
        if (changed.IsNotNull) MuiNativeBuiltinPresentation.PublishInputChanges(ref p, changed, false);
    }
}
