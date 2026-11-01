/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiNativeBuiltinCollection
{
    internal static APTR RedrawTarget<T>(ref T p, APTR state, APTR obj)
        where T : struct, IMuiHeadlessPlatform
    {
        var kind = MuiListCore.Classify(ref p, state, obj);
        return kind == MuiCollectionClass.List ? obj :
            kind == MuiCollectionClass.Listview ? MuiListviewCore.ChildList(ref p, state, obj) : APTR.Null;
    }

    internal static bool TryDispatch<T>(ref T p, APTR state, APTR obj, APTR message, out uint result)
        where T : struct, IMuiLayoutPlatform
    {
        result = 0;
        var kind = MuiListCore.Classify(ref p, state, obj);
        return (kind == MuiCollectionClass.List || kind == MuiCollectionClass.Listview) &&
            MuiCollectionDispatcher.TryDispatchPacket(ref p, state, obj, message, out result);
    }
}
