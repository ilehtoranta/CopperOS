using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMakeObjectBuildShapeRecordTests
{
	private const uint LabelSingleFrame = 1u << 8;
	private const uint LabelLeftAligned = 1u << 10;
	private const uint LabelCentered = 1u << 11;
	private const uint UnknownLabelFlag = 1u << 15;

	[Fact]
	public void BuildShapeRetainsTypedParametersAndNamedConstructionMetadata()
	{
		var labelParameters = new MuiMakeObjectParameterRecord
		{
			First = 0x1234,
			Second = LabelSingleFrame | LabelCentered,
		};

		Assert.True(MuiMakeObjectServiceCore.TryBuildShape(
			MuiMakeObjectServiceCore.MUIO_Label, labelParameters,
			out var labelShape));
		Assert.Equal(MuiMakeObjectServiceCore.MUIO_Label, labelShape.Type);
		Assert.Equal(labelParameters.First, labelShape.Parameters.First);
		Assert.Equal(labelParameters.Second, labelShape.Parameters.Second);
		Assert.Equal(4u, labelShape.TagCount);
		Assert.Equal(1u, labelShape.PreParseKind);

		var buttonParameters = new MuiMakeObjectParameterRecord
		{
			First = 0x5678,
		};
		Assert.True(MuiMakeObjectServiceCore.TryBuildShape(
			MuiMakeObjectServiceCore.MUIO_Button, buttonParameters,
			out var buttonShape));
		Assert.Equal(buttonShape.ClassKind, labelShape.ClassKind);
		Assert.Equal(7u, buttonShape.TagCount);
	}

	[Fact]
	public void BuildShapeRejectsConflictingOrUnknownLabelFlags()
	{
		var conflicting = new MuiMakeObjectParameterRecord
		{
			Second = LabelLeftAligned | LabelCentered,
		};
		var unknown = new MuiMakeObjectParameterRecord
		{
			Second = UnknownLabelFlag,
		};

		Assert.False(MuiMakeObjectServiceCore.TryBuildShape(
			MuiMakeObjectServiceCore.MUIO_Label, conflicting, out _));
		Assert.False(MuiMakeObjectServiceCore.TryBuildShape(
			MuiMakeObjectServiceCore.MUIO_Label, unknown, out _));
	}
}
