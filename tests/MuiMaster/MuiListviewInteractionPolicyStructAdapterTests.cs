using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewInteractionPolicyStructAdapterTests
{
	[Fact]
	public void InteractionPolicyFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewInteractionPolicyState
		{
			Magic = MuiListviewCore.MuiListviewInteractionPolicyState.Cookie,
			Input = 1,
			MultiSelect = 2,
			ScrollerPos = 3,
			DragType = 4,
		};

		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListviewCore.MuiListviewInteractionPolicyField.DragType, 9));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiListviewCore.MuiListviewInteractionPolicyField.DragType,
			out var dragType));
		Assert.Equal(9u, dragType);

		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Input, decoded.Input);
		Assert.Equal(value.MultiSelect, decoded.MultiSelect);
		Assert.Equal(value.ScrollerPos, decoded.ScrollerPos);
		Assert.Equal(9u, decoded.DragType);

		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiListviewCore.MuiListviewInteractionPolicyField.DragType,
			out var dragTypeAddress));
		Assert.Equal(0x3510u, dragTypeAddress.Raw);
	}

	[Fact]
	public void InteractionPolicyAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewInteractionPolicyField)0xFF, out _));
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewInteractionPolicyField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListviewCore.MuiListviewInteractionPolicyField.Input, out _));
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiListviewCore.MuiListviewInteractionPolicyField.Magic, 1));
	}
}
