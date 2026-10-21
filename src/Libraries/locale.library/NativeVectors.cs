/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using CopperSharp.Compiler;
namespace CopperOS.Locale;
public static partial class LocaleNativeLibrary
{
    [M68kExport("copperos.locale.ConvToLower")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint ConvToLower([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 0, character); }
    [M68kExport("copperos.locale.ConvToUpper")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint ConvToUpper([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 1, character); }
    [M68kExport("copperos.locale.IsAlNum")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsAlNum([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 4, character); }
    [M68kExport("copperos.locale.IsAlpha")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsAlpha([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 5, character); }
    [M68kExport("copperos.locale.IsCntrl")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsCntrl([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 6, character); }
    [M68kExport("copperos.locale.IsDigit")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsDigit([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 7, character); }
    [M68kExport("copperos.locale.IsGraph")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsGraph([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 8, character); }
    [M68kExport("copperos.locale.IsLower")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsLower([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 9, character); }
    [M68kExport("copperos.locale.IsPrint")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsPrint([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 10, character); }
    [M68kExport("copperos.locale.IsPunct")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsPunct([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 11, character); }
    [M68kExport("copperos.locale.IsSpace")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsSpace([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 12, character); }
    [M68kExport("copperos.locale.IsUpper")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsUpper([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 13, character); }
    [M68kExport("copperos.locale.IsXDigit")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint IsXDigit([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint character)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, locale), 14, character); }
    [M68kExport("copperos.locale.GetLocaleStr")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint GetLocaleStr([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.D0)] uint id)
    { var p = default(NativeLocalePlatform); return LocaleCore.String(ref p, Locale(ref p, library, locale), id); }
    [M68kExport("copperos.locale.StrnCmp")]
    [return: M68kRegister(M68kRegister.D0)]
    public static int StrnCmp([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.A1)] uint a, [M68kRegister(M68kRegister.A2)] uint b, [M68kRegister(M68kRegister.D0)] int length, [M68kRegister(M68kRegister.D1)] uint type)
    { var p = default(NativeLocalePlatform); return CharacterCore.Compare(ref p, Locale(ref p, library, locale), a, b, length, type); }
    [M68kExport("copperos.locale.StrConvert")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint StrConvert([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.A1)] uint text, [M68kRegister(M68kRegister.A2)] uint buffer, [M68kRegister(M68kRegister.D0)] uint size, [M68kRegister(M68kRegister.D1)] uint type)
    { var p = default(NativeLocalePlatform); return CharacterCore.Convert(ref p, Locale(ref p, library, locale), text, buffer, size, type); }
    [M68kExport("copperos.locale.FormatDate")]
    public static void FormatDate([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.A1)] uint format, [M68kRegister(M68kRegister.A2)] uint stamp, [M68kRegister(M68kRegister.A3)] uint hook)
    { var p = default(NativeLocalePlatform); DateCore.Format(ref p, Locale(ref p, library, locale), format, stamp, hook); }
    [M68kExport("copperos.locale.ParseDate")]
    [return: M68kRegister(M68kRegister.D0)]
    public static int ParseDate([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.A1)] uint stamp, [M68kRegister(M68kRegister.A2)] uint format, [M68kRegister(M68kRegister.A3)] uint hook)
    { var p = default(NativeLocalePlatform); return DateCore.Parse(ref p, Locale(ref p, library, locale), stamp, format, hook) ? 1 : 0; }
    [M68kExport("copperos.locale.FormatString")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint FormatString([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale, [M68kRegister(M68kRegister.A1)] uint format, [M68kRegister(M68kRegister.A2)] uint stream, [M68kRegister(M68kRegister.A3)] uint hook)
    { var p = Bind(library); return FormatCore.Format(ref p, Locale(ref p, library, locale), format, stream, hook); }
}
