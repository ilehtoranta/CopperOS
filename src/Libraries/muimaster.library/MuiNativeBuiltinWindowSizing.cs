/* SPDX-License-Identifier: MIT */
namespace CopperOS.MuiMaster;

internal static class MuiNativeBuiltinWindowSizing
{
    internal static int PreferredExtent(int requested, int current, int minimum,
        int preferred, int borders, int available)
    {
        if (requested > 0) return current;
        if (current <= 0 || available <= 0) return 0;
        var content = minimum > preferred ? minimum : preferred;
        var wanted = content + borders;
        if (wanted < current) wanted = current;
        return wanted > available ? available : wanted;
    }
}
