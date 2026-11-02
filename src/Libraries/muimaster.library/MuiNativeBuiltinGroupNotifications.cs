/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

// Group.mui lets clients listen to children through their enclosing group.
// Keep the group's own registration and forward the same packet to each child.
// https://github.com/amiga-mui/muidev/wiki/MUI_Group
internal static class MuiNativeBuiltinGroupNotifications
{
    internal static bool TryForward<T>(ref T p, APTR state, APTR obj,
        APTR message, uint method, out uint result) where T : struct, IMuiHeadlessPlatform
    {
        result = method == MuiNotifyCore.NotifyMethod ? 1u : 0u;
        if (method != MuiNotifyCore.NotifyMethod && method != MuiNotifyCore.KillNotifyMethod &&
            method != MuiNotifyCore.KillNotifyObjectMethod) return false;
        if (!MuiGroupChangeCore.IsGroupObject(ref p, state, obj) ||
            MuiCommonControlCore.Classify(ref p, state, obj) != MuiControlClass.Unknown ||
            MuiListCore.Classify(ref p, state, obj) != MuiCollectionClass.Unknown) return false;
        for (var index = 0; index < 65535; index++)
        {
            var child = MuiFamilyCore.GetChild(ref p, state, obj, index, APTR.Null);
            if (child.IsNull) return true;
            var childResult = p.DoMethod(child, message);
            if (method == MuiNotifyCore.NotifyMethod)
            {
                if (childResult == 0) result = 0;
            }
            else result += childResult;
        }
        result = 0;
        return true;
    }
}
