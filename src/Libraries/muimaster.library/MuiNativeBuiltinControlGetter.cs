/* SPDX-License-Identifier: MIT */
namespace CopperOS.MuiMaster;

internal static class MuiNativeBuiltinControlGetter
{
    internal static bool PreserveClassResult(uint attribute, uint result)
    {
        // Window.Open reports the actual native handle, not its requested tag.
        if (attribute == MuiWindowPublicCore.Open) return true;
        // String owns its text and derives its integer from that text. The raw
        // public tag sidecar can still reference a caller buffer which has since
        // changed, or omit text synthesized by String.Integer. This also covers
        // a Popstring forwarding its child's successful String.Contents getter.
        // Failed getters retain the native facade fallback; Window.Window still
        // projects the handle and applies any deferred IDCMP requests.
        return result != 0 &&
            (attribute == MuiCommonControlCore.StringContents ||
             attribute == MuiCommonControlCore.StringInteger);
    }
}
