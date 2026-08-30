using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListStructAdapterTests
{
	[Fact]
	public void ListTestPosUsesDedicatedMixedWidthStructAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1800);
		var value = new MuiListTestPosResult
		{
			Entry = -3,
			Column = 2,
			Flags = MuiListTestPosResult.FlagAbove | MuiListTestPosResult.FlagRight,
			XOffset = -4,
			YOffset = 5,
		};
		Assert.True(MuiListTestPosResultCodec.Write(ref platform, address, value));
		Assert.True(MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform,
			address, MuiListTestPosResultField.XOffset, out var xOffset) &&
			xOffset.Raw == 0x1808u);
		Assert.True(MuiListTestPosResultMemoryCodec.TryReadUInt32(ref platform,
			address, MuiListTestPosResultField.Entry, out var entry) &&
			entry == unchecked((uint)-3));
		Assert.True(MuiListTestPosResultMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiListTestPosResultField.YOffset, unchecked((ushort)-6)));
		Assert.True(MuiListTestPosResultCodec.TryRead(ref platform, address,
			out var decoded) && decoded.YOffset == -6 && decoded.Flags == value.Flags);
		Assert.False(MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x51000), MuiListTestPosResultField.XOffset,
			out _));
		Assert.False(MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListTestPosResultField.Entry, out _));
	}

	[Fact]
	public void ListScalarAndDisplayRowsUseDedicatedStructAdapters()
	{
		var platform = CreatePlatform();
		var scalarAddress = APTR.FromPointer(0x1820);
		var rowAddress = APTR.FromPointer(0x1830);
		var scalarRecord = new MuiListScalarStorageRecord { Value = 0xFFFFFFFEu };
		var initialRow = new MuiListDisplayRowRecord { Row = -4 };
		Assert.True(MuiListScalarStorageCodec.Write(ref platform, scalarAddress,
			ref scalarRecord));
		Assert.True(MuiListDisplayRowRecordCodec.Write(ref platform, rowAddress,
			ref initialRow));
		Assert.True(MuiListScalarStorageRecordMemoryCodec.TryGetAddress(ref platform,
			scalarAddress, out var scalarField) && scalarField.Raw == 0x1820u);
		Assert.True(MuiListDisplayRowRecordMemoryCodec.TryGetAddress(ref platform,
			rowAddress, out var rowField) && rowField.Raw == 0x1830u);
		Assert.True(MuiListScalarStorageRecordMemoryCodec.TryReadUInt32(ref platform,
			scalarAddress, out var scalar) && scalar == 0xFFFFFFFEu);
		Assert.True(MuiListDisplayRowRecordMemoryCodec.TryWriteUInt32(ref platform,
			rowAddress, 9));
		Assert.True(MuiListDisplayRowRecordCodec.TryRead(ref platform, rowAddress,
			out var row) && row.Row == 9);
		Assert.False(MuiListScalarStorageRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ListScalarAndDisplayRowsSequentialRecordsPreserveBitsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var scalarAddress = APTR.FromPointer(0x3600);
		var rowAddress = APTR.FromPointer(0x3610);
		var scalar = new MuiListScalarStorageRecord { Value = 0xFFFFFFFEu };
		var row = new MuiListDisplayRowRecord { Row = int.MinValue };
		Assert.True(MuiListScalarStorageCodec.WriteRecord(ref platform,
			scalarAddress, ref scalar));
		Assert.True(MuiListDisplayRowRecordCodec.WriteRecord(ref platform,
			rowAddress, ref row));
		Assert.True(MuiListScalarStorageCodec.TryReadRecord(ref platform,
			scalarAddress, out var decodedScalar));
		Assert.True(MuiListDisplayRowRecordCodec.TryReadRecord(ref platform,
			rowAddress, out var decodedRow));
		Assert.Equal(scalar.Value, decodedScalar.Value);
		Assert.Equal(row.Row, decodedRow.Row);
		Assert.False(MuiListScalarStorageCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFD), out _));
		Assert.False(MuiListDisplayRowRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFD), out _));
	}

	[Fact]
	public void ListStateMemoryAdapterUsesNamedRecordFieldsAndKeepsCursorCompatibility()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1860);

		Assert.True(MuiListCore.MuiListStateMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiListCore.MuiListStateRecordKind.Redraw, MuiListCore.MuiListStateField.Requests, 7));
		Assert.True(MuiListCore.MuiListStateMemoryCodec.TryReadUInt32(ref platform, address,
			MuiListCore.MuiListStateRecordKind.Redraw, MuiListCore.MuiListStateField.Requests,
			out var requests));
		Assert.Equal(7u, requests);
		Assert.True(MuiListCore.MuiListStateMemoryCodec.TryGetAddress(ref platform, address,
			MuiListCore.MuiListStateRecordKind.Redraw, MuiListCore.MuiListStateField.Requests,
			out var requestsAddress));
		Assert.Equal(0x1868u, requestsAddress.Raw);

		var cursor = new MuiListCore.MuiListStateFieldCursor
		{
			Address = address,
			Record = MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			Field = MuiListCore.MuiListStateField.DragType,
		};
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var dragTypeAddress));
		Assert.Equal(0x1880u, dragTypeAddress.Raw);
		Assert.False(MuiListCore.MuiListStateMemoryCodec.TryGetAddress(ref platform, address,
			(MuiListCore.MuiListStateRecordKind)255, MuiListCore.MuiListStateField.Magic, out _));
		Assert.False(MuiListCore.MuiListStateMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListCore.MuiListStateRecordKind.Redraw, MuiListCore.MuiListStateField.Magic,
			out _));
	}

	[Fact]
	public void ListSlotMemoryAdapterUsesNamedEntryAndFlagsFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18A0);
		Assert.True(MuiListSlotMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiListSlotField.Entry, 0x9000u));
		Assert.True(MuiListSlotMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiListSlotField.Flags, 7));
		Assert.True(MuiListSlotMemoryCodec.TryReadUInt32(ref platform, address,
			MuiListSlotField.Entry, out var entry));
		Assert.Equal(0x9000u, entry);
		Assert.True(MuiListSlotMemoryCodec.TryGetAddress(ref platform, address,
			MuiListSlotField.Flags, out var flagsAddress));
		Assert.Equal(0x18A4u, flagsAddress.Raw);
		Assert.False(MuiListSlotMemoryCodec.TryGetAddress(ref platform, address,
			(MuiListSlotField)255, out _));
	}

	[Fact]
	public void ListSlotSequentialRecordPreservesPointerFlagsAndBounds()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18B0);
		var value = new MuiListSlotState
		{
			Entry = APTR.FromPointer(uint.MaxValue),
			Flags = 0xAABBCCDDu,
		};

		Assert.True(MuiListSlotCodec.WriteRecord(ref platform, address, value));
		Assert.True(MuiListSlotCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Entry, decoded.Entry);
		Assert.Equal(value.Flags, decoded.Flags);

		var crossingEnd = APTR.FromPointer(0x20FFC);
		Assert.False(MuiListSlotCodec.WriteRecord(ref platform, crossingEnd,
			value));
		Assert.False(MuiListSlotCodec.TryReadRecord(ref platform, crossingEnd,
			out _));
	}

	[Fact]
	public void ListTitleArrayMemoryAdapterUsesNamedFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18C0);
		Assert.True(MuiListCore.MuiListTitleArrayStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListTitleArrayStateField.Count,
			3));
		Assert.True(MuiListCore.MuiListTitleArrayStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListTitleArrayStateField.Count,
			out var count));
		Assert.Equal(3u, count);
		Assert.True(MuiListCore.MuiListTitleArrayStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListCore.MuiListTitleArrayStateField.Pointers,
			out var pointersAddress));
		Assert.Equal(0x18C4u, pointersAddress.Raw);
		Assert.False(MuiListCore.MuiListTitleArrayStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListTitleArrayStateField)255, out _));
	}

	[Fact]
	public void ListTitleMemoryAdapterUsesNamedMagicAndValueFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18E0);
		Assert.True(MuiListCore.MuiListTitleStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListTitleStateField.Value,
			0x12345678u));
		Assert.True(MuiListCore.MuiListTitleStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListTitleStateField.Value,
			out var value));
		Assert.Equal(0x12345678u, value);
		Assert.True(MuiListCore.MuiListTitleStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListCore.MuiListTitleStateField.Magic,
			out var magicAddress));
		Assert.Equal(address.Raw, magicAddress.Raw);
		Assert.False(MuiListCore.MuiListTitleStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListTitleStateField)255, out _));
	}

	[Fact]
	public void ListSelectionSignalMemoryAdapterUsesNamedMagicAndValueFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1900);
		Assert.True(MuiListCore.MuiListSelectionSignalStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListSelectionSignalStateField.Value, 1));
		Assert.True(MuiListCore.MuiListSelectionSignalStateMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListSelectionSignalStateField.Value, out var value));
		Assert.Equal(1u, value);
		Assert.True(MuiListCore.MuiListSelectionSignalStateMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListCore.MuiListSelectionSignalStateField.Value,
				out var valueAddress));
		Assert.Equal(0x1904u, valueAddress.Raw);
		Assert.False(MuiListCore.MuiListSelectionSignalStateMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListCore.MuiListSelectionSignalStateField)255, out _));
	}

	[Fact]
	public void ListFormatPolicyMemoryAdapterUsesNamedFormatAndColumnFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1920);
		Assert.True(MuiListCore.MuiListFormatPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFormatPolicyStateField.Format, 0x2400u));
		Assert.True(MuiListCore.MuiListFormatPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFormatPolicyStateField.MaxColumns, 4));
		Assert.True(MuiListCore.MuiListFormatPolicyStateMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListFormatPolicyStateField.MaxColumns,
				out var maxColumns));
		Assert.Equal(4u, maxColumns);
		Assert.True(MuiListCore.MuiListFormatPolicyStateMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListCore.MuiListFormatPolicyStateField.Columns,
				out var columnsAddress));
		Assert.Equal(0x192Cu, columnsAddress.Raw);
		Assert.False(MuiListCore.MuiListFormatPolicyStateMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListCore.MuiListFormatPolicyStateField)255, out _));
	}

	[Fact]
	public void ListFontMemoryAdapterUsesNamedMagicAndFontFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1940);
		Assert.True(MuiListCore.MuiListFontStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListFontStateField.Font,
			0x2600u));
		Assert.True(MuiListCore.MuiListFontStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListFontStateField.Font,
			out var font));
		Assert.Equal(0x2600u, font);
		Assert.True(MuiListCore.MuiListFontStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListCore.MuiListFontStateField.Font,
			out var fontAddress));
		Assert.Equal(0x1944u, fontAddress.Raw);
		Assert.False(MuiListCore.MuiListFontStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListFontStateField)255, out _));
	}

	[Fact]
	public void ListRedrawMemoryAdapterUsesNamedDirtyAndRequestFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1960);
		Assert.True(MuiListCore.MuiListRedrawStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListRedrawStateField.Dirty, 1));
		Assert.True(MuiListCore.MuiListRedrawStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListRedrawStateField.Requests,
			4));
		Assert.True(MuiListCore.MuiListRedrawStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListRedrawStateField.Requests,
			out var requests));
		Assert.Equal(4u, requests);
		Assert.True(MuiListCore.MuiListRedrawStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListCore.MuiListRedrawStateField.Dirty,
			out var dirtyAddress));
		Assert.Equal(0x1964u, dirtyAddress.Raw);
		Assert.False(MuiListCore.MuiListRedrawStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListRedrawStateField)255, out _));
	}

	[Fact]
	public void ListActiveMemoryAdapterUsesNamedPresenceAndRowFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1980);
		Assert.True(MuiListCore.MuiListActiveStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListActiveStateField.HasActive,
			1));
		Assert.True(MuiListCore.MuiListActiveStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListActiveStateField.Active,
			7));
		Assert.True(MuiListCore.MuiListActiveStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListActiveStateField.Active,
			out var active));
		Assert.Equal(7u, active);
		Assert.True(MuiListCore.MuiListActiveStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListCore.MuiListActiveStateField.HasActive,
			out var presenceAddress));
		Assert.Equal(0x1984u, presenceAddress.Raw);
		Assert.False(MuiListCore.MuiListActiveStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListActiveStateField)255, out _));
	}

	[Fact]
	public void ListViewportMemoryAdapterUsesNamedPixelAndRowFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x19A0);
		Assert.True(MuiListCore.MuiListViewportStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListViewportStateField.TopPixel,
			64));
		Assert.True(MuiListCore.MuiListViewportStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListViewportStateField.Visible,
			20));
		Assert.True(MuiListCore.MuiListViewportStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListViewportStateField.Visible,
			out var visible));
		Assert.Equal(20u, visible);
		Assert.True(MuiListCore.MuiListViewportStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListCore.MuiListViewportStateField.DropMark,
			out var dropMarkAddress));
		Assert.Equal(0x19BCu, dropMarkAddress.Raw);
		Assert.False(MuiListCore.MuiListViewportStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListViewportStateField)255, out _));
	}

	[Fact]
	public void ListInteractionPolicyMemoryAdapterUsesNamedInputAndScrollerFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x19D0);
		Assert.True(MuiListCore.MuiListInteractionPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListInteractionPolicyStateField.Input, 2));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListInteractionPolicyStateField.ScrollerPos, 12));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListInteractionPolicyStateField.ScrollerPos,
				out var scrollerPos));
		Assert.Equal(12u, scrollerPos);
		Assert.True(MuiListCore.MuiListInteractionPolicyStateMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListCore.MuiListInteractionPolicyStateField.MultiSelect,
				out var multiSelectAddress));
		Assert.Equal(0x19D8u, multiSelectAddress.Raw);
		Assert.False(MuiListCore.MuiListInteractionPolicyStateMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListCore.MuiListInteractionPolicyStateField)255, out _));
	}

	[Fact]
	public void ListClickMemoryAdapterUsesNamedClickProjectionFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x19F0);
		Assert.True(MuiListCore.MuiListClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListClickStateField.ClickColumn,
			3));
		Assert.True(MuiListCore.MuiListClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListClickStateField.Clicks, 2));
		Assert.True(MuiListCore.MuiListClickStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListClickStateField.Clicks,
			out var clicks));
		Assert.Equal(2u, clicks);
		Assert.True(MuiListCore.MuiListClickStateMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiListCore.MuiListClickStateField.DefClickColumn,
			out var defaultColumnAddress));
		Assert.Equal(0x1A04u, defaultColumnAddress.Raw);
		Assert.False(MuiListCore.MuiListClickStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListClickStateField)255, out _));
	}

	[Fact]
	public void ListHookPolicyMemoryAdapterUsesNamedHookFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A20);
		Assert.True(MuiListCore.MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListCore.MuiListHookPolicyStateField.DisplayHook, 0x3000u));
		Assert.True(MuiListCore.MuiListHookPolicyStateMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiListCore.MuiListHookPolicyStateField.DisplayHook,
			out var displayHook));
		Assert.Equal(0x3000u, displayHook);
		Assert.True(MuiListCore.MuiListHookPolicyStateMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiListCore.MuiListHookPolicyStateField.MultiTestHook,
			out var multiTestAddress));
		Assert.Equal(0x1A34u, multiTestAddress.Raw);
		Assert.False(MuiListCore.MuiListHookPolicyStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListHookPolicyStateField)255, out _));
	}

	[Fact]
	public void ListSortMemoryAdapterUsesNamedColumnFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A50);
		Assert.True(MuiListCore.MuiListSortStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListSortStateField.SortColumn,
			2));
		Assert.True(MuiListCore.MuiListSortStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListSortStateField.SortColumn,
			out var sortColumn));
		Assert.Equal(2u, sortColumn);
		Assert.True(MuiListCore.MuiListSortStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListCore.MuiListSortStateField.TitleClick,
			out var titleClickAddress));
		Assert.Equal(0x1A58u, titleClickAddress.Raw);
		Assert.False(MuiListCore.MuiListSortStateMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListCore.MuiListSortStateField)255, out _));
	}

	[Fact]
	public void ListPresentationPolicyMemoryAdapterUsesNamedPolicyFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A70);
		Assert.True(MuiListCore.MuiListPresentationPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.Stripes, 1));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.MinLineHeight, 16));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.MinLineHeight,
				out var lineHeight));
		Assert.Equal(16u, lineHeight);
		Assert.True(MuiListCore.MuiListPresentationPolicyStateMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.DragType,
				out var dragTypeAddress));
		Assert.Equal(0x1A90u, dragTypeAddress.Raw);
		Assert.False(MuiListCore.MuiListPresentationPolicyStateMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListCore.MuiListPresentationPolicyStateField)255, out _));
	}

	[Fact]
	public void ListInsertPositionMemoryAdapterUsesNamedPositionField()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1AC0);
		Assert.True(MuiListCore.MuiListInsertPositionStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListInsertPositionStateField.Position, 23));
		Assert.True(MuiListCore.MuiListInsertPositionStateMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListInsertPositionStateField.Position,
				out var position));
		Assert.Equal(23u, position);
		Assert.True(MuiListCore.MuiListInsertPositionStateMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListCore.MuiListInsertPositionStateField.Position,
				out var positionAddress));
		Assert.Equal(0x1AC4u, positionAddress.Raw);
		Assert.False(MuiListCore.MuiListInsertPositionStateMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListCore.MuiListInsertPositionStateField)255, out _));
	}

	[Fact]
	public void ListPoolPolicyMemoryAdapterUsesNamedPoolFields()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1AE0);
		Assert.True(MuiListCore.MuiListPoolPolicyMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListCore.MuiListPoolPolicyField.Pool, 0x3000u));
		Assert.True(MuiListCore.MuiListPoolPolicyMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListCore.MuiListPoolPolicyField.UsesExternalPool, 1));
		Assert.True(MuiListCore.MuiListPoolPolicyMemoryCodec.TryReadUInt32(ref platform,
			address, MuiListCore.MuiListPoolPolicyField.Pool, out var pool));
		Assert.Equal(0x3000u, pool);
		Assert.True(MuiListCore.MuiListPoolPolicyMemoryCodec.TryGetAddress(ref platform,
			address, MuiListCore.MuiListPoolPolicyField.ThresholdSize,
			out var thresholdAddress));
		Assert.Equal(0x1AECu, thresholdAddress.Raw);
		Assert.False(MuiListCore.MuiListPoolPolicyMemoryCodec.TryGetAddress(ref platform,
			address, (MuiListCore.MuiListPoolPolicyField)255, out _));
	}

	[Fact]
	public void ListFormatDescriptorAdaptersUseNamedDescriptorAndTableFields()
	{
		var platform = CreatePlatform();
		var descriptor = APTR.FromPointer(0x1B00);
		Assert.True(MuiListCore.MuiListFormatDescriptorMemoryCodec.TryWriteUInt32(
			ref platform, descriptor,
			MuiListCore.MuiListFormatDescriptorField.PreparseLength, 6));
		Assert.True(MuiListCore.MuiListFormatDescriptorMemoryCodec.TryReadUInt32(
			ref platform, descriptor,
			MuiListCore.MuiListFormatDescriptorField.PreparseLength,
			out var preparseLength));
		Assert.Equal(6u, preparseLength);
		Assert.True(MuiListCore.MuiListFormatDescriptorMemoryCodec.TryGetAddress(
			ref platform, descriptor,
			MuiListCore.MuiListFormatDescriptorField.PreparseStorageLength,
			out var storageLengthAddress));
		Assert.Equal(0x1B24u, storageLengthAddress.Raw);

		var table = APTR.FromPointer(0x1B40);
		Assert.True(MuiListCore.MuiListFormatDescriptorStateMemoryCodec
			.TryWriteUInt32(ref platform, table,
				MuiListCore.MuiListFormatDescriptorStateField.Columns, 3));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateMemoryCodec
			.TryGetAddress(ref platform, table,
				MuiListCore.MuiListFormatDescriptorStateField.Values,
				out var valuesAddress));
		Assert.Equal(0x1B48u, valuesAddress.Raw);
		Assert.False(MuiListCore.MuiListFormatDescriptorMemoryCodec
			.TryGetAddress(ref platform, descriptor,
				(MuiListCore.MuiListFormatDescriptorField)255, out _));
	}

	[Fact]
	public void ListFormatDescriptorSequentialRecordPreservesAllFieldsAndBounds()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1C40);
		var value = default(MuiListCore.MuiListFormatDescriptor);
		value.Delta = 0xFFFFFFFEu;
		value.Weight = 2;
		value.MinWidth = 8;
		value.MaxWidth = 120;
		value.Column = 3;
		value.Flags = 0xA55Au;
		value.Preparse = APTR.FromPointer(0xFFFFFFF0u);
		value.PreparseLength = 6;
		value.PreparseStorage = APTR.FromPointer(0x00ABCDEFu);
		value.PreparseStorageLength = 7;
		Assert.True(MuiListCore.MuiListFormatDescriptorCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListFormatDescriptorCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Delta, decoded.Delta);
		Assert.Equal(value.Weight, decoded.Weight);
		Assert.Equal(value.MinWidth, decoded.MinWidth);
		Assert.Equal(value.MaxWidth, decoded.MaxWidth);
		Assert.Equal(value.Column, decoded.Column);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Preparse, decoded.Preparse);
		Assert.Equal(value.PreparseLength, decoded.PreparseLength);
		Assert.Equal(value.PreparseStorage, decoded.PreparseStorage);
		Assert.Equal(value.PreparseStorageLength, decoded.PreparseStorageLength);
		Assert.False(MuiListCore.MuiListFormatDescriptorCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FD9), out _));
	}

	[Fact]
	public void ListColumnMetricsAdaptersUseNamedStateAndValueFields()
	{
		var platform = CreatePlatform();
		var metrics = APTR.FromPointer(0x1B80);
		Assert.True(MuiListCore.MuiListColumnMetricsMemoryCodec.TryWriteUInt32(
			ref platform, metrics, MuiListCore.MuiListColumnMetricsField.Width, 96));
		Assert.True(MuiListCore.MuiListColumnMetricsMemoryCodec.TryReadUInt32(
			ref platform, metrics, MuiListCore.MuiListColumnMetricsField.Width,
			out var width));
		Assert.Equal(96u, width);
		Assert.True(MuiListCore.MuiListColumnMetricsMemoryCodec.TryGetAddress(
			ref platform, metrics, MuiListCore.MuiListColumnMetricsField.Values,
			out var valuesAddress));
		Assert.Equal(0x1B8Cu, valuesAddress.Raw);

		var value = APTR.FromPointer(0x1BA0);
		Assert.True(MuiListCore.MuiListColumnMetricMemoryCodec.TryWriteUInt32(
			ref platform, value, MuiListCore.MuiListColumnMetricField.Value, 37));
		Assert.True(MuiListCore.MuiListColumnMetricMemoryCodec.TryReadUInt32(
			ref platform, value, MuiListCore.MuiListColumnMetricField.Value,
			out var measured));
		Assert.Equal(37u, measured);
		Assert.False(MuiListCore.MuiListColumnMetricsMemoryCodec.TryGetAddress(
			ref platform, metrics,
			(MuiListCore.MuiListColumnMetricsField)255, out _));
		Assert.False(MuiListCore.MuiListColumnMetricMemoryCodec.TryGetAddress(
			ref platform, value,
			(MuiListCore.MuiListColumnMetricField)255, out _));
	}

	[Fact]
	public void ListPointerAndOwnedHeaderAdaptersUseNamedRecordFields()
	{
		var platform = CreatePlatform();
		var pointer = APTR.FromPointer(0x1BC0);
		Assert.True(MuiListCore.MuiListPointerSlotMemoryCodec.TryWriteUInt32(
			ref platform, pointer, MuiListCore.MuiListPointerSlotField.Value,
			0x3456u));
		Assert.True(MuiListCore.MuiListPointerSlotMemoryCodec.TryReadUInt32(
			ref platform, pointer, MuiListCore.MuiListPointerSlotField.Value,
			out var pointerValue));
		Assert.Equal(0x3456u, pointerValue);
		Assert.True(MuiListCore.MuiListPointerSlotMemoryCodec.TryGetAddress(
			ref platform, pointer, MuiListCore.MuiListPointerSlotField.Value,
			out var pointerAddress));
		Assert.Equal(pointer.Raw, pointerAddress.Raw);
		var record = new MuiListCore.MuiListPointerSlotRecord
		{
			Value = APTR.FromPointer(0xFFFFFFFEu),
		};
		Assert.True(MuiListCore.MuiListPointerSlotCodec.WriteRecord(ref platform,
			pointer, record));
		Assert.True(MuiListCore.MuiListPointerSlotCodec.TryReadRecord(ref platform,
			pointer, out var decodedRecord));
		Assert.Equal(record.Value, decodedRecord.Value);
		Assert.False(MuiListCore.MuiListPointerSlotCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFE), out _));

		var header = APTR.FromPointer(0x1BE0);
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderMemoryCodec.TryWriteUInt32(
			ref platform, header,
			MuiListCore.MuiListOwnedRecordHeaderField.Length, 48));
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderMemoryCodec.TryReadUInt32(
			ref platform, header,
			MuiListCore.MuiListOwnedRecordHeaderField.Length, out var length));
		Assert.Equal(48u, length);
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderMemoryCodec.TryGetAddress(
			ref platform, header,
			MuiListCore.MuiListOwnedRecordHeaderField.Length,
			out var lengthAddress));
		Assert.Equal(header.Raw, lengthAddress.Raw);
		Assert.False(MuiListCore.MuiListPointerSlotMemoryCodec.TryGetAddress(
			ref platform, pointer,
			(MuiListCore.MuiListPointerSlotField)255, out _));
		Assert.False(MuiListCore.MuiListOwnedRecordHeaderMemoryCodec.TryGetAddress(
			ref platform, header,
			(MuiListCore.MuiListOwnedRecordHeaderField)255, out _));
	}

	[Fact]
	public void ListColumnOrderByteSequentialRecordPreservesValueAndBounds()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1C00);
		var value = default(MuiListCore.MuiListColumnOrderByteRecord);
		value.Value = 0xFE;
		Assert.True(MuiListCore.MuiListColumnOrderByteCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListColumnOrderByteCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Value, decoded.Value);
		Assert.False(MuiListCore.MuiListColumnOrderByteCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x21000), out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
