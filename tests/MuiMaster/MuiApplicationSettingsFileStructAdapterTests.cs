using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSettingsFileStructAdapterTests
{
	[Fact]
	public void ApplicationSettingsFileRecordsUseNamedBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var header = APTR.FromPointer(0x1200);
		Assert.True(MuiApplicationSettingsHeaderMemoryCodec.TryGetAddress(
			ref platform, header, MuiApplicationSettingsHeaderField.PayloadBytes,
			out var payloadField));
		Assert.Equal(APTR.FromPointer(0x120C), payloadField);
		Assert.True(MuiApplicationSettingsHeaderMemoryCodec.TryWrite(ref platform,
			header, MuiApplicationSettingsHeaderField.MagicValue, 0x4D554953u));
		Assert.True(MuiApplicationSettingsHeaderMemoryCodec.TryWrite(ref platform,
			header, MuiApplicationSettingsHeaderField.PayloadBytes, 64));
		Assert.True(MuiApplicationSettingsHeaderMemoryCodec.TryRead(ref platform,
			header, MuiApplicationSettingsHeaderField.MagicValue, out var magic));
		Assert.Equal(0x4D554953u, magic);
		Assert.True(MuiApplicationSettingsHeaderMemoryCodec.TryRead(ref platform,
			header, MuiApplicationSettingsHeaderField.PayloadBytes,
			out var payloadBytes));
		Assert.Equal(64u, payloadBytes);
		Assert.False(MuiApplicationSettingsHeaderMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x40FF1), MuiApplicationSettingsHeaderField.MagicValue,
			out _));
		Assert.False(MuiApplicationSettingsHeaderMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiApplicationSettingsHeaderField.VersionValue, out _));
		Assert.False(MuiApplicationSettingsHeaderMemoryCodec.TryGetAddress(ref platform,
			header, (MuiApplicationSettingsHeaderField)255, out _));

		var record = APTR.FromPointer(0x1300);
		Assert.True(MuiApplicationSettingsRecordMemoryCodec.TryGetAddress(ref platform,
			record, MuiApplicationSettingsRecordField.Length, out var lengthField));
		Assert.Equal(APTR.FromPointer(0x1304), lengthField);
		Assert.True(MuiApplicationSettingsRecordMemoryCodec.TryWrite(ref platform,
			record, MuiApplicationSettingsRecordField.Key, 0x1020));
		Assert.True(MuiApplicationSettingsRecordMemoryCodec.TryRead(ref platform,
			record, MuiApplicationSettingsRecordField.Key, out var key));
		Assert.Equal(0x1020u, key);
		Assert.False(MuiApplicationSettingsRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x40FF9),
			MuiApplicationSettingsRecordField.Length, out _));
		Assert.False(MuiApplicationSettingsRecordMemoryCodec.TryGetAddress(
			ref platform, record, (MuiApplicationSettingsRecordField)255, out _));
	}
}
