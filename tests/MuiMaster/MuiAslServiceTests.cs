using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAslServiceTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Tags = APTR.FromPointer(0x1200);

	[Fact]
	public void AslLeaseRequiresInitializationAndBalancesRequestLifecycle()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.Equal(APTR.Null, MuiAslServiceCore.AllocAslRequest(ref platform,
			State, 4, Tags));
		Assert.True(MuiAslServiceCore.Initialize(ref platform, State));

		var requester = MuiAslServiceCore.AllocAslRequest(ref platform, State, 4,
			Tags);
		Assert.True(requester.IsNotNull);
		Assert.Equal(1u, platform.AslAllocateCount);
		Assert.Equal(4u, platform.LastAslRequestType);
		Assert.Equal(Tags.Raw, platform.LastAslTags.Raw);

		platform.AslRequestResult = 7;
		Assert.Equal(7, MuiAslServiceCore.AslRequest(ref platform, State,
			requester, Tags));
		Assert.Equal(1u, platform.AslRequestCount);
		Assert.Equal(0, MuiAslServiceCore.AslRequest(ref platform, State,
			APTR.FromPointer(0x1F000), Tags));

		Assert.True(MuiAslServiceCore.FreeAslRequest(ref platform, State,
			requester));
		Assert.Equal(1u, platform.AslFreeCount);
		Assert.False(MuiAslServiceCore.FreeAslRequest(ref platform, State,
			requester));
	}

	[Fact]
	public void ReinitializationIsIdempotentAndDoesNotDropOutstandingLease()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiAslServiceCore.Initialize(ref platform, State));
		var requester = MuiAslServiceCore.AllocAslRequest(ref platform, State, 2,
			APTR.Null);
		Assert.True(requester.IsNotNull);
		Assert.True(MuiAslServiceCore.Initialize(ref platform, State));
		Assert.Equal(1, MuiAslServiceCore.AslRequest(ref platform, State,
			requester, APTR.Null));
		Assert.True(MuiAslServiceCore.FreeAslRequest(ref platform, State,
			requester));
	}

	[Fact]
	public void AslStateAndLeaseFieldsUseSemanticRecordBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var state = APTR.FromPointer(0x1A00);
		var cursor = new MuiAslRecordFieldCursor
		{
			Record = state,
			Kind = MuiAslRecordKind.State,
			Field = MuiAslRecordField.Head,
		};
		Assert.True(MuiAslRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address, out var fieldSize));
		Assert.Equal(APTR.FromPointer(0x1A04), address);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			state, MuiAslRecordKind.State, MuiAslRecordField.Head, 0x12345678u));
		Assert.True(MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, state,
			MuiAslRecordKind.State, MuiAslRecordField.Head, out var head));
		Assert.Equal(0x12345678u, head);

		var lease = APTR.FromPointer(0x1B00);
		Assert.True(MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			lease, MuiAslRecordKind.Lease, MuiAslRecordField.Type, 6));
		Assert.True(MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, lease,
			MuiAslRecordKind.Lease, MuiAslRecordField.Type, out var type));
		Assert.Equal(6u, type);
		Assert.False(MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, state,
			MuiAslRecordKind.State, MuiAslRecordField.Type, out _));
		Assert.False(MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiAslRecordKind.Lease,
			MuiAslRecordField.Tags, out _));
	}

	[Fact]
	public void TagControlItemsFollowMoreSkipAndIgnoreSemantics()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var tags = APTR.FromPointer(0x1200);
		var more = APTR.FromPointer(0x1240);
		platform.WriteUInt32(tags, 0, 0x80001234);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, MuiAslTagListCore.TagIgnore);
		platform.WriteUInt32(tags, 12, 0);
		platform.WriteUInt32(tags, 16, MuiAslTagListCore.TagMore);
		platform.WriteUInt32(tags, 20, more.Raw);
		platform.WriteUInt32(more, 0, MuiAslTagListCore.TagSkip);
		platform.WriteUInt32(more, 4, 1);
		platform.WriteUInt32(more, 8, 0x80005678);
		platform.WriteUInt32(more, 12, 99);
		platform.WriteUInt32(more, 16, 0x80009ABC);
		platform.WriteUInt32(more, 20, 42);
		platform.WriteUInt32(more, 24, MuiAslTagListCore.TagDone);
		platform.WriteUInt32(more, 28, 0);

		Assert.True(MuiAslTagListCore.Validate(ref platform, tags));
		Assert.True(MuiAslTagListCore.TryGetData(ref platform, tags,
			0x80009ABC, 11, out var value));
		Assert.Equal(42u, value);
		Assert.True(MuiAslServiceCore.Initialize(ref platform, State));
		var requester = MuiAslServiceCore.AllocAslRequest(ref platform, State, 0,
			tags);
		Assert.True(requester.IsNotNull);
	}

	[Fact]
	public void MalformedAndCyclicTagListsAreRejectedBeforeCapabilityCalls()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiAslTagListCore.TagMore);
		platform.WriteUInt32(tags, 4, tags.Raw);
		Assert.False(MuiAslTagListCore.Validate(ref platform, tags));
		Assert.True(MuiAslServiceCore.Initialize(ref platform, State));
		Assert.Equal(APTR.Null, MuiAslServiceCore.AllocAslRequest(ref platform,
			State, 0, tags));
		Assert.Equal(0u, platform.AslAllocateCount);
		Assert.False(MuiAslTagListCore.Validate(ref platform,
			APTR.FromPointer(0x1201)));
	}

	[Fact]
	public void AslTagItemEntryUsesNamedCursorBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = new MuiAslTagItemCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiAslTagItemVectorCodec.TryGetEntry(ref platform, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x1810), address);
		Assert.True(MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1));
		Assert.Equal(3u, cursor.Index);
		cursor.Base = APTR.FromPointer(0x20FFC);
		cursor.Index = 0;
		Assert.False(MuiAslTagItemVectorCodec.TryGetEntry(ref platform, cursor,
			out _));
	}

	[Fact]
	public void AslTagItemFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x1900);
		var cursor = default(MuiAslTagItemFieldCursor);
		cursor.Record = record;
		cursor.Field = MuiAslTagItemField.Tag;
		Assert.True(MuiAslTagItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var tagAddress));
		Assert.Equal(record.Raw, tagAddress.Raw);
		cursor.Field = MuiAslTagItemField.Data;
		Assert.True(MuiAslTagItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var dataAddress));
		Assert.Equal(record.Raw + 4, dataAddress.Raw);
		Assert.True(MuiAslTagItemFieldCursorCodec.TryWrite(ref platform, record,
			MuiAslTagItemField.Tag, 0x80030001));
		Assert.True(MuiAslTagItemFieldCursorCodec.TryWrite(ref platform, record,
			MuiAslTagItemField.Data, 7));
		Assert.True(MuiAslTagItemCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x80030001u, decoded.Tag);
		Assert.Equal(7u, decoded.Data);
		cursor.Field = (MuiAslTagItemField)255;
		Assert.False(MuiAslTagItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Record = APTR.FromPointer(0x1901);
		cursor.Field = MuiAslTagItemField.Tag;
		Assert.False(MuiAslTagItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}
}
