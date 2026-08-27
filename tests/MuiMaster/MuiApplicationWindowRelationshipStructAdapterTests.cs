using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationWindowRelationshipStructAdapterTests
{
	[Fact]
	public void ApplicationWindowRelationshipStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationWindowRelationshipStateRecord
		{
			Magic = MuiApplicationWindowRelationshipStateRecord.Cookie,
			LastWindow = APTR.Null,
			AddedCount = uint.MaxValue,
		};

		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.Write(
			ref platform, address, value));
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiApplicationWindowRelationshipStateField.LastWindow,
				out var windowAddress));
		Assert.Equal(0x3504u, windowAddress.Raw);
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiApplicationWindowRelationshipStateField.AddedCount,
				out var addedCount));
		Assert.Equal(uint.MaxValue, addedCount);
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiApplicationWindowRelationshipStateField.AddedCount, 7));
		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(7u, decoded.AddedCount);
		Assert.False(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryGetAddress(ref platform, APTR.FromPointer(0x30FF5),
				MuiApplicationWindowRelationshipStateField.Magic, out _));
		Assert.False(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryGetAddress(ref platform, APTR.Null,
				MuiApplicationWindowRelationshipStateField.Magic, out _));
	}
}
