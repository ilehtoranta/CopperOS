using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.Shell;

/// <summary>Result of parsing an isolated Shell pipeline operand.</summary>
public enum ShellScriptPipelineParseStatus : int
{
	NotPipeline = 0,
	Pipeline = 1,
	Malformed = 2,
	Unmapped = 3,
	UnsupportedComposition = 4,
	InsufficientCapacity = 5,
}

/// <summary>One command slice in caller-owned pipeline-plan storage.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 8)]
public struct ShellScriptPipelineSegment
{
	public const uint Size = 8;

	public ShellScriptTextSlice Command;
}

/// <summary>Typed view of a caller-owned array of pipeline segments.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 8)]
public struct ShellScriptPipelinePlan
{
	public APTR SegmentStorage;
	public uint SegmentCount;
}

/// <summary>
/// Codec for guest-resident pipeline segment arrays. Byte positions are kept
/// here at the memory-layout boundary; parser and runner code use structs.
/// </summary>
public static class ShellScriptPipelineSegmentCodec
{
	private const int CommandDataOffset = 0;
	private const int CommandLengthOffset = 4;

	public static bool TryRead<TPlatform>(ref TPlatform platform,
		in ShellScriptPipelinePlan plan, uint index,
		out ShellScriptPipelineSegment segment)
		where TPlatform : struct, IShellPlatform
	{
		segment = default;
		if (index >= plan.SegmentCount || plan.SegmentStorage.IsNull ||
			index >= uint.MaxValue / ShellScriptPipelineSegment.Size)
			return false;
		var recordEnd = (index + 1) * ShellScriptPipelineSegment.Size;
		if (plan.SegmentStorage.Raw > uint.MaxValue - recordEnd)
			return false;
		var address = APTR.FromPointer(plan.SegmentStorage.Raw +
			index * ShellScriptPipelineSegment.Size);
		if ((address.Raw & 3) != 0 || !platform.IsMapped(address,
			ShellScriptPipelineSegment.Size)) return false;
		segment.Command = new ShellScriptTextSlice(
			APTR.FromPointer(platform.ReadUInt32(address, CommandDataOffset)),
			platform.ReadUInt32(address, CommandLengthOffset));
		return segment.Command.Data.IsNotNull && segment.Command.Length != 0 &&
			segment.Command.Data.Raw <= uint.MaxValue - segment.Command.Length &&
			platform.IsMapped(segment.Command.Data, segment.Command.Length);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR storage, uint capacity, uint index,
		in ShellScriptTextSlice command)
		where TPlatform : struct, IShellPlatform
	{
		if (index >= capacity || command.Data.IsNull || command.Length == 0 ||
			command.Data.Raw > uint.MaxValue - command.Length ||
			!platform.IsMapped(command.Data, command.Length) ||
			storage.IsNull || index >= uint.MaxValue /
				ShellScriptPipelineSegment.Size ||
			storage.Raw > uint.MaxValue -
				(index + 1) * ShellScriptPipelineSegment.Size)
			return false;
		var address = APTR.FromPointer(storage.Raw +
			index * ShellScriptPipelineSegment.Size);
		if ((address.Raw & 3) != 0 || !platform.IsMapped(address,
			ShellScriptPipelineSegment.Size)) return false;
		platform.WriteUInt32(address, CommandDataOffset, command.Data.Raw);
		platform.WriteUInt32(address, CommandLengthOffset, command.Length);
		return true;
	}
}

/// <summary>
/// Splits a bounded, isolated pipeline operand into named command slices.
/// AND/OR composition is intentionally handled by the outer compound parser.
/// This parser does not launch commands or enable pipelines in the runner.
/// </summary>
public static class ShellScriptPipelineParser
{
	public static int Split<TPlatform>(ref TPlatform platform,
		in ShellScriptTextSlice source, APTR segmentStorage,
		uint segmentCapacity, out ShellScriptPipelinePlan plan)
		where TPlatform : struct, IShellPlatform
	{
		plan = default;
		var remaining = source;
		var parseStatus = ShellScriptCompoundParser.SplitFirstOperator(
			ref platform, in remaining, out var split);
		if (parseStatus == (int)ShellScriptCompoundParseStatus.Unmapped)
			return (int)ShellScriptPipelineParseStatus.Unmapped;
		if (parseStatus == (int)ShellScriptCompoundParseStatus.Malformed)
			return (int)ShellScriptPipelineParseStatus.Malformed;
		if (parseStatus == (int)ShellScriptCompoundParseStatus.NoOperator ||
			(split.Operator != ShellScriptCompoundOperator.Pipe))
			return (int)ShellScriptPipelineParseStatus.NotPipeline;

		if (segmentStorage.IsNull || (segmentStorage.Raw & 3) != 0 ||
			segmentCapacity < 2 || segmentCapacity > uint.MaxValue /
				ShellScriptPipelineSegment.Size)
			return (int)ShellScriptPipelineParseStatus.InsufficientCapacity;
		var storageLength = segmentCapacity *
			ShellScriptPipelineSegment.Size;
		if (segmentStorage.Raw > uint.MaxValue - storageLength ||
			!platform.IsMapped(segmentStorage, storageLength))
			return (int)ShellScriptPipelineParseStatus.Unmapped;

		var count = 0u;
		while (true)
		{
			if (split.Operator != ShellScriptCompoundOperator.Pipe)
				return (int)ShellScriptPipelineParseStatus.UnsupportedComposition;
			if (count >= segmentCapacity ||
				!ShellScriptPipelineSegmentCodec.TryWrite(ref platform,
					segmentStorage, segmentCapacity, count, in split.First))
				return (int)ShellScriptPipelineParseStatus.InsufficientCapacity;
			count++;
			remaining = split.Deferred;
			parseStatus = ShellScriptCompoundParser.SplitFirstOperator(
				ref platform, in remaining, out split);
			if (parseStatus == (int)ShellScriptCompoundParseStatus.Unmapped)
				return (int)ShellScriptPipelineParseStatus.Unmapped;
			if (parseStatus == (int)ShellScriptCompoundParseStatus.Malformed)
				return (int)ShellScriptPipelineParseStatus.Malformed;
			if (parseStatus == (int)ShellScriptCompoundParseStatus.NoOperator)
			{
				if (count >= segmentCapacity ||
					!ShellScriptPipelineSegmentCodec.TryWrite(ref platform,
						segmentStorage, segmentCapacity, count,
						in remaining))
					return (int)ShellScriptPipelineParseStatus.InsufficientCapacity;
				count++;
				plan = new ShellScriptPipelinePlan
				{
					SegmentStorage = segmentStorage,
					SegmentCount = count,
				};
				return (int)ShellScriptPipelineParseStatus.Pipeline;
			}
		}
	}
}
