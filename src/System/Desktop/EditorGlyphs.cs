namespace CopperOS.Desktop;

/// <summary>Original 5 by 7 glyphs, packed into an 8 by 8 cell; no ROM font data.</summary>
public static class EditorGlyphs
{
    public static byte Row(uint character, int row)
    {
        character &= 255;
        if (character >= 160) return Extended(character, row);
        uint top, bottom;
        switch ((int)character)
        {
            case 'a': top = 0x00001c02; bottom = 0x1e221e00; break;
            case 'b': top = 0x20203c22; bottom = 0x22223c00; break;
            case 'c': top = 0x00001c20; bottom = 0x20201c00; break;
            case 'd': top = 0x02021e22; bottom = 0x22221e00; break;
            case 'e': top = 0x00001c22; bottom = 0x3e201e00; break;
            case 'f': top = 0x0c121038; bottom = 0x10101000; break;
            case 'g': top = 0x00001e22; bottom = 0x221e021c; break;
            case 'h': top = 0x20203c22; bottom = 0x22222200; break;
            case 'i': top = 0x08001808; bottom = 0x08081c00; break;
            case 'j': top = 0x04000c04; bottom = 0x04042418; break;
            case 'k': top = 0x20202224; bottom = 0x38242200; break;
            case 'l': top = 0x18080808; bottom = 0x08081c00; break;
            case 'm': top = 0x0000342a; bottom = 0x2a2a2200; break;
            case 'n': top = 0x00003c22; bottom = 0x22222200; break;
            case 'o': top = 0x00001c22; bottom = 0x22221c00; break;
            case 'p': top = 0x00003c22; bottom = 0x223c2020; break;
            case 'q': top = 0x00001e22; bottom = 0x221e0202; break;
            case 'r': top = 0x00002c32; bottom = 0x20202000; break;
            case 's': top = 0x00001e20; bottom = 0x1c023c00; break;
            case 't': top = 0x10103c10; bottom = 0x10120c00; break;
            case 'u': top = 0x00002222; bottom = 0x22221e00; break;
            case 'v': top = 0x00002222; bottom = 0x22140800; break;
            case 'w': top = 0x00002222; bottom = 0x2a2a1400; break;
            case 'x': top = 0x00002214; bottom = 0x08142200; break;
            case 'y': top = 0x00002222; bottom = 0x221e021c; break;
            case 'z': top = 0x00003e04; bottom = 0x08103e00; break;
            case '0': top = 0x1c22262a; bottom = 0x32221c00; break;
            case '1': top = 0x08180808; bottom = 0x08081c00; break;
            case '2': top = 0x1c220204; bottom = 0x08103e00; break;
            case '3': top = 0x3c02021c; bottom = 0x02023c00; break;
            case '4': top = 0x040c1424; bottom = 0x3e040400; break;
            case '5': top = 0x3e20203c; bottom = 0x02023c00; break;
            case '6': top = 0x1c20203c; bottom = 0x22221c00; break;
            case '7': top = 0x3e020408; bottom = 0x10101000; break;
            case '8': top = 0x1c22221c; bottom = 0x22221c00; break;
            case '9': top = 0x1c22221e; bottom = 0x02021c00; break;
            case ' ': top = 0x00000000; bottom = 0x00000000; break;
            case 'A': top = 0x1c22223e; bottom = 0x22222200; break;
            case 'B': top = 0x3c22223c; bottom = 0x22223c00; break;
            case 'C': top = 0x1e202020; bottom = 0x20201e00; break;
            case 'D': top = 0x3c222222; bottom = 0x22223c00; break;
            case 'E': top = 0x3e20203c; bottom = 0x20203e00; break;
            case 'F': top = 0x3e20203c; bottom = 0x20202000; break;
            case 'G': top = 0x1e20202e; bottom = 0x22221e00; break;
            case 'H': top = 0x2222223e; bottom = 0x22222200; break;
            case 'I': top = 0x3e080808; bottom = 0x08083e00; break;
            case 'J': top = 0x0e040404; bottom = 0x24241800; break;
            case 'K': top = 0x22242830; bottom = 0x28242200; break;
            case 'L': top = 0x20202020; bottom = 0x20203e00; break;
            case 'M': top = 0x22362a2a; bottom = 0x22222200; break;
            case 'N': top = 0x22322a26; bottom = 0x22222200; break;
            case 'O': top = 0x1c222222; bottom = 0x22221c00; break;
            case 'P': top = 0x3c22223c; bottom = 0x20202000; break;
            case 'Q': top = 0x1c222222; bottom = 0x2a241a00; break;
            case 'R': top = 0x3c22223c; bottom = 0x28242200; break;
            case 'S': top = 0x1e20201c; bottom = 0x02023c00; break;
            case 'T': top = 0x3e080808; bottom = 0x08080800; break;
            case 'U': top = 0x22222222; bottom = 0x22221c00; break;
            case 'V': top = 0x22222222; bottom = 0x22140800; break;
            case 'W': top = 0x2222222a; bottom = 0x2a2a1400; break;
            case 'X': top = 0x22221408; bottom = 0x14222200; break;
            case 'Y': top = 0x22221408; bottom = 0x08080800; break;
            case 'Z': top = 0x3e020408; bottom = 0x10203e00; break;
            case '.': top = 0x00000000; bottom = 0x000c0c00; break;
            case ',': top = 0x00000000; bottom = 0x0c0c0800; break;
            case ':': top = 0x000c0c00; bottom = 0x0c0c0000; break;
            case ';': top = 0x000c0c00; bottom = 0x0c0c0800; break;
            case '-': top = 0x0000003e; bottom = 0x00000000; break;
            case '_': top = 0x00000000; bottom = 0x00003e00; break;
            case '+': top = 0x0008083e; bottom = 0x08080000; break;
            case '=': top = 0x00003e00; bottom = 0x3e000000; break;
            case '/': top = 0x02020408; bottom = 0x10202000; break;
            case '\\': top = 0x20201008; bottom = 0x04020200; break;
            case '>': top = 0x20100804; bottom = 0x08102000; break;
            case '<': top = 0x02040810; bottom = 0x08040200; break;
            case '!': top = 0x08080808; bottom = 0x08000800; break;
            case '?': top = 0x1c220204; bottom = 0x08000800; break;
            case '"': top = 0x14141400; bottom = 0x00000000; break;
            case '\'': top = 0x08080400; bottom = 0x00000000; break;
            case '`': top = 0x10080400; bottom = 0x00000000; break;
            case '(': top = 0x04081010; bottom = 0x10080400; break;
            case ')': top = 0x10080404; bottom = 0x04081000; break;
            case '[': top = 0x1c101010; bottom = 0x10101c00; break;
            case ']': top = 0x1c040404; bottom = 0x04041c00; break;
            case '{': top = 0x06080810; bottom = 0x08080600; break;
            case '}': top = 0x30080804; bottom = 0x08083000; break;
            case '*': top = 0x002a1c3e; bottom = 0x1c2a0000; break;
            case '#': top = 0x14143e14; bottom = 0x3e141400; break;
            case '%': top = 0x32340810; bottom = 0x2c0c0000; break;
            case '&': top = 0x18242810; bottom = 0x2a241a00; break;
            case '|': top = 0x08080808; bottom = 0x08080800; break;
            case '~': top = 0x0000122c; bottom = 0x00000000; break;
            case '^': top = 0x08142200; bottom = 0x00000000; break;
            case '$': top = 0x081e281c; bottom = 0x0a3c0800; break;
            case '@': top = 0x1c222e2a; bottom = 0x2e201c00; break;
            default: top = 0x3E222222; bottom = 0x22223E00; break;
        }
        return (byte)((row < 4 ? top : bottom) >> ((3 - (row & 3)) * 8));
    }

