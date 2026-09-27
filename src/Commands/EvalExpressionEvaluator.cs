using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands;

/// <summary>Outcome classes for the bounded MorphOS Eval expression candidate.</summary>
public enum EvalExpressionStatus : byte
{
    Success,
    Malformed,
    Overflow,
    DivideByZero,
    InvalidShift,
}

/// <summary>
/// Independently written bounded integer evaluator for the source-observed
/// MorphOS Eval grammar. It owns no storage and reads only the supplied guest
/// span. Undefined source arithmetic is rejected with an explicit status.
/// </summary>
public static class EvalExpressionEvaluator
{
    /// <summary>
    /// Evaluates one complete expression. Decimal, hexadecimal (<c>0x</c> and
    /// <c>#x</c>), octal, byte-character and the documented arithmetic/bitwise
    /// operators are accepted. The caller retains input and output ownership.
    /// </summary>
    public static bool TryEvaluate<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        value = 0;
        status = EvalExpressionStatus.Malformed;
        if (source.IsNull || source.Raw > uint.MaxValue - sourceLength ||
            !memory.IsMapped(source, sourceLength)) return false;

        var cursor = new Cursor(sourceLength);
        if (!TryParseShift(ref memory, source, ref cursor, out value,
                out status)) return false;
        SkipWhitespace(ref memory, source, ref cursor);
        if (cursor.Position != sourceLength)
        {
            status = EvalExpressionStatus.Malformed;
            value = 0;
            return false;
        }
        status = EvalExpressionStatus.Success;
        return true;
    }

    /// <summary>
    /// Evaluates the small classic arithmetic subset currently measured from
    /// Workbench 3.1. Addition, subtraction, multiplication, division and
    /// remainder have one left-associative level; parentheses still group.
    /// This is intentionally separate from the MorphOS grammar.
    /// </summary>
    public static bool TryEvaluateWorkbench31Arithmetic<TMemory>(ref TMemory memory,
        APTR source, uint sourceLength, out long value,
        out EvalExpressionStatus status) where TMemory : struct, IAmigaGuestMemory
    {
        value = 0;
        status = EvalExpressionStatus.Malformed;
        if (source.IsNull || source.Raw > uint.MaxValue - sourceLength ||
            !memory.IsMapped(source, sourceLength)) return false;

        var cursor = new Cursor(sourceLength);
        if (!TryParseWorkbench31Arithmetic(ref memory, source, ref cursor, out value,
                out status)) return false;
        // Workbench 3.1 accepts the evaluated prefix: captured `2^3`,
        // `2 junk`, and `2+3junk` return 2, 2, and 5 respectively. Do not
        // apply the strict whole-span requirement used by the MorphOS parser.
        status = EvalExpressionStatus.Success;
        return true;
    }

    private static bool TryParseWorkbench31Arithmetic<TMemory>(ref TMemory memory,
        APTR source, ref Cursor cursor, out long value,
        out EvalExpressionStatus status) where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseWorkbench31Primary(ref memory, source, ref cursor, out value,
                out status)) return false;
        while (TryReadWorkbench31ArithmeticOperator(ref memory, source, ref cursor,
            out var op))
        {
            if (!TryParseWorkbench31Primary(ref memory, source, ref cursor,
                    out var right, out status)) return false;
            if (op == Operator.Add || op == Operator.Subtract)
            {
                if (!TryAdd(value, right, op == Operator.Subtract, out value))
                {
                    status = EvalExpressionStatus.Overflow;
                    return false;
                }
            }
            else if (op == Operator.Multiply)
            {
                if (!TryMultiply(value, right, out value))
                {
                    status = EvalExpressionStatus.Overflow;
                    return false;
                }
            }
            else if (op == Operator.Divide || op == Operator.Modulo)
            {
                if (IsZero(right))
                {
                    status = EvalExpressionStatus.DivideByZero;
                    return false;
                }
                if (IsMinimum(value) && IsNegativeOne(right))
                {
                    status = EvalExpressionStatus.Overflow;
                    return false;
                }
                value = DivideOrRemainder(value, right, op == Operator.Modulo);
            }
            else if (op == Operator.And) value = And(value, right);
            else if (op == Operator.Or) value = Or(value, right);
            else if (op == Operator.Xor) value = Xor(value, right);
            else if (op == Operator.Equivalence) value = Not(Xor(value, right));
            else if (!TryGetShiftAmount(right, out var amount))
            {
                status = EvalExpressionStatus.InvalidShift;
                return false;
            }
            else if (op == Operator.LeftShift)
            {
                if (!TryShiftLeft(value, amount, out value))
                {
                    status = EvalExpressionStatus.Overflow;
                    return false;
                }
            }
            else value = ShiftRight(value, amount);
        }
        return true;
    }

    private static bool TryParseWorkbench31Primary<TMemory>(ref TMemory memory,
        APTR source, ref Cursor cursor, out long value,
        out EvalExpressionStatus status) where TMemory : struct, IAmigaGuestMemory
    {
        SkipWhitespace(ref memory, source, ref cursor);
        if (TryConsume(ref memory, source, ref cursor, (byte)'-'))
        {
            if (!TryParseWorkbench31Primary(ref memory, source, ref cursor,
                    out value, out status)) return false;
            if (IsMinimum(value))
            {
                value = 0;
                status = EvalExpressionStatus.Overflow;
                return false;
            }
            value = Negated(value);
            return true;
        }
        if (TryConsume(ref memory, source, ref cursor, (byte)'~'))
        {
            if (!TryParseWorkbench31Primary(ref memory, source, ref cursor,
                    out value, out status)) return false;
            value = Not(value);
            return true;
        }
        if (TryConsume(ref memory, source, ref cursor, (byte)'('))
        {
            if (!TryParseWorkbench31Arithmetic(ref memory, source, ref cursor,
                    out value, out status) || !TryConsume(ref memory, source,
                    ref cursor, (byte)')'))
            {
                status = EvalExpressionStatus.Malformed;
                value = 0;
                return false;
            }
            return true;
        }
        return TryParseNumber(ref memory, source, ref cursor, out value, out status);
    }

    private static bool TryReadWorkbench31ArithmeticOperator<TMemory>(
        ref TMemory memory, APTR source, ref Cursor cursor, out Operator op)
        where TMemory : struct, IAmigaGuestMemory
    {
        var checkpoint = cursor;
        if (TryReadOperator(ref memory, source, ref cursor, out op) &&
            (op == Operator.Add || op == Operator.Subtract ||
             op == Operator.Multiply || op == Operator.Divide ||
             op == Operator.Modulo || op == Operator.And || op == Operator.Or ||
             op == Operator.Xor || op == Operator.Equivalence ||
             op == Operator.LeftShift || op == Operator.RightShift)) return true;
        cursor = checkpoint;
        op = Operator.None;
        return false;
    }

    private static bool TryParseShift<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseEquivalence(ref memory, source, ref cursor, out value,
                out status)) return false;
        while (TryReadOneOf(ref memory, source, ref cursor, Operator.LeftShift,
            Operator.RightShift, out var op))
        {
            if (!TryParseEquivalence(ref memory, source, ref cursor, out var right,
                    out status)) return false;
            if (!TryGetShiftAmount(right, out var amount))
            {
                status = EvalExpressionStatus.InvalidShift;
                return false;
            }
            if (op == Operator.LeftShift)
            {
                if (!TryShiftLeft(value, amount, out value))
                {
                    status = EvalExpressionStatus.Overflow;
                    return false;
                }
            }
            else value = ShiftRight(value, amount);
        }
        return true;
    }

    private static bool TryParseEquivalence<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseOr(ref memory, source, ref cursor, out value, out status)) return false;
        while (TryReadExpected(ref memory, source, ref cursor, Operator.Equivalence))
        {
            if (!TryParseOr(ref memory, source, ref cursor, out var right, out status)) return false;
            value = Not(Xor(value, right));
        }
        return true;
    }

    private static bool TryParseOr<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseXor(ref memory, source, ref cursor, out value, out status)) return false;
        while (TryReadExpected(ref memory, source, ref cursor, Operator.Or))
        {
            if (!TryParseXor(ref memory, source, ref cursor, out var right, out status)) return false;
            value = Or(value, right);
        }
        return true;
    }

    private static bool TryParseXor<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseAnd(ref memory, source, ref cursor, out value, out status)) return false;
        while (TryReadExpected(ref memory, source, ref cursor, Operator.Xor))
        {
            if (!TryParseAnd(ref memory, source, ref cursor, out var right, out status)) return false;
            value = Xor(value, right);
        }
        return true;
    }

    private static bool TryParseAnd<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseAdditive(ref memory, source, ref cursor, out value, out status)) return false;
        while (TryReadExpected(ref memory, source, ref cursor, Operator.And))
        {
            if (!TryParseAdditive(ref memory, source, ref cursor, out var right, out status)) return false;
            value = And(value, right);
        }
        return true;
    }

    private static bool TryParseAdditive<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseMultiplicative(ref memory, source, ref cursor, out value,
                out status)) return false;
        while (TryReadOneOf(ref memory, source, ref cursor, Operator.Add,
            Operator.Subtract, out var op))
        {
            if (!TryParseMultiplicative(ref memory, source, ref cursor, out var right,
                    out status)) return false;
            if (!TryAdd(value, right, op == Operator.Subtract, out value))
            {
                status = EvalExpressionStatus.Overflow;
                return false;
            }
        }
        return true;
    }

    private static bool TryParseMultiplicative<TMemory>(ref TMemory memory,
        APTR source, ref Cursor cursor, out long value,
        out EvalExpressionStatus status) where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParseUnary(ref memory, source, ref cursor, out value, out status)) return false;
        while (TryReadOneOf(ref memory, source, ref cursor, Operator.Multiply,
            Operator.Divide, Operator.Modulo, out var op))
        {
            if (!TryParseUnary(ref memory, source, ref cursor, out var right,
                    out status)) return false;
            if (op == Operator.Multiply)
            {
                if (!TryMultiply(value, right, out value))
                {
                    status = EvalExpressionStatus.Overflow;
                    return false;
                }
            }
            else if (IsZero(right))
            {
                status = EvalExpressionStatus.DivideByZero;
                return false;
            }
            else if (IsMinimum(value) && IsNegativeOne(right))
            {
                status = EvalExpressionStatus.Overflow;
                return false;
            }
            else if (op == Operator.Divide)
            {
                value = DivideOrRemainder(value, right, false);
            }
            else
            {
                value = DivideOrRemainder(value, right, true);
            }
        }
        return true;
    }

    private static bool TryParseUnary<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        SkipWhitespace(ref memory, source, ref cursor);
        if (TryConsume(ref memory, source, ref cursor, (byte)'-'))
        {
            if (!TryParsePower(ref memory, source, ref cursor, out value, out status)) return false;
            if (IsMinimum(value)) { status = EvalExpressionStatus.Overflow; return false; }
            value = Negated(value);
            return true;
        }
        if (TryConsume(ref memory, source, ref cursor, (byte)'~'))
        {
            if (!TryParsePower(ref memory, source, ref cursor, out value, out status)) return false;
            value = Not(value);
            return true;
        }
        return TryParsePower(ref memory, source, ref cursor, out value, out status);
    }

    private static bool TryParsePower<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!TryParsePrimary(ref memory, source, ref cursor, out value, out status)) return false;
        if (!TryReadExpected(ref memory, source, ref cursor, Operator.Power)) return true;
        if (!TryParseUnary(ref memory, source, ref cursor, out var exponent, out status)) return false;
        return TryPower(value, exponent, out value, out status);
    }

    private static bool TryParsePrimary<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        SkipWhitespace(ref memory, source, ref cursor);
        value = 0;
        status = EvalExpressionStatus.Malformed;
        if (TryConsume(ref memory, source, ref cursor, (byte)'('))
        {
            if (!TryParseShift(ref memory, source, ref cursor, out value, out status))
                return false;
            if (!TryConsume(ref memory, source, ref cursor, (byte)')'))
            { status = EvalExpressionStatus.Malformed; return false; }
            return true;
        }
        if (TryConsume(ref memory, source, ref cursor, (byte)'\''))
        {
            if (cursor.Position == cursor.Length) return false;
            value = memory.ReadUInt8(source, (int)cursor.Position++);
            return true;
        }
        return TryParseNumber(ref memory, source, ref cursor, out value, out status);
    }

    private static bool TryParseNumber<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        value = 0;
        status = EvalExpressionStatus.Malformed;
        if (cursor.Position == cursor.Length) return false;
        var first = memory.ReadUInt8(source, (int)cursor.Position);
        if (first == (byte)'#')
        {
            cursor.Position++;
            if (cursor.Position == cursor.Length) return false;
            var prefix = memory.ReadUInt8(source, (int)cursor.Position++);
            if (prefix == (byte)'x') return TryParseUnsigned(ref memory, source,
                ref cursor, 16, true, out value, out status);
            if (prefix is >= (byte)'0' and <= (byte)'7')
            {
                cursor.Position--;
                return TryParseUnsigned(ref memory, source, ref cursor, 8, true,
                    out value, out status);
            }
            return false;
        }
        if (first is < (byte)'0' or > (byte)'9') return false;
        if (first == (byte)'0' && cursor.Position + 1 < cursor.Length)
        {
            var next = memory.ReadUInt8(source, (int)(cursor.Position + 1));
            if (next == (byte)'x')
            {
                cursor.Position += 2;
                return TryParseUnsigned(ref memory, source, ref cursor, 16, true,
                    out value, out status);
            }
            if (next is >= (byte)'0' and <= (byte)'7')
                return TryParseUnsigned(ref memory, source, ref cursor, 8, true,
                    out value, out status);
        }
        return TryParseDecimal(ref memory, source, ref cursor, out value, out status);
    }

    private static bool TryParseDecimal<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out long value, out EvalExpressionStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        uint resultHigh = 0;
        uint resultLow = 0;
        var start = cursor.Position;
        while (cursor.Position < cursor.Length)
        {
            var digit = memory.ReadUInt8(source, (int)cursor.Position);
            if (digit is < (byte)'0' or > (byte)'9') break;
            if (!TryMultiplyUnsignedSmall(resultHigh, resultLow, 10,
                    out resultHigh, out resultLow) ||
                !TryAddUnsignedSmall(ref resultHigh, ref resultLow,
                    (uint)(digit - '0')) ||
                (resultHigh & 0x80000000) != 0)
            {
                value = 0; status = EvalExpressionStatus.Overflow; return false;
            }
            cursor.Position++;
        }
        value = M68kRuntime.CombineInt64(resultHigh, resultLow);
        status = cursor.Position == start ? EvalExpressionStatus.Malformed : EvalExpressionStatus.Success;
        return cursor.Position != start;
    }

    private static bool TryParseUnsigned<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, uint radix, bool requireDigit, out long value,
        out EvalExpressionStatus status) where TMemory : struct, IAmigaGuestMemory
    {
        uint resultHigh = 0;
        uint resultLow = 0;
        var start = cursor.Position;
        while (cursor.Position < cursor.Length)
        {
            var digit = Digit(memory.ReadUInt8(source, (int)cursor.Position));
            if (digit < 0 || (uint)digit >= radix) break;
            if (!TryMultiplyUnsignedSmall(resultHigh, resultLow, radix,
                    out resultHigh, out resultLow) ||
                !TryAddUnsignedSmall(ref resultHigh, ref resultLow, (uint)digit))
            {
                value = 0; status = EvalExpressionStatus.Overflow; return false;
            }
            cursor.Position++;
        }
        value = M68kRuntime.CombineInt64(resultHigh, resultLow);
        status = !requireDigit || cursor.Position != start ? EvalExpressionStatus.Success : EvalExpressionStatus.Malformed;
        return !requireDigit || cursor.Position != start;
    }

    private static bool TryReadExpected<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, Operator expected) where TMemory : struct, IAmigaGuestMemory
    {
        var checkpoint = cursor;
        if (TryReadOperator(ref memory, source, ref cursor, out var actual) && actual == expected)
            return true;
        cursor = checkpoint;
        return false;
    }

    private static bool TryReadOneOf<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, Operator first, Operator second, out Operator op)
        where TMemory : struct, IAmigaGuestMemory => TryReadOneOf(ref memory,
            source, ref cursor, first, second, Operator.None, out op);

    private static bool TryReadOneOf<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, Operator first, Operator second, Operator third,
        out Operator op) where TMemory : struct, IAmigaGuestMemory
    {
        var checkpoint = cursor;
        if (TryReadOperator(ref memory, source, ref cursor, out op) &&
            (op == first || op == second || op == third))
            return true;
        cursor = checkpoint;
        op = Operator.None;
        return false;
    }

    private static bool TryReadOperator<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, out Operator op) where TMemory : struct, IAmigaGuestMemory
    {
        SkipWhitespace(ref memory, source, ref cursor);
        op = default;
        if (cursor.Position == cursor.Length) return false;
        var value = memory.ReadUInt8(source, (int)cursor.Position);
        op = value switch
        {
            (byte)'+' => Operator.Add, (byte)'-' => Operator.Subtract,
            (byte)'*' => Operator.Multiply, (byte)'/' => Operator.Divide,
            (byte)'%' => Operator.Modulo, (byte)'^' => Operator.Power,
            (byte)'&' => Operator.And, (byte)'|' => Operator.Or,
            _ => default,
        };
        if (op != Operator.None) { cursor.Position++; return true; }
        if (value is < (byte)'A' or > (byte)'Z' and < (byte)'a' or > (byte)'z') return false;
        var start = cursor.Position;
        while (cursor.Position < cursor.Length)
        {
            var current = memory.ReadUInt8(source, (int)cursor.Position);
            if (current is < (byte)'A' or > (byte)'Z' and < (byte)'a' or > (byte)'z') break;
            cursor.Position++;
        }
        var length = cursor.Position - start;
        // Eval 50.7 calls strncmp(token, "mod", tokenLength) and the
        // equivalent checks for the other long operator names. That accepts
        // lowercase two-letter prefixes as well as the full word; single
        // letter aliases are handled separately and case-insensitively.
        if (EqualsWordPrefix(ref memory, source, start, length, (byte)'m', (byte)'o', (byte)'d') ||
            EqualsAlias(ref memory, source, start, length, (byte)'m')) op = Operator.Modulo;
        else if (EqualsWordPrefix(ref memory, source, start, length, (byte)'x', (byte)'o', (byte)'r') ||
            EqualsAlias(ref memory, source, start, length, (byte)'x')) op = Operator.Xor;
        else if (EqualsWordPrefix(ref memory, source, start, length, (byte)'e', (byte)'q', (byte)'v') ||
            EqualsAlias(ref memory, source, start, length, (byte)'e')) op = Operator.Equivalence;
        else if (EqualsWordPrefix(ref memory, source, start, length, (byte)'l', (byte)'s', (byte)'h') ||
            EqualsAlias(ref memory, source, start, length, (byte)'l')) op = Operator.LeftShift;
        else if (EqualsWordPrefix(ref memory, source, start, length, (byte)'r', (byte)'s', (byte)'h') ||
            EqualsAlias(ref memory, source, start, length, (byte)'r')) op = Operator.RightShift;
        else { cursor.Position = start; return false; }
        return true;
    }

    private static bool EqualsWordPrefix<TMemory>(ref TMemory memory, APTR source,
        uint start, uint length, byte first, byte second, byte third)
        where TMemory : struct, IAmigaGuestMemory => length is 2 or 3 &&
        memory.ReadUInt8(source, (int)start) == first &&
        memory.ReadUInt8(source, (int)(start + 1)) == second &&
        (length == 2 || memory.ReadUInt8(source, (int)(start + 2)) == third);

    private static bool EqualsAlias<TMemory>(ref TMemory memory, APTR source,
        uint start, uint length, byte expected) where TMemory : struct, IAmigaGuestMemory
    {
        if (length != 1) return false;
        var actual = memory.ReadUInt8(source, (int)start);
        if (actual is >= (byte)'A' and <= (byte)'Z') actual = (byte)(actual + 32);
        return actual == expected;
    }

    private static void SkipWhitespace<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor) where TMemory : struct, IAmigaGuestMemory
    {
        while (cursor.Position < cursor.Length && memory.ReadUInt8(source,
            (int)cursor.Position) is (byte)' ' or (byte)'\t') cursor.Position++;
    }

    private static bool TryConsume<TMemory>(ref TMemory memory, APTR source,
        ref Cursor cursor, byte expected) where TMemory : struct, IAmigaGuestMemory
    {
        SkipWhitespace(ref memory, source, ref cursor);
        if (cursor.Position == cursor.Length || memory.ReadUInt8(source,
            (int)cursor.Position) != expected) return false;
        cursor.Position++;
        return true;
    }

    private static bool TryAdd(long left, long right, bool subtract, out long value)
    {
        var leftLow = M68kRuntime.SplitInt64(left, out var leftHigh);
        var rightLow = M68kRuntime.SplitInt64(right, out var rightHigh);
        var leftNegative = (leftHigh & 0x80000000) != 0;
        var rightNegative = (rightHigh & 0x80000000) != 0;
        if (subtract)
        {
            var borrow = leftLow < rightLow ? 1u : 0u;
            var low = leftLow - rightLow;
            var high = leftHigh - rightHigh - borrow;
            var resultNegative = (high & 0x80000000) != 0;
            value = M68kRuntime.CombineInt64(high, low);
            return leftNegative == rightNegative || resultNegative == leftNegative;
        }
        var carry = unchecked(leftLow + rightLow) < leftLow ? 1u : 0u;
        var sumLow = unchecked(leftLow + rightLow);
        var sumHigh = unchecked(leftHigh + rightHigh + carry);
        var sumNegative = (sumHigh & 0x80000000) != 0;
        value = M68kRuntime.CombineInt64(sumHigh, sumLow);
        return leftNegative != rightNegative || sumNegative == leftNegative;
    }

    private static bool TryShiftLeft(long value, int amount, out long shifted)
    {
        var originalLow = M68kRuntime.SplitInt64(value, out var originalHigh);
        var low = originalLow;
        var high = originalHigh;
        for (var index = 0; index < amount; index++)
        {
            high = (high << 1) | (low >> 31);
            low <<= 1;
        }
        shifted = M68kRuntime.CombineInt64(high, low);
        var restoredHigh = high;
        var restoredLow = low;
        var sign = (restoredHigh & 0x80000000) != 0;
        for (var index = 0; index < amount; index++)
        {
            restoredLow = (restoredLow >> 1) | (restoredHigh << 31);
            restoredHigh = (restoredHigh >> 1) |
                (sign ? 0x80000000u : 0u);
        }
        return restoredHigh == originalHigh && restoredLow == originalLow;
    }

    private static bool TryGetShiftAmount(long value, out int amount)
    {
        var low = M68kRuntime.SplitInt64(value, out var high);
        if (high != 0 || low >= 64)
        {
            amount = 0;
            return false;
        }
        amount = (int)low;
        return true;
    }

    private static long ShiftRight(long value, int amount)
    {
        var low = M68kRuntime.SplitInt64(value, out var high);
        var sign = (high & 0x80000000) != 0;
        for (var index = 0; index < amount; index++)
        {
            low = (low >> 1) | (high << 31);
            high = (high >> 1) | (sign ? 0x80000000u : 0u);
        }
        return M68kRuntime.CombineInt64(high, low);
    }

    // CopperSharp lowers 32-bit XOR but not an Int64 XOR. Keep the evaluator's
    // signed 64-bit result exact by combining two independently lowered words.
    private static long Xor(long left, long right)
    {
        var leftLow = M68kRuntime.SplitInt64(left, out var leftHigh);
        var rightLow = M68kRuntime.SplitInt64(right, out var rightHigh);
        var low = leftLow ^ rightLow;
        var high = leftHigh ^ rightHigh;
        return M68kRuntime.CombineInt64(high, low);
    }

    // Keep all bitwise operations word-sized. CopperSharp currently lowers
    // neither Int64 NOT nor the direct Int64 AND/OR forms in this parser.
    private static long Not(long value)
    {
        var low = ~M68kRuntime.SplitInt64(value, out var high);
        high = ~high;
        return M68kRuntime.CombineInt64(high, low);
    }

    private static long Or(long left, long right)
    {
        var leftLow = M68kRuntime.SplitInt64(left, out var leftHigh);
        var rightLow = M68kRuntime.SplitInt64(right, out var rightHigh);
        var low = leftLow | rightLow;
        var high = leftHigh | rightHigh;
        return M68kRuntime.CombineInt64(high, low);
    }

    private static long And(long left, long right)
    {
        var leftLow = M68kRuntime.SplitInt64(left, out var leftHigh);
        var rightLow = M68kRuntime.SplitInt64(right, out var rightHigh);
        var low = leftLow & rightLow;
        var high = leftHigh & rightHigh;
        return M68kRuntime.CombineInt64(high, low);
    }

    private static bool IsZero(long value)
    {
        var low = M68kRuntime.SplitInt64(value, out var high);
        return low == 0 && high == 0;
    }

    private static bool IsMinimum(long value)
    {
        var low = M68kRuntime.SplitInt64(value, out var high);
        return low == 0 && high == 0x80000000;
    }

    private static bool IsNegativeOne(long value)
    {
        var low = M68kRuntime.SplitInt64(value, out var high);
        return low == uint.MaxValue && high == uint.MaxValue;
    }

    // The resident compiler deliberately does not lower Int64 divide/remainder.
    // Divide exact unsigned lanes, then restore the C# signed quotient/remainder
    // rules. The loop has a fixed 64 iterations and owns no storage.
    private static long DivideOrRemainder(long dividend, long divisor,
        bool remainder)
    {
        var dividendLow = M68kRuntime.SplitInt64(dividend, out var dividendHigh);
        var divisorLow = M68kRuntime.SplitInt64(divisor, out var divisorHigh);
        var dividendNegative = (dividendHigh & 0x80000000) != 0;
        var divisorNegative = (divisorHigh & 0x80000000) != 0;
        if (dividendNegative) Negate(ref dividendHigh, ref dividendLow);
        if (divisorNegative) Negate(ref divisorHigh, ref divisorLow);

        UnsignedDivide(dividendHigh, dividendLow, divisorHigh, divisorLow,
            out var quotientHigh, out var quotientLow, out var remainderHigh,
            out var remainderLow);
        if (remainder)
        {
            if (dividendNegative) Negate(ref remainderHigh, ref remainderLow);
            return M68kRuntime.CombineInt64(remainderHigh, remainderLow);
        }
        if (dividendNegative != divisorNegative)
            Negate(ref quotientHigh, ref quotientLow);
        return M68kRuntime.CombineInt64(quotientHigh, quotientLow);
    }

    private static void UnsignedDivide(uint dividendHigh, uint dividendLow,
        uint divisorHigh, uint divisorLow, out uint quotientHigh,
        out uint quotientLow, out uint remainderHigh, out uint remainderLow)
    {
        quotientHigh = 0;
        quotientLow = 0;
        remainderHigh = 0;
        remainderLow = 0;
        var remainingDividendHigh = dividendHigh;
        var remainingDividendLow = dividendLow;
        for (var bitIndex = 0; bitIndex < 64; bitIndex++)
        {
            var nextBit = (remainingDividendHigh & 0x80000000) != 0 ? 1u : 0u;
            remainingDividendHigh = (remainingDividendHigh << 1) |
                (remainingDividendLow >> 31);
            remainingDividendLow <<= 1;
            var carry = (remainderHigh & 0x80000000) != 0;
            remainderHigh = (remainderHigh << 1) | (remainderLow >> 31);
            remainderLow = (remainderLow << 1) | nextBit;
            if (carry || IsAtLeast(remainderHigh, remainderLow,
                    divisorHigh, divisorLow))
            {
                Subtract(ref remainderHigh, ref remainderLow, divisorHigh,
                    divisorLow);
                if (bitIndex < 32)
                    quotientHigh |= 1u << (31 - bitIndex);
                else
                    quotientLow |= 1u << (63 - bitIndex);
            }
        }
    }

    private static bool IsAtLeast(uint leftHigh, uint leftLow, uint rightHigh,
        uint rightLow) => leftHigh > rightHigh ||
        leftHigh == rightHigh && leftLow >= rightLow;

    private static void Subtract(ref uint high, ref uint low, uint subtrahendHigh,
        uint subtrahendLow)
    {
        var borrow = low < subtrahendLow ? 1u : 0u;
        low -= subtrahendLow;
        high = high - subtrahendHigh - borrow;
    }

    private static void Negate(ref uint high, ref uint low)
    {
        low = unchecked(0u - low);
        high = unchecked(~high + (low == 0 ? 1u : 0u));
    }

    private static bool TryToInt32(long value, out int result)
    {
        var low = M68kRuntime.SplitInt64(value, out var high);
        if (high != 0 && high != uint.MaxValue)
        {
            result = 0;
            return false;
        }
        result = unchecked((int)low);
        return (result < 0) == (high == uint.MaxValue);
    }

    private static long Negated(long value)
    {
        var low = M68kRuntime.SplitInt64(value, out var high);
        Negate(ref high, ref low);
        return M68kRuntime.CombineInt64(high, low);
    }

    private static bool TryMultiply(long left, long right, out long value)
    {
        if (IsZero(left) || IsZero(right)) { value = 0; return true; }
        var leftLow = M68kRuntime.SplitInt64(left, out var leftHigh);
        var rightLow = M68kRuntime.SplitInt64(right, out var rightHigh);
        var negative = ((leftHigh ^ rightHigh) & 0x80000000) != 0;
        if ((leftHigh & 0x80000000) != 0) Negate(ref leftHigh, ref leftLow);
        if ((rightHigh & 0x80000000) != 0) Negate(ref rightHigh, ref rightLow);
        UnsignedMultiply(leftHigh, leftLow, rightHigh, rightLow, out var high,
            out var low, out var overflow);
        if (overflow || negative && (high > 0x80000000 ||
            high == 0x80000000 && low != 0) || !negative &&
            (high & 0x80000000) != 0)
        {
            value = 0;
            return false;
        }
        if (negative) Negate(ref high, ref low);
        value = M68kRuntime.CombineInt64(high, low);
        return true;
    }

    private static void UnsignedMultiply(uint leftHigh, uint leftLow,
        uint rightHigh, uint rightLow, out uint resultHigh, out uint resultLow,
        out bool overflow)
    {
        resultHigh = 0;
        resultLow = 0;
        overflow = false;
        var multiplicandHigh = leftHigh;
        var multiplicandLow = leftLow;
        var multiplierHigh = rightHigh;
        var multiplierLow = rightLow;
        var lostHighBits = false;
        for (var bitIndex = 0; bitIndex < 64; bitIndex++)
        {
            if ((multiplierLow & 1) != 0)
            {
                if (lostHighBits) overflow = true;
                var previousLow = resultLow;
                resultLow += multiplicandLow;
                var carry = resultLow < previousLow ? 1u : 0u;
                var previousHigh = resultHigh;
                resultHigh += multiplicandHigh;
                if (resultHigh < previousHigh) overflow = true;
                previousHigh = resultHigh;
                resultHigh += carry;
                if (resultHigh < previousHigh) overflow = true;
            }
            multiplierLow = (multiplierLow >> 1) | (multiplierHigh << 31);
            multiplierHigh >>= 1;
            if (bitIndex == 63) continue;
            if ((multiplicandHigh & 0x80000000) != 0) lostHighBits = true;
            multiplicandHigh = (multiplicandHigh << 1) | (multiplicandLow >> 31);
            multiplicandLow <<= 1;
        }
    }

    private static bool TryMultiplyUnsignedSmall(uint sourceHigh, uint sourceLow,
        uint factor, out uint productHigh, out uint productLow)
    {
        productHigh = 0;
        productLow = 0;
        var currentHigh = sourceHigh;
        var currentLow = sourceLow;
        var remainingFactor = factor;
        var lostHighBits = false;
        while (remainingFactor != 0)
        {
            if ((remainingFactor & 1) != 0)
            {
                if (lostHighBits || !TryAddUnsigned(ref productHigh,
                        ref productLow, currentHigh, currentLow))
                    return false;
            }
            remainingFactor >>= 1;
            if (remainingFactor == 0) break;
            if ((currentHigh & 0x80000000) != 0) lostHighBits = true;
            currentHigh = (currentHigh << 1) | (currentLow >> 31);
            currentLow <<= 1;
        }
        return true;
    }

    private static bool TryAddUnsignedSmall(ref uint high, ref uint low,
        uint addend)
    {
        var previousLow = low;
        low += addend;
        if (low >= previousLow) return true;
        if (high == uint.MaxValue) return false;
        high++;
        return true;
    }

    private static bool TryAddUnsigned(ref uint high, ref uint low,
        uint addendHigh, uint addendLow)
    {
        var previousLow = low;
        low += addendLow;
        var carry = low < previousLow ? 1u : 0u;
        var previousHigh = high;
        high += addendHigh;
        if (high < previousHigh) return false;
        previousHigh = high;
        high += carry;
        return high >= previousHigh;
    }

    private static bool TryPower(long baseValue, long exponent, out long value,
        out EvalExpressionStatus status)
    {
        // The source helper is a 32-bit repeated multiplication. Preserve that
        // bounded subset explicitly instead of silently widening its behavior.
        if (!TryToInt32(baseValue, out var factor) ||
            !TryToInt32(exponent, out var remaining))
        { value = 0; status = EvalExpressionStatus.Overflow; return false; }
        var result = 1;
        for (; remaining > 0; remaining--)
        {
            if (!TryMultiply(result, factor, out var product) ||
                !TryToInt32(product, out result))
            { value = 0; status = EvalExpressionStatus.Overflow; return false; }
        }
        value = result;
        status = EvalExpressionStatus.Success;
        return true;
    }

    private static int Digit(byte value) => value is >= (byte)'0' and <= (byte)'9' ? value - '0' :
        value is >= (byte)'a' and <= (byte)'f' ? value - 'a' + 10 :
        value is >= (byte)'A' and <= (byte)'F' ? value - 'A' + 10 : -1;

    private enum Operator : byte
    {
        None, Add, Subtract, Multiply, Divide, Modulo, Power, And, Or, Xor,
        Equivalence, LeftShift, RightShift,
    }

    private struct Cursor(uint length)
    {
        public uint Length = length;
        public uint Position;
    }
}
