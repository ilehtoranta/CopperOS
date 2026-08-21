using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupGridTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void GroupGridSpecUsesNamedFieldBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3000);
		var cursor = new MuiGroupGridSpecFieldCursor
		{
			Record = address,
			Field = MuiGroupGridSpecField.VerticalCenter,
		};
		Assert.True(MuiGroupGridSpecFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x301C), fieldAddress);
		Assert.True(MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridSpecField.Columns, 3));
		Assert.True(MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupGridSpecField.Columns, out var columns));
		Assert.Equal(3u, columns);
		Assert.False(MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform,
			address, unchecked((MuiGroupGridSpecField)255), out _));
		Assert.False(MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform,
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
}
