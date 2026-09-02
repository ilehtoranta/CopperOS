using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiScalePresentationStructAdapterTests
{
	[Fact]
	public void ScalePresentationStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiScalePresentationStateRecord
		{
			Magic = MuiScalePresentationStateRecord.Cookie,
			Horizontal = 1,
		};

		Assert.True(MuiScalePresentationStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiScalePresentationStateField.Horizontal,
			out var horizontalAddress));
		Assert.Equal(0x3504u, horizontalAddress.Raw);
		Assert.True(MuiScalePresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiScalePresentationStateField.Horizontal, 0));
		Assert.True(MuiScalePresentationStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(0u, decoded.Horizontal);
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.True(MuiScalePresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiScalePresentationStateField.Magic,
			out var magic));
		Assert.Equal(value.Magic, magic);
		Assert.False(MuiScalePresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiScalePresentationStateField)0xFF, out _));
		Assert.False(MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiScalePresentationStateField.Magic, out _));
		Assert.False(MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiScalePresentationStateField.Magic, out _));
	}
}
