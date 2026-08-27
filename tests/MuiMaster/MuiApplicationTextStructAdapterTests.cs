using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationTextStructAdapterTests
{
	[Fact]
	public void ApplicationTextStructAdapterUsesNamedPointerFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var helpFile = APTR.FromPointer(0x3600);
		var iconifyTitle = APTR.FromPointer(0x3620);
		platform.WriteCString(helpFile, "SYS:Help");
		platform.WriteCString(iconifyTitle, "CopperOS");
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationTextStateRecord
		{
			Magic = MuiApplicationTextStateRecord.Cookie,
			HelpFile = helpFile,
			IconifyTitle = iconifyTitle,
		};

		Assert.True(MuiApplicationTextStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiApplicationTextStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationTextStateField.IconifyTitle,
			out var titleAddress));
		Assert.Equal(0x3508u, titleAddress.Raw);
		Assert.True(MuiApplicationTextStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationTextStateField.HelpFile,
			out var helpFileRaw));
		Assert.Equal(helpFile.Raw, helpFileRaw);
		Assert.True(MuiApplicationTextStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationTextStateField.IconifyTitle,
			0x3624));
		Assert.True(MuiApplicationTextStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x3624u, decoded.IconifyTitle.Raw);
		Assert.False(MuiApplicationTextStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF5),
			MuiApplicationTextStateField.Magic, out _));
		Assert.False(MuiApplicationTextStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationTextStateField.Magic, out _));
	}
}
