/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

/// <summary>Catalog encodings defined by the MorphOS register-ABI catalog tags.</summary>
public static class CharsetCore
{
    public static bool Read<T>(ref T p, ref uint at, uint end, uint encoding, out uint code) where T : struct, ILocalePlatform
    {
        code = 0;
        if (at >= end) return false;
        if (encoding == 2)
        {
            if (end - at < 4) return false;
            code = ((uint)p.Read8(at) << 24) | ((uint)p.Read8(at + 1) << 16) | ((uint)p.Read8(at + 2) << 8) | p.Read8(at + 3); at += 4;
            return code <= 0x10ffff && (code < 0xd800 || code > 0xdfff);
        }
        code = p.Read8(at++);
        if (encoding == 0 || code < 128) return true;
        uint extra, minimum;
        if (code >= 194 && code <= 223) { extra = 1; minimum = 128; code &= 31; }
        else if (code >= 224 && code <= 239) { extra = 2; minimum = 2048; code &= 15; }
        else if (code >= 240 && code <= 244) { extra = 3; minimum = 65536; code &= 7; }
        else return false;
        if (end - at < extra) return false;
        while (extra-- != 0) { uint c = p.Read8(at++); if ((c & 192) != 128) return false; code = (code << 6) | (c & 63); }
        return code >= minimum && code <= 0x10ffff && (code < 0xd800 || code > 0xdfff);
    }
    public static uint Width(uint code, uint encoding) => encoding == 2 ? 4u : encoding == 0 || code < 128 ? 1u : code < 2048 ? 2u : code < 65536 ? 3u : 4u;
    public static void Write<T>(ref T p, ref uint at, uint code, uint encoding) where T : struct, ILocalePlatform
    {
        if (encoding == 2) { p.Write8(at++, (byte)(code >> 24)); p.Write8(at++, (byte)(code >> 16)); p.Write8(at++, (byte)(code >> 8)); p.Write8(at++, (byte)code); }
        else if (encoding == 0) p.Write8(at++, code <= 255 ? (byte)code : (byte)63);
        else if (code < 128) p.Write8(at++, (byte)code);
        else
        {
            uint n = Width(code, 1);
            p.Write8(at++, (byte)((n == 2 ? 192u : n == 3 ? 224u : 240u) | (code >> (int)((n - 1) * 6))));
            for (uint i = n - 1; i != 0;) { --i; p.Write8(at++, (byte)(128 | ((code >> (int)(i * 6)) & 63))); }
        }
    }
}
