using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewExternalScrollerConnectionStructAdapterTests
{
	[Fact]
	public void ConnectionFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewExternalScrollerConnectionState
		{
			Magic = MuiListviewCore.MuiListviewExternalScrollerConnectionState.Cookie,
			Prop = APTR.FromPointer(0x36200),
		};

		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec
			.WriteRecord(ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop,
				0x36400));
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop,
				out var prop));
		Assert.Equal(0x36400u, prop);

		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x36400u, decoded.Prop.Raw);
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop,
				out var propAddress));
		Assert.Equal(0x3504u, propAddress.Raw);
	}

	[Fact]
	public void ConnectionAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryReadUInt32(ref platform, address,
				(MuiListviewCore.MuiListviewExternalScrollerConnectionField)0xFF, out _));
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryWriteUInt32(ref platform, address,
				(MuiListviewCore.MuiListviewExternalScrollerConnectionField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryReadUInt32(ref platform, APTR.FromPointer(0x30FFC),
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop, out _));
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryWriteUInt32(ref platform, APTR.Null,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Magic, 1));
	}
}
