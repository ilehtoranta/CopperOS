using Amiga;

namespace CopperOS.Shell;

/// <summary>Result of recognizing one bounded compound-command line.</summary>
public enum ShellScriptCompoundParseStatus : int
{
	NoOperator = 0,
	Operator = 1,
	Malformed = 2,
	Unmapped = 3,
}

/// <summary>Recognized binary Shell operator between two command slices.</summary>
public enum ShellScriptCompoundOperator : uint
{
	None = 0,
	ConditionalAnd = 1,
	OutputConcatenation = 2,
	Pipe = 3,
}

/// <summary>
/// Typed left/right command slices and operator identity for one recognized
/// MorphOS Shell compound operator. Offsets are represented as slices, not
/// exposed as positional parser results.
/// </summary>
public struct ShellScriptCompoundSplit
{
	public ShellScriptTextSlice First;
	public ShellScriptTextSlice Deferred;
	public ShellScriptCompoundOperator Operator;
}

/// <summary>
/// Bounded lexical scan for whitespace-delimited compound command operators.
/// Quotes, star escapes, and semicolon comments are respected before an
/// operator is recognized.
/// </summary>
public static class ShellScriptCompoundParser
{
	public static int SplitFirstOperator<TPlatform>(
		ref TPlatform platform,
		in ShellScriptTextSlice source,
		out ShellScriptCompoundSplit split)
		where TPlatform : struct, IShellPlatform
	{
		split = default;
		if (source.Length > ShellTextParser.MaximumSourceLength ||
			(source.Length != 0 && (source.Data.IsNull ||
			 source.Data.Raw > uint.MaxValue - source.Length ||
			 !platform.IsMapped(source.Data, source.Length))))
			return (int)ShellScriptCompoundParseStatus.Unmapped;

		uint quoted = 0;
		uint commentStart = source.Length;
		uint operatorStart = uint.MaxValue;
		var compoundOperator = ShellScriptCompoundOperator.None;
		uint operatorLength = 0;
		for (uint position = 0; position < source.Length; position++)
		{
			var value = Read(ref platform, source.Data, position);
			if (value == 0)
				return (int)ShellScriptCompoundParseStatus.Malformed;
			if (quoted == 0 && value == (byte)';')
			{
				commentStart = position;
				break;
			}
			if (value == (byte)'*')
			{
				if (position + 1 >= source.Length ||
					Read(ref platform, source.Data, position + 1) == 0)
					return (int)ShellScriptCompoundParseStatus.Malformed;
				position++;
				continue;
			}
			if (value == (byte)'"')
			{
				quoted ^= 1;
				continue;
			}
			if (quoted != 0)
				continue;

			var candidate = ShellScriptCompoundOperator.None;
			var candidateLength = 0u;
			if (value == (byte)'&' && position + 1 < source.Length &&
				Read(ref platform, source.Data, position + 1) == (byte)'&')
			{
				candidate = ShellScriptCompoundOperator.ConditionalAnd;
				candidateLength = 2;
			}
			else if (value == (byte)'|' && position + 1 < source.Length &&
				Read(ref platform, source.Data, position + 1) == (byte)'|')
			{
				candidate = ShellScriptCompoundOperator.OutputConcatenation;
				candidateLength = 2;
			}
			else if (value == (byte)'|')
			{
				candidate = ShellScriptCompoundOperator.Pipe;
				candidateLength = 1;
			}
			if (candidate == ShellScriptCompoundOperator.None)
				continue;

			var hasLeadingSpace = position == 0 ||
				IsWhitespace(Read(ref platform, source.Data, position - 1));
			var afterOperator = position + candidateLength;
			var hasTrailingSpace = afterOperator == source.Length ||
				(afterOperator < source.Length &&
				 (IsWhitespace(Read(ref platform, source.Data, afterOperator)) ||
				  Read(ref platform, source.Data, afterOperator) == (byte)';'));
			if (hasLeadingSpace && hasTrailingSpace &&
				operatorStart == uint.MaxValue)
			{
				operatorStart = position;
				compoundOperator = candidate;
				operatorLength = candidateLength;
			}
			// A doubled operator is one token even when it is attached text.
			// Do not reinterpret its second character as a standalone pipe.
			position += candidateLength - 1;
		}

		if (quoted != 0)
			return (int)ShellScriptCompoundParseStatus.Malformed;
		if (operatorStart == uint.MaxValue)
		{
			split.First = source;
			return (int)ShellScriptCompoundParseStatus.NoOperator;
		}

		var firstStart = 0u;
		var firstEnd = operatorStart;
		while (firstStart < firstEnd &&
			IsWhitespace(Read(ref platform, source.Data, firstStart)))
			firstStart++;
		while (firstEnd > firstStart &&
			IsWhitespace(Read(ref platform, source.Data, firstEnd - 1)))
			firstEnd--;

		var deferredStart = operatorStart + operatorLength;
		var deferredEnd = commentStart;
		while (deferredStart < deferredEnd &&
			IsWhitespace(Read(ref platform, source.Data, deferredStart)))
			deferredStart++;
		while (deferredEnd > deferredStart &&
			IsWhitespace(Read(ref platform, source.Data, deferredEnd - 1)))
			deferredEnd--;
		if (firstStart == firstEnd || deferredStart == deferredEnd)
			return (int)ShellScriptCompoundParseStatus.Malformed;

		split.First = new ShellScriptTextSlice(
			APTR.FromPointer(source.Data.Raw + firstStart),
			firstEnd - firstStart);
		split.Deferred = new ShellScriptTextSlice(
			APTR.FromPointer(source.Data.Raw + deferredStart),
			deferredEnd - deferredStart);
		split.Operator = compoundOperator;
		return (int)ShellScriptCompoundParseStatus.Operator;
	}

	private static byte Read<TPlatform>(ref TPlatform platform, APTR source,
		uint position) where TPlatform : struct, IShellPlatform =>
		platform.ReadUInt8(source, unchecked((int)position));

	private static bool IsWhitespace(byte value) =>
		value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';
}
