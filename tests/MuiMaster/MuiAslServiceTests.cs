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
	public void AslRecordMemoryAdapterOwnsStructBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var state = APTR.FromPointer(0x1C00);
		var lease = APTR.FromPointer(0x1D00);

		Assert.True(MuiAslRecordMemoryCodec.TryGetAddress(ref platform, state,
			MuiAslRecordKind.State, MuiAslRecordField.Generation, out var generation,
			out var generationSize));
		Assert.Equal(APTR.FromPointer(0x1C08), generation);
		Assert.Equal(MuiAslServiceStateRecord.FieldSize, generationSize);
		Assert.True(MuiAslRecordMemoryCodec.TryWriteUInt32(ref platform, state,
			MuiAslRecordKind.State, MuiAslRecordField.Generation, 9));
		Assert.True(MuiAslRecordMemoryCodec.TryReadUInt32(ref platform, state,
			MuiAslRecordKind.State, MuiAslRecordField.Generation, out var version));
		Assert.Equal(9u, version);

		Assert.True(MuiAslRecordMemoryCodec.TryGetAddress(ref platform, lease,
			MuiAslRecordKind.Lease, MuiAslRecordField.Tags, out var tags,
			out var tagsSize));
		Assert.Equal(APTR.FromPointer(0x1D0C), tags);
		Assert.Equal(MuiAslRequestLeaseRecord.FieldSize, tagsSize);
		Assert.False(MuiAslRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF8), MuiAslRecordKind.Lease,
			MuiAslRecordField.Tags, out _, out _));
		Assert.False(MuiAslRecordMemoryCodec.TryGetAddress(ref platform, state,
			(MuiAslRecordKind)255, MuiAslRecordField.Generation, out _, out _));
		Assert.False(MuiAslRecordMemoryCodec.TryGetAddress(ref platform, state,
			MuiAslRecordKind.State, (MuiAslRecordField)255, out _, out _));
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
	public void MalformedLeaseHeadFailsClosedBeforeCapabilityCalls()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiAslServiceCore.Initialize(ref platform, State));
		var lease = APTR.FromPointer(0x1300);
		Assert.True(MuiAslServiceRecordPacketCore.WriteLease(ref platform, lease,
			APTR.Null, APTR.Null, 4, APTR.Null));
		Assert.True(MuiAslServiceRecordPacketCore.WriteState(ref platform, State,
			MuiAslServiceLayout.Magic, lease, MuiAslServiceLayout.Version));

		Assert.Equal(APTR.Null, MuiAslServiceCore.AllocAslRequest(ref platform,
			State, 4, Tags));
		Assert.Equal(0u, platform.AslAllocateCount);
		Assert.Equal(0, MuiAslServiceCore.AslRequest(ref platform, State,
			APTR.FromPointer(0x1E000), APTR.Null));
		Assert.Equal(0u, platform.AslRequestCount);
		Assert.False(MuiAslServiceCore.FreeAslRequest(ref platform, State,
			APTR.FromPointer(0x1E000)));
		Assert.Equal(0u, platform.AslFreeCount);
	}

	[Fact]
	public void MalformedLiveLeaseFailsClosedBeforeRequestOrRelease()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiAslServiceCore.Initialize(ref platform, State));
		var requester = MuiAslServiceCore.AllocAslRequest(ref platform, State,
			4, APTR.Null);
		Assert.True(requester.IsNotNull);
		Assert.True(MuiAslServiceStateCodec.TryRead(ref platform, State,
			out var state));
		Assert.True(state.Head.IsNotNull);
		Assert.True(MuiAslServiceRecordPacketCore.WriteLease(ref platform,
			state.Head, APTR.Null, APTR.Null, 4, APTR.Null));

		Assert.Equal(0, MuiAslServiceCore.AslRequest(ref platform, State,
			requester, APTR.Null));
		Assert.Equal(0u, platform.AslRequestCount);
		Assert.False(MuiAslServiceCore.FreeAslRequest(ref platform, State,
			requester));
		Assert.Equal(0u, platform.AslFreeCount);
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

	[Fact]
	public void AslTagItemMemoryAdapterOwnsStructBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x1900);
		Assert.True(MuiAslTagItemMessageMemoryCodec.TryGetAddress(ref platform,
			record, MuiAslTagItemField.Data, out var dataAddress));
		Assert.Equal(record.Raw + MuiAslTagItemRecord.DataOffset,
			dataAddress.Raw);
		Assert.True(MuiAslTagItemMessageMemoryCodec.TryWrite(ref platform, record,
			MuiAslTagItemField.Tag, 0x80030001u));
		Assert.True(MuiAslTagItemMessageMemoryCodec.TryRead(ref platform, record,
			MuiAslTagItemField.Tag, out var tag));
		Assert.Equal(0x80030001u, tag);
		Assert.False(MuiAslTagItemMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FFC), MuiAslTagItemField.Data, out _));
		Assert.False(MuiAslTagItemMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x1901), MuiAslTagItemField.Tag, out _));
		Assert.False(MuiAslTagItemMessageMemoryCodec.TryGetAddress(ref platform,
			record, (MuiAslTagItemField)255, out _));
		Assert.False(MuiAslTagItemMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAslTagItemField.Data, out _));
	}

	[Fact]
	public void AslTagItemVectorMemoryAdapterOwnsEntryBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var vector = APTR.FromPointer(0x1800);
		Assert.True(MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
			vector, 2, out var address));
		Assert.Equal(APTR.FromPointer(0x1810), address);
		var position = new MuiAslTagItemCursor
		{
			Base = vector,
			Index = 2,
		};
		Assert.True(MuiAslTagItemVectorMemoryCodec.TryAdvance(ref position, 3));
		Assert.Equal(5u, position.Index);
		Assert.False(MuiAslTagItemVectorMemoryCodec.TryAdvance(ref position, 0));
		position.Index = uint.MaxValue;
		Assert.False(MuiAslTagItemVectorMemoryCodec.TryAdvance(ref position, 1));
		Assert.False(MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
			APTR.FromPointer(0x20FFC), 0, out _));
		Assert.False(MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
			APTR.FromPointer(0xFFFFFFF0), 4, out _));
		Assert.False(MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
			APTR.Null, 0, out _));
	}

	[Fact]
	public void AslTagItemVectorCodecExchangesCompleteNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var vector = APTR.FromPointer(0x1A00);
		var expected = new MuiAslTagItemRecord
		{
			Tag = 0xF00DCAFEu,
			Data = 0x87654321u,
		};

		Assert.True(MuiAslTagItemVectorCodec.TryWrite(ref platform, vector, 0,
			expected));
		Assert.True(MuiAslTagItemVectorCodec.TryRead(ref platform, vector, 0,
			out var actual));
		Assert.Equal(expected.Tag, actual.Tag);
		Assert.Equal(expected.Data, actual.Data);
		var cursor = new MuiAslTagItemCursor { Base = vector, Index = 0 };
		Assert.True(MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
			out var cursorValue));
		Assert.Equal(expected.Data, cursorValue.Data);
		Assert.False(MuiAslTagItemVectorCodec.TryRead(ref platform, vector,
			uint.MaxValue, out _));
		Assert.False(MuiAslTagItemVectorCodec.TryWrite(ref platform,
			APTR.FromPointer(0x20FFC), 0, expected));
		Assert.False(MuiAslTagItemVectorCodec.TryRead(ref platform,
			APTR.FromPointer(0xFFFFFFFF), 1, out _));
	}
}
