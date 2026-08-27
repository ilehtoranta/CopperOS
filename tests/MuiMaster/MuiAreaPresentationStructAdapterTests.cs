using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaPresentationStructAdapterTests
{
	[Fact]
	public void AreaPresentationStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaPresentationStateRecord
		{
			Magic = MuiAreaPresentationStateRecord.Cookie,
			Disabled = 1,
			ShowMe = 0,
			Background = 0x12345678,
			Frame = 0xCAFEBABE,
			CustomBackfill = 1,
		};

		Assert.True(MuiAreaPresentationStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaPresentationStateField.Frame,
			out var frameAddress));
		Assert.Equal(0x3510u, frameAddress.Raw);
		Assert.True(MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaPresentationStateField.Background,
			0x00112233));
		Assert.True(MuiAreaPresentationStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x00112233u, decoded.Background);
		Assert.False(MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaPresentationStateField)255, out _));
		Assert.False(MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaPresentationStateField.Magic, out _));
		Assert.False(MuiAreaPresentationStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
