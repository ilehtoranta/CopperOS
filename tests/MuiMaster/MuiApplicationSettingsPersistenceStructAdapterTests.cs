using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSettingsPersistenceStructAdapterTests
{
	[Fact]
	public void SettingsPersistenceStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationSettingsPersistenceStateRecord
		{
			Magic = MuiApplicationSettingsPersistenceStateRecord.Cookie,
			Operation = 1,
			Name = APTR.FromPointer(uint.MaxValue),
			Requests = 9,
			Saves = 5,
			Loads = 4,
		};

		Assert.True(MuiApplicationSettingsPersistenceStateRecordCodec.Write(
			ref platform, address, value));
		Assert.True(MuiApplicationSettingsPersistenceStateRecordMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Name,
				out var nameAddress));
		Assert.Equal(0x3508u, nameAddress.Raw);
		Assert.True(MuiApplicationSettingsPersistenceStateRecordMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Loads,
				out var loads));
		Assert.Equal(4u, loads);
		Assert.True(MuiApplicationSettingsPersistenceStateRecordMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Loads, 7));
		Assert.True(MuiApplicationSettingsPersistenceStateRecordCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(7u, decoded.Loads);
		Assert.False(MuiApplicationSettingsPersistenceStateRecordMemoryCodec
			.TryGetAddress(ref platform, APTR.FromPointer(0x30FE9),
				MuiApplicationSettingsPersistenceStateField.Magic, out _));
		Assert.False(MuiApplicationSettingsPersistenceStateRecordMemoryCodec
			.TryGetAddress(ref platform, APTR.Null,
				MuiApplicationSettingsPersistenceStateField.Magic, out _));
	}
}
