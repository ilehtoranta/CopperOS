using static CopperOS.Desktop.DesktopMemory;

namespace CopperOS.Desktop;

/// <summary>Byte-preserving bounded text; CRLF is one cursor/deletion unit.</summary>
public static class EditorBuffer
{
    public const int Length = 0, Cursor = 4, Top = 8, Left = 12, Modified = 16, Directory = 20,
        Line = 24, Column = 28, Preferred = 32, Ending = 36, PaintFrom = 40, PaintTo = 44,
        PaintedLine = 48, PaintedColumn = 52, PaintedTop = 56, PaintedLeft = 60,
        Name = 64, Path = 172, Scratch = 448, ScratchCount = 520, Original = 528, Data = 544;
    public const uint Capacity = 16384, Rows = 16, Columns = 72;

    public static void Initialize(uint p)
    { L(p, Ending, 1); L(p, PaintFrom, 0); L(p, PaintTo, uint.MaxValue); L(p, PaintedTop, uint.MaxValue); }

    public static bool Validate(uint p)
    {
        var length = L(p, Length); uint ending = 0;
        for (var i = 0u; i < length; i++)
        {
            var b = Byte(p, i);
            if (b == 13 || b == 10)
            { if (ending == 0) ending = b == 10 ? 1u : i + 1 < length && Byte(p, i + 1) == 10 ? 2u : 3u; }
            else if (b != 9 && (b < 32 || b >= 127 && b < 160)) return false;
        }
        L(p, Ending, ending == 0 ? 1u : ending); Recompute(p, true); return true;
    }

    public static uint Byte(uint p, uint at)
    {
        return (uint)B(p, Data + (int)at) & 255u;
    }
    private static void Byte(uint p, uint at, uint value) => B(p, Data + (int)at, (byte)value);

    public static void Recompute(uint p, bool preferred)
    {
        var end = L(p, Cursor); uint line = 0, column = 0;
        for (var i = 0u; i < end; i++)
        {
            var b = Byte(p, i);
            if (b == 13 || b == 10)
            { if (b == 13 && i + 1 < end && Byte(p, i + 1) == 10) i++; line++; column = 0; }
            else column = b == 9 ? (column + 4) & ~3u : column + 1;
        }
        L(p, Line, line); L(p, Column, column); if (preferred) L(p, Preferred, column);
        var top = L(p, Top); if (line < top) L(p, Top, line); else if (line >= top + Rows) L(p, Top, line - Rows + 1);
        var left = L(p, Left); if (column < left) L(p, Left, column); else if (column >= left + Columns) L(p, Left, column - Columns + 1);
    }

    public static uint LineStart(uint p, uint wanted)
    {
        var end = L(p, Length); uint line = 0, i = 0;
        while (i < end && line < wanted)
        {
            var b = Byte(p, i++);
            if (b == 13 || b == 10) { if (b == 13 && i < end && Byte(p, i) == 10) i++; line++; }
        }
        return i;
    }

    public static uint EndOfLine(uint p, uint start)
    { var end = L(p, Length); while (start < end && Byte(p, start) != 10 && Byte(p, start) != 13) start++; return start; }

    public static void Horizontal(uint p, int delta)
    {
        var at = L(p, Cursor); var end = L(p, Length);
        if (delta < 0 && at != 0) { at--; if (at != 0 && Byte(p, at) == 10 && Byte(p, at - 1) == 13) at--; }
        else if (delta > 0 && at < end) { at += Byte(p, at) == 13 && at + 1 < end && Byte(p, at + 1) == 10 ? 2u : 1u; }
        L(p, Cursor, at); Recompute(p, true);
    }

    public static void Place(uint p, uint line, uint column)
    {
        var at = LineStart(p, line); var end = EndOfLine(p, at); uint visual = 0;
        while (at < end)
        {
            var next = Byte(p, at) == 9 ? (visual + 4) & ~3u : visual + 1;
            if (next > column) break; visual = next; at++;
        }
        L(p, Cursor, at); Recompute(p, false);
    }

    public static void Vertical(uint p, int delta)
    {
        var line = (int)L(p, Line) + delta; if (line < 0) line = 0;
        Place(p, (uint)line, L(p, Preferred));
    }

    public static void HomeEnd(uint p, bool end, bool document)
    {
        var at = document ? end ? L(p, Length) : 0u : LineStart(p, L(p, Line));
        if (end && !document) at = EndOfLine(p, at);
        L(p, Cursor, at); Recompute(p, true);
    }

    private static void Paint(uint p, uint from, bool shifted)
    {
        if (from < L(p, PaintFrom)) L(p, PaintFrom, from);
        var to = shifted ? uint.MaxValue : from; if (to > L(p, PaintTo)) L(p, PaintTo, to);
    }

    public static bool Insert(uint p, uint code)
    {
        var newline = code == 13 || code == 10;
        var amount = newline && L(p, Ending) == 2 ? 2u : 1u;
        var length = L(p, Length); if (length + amount > Capacity) return false;
        var at = L(p, Cursor); var line = L(p, Line);
        for (var i = length; i > at; i--) Byte(p, i + amount - 1, Byte(p, i - 1));
        Byte(p, at, newline ? L(p, Ending) == 1 ? (byte)10 : (byte)13 : (byte)code);
        if (amount == 2) Byte(p, at + 1, 10);
        L(p, Length, length + amount); Byte(p, length + amount, 0); L(p, Cursor, at + amount);
        L(p, Modified, 1); Paint(p, line, newline); Recompute(p, true); return true;
    }

    public static void Erase(uint p, bool backward)
    {
        var at = L(p, Cursor); var length = L(p, Length); var line = L(p, Line);
        if (backward) { if (at == 0) return; at--; if (at > 0 && Byte(p, at) == 10 && Byte(p, at - 1) == 13) at--; }
        if (at == length) return;
        var b = Byte(p, at); var amount = b == 13 && at + 1 < length && Byte(p, at + 1) == 10 ? 2u : 1u;
        for (var i = at; i + amount < length; i++) Byte(p, i, Byte(p, i + amount));
        L(p, Length, length - amount); Byte(p, length - amount, 0); L(p, Cursor, at); L(p, Modified, 1);
        Recompute(p, true); var now = L(p, Line); Paint(p, now < line ? now : line, b == 10 || b == 13);
    }

    // Build one visible row, preserving tabs and original bytes in the document.
    public static uint Row(uint p, uint start)
    {
        for (var i = 0; i < (int)Columns; i++) B(p, Scratch + i, 32);
        var end = EndOfLine(p, start); var left = L(p, Left); uint col = 0, count = 0;
        while (start < end)
        {
            var b = Byte(p, start++);
            if (b == 9) { col = (col + 4) & ~3u; continue; }
            if (col >= left && col < left + Columns)
            { var x = col - left; B(p, Scratch + (int)x, (byte)b); if (b != 32) count = x + 1; }
            col++;
        }
        L(p, ScratchCount, count);
        if (end < L(p, Length)) { var b = Byte(p, end++); if (b == 13 && end < L(p, Length) && Byte(p, end) == 10) end++; }
        return end;
    }
}
