using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDirlistFieldStructAdapterTests
{
	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x80000,
			0x8000, state);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		return platform;
	}

	[Fact]
	public void DirlistByteTotalFieldsUseNamedQuadRecord()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x2200);
		Assert.True(MuiDirlistByteTotalCodec.Write(ref platform, address,
			new MuiDirlistByteTotalState { High = 0x11223344, Low = 0x55667788 }));
		Assert.True(MuiDirlistRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiDirlistRecordKind.ByteTotal, MuiDirlistRecordField.Low,
			0xF00DCAFE));
		Assert.True(MuiDirlistRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiDirlistRecordKind.ByteTotal, MuiDirlistRecordField.High,
			out var high));
		Assert.Equal(0x11223344u, high);
		Assert.True(MuiDirlistByteTotalCodec.TryRead(ref platform, address,
			out var total));
		Assert.Equal(0x11223344u, total.High);
		Assert.Equal(0xF00DCAFEu, total.Low);
	}

	[Fact]
	public void DirlistFilterFieldsPreservePointersAndRejectUnsupportedKinds()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x2300);
		var expected = new MuiDirlistFilterStateRecord
		{
			Magic = MuiDirlistFilterStateRecord.Cookie,
			AcceptPattern = APTR.FromPointer(0x3000),
			RejectPattern = APTR.FromPointer(0x3040),
			Pattern = APTR.FromPointer(0x3080),
			DrawersOnly = 1,
			FilesOnly = 0,
			FilterDrawers = 1,
			MultiSelDirs = 1,
			RejectIcons = 0,
			ExAllType = 2,
			FilterHook = APTR.FromPointer(0x3100),
		};
		Assert.True(MuiDirlistFilterStateRecordCodec.WriteRecord(ref platform,
			address, expected));
		Assert.True(MuiDirlistRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiDirlistRecordKind.FilterState,
			MuiDirlistRecordField.DrawersOnlyValue, 0));
		Assert.True(MuiDirlistRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiDirlistRecordKind.FilterState,
			MuiDirlistRecordField.FilterHookValue, out var hook));
		Assert.Equal(expected.FilterHook.Raw, hook);
		Assert.True(MuiDirlistFilterStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.AcceptPattern, actual.AcceptPattern);
		Assert.Equal(expected.RejectPattern, actual.RejectPattern);
		Assert.Equal(expected.Pattern, actual.Pattern);
		Assert.Equal(0u, actual.DrawersOnly);
		Assert.Equal(expected.FilterDrawers, actual.FilterDrawers);
		Assert.Equal(expected.FilterHook, actual.FilterHook);
		Assert.False(MuiDirlistRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiDirlistRecordKind.FilterState,
			MuiDirlistRecordField.NumFilesValue, 1));
		Assert.False(MuiDirlistRecordMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x80FFE), MuiDirlistRecordKind.FilterState,
			MuiDirlistRecordField.DrawersOnlyValue, out _));
	}
}
