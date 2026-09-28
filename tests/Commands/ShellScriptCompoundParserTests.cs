using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptCompoundParserTests
{
	[Fact]
	public void Compound_split_uses_typed_slices_and_stops_at_comments()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		const string text = " Echo \"one && two\" && Echo right ; ignored || text";
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);

		var status = ShellScriptCompoundParser.SplitFirstOperator(
			ref platform, in source, out var split);

		Assert.Equal(ShellScriptCompoundParseStatus.Operator,
			(ShellScriptCompoundParseStatus)status);
		Assert.Equal(ShellScriptCompoundOperator.ConditionalAnd,
			split.Operator);
		Assert.Equal("Echo \"one && two\"",
			platform.Store.ReadText(split.First.Data, split.First.Length));
		Assert.Equal("Echo right",
			platform.Store.ReadText(split.Deferred.Data,
				split.Deferred.Length));
	}

	[Theory]
	[InlineData("Echo left | More", ShellScriptCompoundOperator.Pipe,
		"Echo left", "More")]
	[InlineData("Echo left || More",
		ShellScriptCompoundOperator.OutputConcatenation,
		"Echo left", "More")]
	[InlineData("Echo title || List | More",
		ShellScriptCompoundOperator.OutputConcatenation,
		"Echo title", "List | More")]
	[InlineData("Echo \"left | right\" || More",
		ShellScriptCompoundOperator.OutputConcatenation,
		"Echo \"left | right\"", "More")]
	[InlineData("Echo left *| right", ShellScriptCompoundOperator.None,
		"Echo left *| right", "")]
	public void Recognizes_pipeline_operators_outside_quoted_or_escaped_text(
		string text, ShellScriptCompoundOperator expectedOperator,
		string expectedFirst, string expectedDeferred)
	{
		EchoCommandTests.TestShellPlatform platform = new();
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);

		var status = ShellScriptCompoundParser.SplitFirstOperator(
			ref platform, in source, out var split);

		if (expectedOperator == ShellScriptCompoundOperator.None)
		{
			Assert.Equal((int)ShellScriptCompoundParseStatus.NoOperator, status);
		}
		else
		{
			Assert.Equal((int)ShellScriptCompoundParseStatus.Operator, status);
		}
		Assert.Equal(expectedOperator, split.Operator);
		Assert.Equal(expectedFirst, platform.Store.ReadText(split.First.Data,
			split.First.Length));
		if (expectedOperator != ShellScriptCompoundOperator.None)
			Assert.Equal(expectedDeferred, platform.Store.ReadText(
				split.Deferred.Data, split.Deferred.Length));
	}

	[Theory]
	[InlineData("Echo left&&right")]
	[InlineData("Echo \"left && right\"")]
	[InlineData("Echo left *&& right")]
	public void Attached_quoted_or_escaped_ampersands_are_not_operators(string text)
	{
		EchoCommandTests.TestShellPlatform platform = new();
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);

		Assert.Equal((int)ShellScriptCompoundParseStatus.NoOperator,
			ShellScriptCompoundParser.SplitFirstOperator(ref platform,
				in source, out _));
	}

	[Theory]
	[InlineData("&& Echo right")]
	[InlineData("Echo left &&")]
	public void Conditional_and_requires_both_command_sides(string text)
	{
		EchoCommandTests.TestShellPlatform platform = new();
		var source = new ShellScriptTextSlice(
			platform.Store.PutAt(16, text), (uint)text.Length);

		Assert.Equal((int)ShellScriptCompoundParseStatus.Malformed,
			ShellScriptCompoundParser.SplitFirstOperator(ref platform,
				in source, out _));
	}
}
