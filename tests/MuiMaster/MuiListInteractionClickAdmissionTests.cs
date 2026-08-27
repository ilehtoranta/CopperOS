using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListInteractionClickAdmissionTests
{
	[Fact]
	public void ListInteractionAndClickRecordsRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var interactionAddress = APTR.FromPointer(0x3340);
		var clickAddress = APTR.FromPointer(0x3370);
		var interaction = new MuiListCore.MuiListInteractionPolicyState
		{
			Magic = MuiListCore.MuiListInteractionPolicyState.Cookie,
			Input = 1,
			MultiSelect = 2,
			ScrollerPos = 1,
		};
		var click = new MuiListCore.MuiListClickState
		{
			Magic = MuiListCore.MuiListClickState.Cookie,
			ClickColumn = 3,
			DoubleClick = 1,
			AgainClick = 1,
			Clicks = 2,
			DefClickColumn = 4,
		};
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.Write(
			ref platform, interactionAddress, interaction));
		Assert.True(MuiListCore.MuiListClickStateCodec.Write(ref platform,
			clickAddress, click));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.TryRead(
			ref platform, interactionAddress, out var readInteraction));
		Assert.Equal(interaction.Input, readInteraction.Input);
		Assert.Equal(interaction.MultiSelect, readInteraction.MultiSelect);
		Assert.Equal(interaction.ScrollerPos, readInteraction.ScrollerPos);
		Assert.True(MuiListCore.MuiListClickStateCodec.TryRead(ref platform,
			clickAddress, out var readClick));
		Assert.Equal(click.ClickColumn, readClick.ClickColumn);
		Assert.Equal(click.DoubleClick, readClick.DoubleClick);
		Assert.Equal(click.AgainClick, readClick.AgainClick);
		Assert.Equal(click.Clicks, readClick.Clicks);
		Assert.Equal(click.DefClickColumn, readClick.DefClickColumn);
	}

	[Fact]
	public void MalformedListInteractionAndClickMagicRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var interactionAddress = APTR.FromPointer(0x33A0);
		var clickAddress = APTR.FromPointer(0x33D0);
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.Write(
			ref platform, interactionAddress, new MuiListCore.MuiListInteractionPolicyState
			{
				Magic = MuiListCore.MuiListInteractionPolicyState.Cookie,
				Input = 1,
				MultiSelect = 2,
				ScrollerPos = 1,
			}));
		Assert.True(MuiListCore.MuiListClickStateCodec.Write(ref platform,
			clickAddress, new MuiListCore.MuiListClickState
			{
				Magic = MuiListCore.MuiListClickState.Cookie,
				ClickColumn = 3,
				Clicks = 5,
			}));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateFieldCursorCodec
			.TryWriteUInt32(ref platform, interactionAddress,
				MuiListCore.MuiListInteractionPolicyStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListClickStateFieldCursorCodec.TryWriteUInt32(
			ref platform, clickAddress, MuiListCore.MuiListClickStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.TryReadStructural(
			ref platform, interactionAddress, out var structuralInteraction));
		Assert.Equal(0u, structuralInteraction.Magic);
		Assert.Equal(2u, structuralInteraction.MultiSelect);
		Assert.False(MuiListCore.MuiListInteractionPolicyStateCodec.TryRead(
			ref platform, interactionAddress, out _));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.TryReadStorage(
			ref platform, interactionAddress, out _));
		Assert.True(MuiListCore.MuiListClickStateCodec.TryReadStructural(
			ref platform, clickAddress, out var structuralClick));
		Assert.Equal(0u, structuralClick.Magic);
		Assert.Equal(5u, structuralClick.Clicks);
		Assert.False(MuiListCore.MuiListClickStateCodec.TryRead(ref platform,
			clickAddress, out _));
		Assert.True(MuiListCore.MuiListClickStateCodec.TryReadStorage(ref platform,
			clickAddress, out _));
	}
}
