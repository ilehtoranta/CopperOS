using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDirlistAdmissionTests
{
	[Fact]
	public void DirlistSortFilterAndScanRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var sortAddress = APTR.FromPointer(0x1500);
		var filterAddress = APTR.FromPointer(0x1520);
		var scanAddress = APTR.FromPointer(0x1560);
		var sort = new MuiDirlistSortStateRecord
		{
			Magic = MuiDirlistSortStateRecord.Cookie,
			SortType = 3,
			SortDirs = 2,
			SortHighLow = 1,
		};
		var filter = new MuiDirlistFilterStateRecord
		{
			Magic = MuiDirlistFilterStateRecord.Cookie,
			AcceptPattern = APTR.Null,
			RejectPattern = APTR.Null,
			Pattern = APTR.Null,
			DrawersOnly = 1,
			FilesOnly = 0,
			FilterDrawers = 1,
			MultiSelDirs = 0,
			RejectIcons = 1,
			ExAllType = 2,
			FilterHook = APTR.Null,
		};
		var scan = new MuiDirlistScanStateRecord
		{
			Magic = MuiDirlistScanStateRecord.Cookie,
			Status = MuiDirlistCore.StatusValid,
			NumFiles = 8,
			NumDrawers = 2,
			NumBytes = 4096,
			IoErr = 0,
		};
		Assert.True(MuiDirlistSortStateRecordCodec.Write(ref platform, sortAddress,
			sort));
		Assert.True(MuiDirlistFilterStateRecordCodec.Write(ref platform,
			filterAddress, filter));
		Assert.True(MuiDirlistScanStateRecordCodec.Write(ref platform, scanAddress,
			scan));
		Assert.True(MuiDirlistSortStateRecordCodec.TryRead(ref platform,
			sortAddress, out var sortRead));
		Assert.True(MuiDirlistFilterStateRecordCodec.TryRead(ref platform,
			filterAddress, out var filterRead));
		Assert.True(MuiDirlistScanStateRecordCodec.TryRead(ref platform,
			scanAddress, out var scanRead));
		Assert.Equal(sort.SortType, sortRead.SortType);
		Assert.Equal(sort.SortDirs, sortRead.SortDirs);
		Assert.Equal(sort.SortHighLow, sortRead.SortHighLow);
		Assert.Equal(filter.DrawersOnly, filterRead.DrawersOnly);
		Assert.Equal(filter.FilterDrawers, filterRead.FilterDrawers);
		Assert.Equal(filter.ExAllType, filterRead.ExAllType);
		Assert.Equal(scan.Status, scanRead.Status);
		Assert.Equal(scan.NumFiles, scanRead.NumFiles);
		Assert.Equal(scan.NumDrawers, scanRead.NumDrawers);
		Assert.Equal(scan.NumBytes, scanRead.NumBytes);
	}

	[Fact]
	public void MalformedDirlistMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreatePlatform();
		var sortAddress = APTR.FromPointer(0x1600);
		var filterAddress = APTR.FromPointer(0x1620);
		var scanAddress = APTR.FromPointer(0x1660);
		Assert.True(MuiDirlistSortStateRecordCodec.Write(ref platform, sortAddress,
			new MuiDirlistSortStateRecord
			{
				Magic = MuiDirlistSortStateRecord.Cookie,
				SortType = 0,
				SortDirs = 0,
				SortHighLow = 0,
			}));
		Assert.True(MuiDirlistFilterStateRecordCodec.Write(ref platform,
			filterAddress, new MuiDirlistFilterStateRecord
			{
				Magic = MuiDirlistFilterStateRecord.Cookie,
				AcceptPattern = APTR.Null,
				RejectPattern = APTR.Null,
				Pattern = APTR.Null,
				DrawersOnly = 0,
				FilesOnly = 0,
				FilterDrawers = 0,
				MultiSelDirs = 0,
				RejectIcons = 0,
				ExAllType = 0,
				FilterHook = APTR.Null,
			}));
		Assert.True(MuiDirlistScanStateRecordCodec.Write(ref platform, scanAddress,
			new MuiDirlistScanStateRecord
			{
				Magic = MuiDirlistScanStateRecord.Cookie,
				Status = MuiDirlistCore.StatusInvalid,
				NumFiles = 0,
				NumDrawers = 0,
				NumBytes = 0,
				IoErr = 0,
			}));
		Assert.True(MuiDirlistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			sortAddress, MuiDirlistRecordKind.SortState,
			MuiDirlistRecordField.Magic, 0));
		Assert.True(MuiDirlistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			filterAddress, MuiDirlistRecordKind.FilterState,
			MuiDirlistRecordField.Magic, 0));
		Assert.True(MuiDirlistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			scanAddress, MuiDirlistRecordKind.ScanState,
			MuiDirlistRecordField.Magic, 0));
		Assert.True(MuiDirlistSortStateRecordCodec.TryReadStructural(ref platform,
			sortAddress, out var sort));
		Assert.True(MuiDirlistFilterStateRecordCodec.TryReadStructural(ref platform,
			filterAddress, out var filter));
		Assert.True(MuiDirlistScanStateRecordCodec.TryReadStructural(ref platform,
			scanAddress, out var scan));
		Assert.Equal(0u, sort.Magic);
		Assert.Equal(0u, filter.Magic);
		Assert.Equal(0u, scan.Magic);
		Assert.False(MuiDirlistSortStateRecordCodec.TryRead(ref platform,
			sortAddress, out _));
		Assert.False(MuiDirlistFilterStateRecordCodec.TryRead(ref platform,
			filterAddress, out _));
		Assert.False(MuiDirlistScanStateRecordCodec.TryRead(ref platform, scanAddress,
			out _));
		Assert.False(MuiDirlistStateAdmission.ValidateSort(sort));
		Assert.False(MuiDirlistStateAdmission.ValidateFilter(filter));
		Assert.False(MuiDirlistStateAdmission.ValidateScan(scan));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