    private static byte Extended(uint c, int row)
    {
        if (c == 160) return 0;
        if (c == 199 || c == 231) return row == 7 ? (byte)8 : Row(c == 199 ? (byte)'C' : (byte)'c', row);
        uint letter = 0, accent = 0;
        if (c >= 192 && c <= 197) { letter = 'A'; accent = (uint)c - 192; }
        else if (c >= 224 && c <= 229) { letter = 'a'; accent = (uint)c - 224; }
        else if (c >= 200 && c <= 203) { letter = 'E'; accent = (uint)c - 200; if (accent == 3) accent = 4; }
        else if (c >= 232 && c <= 235) { letter = 'e'; accent = (uint)c - 232; if (accent == 3) accent = 4; }
        else if (c >= 204 && c <= 207) { letter = 'I'; accent = (uint)c - 204; if (accent == 3) accent = 4; }
        else if (c >= 236 && c <= 239) { letter = 'i'; accent = (uint)c - 236; if (accent == 3) accent = 4; }
        else if (c >= 210 && c <= 214) { letter = 'O'; accent = (uint)c - 210; }
        else if (c >= 242 && c <= 246) { letter = 'o'; accent = (uint)c - 242; }
        else if (c >= 217 && c <= 220) { letter = 'U'; accent = (uint)c - 217; if (accent == 3) accent = 4; }
        else if (c >= 249 && c <= 252) { letter = 'u'; accent = (uint)c - 249; if (accent == 3) accent = 4; }
        else if (c == 209 || c == 241) { letter = c == 209 ? (uint)'N' : 'n'; accent = 3; }
        else if (c == 221 || c == 253 || c == 255) { letter = c == 221 ? (uint)'Y' : 'y'; accent = c == 255 ? 4u : 1; }
        if (letter != 0)
        {
            if (row < 2)
            {
                if (accent == 0) return row == 0 ? (byte)16 : (byte)8;
                if (accent == 1) return row == 0 ? (byte)4 : (byte)8;
                if (accent == 2) return row == 0 ? (byte)8 : (byte)20;
                if (accent == 3) return row == 0 ? (byte)18 : (byte)44;
                if (accent == 4) return row == 0 ? (byte)20 : (byte)0;
                return row == 0 ? (byte)12 : (byte)18;
            }
            return Row((byte)letter, letter < 'a' ? row < 6 ? row - 2 : row - 1 : row);
        }
        uint top, bottom;
        switch ((int)c)
        {
            case 198: top = 0x1e28283e; bottom = 0x28282e00; break;
            case 230: top = 0x00003609; bottom = 0x3f283700; break;
            case 208: top = 0x1c12123a; bottom = 0x12121c00; break;
            case 240: top = 0x180c1a02; bottom = 0x1e221c00; break;
            case 216: top = 0x1c22262a; bottom = 0x32221c00; break;
            case 248: top = 0x00001c26; bottom = 0x2a321c00; break;
            case 222: top = 0x20203c22; bottom = 0x223c2000; break;
            case 254: top = 0x20203c22; bottom = 0x223c2020; break;
            case 223: top = 0x18242428; bottom = 0x242c2000; break;
            case 215: top = 0x00002214; bottom = 0x08142200; break;
            case 247: top = 0x00000800; bottom = 0x3e000800; break;
            default: top = 0x3e222222; bottom = 0x22223e00; break;
        }
        return (byte)((row < 4 ? top : bottom) >> ((3 - (row & 3)) * 8));
    }
}
