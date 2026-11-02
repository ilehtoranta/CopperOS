/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

// The native Group owns the String/Button objects through normal Family
// topology, so layout, native input and recursive teardown see the same tree.
internal static class MuiNativeBuiltinPopstring
{
    internal static bool TryGetString<T>(ref T p, APTR state, APTR obj, uint attribute,
        out uint value) where T : struct, IMuiHeadlessPlatform
    {
        value = 0;
        if (attribute != MuiCommonControlCore.StringContents) return false;
        var cls = MuiHeadlessObjectCore.ObjectClassRecord(ref p, state, obj);
        if (!MuiHeadlessClassCodec.TryRead(ref p, cls, out var metadata)) return false;
        var kind = MuiPopSpecialistCore.ClassifyName(ref p, metadata.Name);
        if (kind != MuiPopSpecialistClass.Popstring && kind != MuiPopSpecialistClass.Popasl) return false;
        return MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, MuiPopAttributes.Popstring_String, out var text) &&
            text != 0 && MuiHeadlessObjectCore.GetAttribute(ref p, state, APTR.FromPointer(text), attribute, out value);
    }

    internal static bool Construct<T>(ref T p, APTR state, APTR obj)
        where T : struct, IMuiHeadlessPlatform
    {
        var cls = MuiHeadlessObjectCore.ObjectClassRecord(ref p, state, obj);
        if (!MuiHeadlessClassCodec.TryRead(ref p, cls, out var metadata)) return false;
        var kind = MuiPopSpecialistCore.ClassifyName(ref p, metadata.Name);
        if (kind != MuiPopSpecialistClass.Popstring && kind != MuiPopSpecialistClass.Popasl) return true;
        if (!MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, MuiPopAttributes.Popstring_String, out var text) ||
            !MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, MuiPopAttributes.Popstring_Button, out var button) ||
            text == 0 || button == 0 || text == button) return false;
        // These tags carry adopted children rather than ordinary scalar values.
        // Constructor failure is unwound by the owning native object dispatcher.
        if (!MuiFamilyCore.AddTail(ref p, state, obj, APTR.FromPointer(text)) ||
            !MuiFamilyCore.AddTail(ref p, state, obj, APTR.FromPointer(button))) return false;
        const uint groupHorizontal = 0x8042536B;
        if (kind == MuiPopSpecialistClass.Popasl &&
            !MuiHeadlessObjectCore.SetAttribute(ref p, state, obj, MuiPopAttributes.Popasl_Active, 0, false)) return false;
        return MuiHeadlessObjectCore.SetAttribute(ref p, state, obj, groupHorizontal, 1, false);
    }
}
