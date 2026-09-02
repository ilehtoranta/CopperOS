using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationRefreshStructAdapterTests
{
	[Fact]
	public void ApplicationRefreshStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationRefreshStateRecord
		{
			Magic = MuiApplicationRefreshStateRecord.Cookie,
			Checks = 3,
			RefreshedWindows = 5,
		};

		Assert.True(MuiApplicationRefreshStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationRefreshStateField.Checks,
			out var checksField));
		Assert.Equal(APTR.FromPointer(0x3504), checksField);
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationRefreshStateField.RefreshedWindows,
			out var windowsField));
		Assert.Equal(APTR.FromPointer(0x3508), windowsField);
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationRefreshStateField.Checks, 4));
		Assert.True(MuiApplicationRefreshStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(4u, decoded.Checks);
		Assert.Equal(value.RefreshedWindows, decoded.RefreshedWindows);
		Assert.False(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiApplicationRefreshStateField.Magic, out _));
		Assert.False(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationRefreshStateField.Checks, out _));
	}

	[Fact]
	public void ApplicationRefreshStateSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationRefreshStateRecord
		{
			Magic = MuiApplicationRefreshStateRecord.Cookie,
			Checks = uint.MaxValue,
			RefreshedWindows = 0xA5A5A5A5u,
		};

		Assert.True(MuiApplicationRefreshStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationRefreshStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Checks, decoded.Checks);
		Assert.Equal(value.RefreshedWindows, decoded.RefreshedWindows);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiApplicationRefreshStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationRefreshStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationRefreshFieldPathUsesCompleteRecordCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationRefreshStateRecord
		{
			Magic = 0x10203040u,
			Checks = 0x50607080u,
			RefreshedWindows = 0x90A0B0C0u,
		};

		Assert.True(MuiApplicationRefreshStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationRefreshStateField.Checks,
			0xF1020304u));
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiApplicationRefreshStateField.RefreshedWindows, out var windows));
		Assert.Equal(initial.RefreshedWindows, windows);
		Assert.True(MuiApplicationRefreshStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(0xF1020304u, updated.Checks);
		Assert.Equal(initial.RefreshedWindows, updated.RefreshedWindows);
		Assert.False(MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, unchecked((MuiApplicationRefreshStateField)255),
			1));
	}
}
