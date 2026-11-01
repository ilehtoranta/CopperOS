/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal interface IMuiBuiltinWindowCapability
{
    APTR OpenBuiltinWindow(APTR obj);
    void CloseBuiltinWindow(APTR window);
    bool PublishBuiltinWindow(APTR obj, APTR window);
}

// Own the Intuition capability separately from the caller's requested Open tag.
// Failed opens restore a closed state; successful opens publish the same handle
// to the typed headless lifetime and native Application event registry.
internal static class MuiNativeBuiltinWindow
{
    internal const uint NativeWindow = 0x7FFE0011;
    internal static bool Synchronize<T, TNative>(ref T p, ref TNative native,
        APTR state, APTR obj) where T : struct, IMuiHeadlessPlatform
        where TNative : struct, IMuiBuiltinWindowCapability
    {
        MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, MuiWindowPublicCore.Open, out var requested);
        MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, NativeWindow, out var handle);
        if ((requested != 0) == (handle != 0)) return true;
        // The raw setter has already recorded the request. Restore the last
        // valid lifecycle before invoking a provider or allocating typed state.
        if (!MuiHeadlessObjectCore.SetAttribute(ref p, state, obj,
            MuiWindowPublicCore.Open, handle == 0 ? 0u : 1u, false)) return false;
        var previous = APTR.FromPointer(handle);
        var opened = requested == 0 ? APTR.Null : native.OpenBuiltinWindow(obj);
        if (requested != 0 && opened.IsNull) return false;
        var lifecycle = new MuiWindowLifecycleStateRecord
        {
            Magic = MuiWindowLifecycleStateRecord.Cookie,
            NativeWindow = opened, Open = opened.IsNotNull ? 1u : 0u,
        };
        if (!MuiApplicationWindowCore.WriteWindowLifecycle(ref p, state, obj, lifecycle))
        {
            if (opened.IsNotNull) native.CloseBuiltinWindow(opened);
            return false;
        }
        if (!native.PublishBuiltinWindow(obj, opened))
        {
            lifecycle.NativeWindow = previous; lifecycle.Open = previous.IsNotNull ? 1u : 0u;
            MuiApplicationWindowCore.WriteWindowLifecycle(ref p, state, obj, lifecycle);
            if (opened.IsNotNull) native.CloseBuiltinWindow(opened);
            return false;
        }
        if (previous.IsNotNull) native.CloseBuiltinWindow(previous);
        return true;
    }
}
