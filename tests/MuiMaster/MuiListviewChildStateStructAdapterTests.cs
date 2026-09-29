using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewChildStateStructAdapterTests
{
	[Fact]
	public void ChildFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewChildState
		{
			Magic = MuiListviewCore.MuiListviewChildState.Cookie,
			Child = APTR.FromPointer(0x00036200),
		};

		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewChildStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewChildStateField.Child,
			0x00036400));
		Assert.True(MuiListviewCore.MuiListviewChildStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListviewCore.MuiListviewChildStateField.Child,
			out var child));
		Assert.Equal(0x00036400u, child);

		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x00036400u, decoded.Child.Raw);

		Assert.True(MuiListviewCore.MuiListviewChildStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListviewCore.MuiListviewChildStateField.Child,
			out var childAddress));
		Assert.Equal(0x3504u, childAddress.Raw);
		var fieldCursor = new MuiListviewCore.MuiListviewChildStateFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewChildStateField.Child,
		};
		Assert.True(MuiListviewCore.MuiListviewChildStateFieldCursorCodec
			.TryGetAddress(ref platform, fieldCursor, out var cursorAddress,
				out var cursorSize));
		Assert.Equal(0x3504u, cursorAddress.Raw);
		Assert.Equal(4u, cursorSize);
	}

	[Fact]
	public void ChildAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewChildStateMemoryCodec.TryReadUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewChildStateField)0xFF, out _));
		Assert.False(MuiListviewCore.MuiListviewChildStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewChildStateField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewChildStateMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiListviewCore.MuiListviewChildStateField.Child, out _));
		Assert.False(MuiListviewCore.MuiListviewChildStateMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListviewCore.MuiListviewChildStateField.Magic,
			1));
	}
}
