/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

// Native controls have a drawing capability and must enter the class setter,
// including String's owned text/integer projections. Preserve the shared tag
// parser and operation-local NoNotify policy instead of storing only scalars.
internal static class MuiNativeBuiltinControlSetter
{
    internal static uint Dispatch<T>(ref T platform, APTR state, APTR obj,
        APTR message, uint method) where T : struct, IMuiLayoutPlatform
    {
        APTR tags;
        if (method == BOOPSI.OM_SET)
        {
            if (!MuiHeadlessOmSetMessageCodec.TryRead(ref platform, message,
                out var packet)) return 0;
            tags = packet.Attributes;
        }
        else if (method == BOOPSI.OM_UPDATE)
        {
            if (!MuiHeadlessOmUpdateMessageCodec.TryRead(ref platform, message,
                out var packet)) return 0;
            tags = packet.Attributes;
        }
        else return MuiCommonControlDispatcher.Dispatch(ref platform, state,
            obj, message);
        return MuiHeadlessObjectCore.SetControlAttributes(ref platform, state,
            obj, tags) ? 1u : 0u;
    }
}
