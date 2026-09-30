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
	public void ListEditFieldAdapterPreservesSignedAndPointerSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x17C0);
		var original = new MuiListCore.MuiListEditState
		{
			Magic = MuiListCore.MuiListEditState.Cookie,
			Row = -7,
			Column = 3,
			Entry = APTR.FromPointer(0x4400),
			EditObject = APTR.FromPointer(0x4500),
			Flags = 0xA55Au,
		};

		Assert.True(MuiListCore.MuiListEditStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListEditMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListEditField.Row,
			unchecked((uint)-11)));
		Assert.True(MuiListCore.MuiListEditStateCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Column, actual.Column);
		Assert.Equal(original.Entry, actual.Entry);
		Assert.Equal(original.EditObject, actual.EditObject);
		Assert.Equal(original.Flags, actual.Flags);
		Assert.Equal(-11, actual.Row);
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
	public void ListStateMemoryAdapterDispatchesThroughCompleteNamedRecord()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18D0);
		var original = new MuiListCore.MuiListRedrawState
		{
			Magic = MuiListCore.MuiListRedrawState.Cookie,
			Dirty = 0x10203040u,
			Requests = 3,
		};

		Assert.True(MuiListCore.MuiListRedrawStateCodec.WriteRecord(ref platform, address,
			original));
		Assert.True(MuiListCore.MuiListStateMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiListCore.MuiListStateRecordKind.Redraw, MuiListCore.MuiListStateField.Requests, 9));
		Assert.True(MuiListCore.MuiListStateMemoryCodec.TryReadUInt32(ref platform, address,
			MuiListCore.MuiListStateRecordKind.Redraw, MuiListCore.MuiListStateField.Dirty, out var dirty));
		Assert.Equal(original.Dirty, dirty);
		Assert.True(MuiListCore.MuiListRedrawStateCodec.TryReadRecord(ref platform, address,
			out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Dirty, actual.Dirty);
		Assert.Equal(9u, actual.Requests);
		Assert.False(MuiListCore.MuiListStateMemoryCodec.TryReadUInt32(ref platform, address,
			MuiListCore.MuiListStateRecordKind.Redraw, MuiListCore.MuiListStateField.Position, out _));
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
	public void ListSlotFieldAdapterPreservesEntryAndFlagsSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18A8);
		var original = new MuiListSlotState
		{
			Entry = APTR.FromPointer(0x9120),
			Flags = 0x10203040u,
		};

		Assert.True(MuiListSlotCodec.WriteRecord(ref platform, address, original));
		Assert.True(MuiListSlotMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiListSlotField.Flags, 0xAABBCCDDu));
		Assert.True(MuiListSlotCodec.TryReadRecord(ref platform, address,
			out var actual));
		Assert.Equal(original.Entry, actual.Entry);
		Assert.Equal(0xAABBCCDDu, actual.Flags);
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
	public void ListTitleArrayFieldAdapterPreservesMagicAndPointersSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18D0);
		var original = new MuiListCore.MuiListTitleArrayState
		{
			Magic = MuiListCore.MuiListTitleArrayState.Cookie,
			Pointers = APTR.FromPointer(0x9A20),
			Count = 2,
		};

		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListTitleArrayStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListTitleArrayStateField.Count,
			7));
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Pointers, actual.Pointers);
		Assert.Equal(7u, actual.Count);
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
	public void ListTitleFieldAdapterPreservesMagicSibling()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x18F0);
		var original = new MuiListCore.MuiListTitleState
		{
			Magic = MuiListCore.MuiListTitleState.Cookie,
			Value = 0x0000A120u,
		};

		Assert.True(MuiListCore.MuiListTitleStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListTitleStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListTitleStateField.Value,
			0xFFFFFFFFu));
		Assert.True(MuiListCore.MuiListTitleStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(0xFFFFFFFFu, actual.Value);
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
	public void ListSelectionSignalFieldAdapterPreservesMagicSibling()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1910);
		var original = new MuiListCore.MuiListSelectionSignalState
		{
			Magic = MuiListCore.MuiListSelectionSignalState.Cookie,
			Value = 1,
		};

		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListSelectionSignalStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListSelectionSignalStateField.Value, 0));
		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec
			.TryReadStructural(ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(0u, actual.Value);
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
	public void ListFormatPolicyFieldAdapterPreservesFormatAndColumnSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1930);
		var original = new MuiListCore.MuiListFormatPolicyState
		{
			Magic = MuiListCore.MuiListFormatPolicyState.Cookie,
			Format = APTR.FromPointer(0xA400),
			MaxColumns = 8,
			Columns = 3,
		};

		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListFormatPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFormatPolicyStateField.Columns, 6));
		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec
			.TryReadStructural(ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Format, actual.Format);
		Assert.Equal(original.MaxColumns, actual.MaxColumns);
		Assert.Equal(6u, actual.Columns);
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
	public void ListFontFieldAdapterPreservesMagicSibling()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1950);
		var original = new MuiListCore.MuiListFontState
		{
			Magic = MuiListCore.MuiListFontState.Cookie,
			Font = APTR.FromPointer(0xA600),
		};

		Assert.True(MuiListCore.MuiListFontStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListFontStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListFontStateField.Font,
			0x0000A640u));
		Assert.True(MuiListCore.MuiListFontStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(APTR.FromPointer(0xA640), actual.Font);
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
	public void ListRedrawFieldAdapterPreservesDirtyAndMagicSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1970);
		var original = new MuiListCore.MuiListRedrawState
		{
			Magic = MuiListCore.MuiListRedrawState.Cookie,
			Dirty = 1,
			Requests = 9,
		};

		Assert.True(MuiListCore.MuiListRedrawStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListRedrawStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListRedrawStateField.Requests,
			12));
		Assert.True(MuiListCore.MuiListRedrawStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Dirty, actual.Dirty);
		Assert.Equal(12u, actual.Requests);
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
	public void ListActiveFieldAdapterPreservesPresenceAndMagicSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1990);
		var original = new MuiListCore.MuiListActiveState
		{
			Magic = MuiListCore.MuiListActiveState.Cookie,
			HasActive = 1,
			Active = 7,
		};

		Assert.True(MuiListCore.MuiListActiveStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListActiveStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListActiveStateField.Active,
			12));
		Assert.True(MuiListCore.MuiListActiveStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.HasActive, actual.HasActive);
		Assert.Equal(12u, actual.Active);
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
	public void ListViewportFieldAdapterPreservesMetricsSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x19C0);
		var original = new MuiListCore.MuiListViewportState
		{
			Magic = MuiListCore.MuiListViewportState.Cookie,
			TopPixel = 64,
			VisiblePixel = 128,
			TotalPixel = 512,
			First = 4,
			LineHeight = 16,
			Visible = 20,
			DropMark = 7,
		};

		Assert.True(MuiListCore.MuiListViewportStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListViewportStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListViewportStateField.Visible,
			24));
		Assert.True(MuiListCore.MuiListViewportStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.TopPixel, actual.TopPixel);
		Assert.Equal(original.TotalPixel, actual.TotalPixel);
		Assert.Equal(original.DropMark, actual.DropMark);
		Assert.Equal(24u, actual.Visible);
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
	public void ListInteractionPolicyFieldAdapterPreservesPolicySiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x19E0);
		var original = new MuiListCore.MuiListInteractionPolicyState
		{
			Magic = MuiListCore.MuiListInteractionPolicyState.Cookie,
			Input = 1,
			MultiSelect = 2,
			ScrollerPos = 3,
		};

		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListInteractionPolicyStateField.MultiSelect, 5));
		Assert.True(MuiListCore.MuiListInteractionPolicyStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Input, actual.Input);
		Assert.Equal(original.ScrollerPos, actual.ScrollerPos);
		Assert.Equal(5u, actual.MultiSelect);
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
	public void ListClickFieldAdapterPreservesClickAndBooleanSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A10);
		var original = new MuiListCore.MuiListClickState
		{
			Magic = MuiListCore.MuiListClickState.Cookie,
			ClickColumn = 2,
			DoubleClick = 1,
			AgainClick = 0,
			Clicks = 3,
			DefClickColumn = 4,
		};

		Assert.True(MuiListCore.MuiListClickStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListClickStateField.Clicks, 5));
		Assert.True(MuiListCore.MuiListClickStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.ClickColumn, actual.ClickColumn);
		Assert.Equal(original.DoubleClick, actual.DoubleClick);
		Assert.Equal(original.AgainClick, actual.AgainClick);
		Assert.Equal(original.DefClickColumn, actual.DefClickColumn);
		Assert.Equal(5u, actual.Clicks);
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
	public void ListHookPolicyFieldAdapterPreservesHookSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A40);
		var original = new MuiListCore.MuiListHookPolicyState
		{
			Magic = MuiListCore.MuiListHookPolicyState.Cookie,
			ConstructHook = 0x1001,
			DestructHook = 0x1002,
			DisplayHook = 0x1003,
			CompareHook = 0x1004,
			MultiTestHook = 0x1005,
		};

		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListCore.MuiListHookPolicyStateField.DisplayHook, 0x2003));
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.ConstructHook, actual.ConstructHook);
		Assert.Equal(original.DestructHook, actual.DestructHook);
		Assert.Equal(original.CompareHook, actual.CompareHook);
		Assert.Equal(original.MultiTestHook, actual.MultiTestHook);
		Assert.Equal(0x2003u, actual.DisplayHook);
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
	public void ListSortFieldAdapterPreservesColumnSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A60);
		var original = new MuiListCore.MuiListSortState
		{
			Magic = MuiListCore.MuiListSortState.Cookie,
			SortColumn = 2,
			TitleClick = 1,
		};

		Assert.True(MuiListCore.MuiListSortStateCodec.WriteRecord(ref platform,
			address, original));
		Assert.True(MuiListCore.MuiListSortStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListSortStateField.SortColumn, 5));
		Assert.True(MuiListCore.MuiListSortStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.TitleClick, actual.TitleClick);
		Assert.Equal(5u, actual.SortColumn);
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
	public void ListPresentationPolicyFieldAdapterPreservesPolicySiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1B00);
		var original = new MuiListCore.MuiListPresentationPolicyState
		{
			Magic = MuiListCore.MuiListPresentationPolicyState.Cookie,
			Editable = 1,
			Quiet = 2,
			AdjustHeight = 3,
			AdjustWidth = 4,
			Stripes = 5,
			ShowDropMarks = 6,
			DragSortable = 7,
			DragType = 8,
			AutoVisible = 9,
			AutoLineHeight = 10,
			MinLineHeight = 11,
		};

		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.DragType, 0x40));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Editable, actual.Editable);
		Assert.Equal(original.Quiet, actual.Quiet);
		Assert.Equal(original.AdjustHeight, actual.AdjustHeight);
		Assert.Equal(original.AdjustWidth, actual.AdjustWidth);
		Assert.Equal(original.Stripes, actual.Stripes);
		Assert.Equal(original.ShowDropMarks, actual.ShowDropMarks);
		Assert.Equal(original.DragSortable, actual.DragSortable);
		Assert.Equal(0x40u, actual.DragType);
		Assert.Equal(original.AutoVisible, actual.AutoVisible);
		Assert.Equal(original.AutoLineHeight, actual.AutoLineHeight);
		Assert.Equal(original.MinLineHeight, actual.MinLineHeight);
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
	public void ListInsertPositionFieldAdapterPreservesMagicSibling()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1AD0);
		var original = new MuiListCore.MuiListInsertPositionState
		{
			Magic = MuiListCore.MuiListInsertPositionState.Cookie,
			Position = 23,
		};

		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListInsertPositionStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListInsertPositionStateField.Position, 31));
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(31u, actual.Position);
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
	public void ListPoolPolicyFieldAdapterPreservesPoolAndOwnershipSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1B00);
		var original = new MuiListCore.MuiListPoolPolicyState
		{
			Magic = MuiListCore.MuiListPoolPolicyState.Cookie,
			Pool = APTR.FromPointer(0x4400),
			PuddleSize = 4096,
			ThresholdSize = 8192,
			UsesExternalPool = 1,
		};

		Assert.True(MuiListCore.MuiListPoolPolicyStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListPoolPolicyMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListCore.MuiListPoolPolicyField.ThresholdSize, 16384));
		Assert.True(MuiListCore.MuiListPoolPolicyStateCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Pool, actual.Pool);
		Assert.Equal(original.PuddleSize, actual.PuddleSize);
		Assert.Equal(original.UsesExternalPool, actual.UsesExternalPool);
		Assert.Equal(16384u, actual.ThresholdSize);
	}

	[Fact]
	public void ListFormatDescriptorFieldAdapterPreservesPointerAndWidthSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1B40);
		var original = new MuiListCore.MuiListFormatDescriptor
		{
			Delta = 0xFFFFFFFEu,
			Weight = 3,
			MinWidth = 8,
			MaxWidth = 120,
			Column = 2,
			Flags = 0xA55Au,
			Preparse = APTR.FromPointer(0x4400),
			PreparseLength = 4,
			PreparseStorage = APTR.FromPointer(0x4500),
			PreparseStorageLength = 5,
		};

		Assert.True(MuiListCore.MuiListFormatDescriptorCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListFormatDescriptorMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListCore.MuiListFormatDescriptorField.PreparseLength, 9));
		Assert.True(MuiListCore.MuiListFormatDescriptorCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Delta, actual.Delta);
		Assert.Equal(original.Weight, actual.Weight);
		Assert.Equal(original.MinWidth, actual.MinWidth);
		Assert.Equal(original.MaxWidth, actual.MaxWidth);
		Assert.Equal(original.Column, actual.Column);
		Assert.Equal(original.Flags, actual.Flags);
		Assert.Equal(original.Preparse, actual.Preparse);
		Assert.Equal(original.PreparseStorage, actual.PreparseStorage);
		Assert.Equal(original.PreparseStorageLength, actual.PreparseStorageLength);
		Assert.Equal(9u, actual.PreparseLength);
	}

	[Fact]
	public void ListFormatDescriptorStateFieldAdapterPreservesValuesSibling()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1B80);
		var original = new MuiListCore.MuiListFormatDescriptorState
		{
			Magic = MuiListCore.MuiListFormatDescriptorState.Cookie,
			Columns = 3,
			Values = APTR.FromPointer(0x5000),
		};

		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFormatDescriptorStateField.Columns, 5));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Values, actual.Values);
		Assert.Equal(5u, actual.Columns);
	}

	[Fact]
	public void ListColumnMetricsFieldAdapterPreservesColumnsAndValuesSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BC0);
		var original = new MuiListCore.MuiListColumnMetricsState
		{
			Magic = MuiListCore.MuiListColumnMetricsState.Cookie,
			Width = 720,
			Columns = 3,
			Values = APTR.FromPointer(0x5000),
		};

		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListColumnMetricsMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListColumnMetricsField.Width,
			800));
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Columns, actual.Columns);
		Assert.Equal(original.Values, actual.Values);
		Assert.Equal(800u, actual.Width);
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
	public void ListFormatDescriptorVectorBridgeUsesNamedRecords()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x5000);
		var expected = new MuiListCore.MuiListFormatDescriptor
		{
			Delta = 0xFEDCBA98u,
			Weight = 100,
			MinWidth = 16,
			MaxWidth = 0x80000001u,
			Column = 3,
			Flags = 0xA55Au,
			Preparse = APTR.FromPointer(0xFFFFFFF0u),
			PreparseLength = 6,
			PreparseStorage = APTR.FromPointer(0xABCDEF01u),
			PreparseStorageLength = 7,
		};

		Assert.True(MuiListCore.MuiListFormatDescriptorVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListCore.MuiListFormatDescriptorVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Delta, decoded.Delta);
		Assert.Equal(expected.Weight, decoded.Weight);
		Assert.Equal(expected.MinWidth, decoded.MinWidth);
		Assert.Equal(expected.MaxWidth, decoded.MaxWidth);
		Assert.Equal(expected.Column, decoded.Column);
		Assert.Equal(expected.Flags, decoded.Flags);
		Assert.Equal(expected.Preparse, decoded.Preparse);
		Assert.Equal(expected.PreparseLength, decoded.PreparseLength);
		Assert.Equal(expected.PreparseStorage, decoded.PreparseStorage);
		Assert.Equal(expected.PreparseStorageLength,
			decoded.PreparseStorageLength);
		Assert.False(MuiListCore.MuiListFormatDescriptorVectorCodec.TryRead(
			ref platform, vector,
			MuiListCore.MuiListFormatDescriptorCursor.MaximumEntries, out _));
		Assert.False(MuiListCore.MuiListFormatDescriptorVectorCodec.TryWrite(
			ref platform, APTR.FromPointer(0x20FF0u), 1, expected));
		Assert.False(MuiListCore.MuiListFormatDescriptorVectorCodec.TryRead(
			ref platform, APTR.FromPointer(0xFFFFFFF0u), 4, out _));
	}

	[Fact]
	public void ListFormatDescriptorVectorCursorExchangesCompleteNamedRecords()
	{
		var platform = CreatePlatform();
		var cursor = new MuiListCore.MuiListFormatDescriptorCursor
		{
			Base = APTR.FromPointer(0x5000),
			Index = 2,
		};
		var expected = new MuiListCore.MuiListFormatDescriptor
		{
			Delta = 0xFEDCBA98u,
			Weight = 100,
			MinWidth = 16,
			MaxWidth = 0x80000001u,
			Column = 3,
			Flags = 0xA55Au,
			Preparse = APTR.FromPointer(0xFFFFFFF0u),
			PreparseLength = 6,
			PreparseStorage = APTR.FromPointer(0xABCDEF01u),
			PreparseStorageLength = 7,
		};

		Assert.True(MuiListCore.MuiListFormatDescriptorVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiListCore.MuiListFormatDescriptorVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Delta, decoded.Delta);
		Assert.Equal(expected.Weight, decoded.Weight);
		Assert.Equal(expected.MinWidth, decoded.MinWidth);
		Assert.Equal(expected.MaxWidth, decoded.MaxWidth);
		Assert.Equal(expected.Column, decoded.Column);
		Assert.Equal(expected.Flags, decoded.Flags);
		Assert.Equal(expected.Preparse, decoded.Preparse);
		Assert.Equal(expected.PreparseLength, decoded.PreparseLength);
		Assert.Equal(expected.PreparseStorage, decoded.PreparseStorage);
		Assert.Equal(expected.PreparseStorageLength,
			decoded.PreparseStorageLength);

		cursor.Index = MuiListCore.MuiListFormatDescriptorCursor.MaximumEntries;
		Assert.False(MuiListCore.MuiListFormatDescriptorVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiListCore.MuiListFormatDescriptorVectorCodec.TryWrite(
			ref platform, cursor, expected));
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
	public void ListColumnMetricFieldAdapterExchangesNamedValueRecord()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BE0);
		Assert.True(MuiListCore.MuiListColumnMetricCodec.WriteRecord(
			ref platform, address,
			new MuiListCore.MuiListColumnMetricValue { Value = 0xFEDCBA98u }));
		Assert.True(MuiListCore.MuiListColumnMetricMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListColumnMetricField.Value,
			0x80000001u));
		Assert.True(MuiListCore.MuiListColumnMetricCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(0x80000001u, actual.Value);
		Assert.False(MuiListCore.MuiListColumnMetricMemoryCodec.TryReadUInt32(
			ref platform, address,
			(MuiListCore.MuiListColumnMetricField)255, out _));
	}

	[Fact]
	public void ListColumnMetricVectorBridgeUsesNamedValues()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x5000);
		var expected = new MuiListCore.MuiListColumnMetricValue
		{
			Value = 0xFEDCBA98u,
		};

		Assert.True(MuiListCore.MuiListColumnMetricVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListCore.MuiListColumnMetricVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		Assert.True(MuiListCore.MuiListColumnMetricVectorCodec.TryWriteValue(
			ref platform, APTR.FromPointer(0x5400), 3, 0x80000001u));
		Assert.True(MuiListCore.MuiListColumnMetricVectorCodec.TryReadValue(
			ref platform, APTR.FromPointer(0x5400), 3, out var rawValue));
		Assert.Equal(0x80000001u, rawValue);
		Assert.False(MuiListCore.MuiListColumnMetricVectorCodec.TryRead(
			ref platform, vector,
			MuiListCore.MuiListColumnMetricCursor.MaximumEntries, out _));
		Assert.False(MuiListCore.MuiListColumnMetricVectorCodec.TryWrite(
			ref platform, APTR.FromPointer(0x20FFDu), 0, expected));
		Assert.False(MuiListCore.MuiListColumnMetricVectorCodec.TryRead(
			ref platform, APTR.FromPointer(0xFFFFFFFEu), 1, out _));
	}

	[Fact]
	public void ListColumnMetricVectorCursorExchangesNamedValues()
	{
		var platform = CreatePlatform();
		var cursor = new MuiListCore.MuiListColumnMetricCursor
		{
			Base = APTR.FromPointer(0x5000),
			Index = 2,
		};
		var expected = new MuiListCore.MuiListColumnMetricValue
		{
			Value = 0xFEDCBA98u,
		};

		Assert.True(MuiListCore.MuiListColumnMetricVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiListCore.MuiListColumnMetricVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		cursor.Index = MuiListCore.MuiListColumnMetricCursor.MaximumEntries;
		Assert.False(MuiListCore.MuiListColumnMetricVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiListCore.MuiListColumnMetricVectorCodec.TryWrite(
			ref platform, cursor, expected));
	}

	[Fact]
	public void ListColumnGeometryFieldAdapterPreservesWidthSibling()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1C20);
		var original = new MuiListCore.MuiListColumnGeometry
		{
			Offset = 0xFEDCBA98u,
			Width = 0x80000001u,
		};

		Assert.True(MuiListCore.MuiListColumnGeometryCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListColumnGeometryMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListColumnGeometryField.Offset,
			16));
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(16u, actual.Offset);
		Assert.Equal(original.Width, actual.Width);
	}

	[Fact]
	public void ListColumnLayoutFieldAdapterPreservesColumnsAndValuesSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1C40);
		var original = new MuiListCore.MuiListColumnLayoutState
		{
			Magic = MuiListCore.MuiListColumnLayoutState.Cookie,
			Width = 640,
			Columns = 3,
			Values = APTR.FromPointer(0x5000),
		};

		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListColumnLayoutMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListColumnLayoutField.Width,
			800));
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Columns, actual.Columns);
		Assert.Equal(original.Values, actual.Values);
		Assert.Equal(800u, actual.Width);
	}

	[Fact]
	public void ListColumnVisibilityFieldAdapterPreservesMaskSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1C80);
		var original = new MuiListCore.MuiListColumnVisibilityState
		{
			Magic = MuiListCore.MuiListColumnVisibilityState.Cookie,
			Low = 0x00000005,
			High = 0x80000000,
			Word2 = 0x00000012,
			Word3 = 0x00000023,
			Word4 = 0x00000034,
			Word5 = 0x00000045,
			Word6 = 0x00000056,
			Word7 = 0x80000001,
		};

		Assert.True(MuiListCore.MuiListColumnVisibilityStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListColumnVisibilityStateMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnVisibilityStateField.Word7,
				0x40000000));
		Assert.True(MuiListCore.MuiListColumnVisibilityStateCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(original.Low, actual.Low);
		Assert.Equal(original.High, actual.High);
		Assert.Equal(original.Word2, actual.Word2);
		Assert.Equal(original.Word3, actual.Word3);
		Assert.Equal(original.Word4, actual.Word4);
		Assert.Equal(original.Word5, actual.Word5);
		Assert.Equal(original.Word6, actual.Word6);
		Assert.Equal(0x40000000u, actual.Word7);
	}

	[Fact]
	public void ListColumnOrderFieldAdapterPreservesPointerAndReservedSiblings()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1CC0);
		var original = new MuiListCore.MuiListColumnOrderState
		{
			Magic = MuiListCore.MuiListColumnOrderState.Cookie,
			Count = 4,
			Values = APTR.FromPointer(0x6000),
			Reserved = 0xA55A,
		};

		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.WriteRecord(
			ref platform, address, original));
		Assert.True(MuiListCore.MuiListColumnOrderStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListColumnOrderStateField.Count,
			8));
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(original.Magic, actual.Magic);
		Assert.Equal(8u, actual.Count);
		Assert.Equal(original.Values, actual.Values);
		Assert.Equal(original.Reserved, actual.Reserved);
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
	public void ListPointerVectorBridgeUsesNamedRecords()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x6000);
		var expected = new MuiListCore.MuiListPointerSlotRecord
		{
			Value = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiListCore.MuiListPointerVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListCore.MuiListPointerVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		Assert.True(MuiListCore.MuiListPointerVectorCodec.TryWriteValue(
			ref platform, APTR.FromPointer(0x6400), 3, 0x80000001u));
		Assert.True(MuiListCore.MuiListPointerVectorCodec.TryReadValue(
			ref platform, APTR.FromPointer(0x6400), 3, out var rawValue));
		Assert.Equal(0x80000001u, rawValue);
		Assert.False(MuiListCore.MuiListPointerVectorCodec.TryRead(
			ref platform, vector,
			MuiListCore.MuiListPointerVectorCursor.MaximumEntries, out _));
		Assert.False(MuiListCore.MuiListPointerVectorCodec.TryWriteValue(
			ref platform, APTR.FromPointer(0x20FFEu), 1, 0));
		Assert.False(MuiListCore.MuiListPointerVectorCodec.TryReadValue(
			ref platform, APTR.FromPointer(0xFFFFFFF0u), 4, out _));
	}

	[Fact]
	public void ListPointerVectorCursorExchangesCompleteNamedRecords()
	{
		var platform = CreatePlatform();
		var cursor = new MuiListCore.MuiListPointerVectorCursor
		{
			Base = APTR.FromPointer(0x6000),
			Index = 2,
		};
		var expected = new MuiListCore.MuiListPointerSlotRecord
		{
			Value = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiListCore.MuiListPointerVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiListCore.MuiListPointerVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		cursor.Index = MuiListCore.MuiListPointerVectorCursor.MaximumEntries;
		Assert.False(MuiListCore.MuiListPointerVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiListCore.MuiListPointerVectorCodec.TryWrite(
			ref platform, cursor, expected));
	}

	[Fact]
	public void ListPointerSlotVectorBridgeUsesNamedRecords()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x6800);
		var expected = new MuiListCore.MuiListPointerSlotRecord
		{
			Value = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiListCore.MuiListPointerSlotVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListCore.MuiListPointerSlotVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		Assert.True(MuiListCore.MuiListPointerSlotVectorCodec.TryWriteValue(
			ref platform, APTR.FromPointer(0x6C00), 3, 0x80000001u));
		Assert.True(MuiListCore.MuiListPointerSlotVectorCodec.TryReadValue(
			ref platform, APTR.FromPointer(0x6C00), 3, out var rawValue));
		Assert.Equal(0x80000001u, rawValue);
		Assert.False(MuiListCore.MuiListPointerSlotVectorCodec.TryRead(
			ref platform, vector,
			MuiListCore.MuiListPointerSlotCursor.MaximumEntries, out _));
		Assert.False(MuiListCore.MuiListPointerSlotVectorCodec.TryWriteValue(
			ref platform, APTR.FromPointer(0x20FFEu), 1, 0));
		Assert.False(MuiListCore.MuiListPointerSlotVectorCodec.TryReadValue(
			ref platform, APTR.FromPointer(0xFFFFFFF0u), 4, out _));
	}

	[Fact]
	public void ListPointerSlotVectorCursorExchangesCompleteNamedRecords()
	{
		var platform = CreatePlatform();
		var cursor = new MuiListCore.MuiListPointerSlotCursor
		{
			Base = APTR.FromPointer(0x6800),
			Index = 2,
		};
		var expected = new MuiListCore.MuiListPointerSlotRecord
		{
			Value = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiListCore.MuiListPointerSlotVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiListCore.MuiListPointerSlotVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		cursor.Index = MuiListCore.MuiListPointerSlotCursor.MaximumEntries;
		Assert.False(MuiListCore.MuiListPointerSlotVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiListCore.MuiListPointerSlotVectorCodec.TryWrite(
			ref platform, cursor, expected));
	}

	[Fact]
	public void ListSlotVectorBridgeUsesNamedRecords()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x7000);
		var expected = new MuiListSlotState
		{
			Entry = APTR.FromPointer(0xFEDCBA98u),
			Flags = 0x80000001u,
		};

		Assert.True(MuiListCore.MuiListSlotVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListCore.MuiListSlotVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Entry, decoded.Entry);
		Assert.Equal(expected.Flags, decoded.Flags);
		Assert.False(MuiListCore.MuiListSlotVectorCodec.TryRead(
			ref platform, vector, MuiListCore.MuiListSlotCursor.MaximumEntries,
			out _));
		Assert.False(MuiListCore.MuiListSlotVectorCodec.TryWrite(
			ref platform, APTR.FromPointer(0x20FFEu), 1, expected));
		Assert.False(MuiListCore.MuiListSlotVectorCodec.TryRead(
			ref platform, APTR.FromPointer(0xFFFFFFF0u), 4, out _));
	}

	[Fact]
	public void ListSlotVectorCursorExchangesCompleteNamedRecords()
	{
		var platform = CreatePlatform();
		var cursor = new MuiListCore.MuiListSlotCursor
		{
			Base = APTR.FromPointer(0x7000),
			Index = 2,
		};
		var expected = new MuiListSlotState
		{
			Entry = APTR.FromPointer(0xFEDCBA98u),
			Flags = 0x80000001u,
		};

		Assert.True(MuiListCore.MuiListSlotVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiListCore.MuiListSlotVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Entry, decoded.Entry);
		Assert.Equal(expected.Flags, decoded.Flags);
		cursor.Index = MuiListCore.MuiListSlotCursor.MaximumEntries;
		Assert.False(MuiListCore.MuiListSlotVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiListCore.MuiListSlotVectorCodec.TryWrite(
			ref platform, cursor, expected));
	}

	[Fact]
	public void ListColumnGeometryVectorBridgeUsesNamedRecords()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x7800);
		var expected = new MuiListCore.MuiListColumnGeometry
		{
			Offset = 0xFEDCBA98u,
			Width = 0x80000001u,
		};

		Assert.True(MuiListCore.MuiListColumnGeometryVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListCore.MuiListColumnGeometryVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Offset, decoded.Offset);
		Assert.Equal(expected.Width, decoded.Width);
		Assert.False(MuiListCore.MuiListColumnGeometryVectorCodec.TryRead(
			ref platform, vector,
			MuiListCore.MuiListColumnGeometryCursor.MaximumEntries, out _));
		Assert.False(MuiListCore.MuiListColumnGeometryVectorCodec.TryWrite(
			ref platform, APTR.FromPointer(0x20FF9u), 0, expected));
		Assert.False(MuiListCore.MuiListColumnGeometryVectorCodec.TryRead(
			ref platform, APTR.FromPointer(0xFFFFFFF0u), 4, out _));
	}

	[Fact]
	public void ListColumnGeometryVectorCursorExchangesCompleteNamedRecords()
	{
		var platform = CreatePlatform();
		var cursor = new MuiListCore.MuiListColumnGeometryCursor
		{
			Base = APTR.FromPointer(0x7800),
			Index = 2,
		};
		var expected = new MuiListCore.MuiListColumnGeometry
		{
			Offset = 0xFEDCBA98u,
			Width = 0x80000001u,
		};

		Assert.True(MuiListCore.MuiListColumnGeometryVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiListCore.MuiListColumnGeometryVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Offset, decoded.Offset);
		Assert.Equal(expected.Width, decoded.Width);
		cursor.Index = MuiListCore.MuiListColumnGeometryCursor.MaximumEntries;
		Assert.False(MuiListCore.MuiListColumnGeometryVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiListCore.MuiListColumnGeometryVectorCodec.TryWrite(
			ref platform, cursor, expected));
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

	[Fact]
	public void ListColumnOrderByteFieldAdapterUsesNamedByteStorage()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1C20);
		Assert.True(MuiListCore.MuiListColumnOrderByteRecordMemoryCodec
			.TryWriteByte(ref platform, address,
				MuiListCore.MuiListColumnOrderByteField.Value, 0xFE));
		Assert.True(MuiListCore.MuiListColumnOrderByteRecordMemoryCodec
			.TryReadByte(ref platform, address,
				MuiListCore.MuiListColumnOrderByteField.Value, out var value));
		Assert.Equal((byte)0xFE, value);
		Assert.True(MuiListCore.MuiListColumnOrderByteRecordMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListCore.MuiListColumnOrderByteField.Value, out var field));
		Assert.Equal(address.Raw, field.Raw);
		Assert.False(MuiListCore.MuiListColumnOrderByteRecordMemoryCodec
			.TryReadByte(ref platform, APTR.Null,
				MuiListCore.MuiListColumnOrderByteField.Value, out _));
		Assert.False(MuiListCore.MuiListColumnOrderByteRecordMemoryCodec
			.TryWriteByte(ref platform, address,
				(MuiListCore.MuiListColumnOrderByteField)255, 0));
	}

	[Fact]
	public void ListColumnOrderByteVectorBridgeUsesNamedValues()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x5800);

		Assert.True(MuiListCore.MuiListColumnOrderByteVectorCodec
			.TryWriteValue(ref platform, vector, 3, 0xFE));
		Assert.True(MuiListCore.MuiListColumnOrderByteVectorCodec
			.TryReadValue(ref platform, vector, 3, out var decoded));
		Assert.Equal((byte)0xFE, decoded);
		var expected = new MuiListCore.MuiListColumnOrderByteRecord
		{
			Value = 0x01,
		};
		Assert.True(MuiListCore.MuiListColumnOrderByteVectorCodec.TryWrite(
			ref platform, vector, 4, expected));
		Assert.True(MuiListCore.MuiListColumnOrderByteVectorCodec.TryRead(
			ref platform, vector, 4, out var record));
		Assert.Equal(expected.Value, record.Value);
		Assert.False(MuiListCore.MuiListColumnOrderByteVectorCodec.TryReadValue(
			ref platform, vector,
			MuiListCore.MuiListColumnOrderByteCursor.MaximumEntries, out _));
		Assert.False(MuiListCore.MuiListColumnOrderByteVectorCodec.TryWriteValue(
			ref platform, APTR.FromPointer(0x21000u), 0, 0));
		Assert.False(MuiListCore.MuiListColumnOrderByteVectorCodec.TryReadValue(
			ref platform, APTR.FromPointer(0xFFFFFFFFu), 1, out _));
	}

	[Fact]
	public void ListColumnOrderByteVectorCursorExchangesNamedRecords()
	{
		var platform = CreatePlatform();
		var cursor = new MuiListCore.MuiListColumnOrderByteCursor
		{
			Base = APTR.FromPointer(0x5800),
			Index = 3,
		};
		var expected = new MuiListCore.MuiListColumnOrderByteRecord
		{
			Value = 0xFE,
		};

		Assert.True(MuiListCore.MuiListColumnOrderByteVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiListCore.MuiListColumnOrderByteVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		cursor.Index = MuiListCore.MuiListColumnOrderByteCursor.MaximumEntries;
		Assert.False(MuiListCore.MuiListColumnOrderByteVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiListCore.MuiListColumnOrderByteVectorCodec.TryWrite(
			ref platform, cursor, expected));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
