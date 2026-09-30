using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Version 40.1 FULL suffix parser and formatter. The caller
/// supplies canonical numeric fields and retains the Utility library lease.
/// Every temporary record is on this invocation's stack; an optional Extra
/// copy is allocated to its exact first-line length and freed before return.
/// </summary>
public static class NativeWorkbench31VersionFull
{
    // Source: original C/Version SHA-256
    // dc2f55cd48b37bd1efecdbf07d38757463129dd9076ea6bf1b5d967f2189f224,
    // CODE 0x054c-0x0978 (parser), 0x0164-0x01e2 (FULL output).
    // Scratch: DateTime 0..25; date string 28..47; ClockData 48..61;
    // StrToLong LONG 64..67; VPrintf LONG arguments 68..79.
    private struct Scratch
    {
#pragma warning disable CS0649 // Accessed through native byte offsets.
        public uint A0, A1, A2, A3, A4, A5, A6, A7, A8, A9;
        public uint B0, B1, B2, B3, B4, B5, B6, B7, B8, B9;
#pragma warning restore CS0649

        public static APTR AddressOf(ref Scratch value) =>
            throw new System.NotSupportedException(
                "VersionFull.Scratch.AddressOf is lowered by CopperSharp.");
    }

    [AmigaLibrary(Utility.Name, AmigaLibraryBasePolicy.CallerProvided)]
    private static class DateUtilityApi
    {
        [AmigaLvo(UtilityLvo.Date2Amiga)]
        [return: M68kRegister(M68kRegister.D0)]
        public static extern uint Date2Amiga(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] APTR clockData);
    }

    public static int Print(APTR canonicalName, uint version, uint revision,
        APTR idString, APTR utilityBase)
    {
        var storage = default(Scratch);
        var scratch = Scratch.AddressOf(ref storage);
        var dateText = APTR.FromPointer(scratch.Raw + 28);
        var formatArgs = APTR.FromPointer(scratch.Raw + 68);
        var hasDate = ParseSuffix(idString, utilityBase, scratch,
            out var extraStart);
        var extra = APTR.Null;
        if (extraStart != 0)
        {
            var length = 0u;
            while (!IsLineEnd(ReadByte(extraStart + length))) length++;
            if (length != 0)
            {
                extra = Exec.AllocVec(length + 1,
                    (uint)Exec.MemoryFlags.Clear);
                if (extra.IsNull) return (int)DOS.Error.NoFreeStore;
                for (var index = 0u; index < length; index++)
                    APTR.WriteUInt8(extra, unchecked((int)index),
                        ReadByte(extraStart + index));
            }
        }

        APTR.WriteUInt32(formatArgs, 0, canonicalName.Raw);
        APTR.WriteUInt32(formatArgs, 4, version);
        APTR.WriteUInt32(formatArgs, 8, revision);
        DOS.VPrintf("%s %ld.%ld", formatArgs);
        if (hasDate)
        {
            APTR.WriteUInt8(scratch, DosLayout.DateTime.Format, 4);
            APTR.WriteUInt8(scratch, DosLayout.DateTime.Flags, 0);
            APTR.WriteUInt32(scratch, DosLayout.DateTime.Day, 0);
            APTR.WriteUInt32(scratch, DosLayout.DateTime.Date, dateText.Raw);
            APTR.WriteUInt32(scratch, DosLayout.DateTime.Time, 0);
            // The original calls DateToStr even though its return is ignored.
            DOS.DateToStr(scratch);
            APTR.WriteUInt32(formatArgs, 0, dateText.Raw);
            DOS.VPrintf(" (%s)", formatArgs);
        }
        if (extra.IsNotNull)
        {
            APTR.WriteUInt32(formatArgs, 0, extra.Raw);
            DOS.VPrintf("\n%s", formatArgs);
        }
        DOS.PutStr("\n");
        if (extra.IsNotNull) Exec.FreeVec(extra);
        // Source output calls do not turn their return values or IoErr into
        // command failure. Allocation failure above is the helper's error.
        return 0;
    }

    private static bool ParseSuffix(APTR idString, APTR utilityBase,
        APTR scratch, out uint extraStart)
    {
        extraStart = 0;
        if (idString.IsNull) return false;
        var cursor = idString.Raw;
        if (ReadByte(cursor) == '$' && ReadByte(cursor + 1) == 'V' &&
            ReadByte(cursor + 2) == 'E' && ReadByte(cursor + 3) == 'R' &&
            ReadByte(cursor + 4) == ':') cursor += 5;
        SkipSpaces(ref cursor);

        // Original name scanning consumes entire space-delimited tokens
        // until the next token starts with a decimal digit. No name cap.
        while (true)
        {
            while (ReadByte(cursor) != ' ' && !IsLineEnd(ReadByte(cursor)))
                cursor++;
            SkipSpaces(ref cursor);
            var value = ReadByte(cursor);
            if (IsLineEnd(value) || (value >= '0' && value <= '9')) break;
        }

        var number = APTR.FromPointer(scratch.Raw + 64);
        ReadNumber(ref cursor, number, out var ignoredMajor);
        SkipSpaces(ref cursor);
        extraStart = cursor;
        if (ReadByte(cursor) != '.') return false;
        cursor++;
        if (!ReadNumber(ref cursor, number, out var ignoredRevision))
            return false;

        // Text between a parsed revision and '(' is skipped rather than
        // treated as Extra. Without parentheses there is no date/Extra.
        while (!IsLineEnd(ReadByte(cursor)) && ReadByte(cursor) != '(')
            cursor++;
        extraStart = cursor;
        if (ReadByte(cursor) != '(') return false;

        var dateText = APTR.FromPointer(scratch.Raw + 28);
        var inner = cursor + 1;
        for (var index = 0u; index < 19; index++)
        {
            var value = ReadByte(inner + index);
            if (value == ')' || value == 0) break;
            APTR.WriteUInt8(dateText, unchecked((int)index), value);
        }
        // The cleared twentieth byte is the original fallback buffer's NUL.
        cursor = inner;
        var clock = APTR.FromPointer(scratch.Raw + 48);
        if (ReadNumber(ref cursor, number, out var day) &&
            ReadByte(cursor) == '.')
        {
            cursor++;
            if (ReadNumber(ref cursor, number, out var month) &&
                ReadByte(cursor) == '.')
            {
                cursor++;
                if (ReadNumber(ref cursor, number, out var year))
                {
                    if (ReadByte(cursor) == ')') cursor++;
                    SkipSpaces(ref cursor);
                    extraStart = cursor;
                    // ClockData is sec/min/hour/day/month/year/wday UWORDs.
                    // The source adds 1900 unconditionally, then narrows.
                    APTR.WriteUInt16(clock, 6, unchecked((ushort)day));
                    APTR.WriteUInt16(clock, 8, unchecked((ushort)month));
                    APTR.WriteUInt16(clock, 10,
                        unchecked((ushort)(year + 1900u)));
                    // Deliberately Date2Amiga (-126), not CheckDate (-132).
                    var seconds = DateUtilityApi.Date2Amiga(utilityBase, clock);
                    if (seconds != 0)
                    {
                        // Original 0x11b4 is signed LONG division, including
                        // signed remainders for Date2Amiga's high-bit values.
                        var signedSeconds = unchecked((int)seconds);
                        APTR.WriteUInt32(scratch, DosLayout.DateStamp.Days,
                            unchecked((uint)(signedSeconds / 86400)));
                        APTR.WriteUInt32(scratch, DosLayout.DateStamp.Minutes,
                            unchecked((uint)((signedSeconds % 86400) / 60)));
                        APTR.WriteUInt32(scratch, DosLayout.DateStamp.Ticks,
                            unchecked((uint)((signedSeconds % 60) * 50)));
                        return true;
                    }
                }
            }
        }

        // Zeroed DateTime, format DOS(0), with only dt_StrDate set. A
        // fallback-only date leaves Extra beginning at the original '('.
        // A fully parsed numeric date already advanced Extra even when the
        // Date2Amiga result was zero and this fallback is still required.
        APTR.WriteUInt32(scratch, DosLayout.DateTime.Date, dateText.Raw);
        return DOS.StrToDate(scratch) != 0;
    }

    private static bool ReadNumber(ref uint cursor, APTR number,
        out uint value)
    {
        var consumed = DOS.StrToLong(CString.FromPointer(cursor), number);
        value = 0;
        if (consumed <= 0) return false;
        value = APTR.ReadUInt32(number, 0);
        cursor += unchecked((uint)consumed);
        return true;
    }

    private static void SkipSpaces(ref uint cursor)
    {
        while (ReadByte(cursor) == ' ') cursor++;
    }

    private static byte ReadByte(uint address) =>
        APTR.ReadUInt8(APTR.FromPointer(address), 0);

    private static bool IsLineEnd(uint value) =>
        value == 0 || value == '\n' || value == '\r';
}
