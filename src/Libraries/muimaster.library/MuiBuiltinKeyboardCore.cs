/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiBuiltinKeyboardCore
{
    internal static uint Handle<T>(ref T p, APTR state, APTR window, APTR message,
        int muiKey, out APTR changed) where T : struct, IMuiLayoutPlatform
        => Handle(ref p, state, window, message, muiKey, out changed, out _, out _);

    internal static uint Handle<T>(ref T p, APTR state, APTR window, APTR message,
        int muiKey, out APTR changed, out bool contentsChanged, out bool acknowledged)
        where T : struct, IMuiLayoutPlatform
    {
        changed = APTR.Null;
        contentsChanged = false; acknowledged = false;
        if (!MuiIntuiMessageCodec.TryReadRawKeyRecord(ref p, message, out var input) ||
            MuiIntuiMessageCodec.IsRawKeyRelease(input.Code) ||
            !MuiHeadlessObjectCore.GetAttribute(ref p, state, window,
                MuiWindowPublicCore.ActiveObject, out var active) || active == 0) return 0;
        var obj = APTR.FromPointer(active);
        if (MuiCommonControlCore.Classify(ref p, state, obj) != MuiControlClass.String ||
            MuiApplicationWindowCore.FindContainingWindow(ref p, state, obj) != window ||
            !MuiNativeBuiltinPresentation.IsVisibleInTree(ref p, state, obj)) return 0;
        var parent = obj;
        for (var depth = 0; parent.IsNotNull && depth < 64; depth++,
            parent = MuiHeadlessObjectCore.ParentObject(ref p, state, parent))
        {
            if (MuiHeadlessObjectCore.GetAttribute(ref p, state, parent,
                MuiCommonControlCore.Disabled, out var disabled) && disabled != 0) return 0;
            if (parent == window) break;
        }
        if (parent != window) return 0;
        // Area shortcuts activate buttons. A focused String must receive the
        // printable key as text, including its own ControlChar. Real Return
        // and keypad Enter still issue the String accept command.
        var key = input.Code is 0x43 or 0x44 ? (int)MuiNativeHandleEventKey.Press :
            muiKey == (int)MuiNativeHandleEventKey.Press ? -1 : muiKey;
        MuiHeadlessObjectCore.GetRawAttributeGeneration(ref p, state, obj,
            MuiCommonControlCore.StringContents, out _, out var beforeContents);
        MuiHeadlessObjectCore.GetRawAttributeGeneration(ref p, state, obj,
            MuiCommonControlCore.StringAcknowledge, out _, out var beforeAcknowledge);
        var result = MuiCommonControlCore.HandleNativeBuiltinEvent(ref p, state, obj, message, key);
        if (result != 0)
        {
            changed = obj;
            contentsChanged = MuiHeadlessObjectCore.GetRawAttributeGeneration(ref p, state, obj,
                MuiCommonControlCore.StringContents, out _, out var afterContents) && afterContents != beforeContents;
            acknowledged = MuiHeadlessObjectCore.GetRawAttributeGeneration(ref p, state, obj,
                MuiCommonControlCore.StringAcknowledge, out _, out var afterAcknowledge) && afterAcknowledge != beforeAcknowledge;
        }
        return result;
    }
}
