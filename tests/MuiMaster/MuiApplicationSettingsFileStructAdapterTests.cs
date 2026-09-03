using System.Runtime.InteropServices;
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

	[Fact]
	public void ApplicationSettingsFileStructCodecsRoundTripNamedValues()
	{
		Assert.Equal(16, Marshal.SizeOf<MuiApplicationSettingsHeader>());
		Assert.Equal(8, Marshal.SizeOf<MuiApplicationSettingsRecord>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var headerAddress = APTR.FromPointer(0x2200);
		var recordAddress = APTR.FromPointer(0x2240);
		var header = new MuiApplicationSettingsHeader
		{
			MagicValue = 0x4D554953u,
			VersionValue = 0x80000001u,
			RecordCount = 0xFEDCBA98u,
			PayloadBytes = 0x81234567u,
		};
		var record = new MuiApplicationSettingsRecord
		{
			Key = 0xF1234567u,
			Length = 0x80000011u,
		};
		Assert.True(MuiApplicationSettingsHeaderStructCodec.Write(ref platform,
			headerAddress, header));
		Assert.True(MuiApplicationSettingsHeaderStructCodec.TryRead(ref platform,
			headerAddress, out var readHeader));
		Assert.Equal(header.MagicValue, readHeader.MagicValue);
		Assert.Equal(header.VersionValue, readHeader.VersionValue);
		Assert.Equal(header.RecordCount, readHeader.RecordCount);
		Assert.Equal(header.PayloadBytes, readHeader.PayloadBytes);
		Assert.True(MuiApplicationSettingsRecordStructCodec.Write(ref platform,
			recordAddress, record));
		Assert.True(MuiApplicationSettingsRecordStructCodec.TryRead(ref platform,
			recordAddress, out var readRecord));
		Assert.Equal(record.Key, readRecord.Key);
		Assert.Equal(record.Length, readRecord.Length);

		// The public production wrappers use the same named sequential codecs.
		Assert.True(MuiApplicationSettingsHeaderCodec.TryRead(ref platform,
			headerAddress, out var wrappedHeader));
		Assert.Equal(header.PayloadBytes, wrappedHeader.PayloadBytes);
		Assert.True(MuiApplicationSettingsRecordCodec.TryRead(ref platform,
			recordAddress, out var wrappedRecord));
		Assert.Equal(record.Key, wrappedRecord.Key);
		Assert.False(MuiApplicationSettingsHeaderStructCodec.TryRead(ref platform,
			APTR.FromPointer(0x40FF1), out _));
		Assert.False(MuiApplicationSettingsRecordStructCodec.TryRead(ref platform,
			APTR.FromPointer(0x40FF9), out _));
		Assert.False(MuiApplicationSettingsHeaderStructCodec.Write(ref platform,
			APTR.Null, header));
	}

	[Fact]
	public void ApplicationSettingsFieldAccessUsesCompleteNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var headerAddress = APTR.FromPointer(0x2400);
		var header = new MuiApplicationSettingsHeader
		{
			MagicValue = MuiApplicationSettingsFileCore.Magic,
			VersionValue = MuiApplicationSettingsFileCore.Version,
			RecordCount = 3,
			PayloadBytes = 64,
		};
		Assert.True(MuiApplicationSettingsHeaderCodec.WriteStructural(ref platform,
			headerAddress, header));
		Assert.True(MuiApplicationSettingsHeaderMemoryCodec.TryWrite(ref platform,
			headerAddress, MuiApplicationSettingsHeaderField.RecordCount, 4));
		Assert.True(MuiApplicationSettingsHeaderMemoryCodec.TryRead(ref platform,
			headerAddress, MuiApplicationSettingsHeaderField.MagicValue,
			out var magic));
		Assert.Equal(header.MagicValue, magic);
		Assert.True(MuiApplicationSettingsHeaderCodec.TryReadStructural(ref platform,
			headerAddress, out var headerAfter));
		Assert.Equal(4u, headerAfter.RecordCount);
		Assert.Equal(header.PayloadBytes, headerAfter.PayloadBytes);

		var recordAddress = APTR.FromPointer(0x2440);
		var record = new MuiApplicationSettingsRecord { Key = 9, Length = 12 };
		Assert.True(MuiApplicationSettingsRecordCodec.WriteStructural(ref platform,
			recordAddress, record));
		Assert.True(MuiApplicationSettingsRecordMemoryCodec.TryWrite(ref platform,
			recordAddress, MuiApplicationSettingsRecordField.Length, 16));
		Assert.True(MuiApplicationSettingsRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var recordAfter));
		Assert.Equal(record.Key, recordAfter.Key);
		Assert.Equal(16u, recordAfter.Length);
		Assert.False(MuiApplicationSettingsHeaderMemoryCodec.TryRead(ref platform,
			headerAddress, (MuiApplicationSettingsHeaderField)255, out _));
		Assert.False(MuiApplicationSettingsRecordMemoryCodec.TryWrite(ref platform,
			APTR.FromPointer(0x40FF9), MuiApplicationSettingsRecordField.Key, 1));
	}
}
