using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSettingsPanelStructAdapterTests
{
	[Fact]
	public void ApplicationSettingsPanelStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationSettingsPanelStateRecord
		{
			Magic = MuiApplicationSettingsPanelStateRecord.Cookie,
			Number = 9,
			Panel = APTR.Null,
			Requests = 4,
		};

		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationSettingsPanelStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationSettingsPanelStateField.Panel,
			out var panelAddress));
		Assert.Equal(0x3508u, panelAddress.Raw);
		Assert.True(MuiApplicationSettingsPanelStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSettingsPanelStateField.Panel,
			0x12345678));
		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x12345678u, decoded.Panel.Raw);
		Assert.False(MuiApplicationSettingsPanelStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiApplicationSettingsPanelStateField)255,
			out _));
		Assert.False(MuiApplicationSettingsPanelStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationSettingsPanelStateField.Magic,
			out _));
		Assert.False(MuiApplicationSettingsPanelStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ApplicationSettingsPanelSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationSettingsPanelStateRecord
		{
			Magic = MuiApplicationSettingsPanelStateRecord.Cookie,
			Number = uint.MaxValue,
			Panel = APTR.FromPointer(uint.MaxValue),
			Requests = 0xA5A5A5A5u,
		};

		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Number, decoded.Number);
		Assert.Equal(value.Panel, decoded.Panel);
		Assert.Equal(value.Requests, decoded.Requests);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiApplicationSettingsPanelStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationSettingsPanelStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}
}
