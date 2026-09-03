using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupGridTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint GridStateKey = 0x0D100014u;
	private const uint ShowMe = 0x80429BA8;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;
	private const uint InnerLeft = 0x804228F8;
	private const uint InnerTop = 0x80421EB6;
	private const uint SameWidth = 0x8042B3EC;
	private const uint SameHeight = 0x8042037E;
	private const uint MaxWidth = 0x8042F112;
	private const uint MaxHeight = 0x804293E4;

	[Fact]
	public void GroupGridSpecUsesNamedStructMemoryBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3000);
		Assert.True(MuiGroupGridSpecMemoryCodec.TryGetAddress(ref platform,
			address, MuiGroupGridSpecField.VerticalCenter, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x301C), fieldAddress);
		var cursor = new MuiGroupGridSpecFieldCursor
		{
			Record = address,
			Field = MuiGroupGridSpecField.VerticalCenter,
		};
		Assert.True(MuiGroupGridSpecFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var compatibilityFieldAddress));
		Assert.Equal(APTR.FromPointer(0x301C), compatibilityFieldAddress);
		Assert.True(MuiGroupGridSpecMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridSpecField.Columns, 3));
		Assert.True(MuiGroupGridSpecMemoryCodec.TryReadUInt32(ref platform,
			address, MuiGroupGridSpecField.Columns, out var columns));
		Assert.Equal(3u, columns);
		Assert.False(MuiGroupGridSpecMemoryCodec.TryReadUInt32(ref platform,
			address, unchecked((MuiGroupGridSpecField)255), out _));
		Assert.False(MuiGroupGridSpecMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiGroupGridSpecField.Rows, out _));

		var expected = new MuiGroupGridSpec
		{
			Columns = 2,
			Rows = 4,
			HorizontalSpacing = 5,
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 0,
			HorizontalCenter = 2,
			VerticalCenter = 1,
		};
		Assert.True(MuiGroupGridSpecCodec.Write(ref platform, address, expected));
		Assert.True(MuiGroupGridSpecCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Columns, actual.Columns);
		Assert.Equal(expected.Rows, actual.Rows);
		Assert.Equal(expected.HorizontalSpacing, actual.HorizontalSpacing);
		Assert.Equal(expected.VerticalSpacing, actual.VerticalSpacing);
		Assert.Equal(expected.SameWidth, actual.SameWidth);
		Assert.Equal(expected.SameHeight, actual.SameHeight);
		Assert.Equal(expected.HorizontalCenter, actual.HorizontalCenter);
		Assert.Equal(expected.VerticalCenter, actual.VerticalCenter);
	}

	[Fact]
	public void GroupGridSpecFieldAccessUsesCompleteNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var original = new MuiGroupGridSpec
		{
			Columns = 2,
			Rows = 4,
			HorizontalSpacing = 5,
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 0,
			HorizontalCenter = 2,
			VerticalCenter = 3,
		};
		Assert.True(MuiGroupGridSpecCodec.Write(ref platform, address, original));
		Assert.True(MuiGroupGridSpecMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridSpecField.Columns, 8));
		Assert.True(MuiGroupGridSpecMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridSpecField.HorizontalSpacing, 12));
		Assert.True(MuiGroupGridSpecMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridSpecField.VerticalCenter, 9));
		Assert.True(MuiGroupGridSpecCodec.TryRead(ref platform, address,
			out var after));
		Assert.Equal(8u, after.Columns);
		Assert.Equal(original.Rows, after.Rows);
		Assert.Equal(12u, after.HorizontalSpacing);
		Assert.Equal(original.VerticalSpacing, after.VerticalSpacing);
		Assert.Equal(original.SameWidth, after.SameWidth);
		Assert.Equal(original.SameHeight, after.SameHeight);
		Assert.Equal(original.HorizontalCenter, after.HorizontalCenter);
		Assert.Equal(9u, after.VerticalCenter);

		Assert.False(MuiGroupGridSpecMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x40FE1), MuiGroupGridSpecField.Rows, out _));
		Assert.False(MuiGroupGridSpecMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x40FE1), MuiGroupGridSpecField.Columns, 1));
		Assert.False(MuiGroupGridSpecMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiGroupGridSpecField)255, out _));
	}

	[Fact]
	public void GroupGridSpecSequentialRecordRoundTripsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3000);
		var expected = new MuiGroupGridSpec
		{
			Columns = 2,
			Rows = 4,
			HorizontalSpacing = unchecked((uint)-8),
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 0,
			HorizontalCenter = 2,
			VerticalCenter = 1,
		};
		Assert.True(MuiGroupGridSpecCodec.WriteRecord(ref platform, address,
			expected));
		Assert.True(MuiGroupGridSpecCodec.TryReadRecord(ref platform, address,
			out var actual));
		Assert.Equal(expected.Columns, actual.Columns);
		Assert.Equal(expected.Rows, actual.Rows);
		Assert.Equal(expected.HorizontalSpacing, actual.HorizontalSpacing);
		Assert.Equal(expected.VerticalSpacing, actual.VerticalSpacing);
		Assert.Equal(expected.SameWidth, actual.SameWidth);
		Assert.Equal(expected.SameHeight, actual.SameHeight);
		Assert.Equal(expected.HorizontalCenter, actual.HorizontalCenter);
		Assert.Equal(expected.VerticalCenter, actual.VerticalCenter);
		Assert.False(MuiGroupGridSpecCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x40FFC), out _));
	}

	[Fact]
	public void GroupGridStateUsesNamedGuestFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3000);
		var cursor = new MuiGroupGridStateFieldCursor
		{
			Address = address,
			Field = MuiGroupGridStateField.VerticalCenter,
		};
		Assert.True(MuiGroupGridStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x3020), fieldAddress);
		Assert.True(MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridStateField.Columns, 2));
		Assert.True(MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupGridStateField.Columns, out var columns));
		Assert.Equal(2u, columns);
		cursor.Field = unchecked((MuiGroupGridStateField)255);
		Assert.False(MuiGroupGridStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiGroupGridStateField.VerticalCenter;
		Assert.False(MuiGroupGridStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));

		var expected = new MuiGroupGridStateRecord
		{
			Magic = MuiGroupGridStateRecord.Cookie,
			Columns = 2,
			Rows = 3,
			HorizontalSpacing = 4,
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 1,
			HorizontalCenter = 2,
			VerticalCenter = 1,
		};
		Assert.True(MuiGroupGridStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiGroupGridStateRecordCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Columns, actual.Columns);
		Assert.Equal(expected.Rows, actual.Rows);
		Assert.Equal(expected.HorizontalSpacing, actual.HorizontalSpacing);
		Assert.Equal(expected.VerticalSpacing, actual.VerticalSpacing);
		Assert.Equal(expected.SameWidth, actual.SameWidth);
		Assert.Equal(expected.SameHeight, actual.SameHeight);
		Assert.Equal(expected.HorizontalCenter, actual.HorizontalCenter);
		Assert.Equal(expected.VerticalCenter, actual.VerticalCenter);
	}

	[Fact]
	public void GroupGridStateAdmissionValidatesShapeAndLiveOwner()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var value = new MuiGroupGridStateRecord
		{
			Magic = MuiGroupGridStateRecord.Cookie,
			Columns = 2,
			Rows = 3,
			HorizontalSpacing = 4,
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 1,
			HorizontalCenter = 2,
			VerticalCenter = 1,
		};
		Assert.True(MuiGroupGridStateAdmission.Validate(value));
		Assert.True(MuiGroupGridStateAdmission.ValidateLive(ref platform, State,
			group, value));
		value.HorizontalCenter = 3;
		Assert.False(MuiGroupGridStateAdmission.Validate(value));
		Assert.False(MuiGroupGridStateAdmission.ValidateLive(ref platform, State,
			group, value));
		value.HorizontalCenter = 2;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, group));
		Assert.False(MuiGroupGridStateAdmission.ValidateLive(ref platform, State,
			group, value));
	}

	[Fact]
	public void GroupGridPublishesSanitizedStateAtLayoutBoundary()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042F416, 2);
		Set(ref platform, group, 0x8042B68F, 2);
		Set(ref platform, group, 0x8042C651, 4);
		Set(ref platform, group, 0x8042E1BF, 6);
		Set(ref platform, group, 0x80420860, 1);
		Set(ref platform, group, 0x8042CC64, 2);
		Set(ref platform, group, 0x8042C008, 2);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			100, 60));
		Assert.True(MuiGroupGridCore.TryGetStateRecord(ref platform, State, group,
			out var record));
		Assert.Equal(MuiGroupGridStateRecord.Cookie, record.Magic);
		Assert.Equal(2u, record.Columns);
		Assert.Equal(2u, record.Rows);
		Assert.Equal(4u, record.HorizontalSpacing);
		Assert.Equal(6u, record.VerticalSpacing);
		Assert.Equal(1u, record.SameWidth);
		Assert.Equal(1u, record.SameHeight);
		Assert.Equal(2u, record.HorizontalCenter);
		Assert.Equal(2u, record.VerticalCenter);
	}

	[Fact]
	public void GroupGridPolicyGettersPreferNamedRecordAndOmGetUsesProjection()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042F416, 2);
		Set(ref platform, group, 0x8042B68F, 2);
		Set(ref platform, group, 0x8042C651, 4);
		Set(ref platform, group, 0x8042E1BF, 6);
		Set(ref platform, group, 0x80420860, 1);
		Set(ref platform, group, 0x8042CC64, 2);
		Set(ref platform, group, 0x8042C008, 2);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			100, 60));
		Assert.True(MuiGroupGridCore.TryGetStateRecord(ref platform, State, group,
			out var record));

		// Raw compatibility writes cannot replace the named public projection.
		Set(ref platform, group, 0x8042F416, 9);
		Set(ref platform, group, 0x8042C651, 99);
		Set(ref platform, group, 0x80420860, 0);
		Set(ref platform, group, 0x8042CC64, 0);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			0x8042F416, out var columns));
		Assert.Equal(record.Columns, columns);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			0x8042C651, out var horizontalSpacing));
		Assert.Equal(record.HorizontalSpacing, horizontalSpacing);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			0x80420860, out var sameSize));
		Assert.Equal(1u, sameSize);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			0x8042CC64, out var horizontalCenter));
		Assert.Equal(record.HorizontalCenter, horizontalCenter);

		var message = APTR.FromPointer(0x7800);
		var storage = APTR.FromPointer(0x7900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute, 0x80420860));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			group, message));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var stored));
		Assert.Equal(1u, stored.Value);
	}

	[Fact]
	public void MalformedNamedGridPolicyFailsClosedBeforeGetterAndLayout()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Set(ref platform, group, 0x8042F416, 1);
		Set(ref platform, group, 0x8042B68F, 1);
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 3, 4,
			80, 30));
		var beforeWidth = Get(ref platform, child, Width);
		var block = MuiStoreCore.DataspaceFind(ref platform, State, group,
			GridStateKey);
		Assert.True(MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform,
			block, MuiGroupGridStateField.HorizontalCenter, 3));
		Assert.True(MuiGroupGridStateRecordCodec.TryReadStructural(ref platform,
			block, out var malformed));
		Assert.Equal(3u, malformed.HorizontalCenter);
		Assert.False(MuiGroupGridStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.False(MuiGroupGridStateAdmission.Validate(malformed));

		Assert.False(MuiGroupGridCore.TryGetStateRecord(ref platform, State, group,
			out _));
		Assert.False(MuiGroupGridCore.TryRead(ref platform, State, group,
			out _));
		Assert.False(MuiGroupGridCore.TryGetAttribute(ref platform, State, group,
			0x8042F416, out _));
		var storage = APTR.FromPointer(0x1800);
		Assert.False(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.False(MuiGroupLayoutCore.Layout(ref platform, State, group, 3, 4,
			80, 30));
		Assert.Equal(beforeWidth, Get(ref platform, child, Width));
		Assert.Equal(1u, GetRaw(ref platform, group, 0x8042F416));
		Assert.True(MuiGroupGridStateRecordCodec.TryReadStructural(ref platform,
			block, out malformed));
		Assert.Equal(3u, malformed.HorizontalCenter);
	}

	[Fact]
	public void GridDimensionPolicyRecordsExplicitDivisibility()
	{
		var complete = MuiGroupGridCore.ResolveDimensionPolicy(
			new MuiGroupGridSpec { Columns = 2 }, 4);
		Assert.Equal(4, complete.Count);
		Assert.Equal(2, complete.Columns);
		Assert.Equal(2, complete.Rows);
		Assert.Equal(0, complete.Remainder);
		Assert.Equal(1u, complete.Divisible);

		var incomplete = MuiGroupGridCore.ResolveDimensionPolicy(
			new MuiGroupGridSpec { Columns = 3 }, 4);
		Assert.Equal(3, incomplete.Columns);
		Assert.Equal(2, incomplete.Rows);
		Assert.Equal(1, incomplete.Remainder);
		Assert.Equal(0u, incomplete.Divisible);
		Assert.Equal(3u, incomplete.ExplicitColumns);

		var incompleteRows = MuiGroupGridCore.ResolveDimensionPolicy(
			new MuiGroupGridSpec { Rows = 2 }, 3);
		Assert.Equal(2, incompleteRows.Columns);
		Assert.Equal(2, incompleteRows.Rows);
		Assert.Equal(1, incompleteRows.Remainder);
		Assert.Equal(0u, incompleteRows.Divisible);
		Assert.Equal(2u, incompleteRows.ExplicitRows);
	}

	[Fact]
	public void GridShowMeFalseReceivesZeroAreaGeometry()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Set(ref platform, group, 0x8042F416, 1);
		Set(ref platform, group, 0x8042B68F, 1);
		Set(ref platform, child, ShowMe, 0);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			40, 20));
		Assert.Equal(0u, Get(ref platform, child, Width));
		Assert.Equal(0u, Get(ref platform, child, Height));
	}

	[Fact]
	public void GridSpacingPercentUsesLayoutExtent()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042F416, 2);
		Set(ref platform, group, 0x8042866D, unchecked((uint)-25));
		var spec = MuiGroupGridCore.Read(ref platform, State, group);
		Assert.Equal(unchecked((uint)-25), spec.HorizontalSpacing);
		Assert.Equal(25, MuiGroupSpacingCore.ResolveForLayout(
			spec.HorizontalSpacing, 100).Pixels);
		Assert.True(MuiAreaLayoutCore.TryReadLayoutPolicyState(ref platform, State,
			first, out var firstPolicy));
		Assert.Equal(1u, firstPolicy.ShowMe);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.True(MuiAreaLayoutCore.TryReadGeometryState(ref platform, State,
			first, out var firstGeometry));
		Assert.True(MuiAreaLayoutCore.TryReadGeometryState(ref platform, State,
			second, out var secondGeometry));
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, first,
			Width, out var firstRawWidth);
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, first,
			LeftEdge, out var firstRawLeft);
		Assert.Equal(37u, firstRawWidth);
		Assert.Equal(0u, firstRawLeft);
		Assert.Equal(37, firstGeometry.Width);
		Assert.Equal(62, secondGeometry.Left);
		Assert.Equal(37u, Get(ref platform, first, Width));
		Assert.Equal(62u, Get(ref platform, second, LeftEdge));
	}

	[Fact]
	public void GridPreservesUnboundedColumnMaximum()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042F416, 2);
		Set(ref platform, first, MaxWidth, 10);
		Set(ref platform, second, MaxWidth, 0);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)0, platform.ReadUInt16(storage, 4));
	}

	[Fact]
	public void GridPreservesUnboundedRowMaximum()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042B68F, 2);
		Set(ref platform, first, MaxHeight, 10);
		Set(ref platform, second, MaxHeight, 0);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)0, platform.ReadUInt16(storage, 6));
	}

	[Fact]
	public void GridReservesColumnMinimumBeforeWeightedSharing()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042F416, 2);
		Set(ref platform, first, InnerLeft, 60);
		Set(ref platform, second, InnerLeft, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(60u, Get(ref platform, first, Width));
		Assert.Equal(75u, Get(ref platform, second, 0x8042BEC6));
	}

	[Fact]
	public void GridReservesRowMinimumBeforeWeightedSharing()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042B68F, 2);
		Set(ref platform, first, InnerTop, 50);
		Set(ref platform, second, InnerTop, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			20, 100));
		Assert.Equal(50u, Get(ref platform, first, Height));
		Assert.Equal(70u, Get(ref platform, second, 0x8042509B));
	}

	[Fact]
	public void GridHonorsFiniteColumnMaximumWithoutPreferredExtent()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042F416, 2);
		Set(ref platform, first, MaxWidth, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(10u, Get(ref platform, first, Width));
		Assert.Equal(10u, Get(ref platform, second, 0x8042BEC6));
	}

	[Fact]
	public void GridHonorsFiniteRowMaximumWithoutPreferredExtent()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042B68F, 2);
		Set(ref platform, first, MaxHeight, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			20, 100));
		Assert.Equal(10u, Get(ref platform, first, Height));
		Assert.Equal(10u, Get(ref platform, second, 0x8042509B));
	}

	[Fact]
	public void GridSameWidthUsesOneCommonBoundedChildExtent()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042F416, 2);
		Set(ref platform, group, SameWidth, 1);
		Set(ref platform, first, InnerLeft, 5);
		Set(ref platform, first, MaxWidth, 30);
		Set(ref platform, second, InnerLeft, 9);
		Set(ref platform, second, MaxWidth, 20);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(20u, Get(ref platform, first, Width));
		Assert.Equal(20u, Get(ref platform, second, Width));
		Assert.Equal(15u, Get(ref platform, first, 0x8042BEC6));
		Assert.Equal(65u, Get(ref platform, second, 0x8042BEC6));
	}

	[Fact]
	public void GridSameHeightUsesOneCommonBoundedChildExtent()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, 0x8042B68F, 2);
		Set(ref platform, group, SameHeight, 1);
		Set(ref platform, first, InnerTop, 5);
		Set(ref platform, first, MaxHeight, 30);
		Set(ref platform, second, InnerTop, 10);
		Set(ref platform, second, MaxHeight, 20);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			20, 100));
		Assert.Equal(20u, Get(ref platform, first, Height));
		Assert.Equal(20u, Get(ref platform, second, Height));
		Assert.Equal(15u, Get(ref platform, first, 0x8042509B));
		Assert.Equal(65u, Get(ref platform, second, 0x8042509B));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Group.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static void Set(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value) => Assert.True(
		MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj, attribute,
			value, false));

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}

	private static uint GetRaw(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
