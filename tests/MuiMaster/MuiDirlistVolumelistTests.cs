using System.Text;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Focused MG08 coverage for Dirlist.mui and Volumelist.mui built on the shared
// MuiListCore backbone and the narrow IMuiDirectoryCapability seam. Exercises
// class-aware construction, synchronous bounded scans, status/counters,
// filters/patterns/sort, FilterHook override, clean missing-directory and
// mid-scan failures with IoErr, path computation, the MUIM_Dirlist_* methods,
// failure-atomic owned-record disposal, and Volumelist population/ExampleMode.
public sealed class MuiDirlistVolumelistTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	private const uint Directory = 0x8042ea41u;
	private const uint Status = 0x804240deu;
	private const uint NumFiles = 0x8042a6f0u;
	private const uint NumDrawers = 0x80429cb8u;
	private const uint NumBytes = 0x80429e26u;
	private const uint NumBytes64 = 0x80428050u;
	private const uint PathAttr = 0x80426176u;
	private const uint DrawersOnly = 0x8042b379u;
	private const uint FilesOnly = 0x8042896au;
	private const uint RejectIcons = 0x80424808u;
	private const uint FilterDrawers = 0x80424ad2u;
	private const uint AcceptPattern = 0x8042760au;
	private const uint RejectPattern = 0x804259c7u;
	private const uint SortDirs = 0x8042bbb9u;
	private const uint SortHighLow = 0x80421896u;
	private const uint SortType = 0x804228bcu;
	private const uint FilterHook = 0x8042ae19u;
	private const uint ExampleMode = 0x804246a5u;
	private const uint ListActive = 0x8042391cu;

	private const uint ReReadMethod = 0x80422d71u;

	private const uint StatusInvalid = 0;
	private const uint StatusValid = 2;

	private const uint SortDirsLast = 1;

	// -------------------------------------------------------------- common

	[Fact]
	public void ClassifierIdentifiesDirlistAndVolumelist()
	{
		var platform = CreatePlatform(out var dirlistClass, out var volumelistClass,
			out _);
		Assert.Equal(MuiCollectionClass.Dirlist, MuiListCore.ClassifyRecord(
			ref platform, dirlistClass));
		Assert.Equal(MuiCollectionClass.Volumelist, MuiListCore.ClassifyRecord(
			ref platform, volumelistClass));
	}

	[Fact]
	public void DirlistPacketCodecUsesNamedRecordsAndRejectsMalformedPackets()
	{
		var p = CreatePlatform(out _, out _, out _);
		var packet = APTR.FromPointer(0x2600);

		Assert.True(MuiDirlistMessageCodec.WriteSet(ref p, packet,
			MuiDirlistMessageCodec.Set, 0x8042ea41u, 0x2700));
		Assert.True(MuiDirlistMessageCodec.TryReadSet(ref p, packet,
			MuiDirlistMessageCodec.Set, out var set));
		Assert.Equal(0x8042ea41u, set.Attribute);
		Assert.Equal(0x2700u, set.Value);

		Assert.True(MuiDirlistMessageCodec.WriteRename(ref p, packet,
			MuiDirlistMessageCodec.SetComment, 3, 0x2800));
		Assert.True(MuiDirlistMessageCodec.TryReadRename(ref p, packet,
			MuiDirlistMessageCodec.SetComment, out var comment));
		Assert.Equal(3u, comment.Entry);
		Assert.Equal(0x2800u, comment.Name);

		Assert.True(MuiDirlistMessageCodec.WriteProtection(ref p, packet,
			7, 0x12345678));
		Assert.True(MuiDirlistMessageCodec.TryReadProtection(ref p, packet,
			out var protection));
		Assert.Equal(7u, protection.Entry);
		Assert.Equal(0x12345678u, protection.Protection);

		Assert.True(MuiDirlistMessageCodec.WriteGetEntry(ref p, packet,
			unchecked((uint)-2), 0x2900));
		Assert.True(MuiDirlistMessageCodec.TryReadGetEntry(ref p, packet,
			out var getEntry));
		Assert.Equal(unchecked((uint)-2), getEntry.Position);
		Assert.Equal(0x2900u, getEntry.Storage);

		Assert.True(MuiDirlistMessageCodec.WriteMethod(ref p, packet,
			MuiDirlistMessageCodec.ReRead));
		Assert.True(MuiDirlistMessageCodec.IsValidMethod(ref p, packet,
			MuiDirlistMessageCodec.ReRead));
		Assert.True(MuiDirlistMessageCodec.WriteMethod(ref p, packet,
			MuiDirlistMessageCodec.ListClear));
		Assert.True(MuiDirlistMessageCodec.IsValidMethod(ref p, packet,
			MuiDirlistMessageCodec.ListClear));

		Assert.False(MuiDirlistMessageCodec.WriteSet(ref p, packet,
			0x80420000u, 1, 2));
		Assert.False(MuiDirlistMessageCodec.TryReadGetEntry(ref p,
			APTR.FromPointer(0x80FFF), out _));
		Assert.False(MuiDirlistMessageCodec.IsValidMethod(ref p, packet,
			0x80420000u));
	}

	[Fact]
	public void DirlistMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _, out _, out _);
		var address = APTR.FromPointer(0x2A00);
		Assert.True(MuiDirlistMessageCodec.WriteMethod(ref platform, address,
			MuiDirlistMessageCodec.ReRead));
		Assert.True(MuiDirlistMessageCodec.TryReadMethodId(ref platform, address,
			out var packet));
		Assert.Equal(MuiDirlistMessageCodec.ReRead, packet.MethodId);
		Assert.False(MuiDirlistMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void DirlistMethodReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _, out _, out _);
		var packet = APTR.FromPointer(0x2A00);
		Assert.True(MuiDirlistMessageCodec.WriteSet(ref platform, packet,
			MuiDirlistMessageCodec.Set, 7, 9));
		Assert.True(MuiDirlistMessageCodec.TryReadSet(ref platform, packet,
			MuiDirlistMessageCodec.Set, out var set));
		Assert.Equal(MuiDirlistMessageCodec.Set, set.MethodId);
		Assert.False(MuiDirlistMessageCodec.TryReadSet(ref platform, packet,
			MuiDirlistMessageCodec.NoNotifySet, out _));

		Assert.True(MuiDirlistMessageCodec.WriteMethod(ref platform, packet,
			MuiDirlistMessageCodec.ReRead));
		Assert.True(MuiDirlistMessageCodec.TryReadMethod(ref platform, packet,
			MuiDirlistMessageCodec.ReRead, out var method));
		Assert.Equal(MuiDirlistMessageCodec.ReRead, method.MethodId);
		Assert.False(MuiDirlistMessageCodec.TryReadMethod(ref platform, packet,
			MuiDirlistMessageCodec.ListClear, out _));
	}

	[Fact]
	public void DirlistFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = CreatePlatform(out _, out _, out _);
		var packet = APTR.FromPointer(0x2A00);
		var cursor = default(MuiDirlistFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiDirlistPacketKind.Set;
		cursor.Field = MuiDirlistField.MethodId;
		Assert.True(MuiDirlistFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(0x2A00u, address.Raw);
		cursor.Field = MuiDirlistField.Attribute;
		Assert.True(MuiDirlistFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(0x2A04u, address.Raw);
		cursor.Field = MuiDirlistField.Value;
		Assert.True(MuiDirlistFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(0x2A08u, address.Raw);

		Assert.True(MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiDirlistPacketKind.GetEntry, MuiDirlistField.Position,
			unchecked((uint)-2)));
		Assert.True(MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiDirlistPacketKind.GetEntry, MuiDirlistField.Position,
			out var position));
		Assert.Equal(unchecked((uint)-2), position);
		cursor.Packet = MuiDirlistPacketKind.Protection;
		cursor.Field = MuiDirlistField.Attribute;
		Assert.False(MuiDirlistFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiDirlistPacketKind.Rename;
		cursor.Field = MuiDirlistField.Name;
		Assert.False(MuiDirlistFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void DirlistFixedWireCodecsUseNamedFields()
	{
		var platform = CreatePlatform(out _, out _, out _);
		var entryAddress = APTR.FromPointer(0x2A00);
		var entry = default(MuiDirlistEntryWireState);
		entry.RecordSize = 64;
		entry.Type = -3;
		entry.SizeLow = 0x1234;
		entry.SizeHigh = 2;
		entry.Protection = 0xF0;
		entry.Days = 10;
		entry.Mins = 20;
		entry.Ticks = 30;
		entry.CommentOffset = 48;
		Assert.True(MuiDirlistEntryWireCodec.Write(ref platform, entryAddress,
			entry));
		Assert.True(MuiDirlistEntryWireCodec.TryRead(ref platform, entryAddress,
			out var readEntry));
		Assert.Equal(entry.RecordSize, readEntry.RecordSize);
		Assert.Equal(entry.Type, readEntry.Type);
		Assert.Equal(entry.SizeLow, readEntry.SizeLow);
		Assert.Equal(entry.SizeHigh, readEntry.SizeHigh);
		Assert.Equal(entry.Protection, readEntry.Protection);
		Assert.Equal(entry.Days, readEntry.Days);
		Assert.Equal(entry.Mins, readEntry.Mins);
		Assert.Equal(entry.Ticks, readEntry.Ticks);
		Assert.Equal(entry.CommentOffset, readEntry.CommentOffset);

		var scanAddress = APTR.FromPointer(0x2B00);
		var scan = default(MuiDirlistScanEntryWireState);
		scan.Type = 2;
		scan.SizeLow = 0x5678;
		scan.SizeHigh = 4;
		scan.Protection = 7;
		scan.Days = 11;
		scan.Mins = 22;
		scan.Ticks = 33;
		Assert.True(MuiDirlistScanEntryWireCodec.Write(ref platform,
			scanAddress, scan));
		Assert.True(MuiDirlistScanEntryWireCodec.TryRead(ref platform,
			scanAddress, out var readScan));
		Assert.Equal(scan.Type, readScan.Type);
		Assert.Equal(scan.SizeLow, readScan.SizeLow);
		Assert.Equal(scan.SizeHigh, readScan.SizeHigh);
		Assert.Equal(scan.Protection, readScan.Protection);
		Assert.Equal(scan.Days, readScan.Days);
		Assert.Equal(scan.Mins, readScan.Mins);
		Assert.Equal(scan.Ticks, readScan.Ticks);
		Assert.False(MuiDirlistEntryWireCodec.TryRead(ref platform, APTR.Null,
			out _));
		Assert.False(MuiDirlistScanEntryWireCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void DirlistRecordFieldCursorUsesSemanticWireKinds()
	{
		var platform = CreatePlatform(out _, out _, out _);
		var cursor = default(MuiDirlistRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x2C00);
		cursor.Record = MuiDirlistRecordKind.ByteTotal;
		cursor.Field = MuiDirlistRecordField.Low;
		Assert.True(MuiDirlistRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(0x2C04u, fieldAddress.Raw);
		cursor.Record = MuiDirlistRecordKind.EntryWire;
		cursor.Field = MuiDirlistRecordField.CommentOffset;
		Assert.True(MuiDirlistRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress));
		Assert.Equal(0x2C20u, fieldAddress.Raw);
		Assert.True(MuiDirlistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			cursor.Address, MuiDirlistRecordKind.ScanEntryWire,
			MuiDirlistRecordField.SizeHigh, 0xAABBCCDDu));
		Assert.True(MuiDirlistRecordFieldCursorCodec.TryReadUInt32(ref platform,
			cursor.Address, MuiDirlistRecordKind.ScanEntryWire,
			MuiDirlistRecordField.SizeHigh, out var sizeHigh));
		Assert.Equal(0xAABBCCDDu, sizeHigh);
		cursor.Record = MuiDirlistRecordKind.ScanEntryWire;
		cursor.Field = MuiDirlistRecordField.CommentOffset;
		Assert.False(MuiDirlistRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiDirlistRecordField.Type;
		Assert.False(MuiDirlistRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	// --------------------------------------------------------------- Dirlist

	[Fact]
	public void DirlistScanReportsValidStatusCountersAndEntries()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.Equal(StatusValid, Get(ref platform, dirlist, Status));
		Assert.Equal(5u, MuiListCore.EntryCount(ref platform, State, dirlist));
		Assert.Equal(3u, Get(ref platform, dirlist, NumFiles));
		Assert.Equal(2u, Get(ref platform, dirlist, NumDrawers));
		Assert.Equal(600u, Get(ref platform, dirlist, NumBytes)); // 300+200+100
	}

	[Fact]
	public void NamedDirlistFilterStateTracksOwnedPatternsAndCanonicalFlags()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		var accept = APTR.FromPointer(0x4300);
		platform.WriteCString(accept, "*.info");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			AcceptPattern, accept.Raw));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			DrawersOnly, 7));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			FilesOnly, 0));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			FilterDrawers, 3));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			RejectIcons, 5));
		Assert.True(MuiDirlistCore.TryReadFilterState(ref platform, State, dirlist,
			out var filter));
		Assert.NotEqual(APTR.Null, filter.AcceptPattern);
		Assert.NotEqual(accept.Raw, filter.AcceptPattern.Raw);
		Assert.Equal(1u, filter.DrawersOnly);
		Assert.Equal(0u, filter.FilesOnly);
		Assert.Equal(1u, filter.FilterDrawers);
		Assert.Equal(1u, filter.RejectIcons);
		Assert.Equal(0u, filter.MultiSelDirs);
		Assert.Equal(0u, filter.ExAllType);
		Assert.Equal(APTR.Null, filter.FilterHook);
	}

	[Fact]
	public void NamedDirlistSortStateCanonicalizesSelectors()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortType, 99));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortDirs, 99));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortHighLow, 7));
		Assert.True(MuiDirlistCore.TryReadSortState(ref platform, State, dirlist,
			out var sort));
		Assert.Equal(0u, sort.SortType);
		Assert.Equal(0u, sort.SortDirs);
		Assert.Equal(1u, sort.SortHighLow);

		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortType, 2));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortDirs, 1));
		Assert.True(MuiDirlistCore.TryReadSortState(ref platform, State, dirlist,
			out sort));
		Assert.Equal(2u, sort.SortType);
		Assert.Equal(1u, sort.SortDirs);
	}

	[Fact]
	public void DirlistSortUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.TryGetSortStateRecord(ref platform, State,
			dirlist, out var record));
		Assert.Equal(MuiDirlistSortStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.SortType);
		Assert.Equal(0u, record.SortDirs);

		// A raw backing-word write does not replace the canonical guest record.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, dirlist,
			SortType, 4, false));
		Assert.True(MuiDirlistCore.TryReadSortState(ref platform, State, dirlist,
			out var state));
		Assert.Equal(0u, state.SortType);
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortType, 2));
		Assert.True(MuiDirlistCore.TryGetSortStateRecord(ref platform, State,
			dirlist, out record));
		Assert.Equal(2u, record.SortType);
	}

	[Fact]
	public void DirlistFilterUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.TryGetFilterStateRecord(ref platform, State,
			dirlist, out var record));
		Assert.Equal(MuiDirlistFilterStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.DrawersOnly);
		Assert.Equal(0u, record.ExAllType);

		// Raw backing writes remain outside the canonical filter record until the
		// class-aware setter crosses the synchronization boundary.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, dirlist,
			DrawersOnly, 9, false));
		Assert.True(MuiDirlistCore.TryReadFilterState(ref platform, State, dirlist,
			out var state));
		Assert.Equal(0u, state.DrawersOnly);

		var accept = APTR.FromPointer(0x4380);
		platform.WriteCString(accept, "*.info");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			AcceptPattern, accept.Raw));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			DrawersOnly, 1));
		Assert.True(MuiDirlistCore.TryGetFilterStateRecord(ref platform, State,
			dirlist, out record));
		Assert.Equal(1u, record.DrawersOnly);
		Assert.NotEqual(APTR.Null, record.AcceptPattern);
		Assert.NotEqual(accept.Raw, record.AcceptPattern.Raw);
	}

	[Fact]
	public void DirlistPolicyGettersPreferNamedRecordsAndOmGetUsesProjection()
	{
		var platform = CreatePlatform(out var dirlistClass, out var volumelistClass,
			out _);
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			DrawersOnly, 1));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortDirs, SortDirsLast));
		Assert.True(MuiDirlistCore.TryGetFilterStateRecord(ref platform, State,
			dirlist, out var filter));
		Assert.True(MuiDirlistCore.TryGetSortStateRecord(ref platform, State,
			dirlist, out var sort));

		// Direct raw writes cannot replace the canonical named policy records.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, dirlist,
			DrawersOnly, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, dirlist,
			SortDirs, 0, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, dirlist,
			DrawersOnly, out var drawersOnly));
		Assert.Equal(filter.DrawersOnly, drawersOnly);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, dirlist,
			SortDirs, out var sortDirs));
		Assert.Equal(sort.SortDirs, sortDirs);

		var message = APTR.FromPointer(0x7800);
		var storage = APTR.FromPointer(0x7900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute, DrawersOnly));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		Assert.True(MuiDirlistDispatcher.TryDispatchPacket(ref platform, State,
			dirlist, message, out var result));
		Assert.Equal(1u, result);
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var stored));
		Assert.Equal(filter.DrawersOnly, stored.Value);

		// Volumelist inherits the Dirlist projection and adds its own named
		// ExampleMode record to the class-gated generic getter path.
		var tags = APTR.FromPointer(0x7A00);
		platform.WriteUInt32(tags, 0, ExampleMode);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var volumes = MuiVolumelistCore.CreateVolumelist(ref platform, State,
			volumelistClass, tags);
		Assert.NotEqual(APTR.Null, volumes);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, volumes,
			ExampleMode, 0, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, volumes,
			ExampleMode, out var exampleMode));
		Assert.Equal(1u, exampleMode);
	}

	[Fact]
	public void DirlistScanUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.TryGetScanStateRecord(ref platform, State,
			dirlist, out var record));
		Assert.Equal(MuiDirlistScanStateRecord.Cookie, record.Magic);
		Assert.Equal(StatusValid, record.Status);
		Assert.Equal(3u, record.NumFiles);
		Assert.Equal(2u, record.NumDrawers);

		var published = default(MuiDirlistScanState);
		published.Status = MuiDirlistCore.StatusReading;
		published.NumFiles = 7;
		published.NumDrawers = 4;
		published.NumBytes = 1234;
		published.IoErr = 205;
		MuiDirlistCore.PublishScanState(ref platform, State, dirlist, published,
			false);
		Assert.True(MuiDirlistCore.TryReadScanState(ref platform, State, dirlist,
			out var state));
		Assert.Equal(MuiDirlistCore.StatusReading, state.Status);
		Assert.Equal(7u, state.NumFiles);
		Assert.Equal(4u, state.NumDrawers);
		Assert.Equal(1234u, state.NumBytes);
		Assert.Equal(205, state.IoErr);
	}

	[Fact]
	public void NamedDirlistScanStatePublishesValidAndInvalidResults()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.TryReadScanState(ref platform, State, dirlist,
			out var scan));
		Assert.Equal(StatusValid, scan.Status);
		Assert.Equal(3u, scan.NumFiles);
		Assert.Equal(2u, scan.NumDrawers);
		Assert.Equal(600u, scan.NumBytes);
		Assert.Equal(0, scan.IoErr);

		var missing = APTR.FromPointer(0x4500);
		platform.WriteCString(missing, "Missing:");
		platform.DirectoryMissing = true;
		Assert.False(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			Directory, missing.Raw));
		Assert.True(MuiDirlistCore.TryReadScanState(ref platform, State, dirlist,
			out scan));
		Assert.Equal(0u, scan.Status);
		Assert.Equal(0u, scan.NumFiles);
		Assert.Equal(0u, scan.NumDrawers);
		Assert.Equal(0u, scan.NumBytes);
		Assert.Equal(205, scan.IoErr);
	}

	[Fact]
	public void NamedDirlistEntryStateDecodesOwnedRecordFields()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		var entry = MuiListCore.GetEntry(ref platform, State, dirlist, 0,
			APTR.Null);
		Assert.True(MuiDirlistCore.TryReadEntryState(ref platform, entry,
			out var record));
		Assert.Equal(entry.Raw, record.Address.Raw);
		Assert.Equal(48u, record.RecordSize);
		Assert.Equal(2, record.Type);
		Assert.Equal(0u, record.SizeLow);
		Assert.Equal(0u, record.SizeHigh);
		Assert.Equal("drawerA", ReadCString(ref platform, record.Name));
		Assert.Equal(7u, record.NameLength);
		Assert.Equal(0u, record.CommentLength);
		Assert.Equal(string.Empty, ReadCString(ref platform, record.Comment));

		var malformed = APTR.FromPointer(0x7000);
		platform.WriteUInt32(malformed, 0, 4);
		Assert.False(MuiDirlistCore.TryReadEntryState(ref platform, malformed,
			out _));
	}

	[Fact]
	public void NamedDirlistScanEntryStateDecodesCapabilityScratch()
	{
		var platform = CreatePlatform(out _, out _, out _);
		platform.DirectoryCount = 5;
		var scratch = APTR.FromPointer(0x7100);
		Assert.True(platform.DirectoryEntry(APTR.FromPointer(0x5000), 0,
			scratch));
		Assert.True(MuiDirlistCore.TryReadScanEntryState(ref platform, scratch,
			out var entry));
		Assert.Equal(scratch.Raw, entry.Address.Raw);
		Assert.Equal(2, entry.Type);
		Assert.Equal(0u, entry.SizeLow);
		Assert.Equal(7u, entry.NameLength);
		Assert.Equal("drawerB", ReadCString(ref platform, entry.Name));
		Assert.Equal(0u, entry.CommentLength);
		entry.Type = -3;
		Assert.True(MuiDirlistCore.WriteScanEntryState(ref platform, scratch,
			entry));
		Assert.True(MuiDirlistCore.TryReadScanEntryState(ref platform, scratch,
			out var rewritten));
		Assert.Equal(-3, rewritten.Type);

		platform.Clear(scratch, MuiDirlistCore.ScanEntrySize);
		for (var i = 0; i < 108; i++)
			platform.WriteUInt8(scratch, MuiDirlistCore.ScanName + i, (byte)'x');
		Assert.False(MuiDirlistCore.TryReadScanEntryState(ref platform, scratch,
			out _));
	}

	[Fact]
	public void DirlistDrawersOnlyAndFilesOnlyFilterEntries()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var drawers = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, drawers,
			DrawersOnly, 1));
		Assert.True(MuiDirlistCore.ReRead(ref platform, State, drawers));
		Assert.Equal(2u, MuiListCore.EntryCount(ref platform, State, drawers));

		var files = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, files,
			FilesOnly, 1));
		Assert.True(MuiDirlistCore.ReRead(ref platform, State, files));
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, files));
	}

	[Fact]
	public void DirlistRejectIconsExcludesInfoFiles()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			RejectIcons, 1));
		Assert.True(MuiDirlistCore.ReRead(ref platform, State, dirlist));
		// beta.info is dropped: two files remain plus the two drawers.
		Assert.Equal(4u, MuiListCore.EntryCount(ref platform, State, dirlist));
		Assert.Equal(2u, Get(ref platform, dirlist, NumFiles));
	}

	[Fact]
	public void DirlistAcceptAndRejectPatternsFilterFiles()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var accept = CreateDirlist(ref platform, dirlistClass, "Data:");
		var pattern = WriteString(ref platform, 0x6000, "#?.txt");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, accept,
			AcceptPattern, pattern.Raw));
		Assert.True(MuiDirlistCore.ReRead(ref platform, State, accept));
		// FilterDrawers defaults FALSE: the two drawers pass unfiltered, only the
		// two *.txt files match, beta.info is rejected.
		Assert.Equal(4u, MuiListCore.EntryCount(ref platform, State, accept));
		Assert.Equal(2u, Get(ref platform, accept, NumFiles));

		var reject = CreateDirlist(ref platform, dirlistClass, "Data:");
		var info = WriteString(ref platform, 0x6100, "#?.info");
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, reject,
			RejectPattern, info.Raw));
		Assert.True(MuiDirlistCore.ReRead(ref platform, State, reject));
		Assert.Equal(4u, MuiListCore.EntryCount(ref platform, State, reject));
	}

	[Fact]
	public void DirlistSortsDrawersFirstThenByNameAndReverses()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		// Default: drawers first, name ascending.
		Assert.Equal("drawerA", EntryName(ref platform, dirlist, 0));
		Assert.Equal("drawerB", EntryName(ref platform, dirlist, 1));
		Assert.Equal("alpha.txt", EntryName(ref platform, dirlist, 2));
		Assert.Equal("beta.info", EntryName(ref platform, dirlist, 3));
		Assert.Equal("charlie.txt", EntryName(ref platform, dirlist, 4));
		// SortHighLow reverses within the drawer/file grouping.
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortHighLow, 1));
		Assert.Equal("drawerB", EntryName(ref platform, dirlist, 0));
		Assert.Equal("drawerA", EntryName(ref platform, dirlist, 1));
		Assert.Equal("charlie.txt", EntryName(ref platform, dirlist, 2));
		// SortDirs_Last places files before drawers.
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortHighLow, 0));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			SortDirs, SortDirsLast));
		Assert.Equal("alpha.txt", EntryName(ref platform, dirlist, 0));
		Assert.Equal("drawerA", EntryName(ref platform, dirlist, 3));
	}

	[Fact]
	public void DirlistMissingDirectorySetsInvalidWithIoErr()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		platform.DirectoryMissing = true;
		platform.DirectoryErrorCode = 205;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Missing:");
		Assert.Equal(StatusInvalid, Get(ref platform, dirlist, Status));
		Assert.Equal(0u, MuiListCore.EntryCount(ref platform, State, dirlist));
		Assert.Equal(205, MuiDirlistCore.IoErr(ref platform, State, dirlist));
		Assert.False(MuiDirlistCore.ReRead(ref platform, State, dirlist));
	}

	[Fact]
	public void DirlistMidScanFailureLeavesCleanInvalidState()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		platform.DirectoryFailIndex = 2; // fail part-way through the scan
		platform.DirectoryErrorCode = 103;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		// A mid-scan failure must not leave a partial list.
		Assert.Equal(StatusInvalid, Get(ref platform, dirlist, Status));
		Assert.Equal(0u, MuiListCore.EntryCount(ref platform, State, dirlist));
		Assert.Equal(0u, Get(ref platform, dirlist, NumFiles));
		Assert.Equal(103, MuiDirlistCore.IoErr(ref platform, State, dirlist));
	}

	[Fact]
	public void DirlistNullDirectoryClearsAndInvalidates()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.Equal(StatusValid, Get(ref platform, dirlist, Status));
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			Directory, 0));
		Assert.Equal(StatusInvalid, Get(ref platform, dirlist, Status));
		Assert.Equal(0u, MuiListCore.EntryCount(ref platform, State, dirlist));
	}

	[Fact]
	public void DirlistReReadThroughDispatcherRepopulates()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.Equal(5u, MuiListCore.EntryCount(ref platform, State, dirlist));
		platform.DirectoryCount = 3;
		var packet = APTR.FromPointer(0x6200);
		platform.WriteUInt32(packet, 0, ReReadMethod);
		Assert.Equal(1u, MuiDirlistDispatcher.Dispatch(ref platform, State,
			dirlist, packet));
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, dirlist));
	}

	[Fact]
	public void DirlistMutatorMethodsReturnIoErrCodes()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		var newName = WriteString(ref platform, 0x6300, "renamed");
		Assert.Equal(0, MuiDirlistCore.Rename(ref platform, State, dirlist, 0,
			newName));
		// A failing mutator surfaces the capability's IoErr code.
		platform.SetProtectionResult = 216;
		Assert.Equal(216, MuiDirlistCore.SetProtection(ref platform, State, dirlist,
			0, 0xF));
		Assert.Equal(216, MuiDirlistCore.IoErr(ref platform, State, dirlist));
		// A successful SetProtection updates the entry protection in place.
		platform.SetProtectionResult = 0;
		Assert.Equal(0, MuiDirlistCore.SetProtection(ref platform, State, dirlist,
			0, 0x5));
		var entry = MuiListCore.GetEntry(ref platform, State, dirlist, 0, APTR.Null);
		Assert.True(MuiDirlistCore.TryReadEntryState(ref platform, entry,
			out var updated));
		Assert.Equal(0x5u, updated.Protection);
		// An out-of-range row is reported as ERROR_OBJECT_NOT_FOUND.
		Assert.Equal(205, MuiDirlistCore.SetComment(ref platform, State, dirlist,
			99, newName));
	}

	[Fact]
	public void DirlistFilterHookOverridesOtherFilters()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		// A hook whose entry returns TRUE (test Invoke -> 1) must include every
		// entry even though DrawersOnly is set (all other filters are ignored).
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			DrawersOnly, 1));
		var hook = APTR.FromPointer(0x6400);
		platform.WriteUInt32(hook, 8, 0); // h_Entry
		Assert.True(MuiDirlistCore.SetAttribute(ref platform, State, dirlist,
			FilterHook, hook.Raw));
		Assert.True(MuiDirlistCore.ReRead(ref platform, State, dirlist));
		Assert.Equal(5u, MuiListCore.EntryCount(ref platform, State, dirlist));
	}

	[Fact]
	public void DirlistPathReflectsActiveEntry()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		// No active entry -> NULL path.
		Assert.True(MuiDirlistCore.GetAttribute(ref platform, State, dirlist,
			PathAttr, out var none));
		Assert.Equal(0u, none);
		// Active row 2 (sorted) is "alpha.txt"; the volume path joins without a
		// separator because "Data:" already ends in ':'.
		MuiListCore.SetAttribute(ref platform, State, dirlist, ListActive,
			2, false);
		Assert.True(MuiDirlistCore.GetAttribute(ref platform, State, dirlist,
			PathAttr, out var path));
		Assert.Equal("Data:alpha.txt", ReadCString(ref platform,
			APTR.FromPointer(path)));
	}

	[Fact]
	public void DirlistNumBytes64ReturnsPointerToQuadTotal()
	{
		var platform = CreatePlatform(out var dirlistClass, out _, out _);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.True(MuiDirlistCore.GetAttribute(ref platform, State, dirlist,
			NumBytes64, out var quad));
		Assert.NotEqual(0u, quad);
		var q = APTR.FromPointer(quad);
		Assert.True(MuiDirlistCore.TryReadByteTotalState(ref platform, q,
			out var total));
		Assert.Equal(0u, total.High);
		Assert.Equal(600u, total.Low);
	}

	[Fact]
	public void NamedDirlistByteTotalStateReadsAndWritesGuestQuad()
	{
		var platform = CreatePlatform(out _, out _, out _);
		var storage = APTR.FromPointer(0x7500);
		var expected = default(MuiDirlistByteTotalState);
		expected.High = 1;
		expected.Low = 0xFFFFFFFF;
		Assert.True(MuiDirlistCore.WriteByteTotalState(ref platform, storage,
			expected));
		Assert.True(MuiDirlistCore.TryReadByteTotalState(ref platform, storage,
			out var actual));
		Assert.Equal(expected.High, actual.High);
		Assert.Equal(expected.Low, actual.Low);
		Assert.False(MuiDirlistCore.TryReadByteTotalState(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void DirlistByteTotalCodecUsesNamedQuadFields()
	{
		var platform = CreatePlatform(out _, out _, out _);
		var address = APTR.FromPointer(0x7600);
		var expected = default(MuiDirlistByteTotalState);
		expected.High = 3;
		expected.Low = 0x10203040;
		Assert.True(MuiDirlistByteTotalCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiDirlistByteTotalCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.High, actual.High);
		Assert.Equal(expected.Low, actual.Low);
		Assert.False(MuiDirlistByteTotalCodec.TryRead(ref platform, APTR.Null,
			out _));
	}

	[Fact]
	public void DirlistDisposalFreesOwnedRecordsWithoutLeak()
	{
		var platform = CreatePlatform(out var dirlistClass, out var volumelistClass,
			out var otherClass);
		platform.DirectoryCount = 5;
		var dirlist = CreateDirlist(ref platform, dirlistClass, "Data:");
		Assert.Equal(5u, MuiListCore.EntryCount(ref platform, State, dirlist));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			dirlist));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			dirlistClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			volumelistClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	// ------------------------------------------------------------ Volumelist

	[Fact]
	public void VolumelistPopulatesVolumesAsDrawers()
	{
		var platform = CreatePlatform(out _, out var volumelistClass, out _);
		platform.VolumeCount = 3;
		var volumes = MuiVolumelistCore.CreateVolumelist(ref platform, State,
			volumelistClass, APTR.Null);
		Assert.NotEqual(APTR.Null, volumes);
		Assert.Equal(StatusValid, Get(ref platform, volumes, Status));
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, volumes));
		Assert.Equal(3u, Get(ref platform, volumes, NumDrawers));
		Assert.Equal(0u, Get(ref platform, volumes, NumFiles));
	}

	[Fact]
	public void VolumelistExampleModeUsesSyntheticEntries()
	{
		var platform = CreatePlatform(out _, out var volumelistClass, out _);
		platform.VolumeCount = 0; // no real volumes; ExampleMode must still fill
		var tags = APTR.FromPointer(0x6500);
		platform.WriteUInt32(tags, 0, ExampleMode);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var volumes = MuiVolumelistCore.CreateVolumelist(ref platform, State,
			volumelistClass, tags);
		Assert.NotEqual(APTR.Null, volumes);
		Assert.Equal(StatusValid, Get(ref platform, volumes, Status));
		Assert.Equal(2u, MuiListCore.EntryCount(ref platform, State, volumes));
		Assert.Equal("Example0:", EntryName(ref platform, volumes, 0));
		Assert.Equal("Example1:", EntryName(ref platform, volumes, 1));
	}

	[Fact]
	public void VolumelistExampleModeUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out _, out var volumelistClass, out _);
		platform.VolumeCount = 0;
		var tags = APTR.FromPointer(0x1D00);
		platform.WriteUInt32(tags, 0, ExampleMode);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var volumes = MuiVolumelistCore.CreateVolumelist(ref platform, State,
			volumelistClass, tags);
		Assert.NotEqual(APTR.Null, volumes);
		Assert.True(MuiVolumelistCore.TryGetModeStateRecord(ref platform, State,
			volumes, out var initial));
		Assert.Equal(MuiVolumelistCore.MuiVolumelistModeStateRecord.Cookie,
			initial.Magic);
		Assert.Equal(1u, initial.ExampleMode);
		Assert.True(MuiVolumelistCore.GetAttribute(ref platform, State, volumes,
			ExampleMode, out var publicValue));
		Assert.Equal(1u, publicValue);

		Assert.True(MuiVolumelistCore.SetAttribute(ref platform, State, volumes,
			ExampleMode, 0));
		Assert.True(MuiVolumelistCore.TryGetModeStateRecord(ref platform, State,
			volumes, out var changed));
		Assert.Equal(0u, changed.ExampleMode);
		Assert.True(MuiVolumelistCore.GetAttribute(ref platform, State, volumes,
			ExampleMode, out publicValue));
		Assert.Equal(0u, publicValue);
	}

	[Fact]
	public void VolumelistDisposalBalancesAllocations()
	{
		var platform = CreatePlatform(out var dirlistClass, out var volumelistClass,
			out var otherClass);
		platform.VolumeCount = 3;
		var volumes = MuiVolumelistCore.CreateVolumelist(ref platform, State,
			volumelistClass, APTR.Null);
		Assert.NotEqual(APTR.Null, volumes);
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			volumes));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			dirlistClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			volumelistClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	// -------------------------------------------------------------- helpers

	private static APTR CreateDirlist(ref MuiHeadlessTestPlatform platform,
		APTR dirlistClass, string directory)
	{
		var path = WriteString(ref platform, 0x5000, directory);
		var tags = APTR.FromPointer(0x5100);
		platform.WriteUInt32(tags, 0, Directory);
		platform.WriteUInt32(tags, 4, path.Raw);
		platform.WriteUInt32(tags, 8, 0);
		return MuiDirlistCore.CreateDirlist(ref platform, State, dirlistClass, tags);
	}

	private static APTR WriteString(ref MuiHeadlessTestPlatform platform,
		uint address, string value)
	{
		var target = APTR.FromPointer(address);
		platform.WriteCString(target, value);
		return target;
	}

	private static string EntryName(ref MuiHeadlessTestPlatform platform, APTR obj,
		int row)
	{
		var entry = MuiListCore.GetEntry(ref platform, State, obj, row, APTR.Null);
		return MuiDirlistCore.TryReadEntryState(ref platform, entry,
			out var record) ? ReadCString(ref platform, record.Name) : string.Empty;
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform platform,
		APTR address)
	{
		if (address.IsNull) return string.Empty;
		var builder = new StringBuilder();
		for (var i = 0; i < 4096; i++)
		{
			var ch = platform.ReadUInt8(address, i);
			if (ch == 0) break;
			builder.Append((char)ch);
		}
		return builder.ToString();
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		MuiDirlistCore.GetAttribute(ref platform, State, obj, attribute,
			out var value);
		return value;
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR dirlistClass,
		out APTR volumelistClass, out APTR otherClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x80000, 0x8000, State);
		var dirlistName = APTR.FromPointer(0x1100);
		var volumelistName = APTR.FromPointer(0x1140);
		var otherName = APTR.FromPointer(0x1180);
		platform.WriteCString(dirlistName, "Dirlist.mui");
		platform.WriteCString(volumelistName, "Volumelist.mui");
		platform.WriteCString(otherName, "Group.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		dirlistClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			dirlistName, APTR.Null, 0, APTR.FromPointer(1), false);
		volumelistClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			volumelistName, APTR.Null, 0, APTR.FromPointer(1), false);
		otherClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			otherName, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
