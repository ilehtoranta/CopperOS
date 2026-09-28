using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptPipelineParserTests
{
	[Fact]
	public void Produces_typed_segments_for_a_multi_command_pipeline()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		const string text = "Echo \"left | text\" | List | More";
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);
		var segmentStorage = APTR.FromPointer(512);

		var status = ShellScriptPipelineParser.Split(ref platform,
			in source, segmentStorage, 4, out var plan);

		Assert.Equal((int)ShellScriptPipelineParseStatus.Pipeline, status);
		Assert.Equal(3u, plan.SegmentCount);
		Assert.Equal(segmentStorage.Raw, plan.SegmentStorage.Raw);
		Assert.Equal("Echo \"left | text\"", ReadSegment(ref platform,
			in plan, 0));
		Assert.Equal("List", ReadSegment(ref platform, in plan, 1));
		Assert.Equal("More", ReadSegment(ref platform, in plan, 2));
	}

	[Theory]
	[InlineData("Echo plain")]
	[InlineData("Echo left || More")]
	[InlineData("Echo left && More")]
	public void Does_not_treat_non_pipeline_compounds_as_pipelines(string text)
	{
		EchoCommandTests.TestShellPlatform platform = new();
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);
		var segmentStorage = APTR.FromPointer(512);

		var status = ShellScriptPipelineParser.Split(ref platform,
			in source, segmentStorage, 4, out var plan);

		Assert.Equal((int)ShellScriptPipelineParseStatus.NotPipeline, status);
		Assert.Equal(0u, plan.SegmentCount);
	}

	[Fact]
	public void Rejects_mixed_and_or_composition_until_outer_planning_exists()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		const string text = "Echo left | List || More";
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);
		var segmentStorage = APTR.FromPointer(512);

		var status = ShellScriptPipelineParser.Split(ref platform,
			in source, segmentStorage, 4, out var plan);

		Assert.Equal((int)ShellScriptPipelineParseStatus.UnsupportedComposition,
			status);
		Assert.Equal(0u, plan.SegmentCount);
	}

	[Fact]
	public void Rejects_a_pipeline_that_exceeds_the_supplied_segment_capacity()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		const string text = "Echo left | List | More";
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);
		var segmentStorage = APTR.FromPointer(512);

		var status = ShellScriptPipelineParser.Split(ref platform,
			in source, segmentStorage, 2, out var plan);

		Assert.Equal((int)ShellScriptPipelineParseStatus.InsufficientCapacity,
			status);
		Assert.Equal(0u, plan.SegmentCount);
	}

	private static string ReadSegment(
		ref EchoCommandTests.TestShellPlatform platform,
		in ShellScriptPipelinePlan plan, uint index)
	{
		Assert.True(ShellScriptPipelineSegmentCodec.TryRead(ref platform,
			in plan, index, out var segment));
		return platform.Store.ReadText(segment.Command.Data,
			segment.Command.Length);
	}
}
