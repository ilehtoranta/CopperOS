using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListInteractionPolicyStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedInteractionPolicyRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListInteractionPolicyState
		{
			Magic = MuiListCore.MuiListInteractionPolicyState.Cookie,
			Input = 1,
			MultiSelect = 2,
			ScrollerPos = 3,
		};
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListInteractionPolicyStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListInteractionPolicyStateField.ScrollerPos,
		};
		Assert.True(MuiListCore.MuiListInteractionPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x350Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListInteractionPolicyStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListInteractionPolicyStateField.MultiSelect, 4));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListInteractionPolicyStateField.Input, out var input));
		Assert.Equal(1u, input);
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Input, decoded.Input);
		Assert.Equal(4u, decoded.MultiSelect);
		Assert.Equal(value.ScrollerPos, decoded.ScrollerPos);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListInteractionPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListInteractionPolicyStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListInteractionPolicyStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListInteractionPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListInteractionPolicyStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListInteractionPolicyStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListInteractionPolicyStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
				MuiListCore.MuiListInteractionPolicyStateField.Magic, out _, out _));
	}
}
